using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace PropertyChanged.SourceGenerator.Analysis;

public class MemberAnalysisBuilder
{
    public ISymbol Property { get; set; } = null!;
    public string Modifiers { get; set; } = null!;
    public string BackingFieldName { get; set; } = null!;
    public ITypeSymbol Type { get; set; } = null!;
    public IReadOnlyList<AttributeData> Attributes { get; set; } = null!;
    public NullableContextOptions? NullableContextOverride { get; set; }
    public Accessibility? GetterAccessibility { get; set; }
    public Accessibility? SetterAccessibility { get; set; }
    public OnPropertyNameChangedInfo? OnPropertyNameChanged { get; set; }
    public OnPropertyNameChangedInfo? OnPropertyNameChanging { get; set; }

    private HashSet<AlsoNotifyMember>? alsoNotify;

    public void AddAlsoNotify(AlsoNotifyMember alsoNotify)
    {
        this.alsoNotify ??= new HashSet<AlsoNotifyMember>(AlsoNotifyMemberNameOnlyComparer.Instance);
        this.alsoNotify.Add(alsoNotify);
    }

    public MemberAnalysis Build()
    {
        return new MemberAnalysis()
        {
            Name = this.Property.ToDisplayString(SymbolDisplayFormats.SymbolName),
            Modifiers = this.Modifiers,
            FullyQualifiedTypeName = this.Type.ToDisplayString(SymbolDisplayFormats.FullyQualifiedTypeName),
            BackingFieldName = this.BackingFieldName,
            NullableContextOverride = this.NullableContextOverride,
            GetterAccessibility = this.GetterAccessibility,
            SetterAccessibility = this.SetterAccessibility,
            OnPropertyNameChanged = this.OnPropertyNameChanged,
            OnPropertyNameChanging = this.OnPropertyNameChanging,
            AlsoNotify = this.alsoNotify == null
                 ? ReadOnlyEquatableList<AlsoNotifyMember>.Empty
                 : new ReadOnlyEquatableList<AlsoNotifyMember>(this.alsoNotify.OrderBy(x => x.Name).ToList()),
        };
    }
}

public class MemberAnalysis : IMember
{
    public required string Name { get; init; }
    public required string Modifiers { get; init; }
    public required string FullyQualifiedTypeName { get; init; }
    public required string BackingFieldName { get; init; }
    public required NullableContextOptions? NullableContextOverride { get; init; }
    public required Accessibility? GetterAccessibility { get; init; }
    public required Accessibility? SetterAccessibility { get; init; }
    public required OnPropertyNameChangedInfo? OnPropertyNameChanged { get; init; }
    public required OnPropertyNameChangedInfo? OnPropertyNameChanging { get; init; }
    public required ReadOnlyEquatableList<AlsoNotifyMember> AlsoNotify { get; init; }
}
