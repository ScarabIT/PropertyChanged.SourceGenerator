using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace PropertyChanged.SourceGenerator.Analysis;

public class TypeAnalysisLookups
{
    private readonly TypeAnalysisBuilder typeAnalysis;
    private readonly List<TypeAnalysisBuilder> baseTypeAnalyses;

    private Dictionary<string, MemberAnalysisBuilder>? nameLookup;
    private Dictionary<ISymbol, MemberAnalysisBuilder>? symbolLookup;

    private Dictionary<string, MemberAnalysisBuilder>? baseNameLookup;


    public TypeAnalysisLookups(TypeAnalysisBuilder typeAnalysis, List<TypeAnalysisBuilder> baseTypeAnalyses)
    {
        this.typeAnalysis = typeAnalysis;
        this.baseTypeAnalyses = baseTypeAnalyses;
    }

    public bool TryGetThisType(string name, [NotNullWhen(true)] out MemberAnalysisBuilder? memberAnalysis)
    {
        this.nameLookup ??= this.typeAnalysis.Members.ToDictionary(x => x.Name, x => x, StringComparer.Ordinal);
        return this.nameLookup.TryGetValue(name, out memberAnalysis);
    }

    public bool TryGetThisType(ISymbol symbol, [NotNullWhen(true)] out MemberAnalysisBuilder? memberAnalysis)
    {
        this.symbolLookup ??= this.typeAnalysis.Members.ToDictionary(x => x.BackingMember, x => x, SymbolEqualityComparer.Default);
        return this.symbolLookup.TryGetValue(symbol, out memberAnalysis);
    }

    public bool TryGetBaseType(string name, [NotNullWhen(true)] out MemberAnalysisBuilder? memberAnalysis)
    {
        if (this.baseNameLookup == null)
        {
            this.baseNameLookup = new(StringComparer.Ordinal);
            foreach (var baseType in this.baseTypeAnalyses)
            {
                foreach (var member in baseType.Members)
                {
                    this.baseNameLookup.Add(member.Name, member);
                }
            }
        }
        return this.baseNameLookup.TryGetValue(name, out memberAnalysis);
    }
}
