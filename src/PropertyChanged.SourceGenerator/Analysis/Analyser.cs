using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static PropertyChanged.SourceGenerator.Analysis.Utils;

namespace PropertyChanged.SourceGenerator.Analysis;

public partial class Analyser
{
    private readonly DiagnosticReporter diagnostics;
    private readonly Compilation compilation;
    private readonly NullableContextOptions nullableContextOptions;
    private readonly ConfigurationParser configurationParser;

    private readonly PropertyChangedInterfaceAnalyser? propertyChangedInterfaceAnalyser;
    private readonly PropertyChangingInterfaceAnalyser? propertyChangingInterfaceAnalyser;
    public Analyser(
        DiagnosticReporter diagnostics,
        Compilation compilation,
        NullableContextOptions nullableContextOptions,
        ConfigurationParser configurationParser)
    {
        this.diagnostics = diagnostics;
        this.compilation = compilation;
        this.nullableContextOptions = nullableContextOptions;
        this.configurationParser = configurationParser;

        var inpchangedSymbol = compilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanged");
        if (inpchangedSymbol == null)
        {
            this.diagnostics.ReportCouldNotFindInpc();
        }
        else
        {
            // Assume that all of these are in the same assembly which speeds up GetTypeByMetadataName slightly
            var inpcAssembly = inpchangedSymbol.ContainingAssembly;
            var inpchangingSymbol = inpcAssembly.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanging");

            // Fetching these symbols once for all types is probably cheaper than calculating the fully-qualified metadata name for each
            // symbol we want to test
            var propertyChangedEventArgsSymbol = inpcAssembly.GetTypeByMetadataName("System.ComponentModel.PropertyChangedEventArgs");
            var propertyChangingEventArgsSymbol = inpcAssembly.GetTypeByMetadataName("System.ComponentModel.PropertyChangingEventArgs");
        
            this.propertyChangedInterfaceAnalyser = new(inpchangedSymbol, "System.ComponentModel.PropertyChangedEventHandler", propertyChangedEventArgsSymbol!, this.diagnostics, compilation);
            this.propertyChangingInterfaceAnalyser = new(inpchangingSymbol!, "System.ComponentModel.PropertyChangingEventHandler", propertyChangingEventArgsSymbol!, this.diagnostics, compilation);
        }
    }

    public IEnumerable<TypeAnalysis> Analyse(IReadOnlyDictionary<INamedTypeSymbol, AnalyserInput> inputsLookup, CancellationToken token)
    {
        var results = new Dictionary<INamedTypeSymbol, TypeAnalysisBuilder?>(SymbolEqualityComparer.Default);

        foreach (var input in inputsLookup)
        {
            Analyse(input.Key, input.Value);
        }

        return results.Values.Where(x => x!.CanGenerate).Select(x => x!.Build());

        // If we've been given a base type which we shouldn't analyse directly, but we do need to discover *its* base types,
        // typeSymbol will be set but input will be null
        void Analyse(INamedTypeSymbol typeSymbol, AnalyserInput? input)
        {
            token.ThrowIfCancellationRequested();

            // Make sure it's not e.g. C<Foo>, rather C<T>. This should have been done be AnalyserInput ctor
            Debug.Assert(SymbolEqualityComparer.Default.Equals(typeSymbol, typeSymbol.OriginalDefinition));

            // If we've already analysed this one, return
            if (results.ContainsKey(typeSymbol))
                return;

            // If we haven't analysed its base type yet, do that now. This will then happen recursively
            // Special-case System.Object, as we'll hit it a lot
            // Use OriginalDefinition here, in case the child inerits from e.g. Base<Foo>: we want to analyse
            // Base<T>
            if (typeSymbol.BaseType != null
                && typeSymbol.BaseType.SpecialType != SpecialType.System_Object
                && !results.ContainsKey(typeSymbol.BaseType.OriginalDefinition))
            {
                var baseType = typeSymbol.BaseType.OriginalDefinition;
                AnalyserInput? baseTypeInput = inputsLookup.TryGetValue(baseType, out var baseTypeInputOpt) ? baseTypeInputOpt : null;
                Analyse(baseType, baseTypeInput);
            }

            // If we're not actually supposed to analyse this type, bail. We have to do this after the base
            // type analysis check above, as we can have TypeWeAnalyse depends on TypeWeDontAnalyse depends
            // on TypeWeAnalyse.
            if (!inputsLookup.ContainsKey(typeSymbol))
                return;

            // Right, we know we've analysed all of the base types by now. Fetch them.
            // This is ordered with types lower in the hierarchy last.
            var baseTypes = new List<TypeAnalysisBuilder>();
            for (var t = typeSymbol.BaseType?.OriginalDefinition; t != null && t.SpecialType != SpecialType.System_Object; t = t.BaseType?.OriginalDefinition)
            {
                if (results.TryGetValue(t, out var baseTypeAnalysis))
                {
                    Debug.Assert(baseTypeAnalysis != null);
                    baseTypes.Add(baseTypeAnalysis!);
                }
            }

            // We're set! Analyse it
            results.Add(typeSymbol, this.Analyse(input!.Value, baseTypes, token));
        }
    }

    private TypeAnalysisBuilder Analyse(AnalyserInput input, List<TypeAnalysisBuilder> baseTypeAnalyses, CancellationToken token)
    {
        var typeAnalysis = new TypeAnalysisBuilder()
        {
            CanGenerate = true,
            TypeSymbol = input.TypeSymbol,
        };

        if (baseTypeAnalyses.FirstOrDefault()?.HadException == true)
        {
            // If we failed to analyse the base type because of an exception, we don't stand a chance. Bail now.
            this.diagnostics.ReportUnhandledExceptionOnParent(input.TypeSymbol);
            typeAnalysis.HadException = true;
            typeAnalysis.CanGenerate = false;
        }
        else
        {
            try
            {
                this.AnalyseInner(typeAnalysis, input.Attributes, baseTypeAnalyses, token);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                this.diagnostics.ReportUnhandledException(input.TypeSymbol, e);
                typeAnalysis.HadException = true;
                typeAnalysis.CanGenerate = false;
            }
        }

        return typeAnalysis;
    }

    private void AnalyseInner(
        TypeAnalysisBuilder typeAnalysis,
        IReadOnlyDictionary<ISymbol, List<AttributeData>> members,
        List<TypeAnalysisBuilder> baseTypeAnalyses,
        CancellationToken token)
    {
        if (this.propertyChangedInterfaceAnalyser == null)
            throw new InvalidOperationException();

        var config = this.configurationParser.Parse(typeAnalysis.TypeSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree, this.diagnostics);

        typeAnalysis.NullableContext = this.nullableContextOptions;

        // These are relatively expensive
        typeAnalysis.INotifyPropertyChanged = this.propertyChangedInterfaceAnalyser.CreateInterfaceAnalysis(typeAnalysis.TypeSymbol, baseTypeAnalyses, config);
        token.ThrowIfCancellationRequested();

        typeAnalysis.INotifyPropertyChanging = this.propertyChangingInterfaceAnalyser!.CreateInterfaceAnalysis(typeAnalysis.TypeSymbol, baseTypeAnalyses, config);
        token.ThrowIfCancellationRequested();

        (typeAnalysis.INotifyPropertyChanged, typeAnalysis.INotifyPropertyChanging) = InterfaceAnalyser.PopulateRaiseMethodNameIfEmpty(typeAnalysis.INotifyPropertyChanged, typeAnalysis.INotifyPropertyChanging, config);
        this.ResoveInheritedIsChanged(typeAnalysis, baseTypeAnalyses);

        foreach (var kvp in members)
        {
            token.ThrowIfCancellationRequested();

            var (member, attributes) = (kvp.Key, kvp.Value);
            MemberAnalysisBuilder? memberAnalysis = null;
            switch (member)
            {
                case IPropertySymbol property when this.GetNotifyAttribute(attributes) is { } attribute:
                    memberAnalysis = this.AnalyseProperty(property, attribute, attributes, config);
                    break;

                case var _ when member is IFieldSymbol or IPropertySymbol:
                    this.EnsureNoUnexpectedAttributes(member, attributes);
                    break;
            }

            if (memberAnalysis != null)
            {
                typeAnalysis.Members.Add(memberAnalysis);
            }

            this.ResolveIsChangedMember(typeAnalysis, member, attributes, memberAnalysis);
        }

        token.ThrowIfCancellationRequested();

        // Now that we've got all members, we can do inter-member analysis

        this.ReportPropertyNameCollisions(typeAnalysis, baseTypeAnalyses);
        this.ResolveAlsoNotify(typeAnalysis, baseTypeAnalyses);
        this.ResolveDependsOn(typeAnalysis, members, config);

        if (!IsPartial(typeAnalysis.TypeSymbol))
        {
            typeAnalysis.CanGenerate = false;
            if (typeAnalysis.Members.Count > 0)
            {
                this.diagnostics.ReportTypeIsNotPartial(typeAnalysis.TypeSymbol);
            }
        }

        token.ThrowIfCancellationRequested();

        for (var outerType = typeAnalysis.TypeSymbol.ContainingType; outerType != null; outerType = outerType.ContainingType)
        {
            if (!IsPartial(outerType))
            {
                typeAnalysis.CanGenerate = false;
                if (typeAnalysis.Members.Count > 0)
                {
                    this.diagnostics.ReportOuterTypeIsNotPartial(outerType, typeAnalysis.TypeSymbol);
                }
            }
        }

        bool IsPartial(INamedTypeSymbol type) =>
            type.DeclaringSyntaxReferences.Any(x =>
                x.GetSyntax() is TypeDeclarationSyntax syntax &&
                syntax.Modifiers.Any(SyntaxKind.PartialKeyword));
    }

    

    private MemberAnalysisBuilder? AnalyseProperty(IPropertySymbol property, AttributeData notifyAttribute, List<AttributeData> attributes, Configuration config)
    {
        if (!IsPartial(property))
        {
            // TODO
            return null;
        }
        if (property.GetMethod == null || property.SetMethod == null)
        {
            this.diagnostics.RaiseBackingPropertyMustHaveGetterAndSetter(property);
            return null;
        }
        if (property.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not PropertyDeclarationSyntax declarationSyntax)
        {
            // TODO? Can this even happen?
            return null;
        }

        string? explicitName = null;

        foreach (var arg in notifyAttribute.ConstructorArguments)
        {
            if (arg.Type?.SpecialType == SpecialType.System_String)
            {
                explicitName = (string?)arg.Value;
            }
        }

        string name = explicitName ?? this.TransformName(property, config);
        var result = new MemberAnalysisBuilder()
        {
            Property = property,
            Modifiers = declarationSyntax.Modifiers.ToString(),
            BackingFieldName = name,
            Type = property.Type,
            Attributes = attributes,
            GetterAccessibility = property.GetMethod.DeclaredAccessibility == property.DeclaredAccessibility ? null : property.GetMethod.DeclaredAccessibility,
            SetterAccessibility = property.SetMethod.DeclaredAccessibility == property.DeclaredAccessibility ? null : property.SetMethod.DeclaredAccessibility,
            OnPropertyNameChanged = this.propertyChangedInterfaceAnalyser!.FindOnPropertyNameChangedMethod(property.ContainingType, name, property.Type, property.ContainingType),
            OnPropertyNameChanging = this.propertyChangingInterfaceAnalyser!.FindOnPropertyNameChangedMethod(property.ContainingType, name, property.Type, property.ContainingType),
        };

        if (property.Type.IsReferenceType)
        {
            if (this.nullableContextOptions.HasFlag(NullableContextOptions.Annotations) && property.Type.NullableAnnotation == NullableAnnotation.None)
            {
                result.NullableContextOverride = NullableContextOptions.Disable;
            }
            else if (this.nullableContextOptions == NullableContextOptions.Disable && property.Type.NullableAnnotation != NullableAnnotation.None)
            {
                result.NullableContextOverride = NullableContextOptions.Annotations;
            }
        }

        return result;

        bool IsPartial(IPropertySymbol property) =>
            property.DeclaringSyntaxReferences.Any(x =>
                x.GetSyntax() is PropertyDeclarationSyntax syntax &&
                syntax.Modifiers.Any(SyntaxKind.PartialKeyword));
    }

    private string TransformName(ISymbol member, Configuration config)
    {
        string name = member.Name;
        foreach (string removePrefix in config.RemovePrefixes)
        {
            if (name.StartsWith(removePrefix))
            {
                name = name.Substring(removePrefix.Length);
            }
        }
        foreach (string removeSuffix in config.RemoveSuffixes)
        {
            if (name.EndsWith(removeSuffix))
            {
                name = name.Substring(0, name.Length - removeSuffix.Length);
            }
        }
        switch (config.FirstLetterCapitalisation)
        {
            case Capitalisation.None:
                break;
            case Capitalisation.Uppercase:
                name = char.ToUpper(name[0]) + name.Substring(1);
                break;
            case Capitalisation.Lowercase:
                name = char.ToLower(name[0]) + name.Substring(1);
                break;
        }
        if (config.AddPrefix != null)
        {
            name = config.AddPrefix + name;
        }
        if (config.AddSuffix != null)
        {
            name += config.AddSuffix;
        }

        return name;
    }

    private void ReportPropertyNameCollisions(TypeAnalysisBuilder typeAnalysis, List<TypeAnalysisBuilder> baseTypeAnalyses)
    {
        // TODO: This could be smarter. We can ignore private members in base classes, for instance
        // We treat members we're generating on base types as already having been generated for the purposes of
        // these diagnostics
        var allDeclaredMemberNames = new HashSet<string>(TypeAndBaseTypes(typeAnalysis.TypeSymbol)
            .SelectMany(x => x.MemberNames)
            .Concat(baseTypeAnalyses.SelectMany(x => x.Members.Select(y => y.BackingFieldName))));
        for (int i = typeAnalysis.Members.Count - 1; i >= 0; i--)
        {
            var member = typeAnalysis.Members[i];
            if (allDeclaredMemberNames.Contains(member.BackingFieldName))
            {
                this.diagnostics.ReportMemberWithNameAlreadyExists(member.Property, member.BackingFieldName);
                typeAnalysis.Members.RemoveAt(i);
            }
        }

        foreach (var collision in typeAnalysis.Members.GroupBy(x => x.BackingFieldName).Where(x => x.Count() > 1))
        {
            var members = collision.ToList();
            for (int i = 0; i < members.Count; i++)
            {
                var collidingMember = members[i == 0 ? 1 : 0];
                this.diagnostics.ReportAnotherMemberHasSameGeneratedName(members[i].Property, collidingMember.Property, members[i].BackingFieldName);
                typeAnalysis.Members.Remove(members[i]);
            }
        }
    }

    private static IEnumerable<string?> ExtractAttributeStringParams(AttributeData attribute)
    {
        IEnumerable<string?> values;

        if (attribute.ConstructorArguments.Length == 1 &&
            attribute.ConstructorArguments[0].Kind == TypedConstantKind.Array &&
            !attribute.ConstructorArguments[0].Values.IsDefault)
        {
            values = attribute.ConstructorArguments[0].Values
                .Where(x => x.Kind == TypedConstantKind.Primitive && x.Value is null or string)
                .Select(x => x.Value)
                .Cast<string?>();
        }
        else
        {
            values = attribute.ConstructorArguments
                .Where(x => x.Kind == TypedConstantKind.Primitive && x.Value is null or string)
                .Select(x => x.Value)
                .Cast<string?>();
        }

        return values;
    }


    private void EnsureNoUnexpectedAttributes(ISymbol member, IEnumerable<AttributeData> attributes)
    {
        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass?.Name == "AlsoNotifyAttribute")
            {
                this.diagnostics.ReportAlsoNotifyAttributeNotValidOnMember(attribute, member);
            }
            else if (attribute.AttributeClass?.Name == "PropertyAttributeAttribute")
            {
                this.diagnostics.ReportAlsoNotifyAttributeNotValidOnMember(attribute, member);
            }
        }
    }

    private static ITypeSymbol? GetMemberType(ISymbol member)
    {
        return member switch
        {
            IFieldSymbol field => field.Type,
            IPropertySymbol property => property.Type,
            _ => null,
        };
    }

    private AttributeData? GetNotifyAttribute(IEnumerable<AttributeData> attributes)
    {
        return attributes.SingleOrDefault(x => x.AttributeClass?.Name == "NotifyAttribute");
    }

    private (OnPropertyNameChangedInfo? onPropertyNameChanged, OnPropertyNameChangedInfo? onPropertyNameChanging) FindOnPropertyNameChangedMethod(
        INamedTypeSymbol typeSymbol,
        IPropertySymbol property) =>
            (this.propertyChangedInterfaceAnalyser!.FindOnPropertyNameChangedMethod(typeSymbol, property.Name, property.Type, property.ContainingType),
            this.propertyChangingInterfaceAnalyser!.FindOnPropertyNameChangedMethod(typeSymbol, property.Name, property.Type, property.ContainingType));


}
