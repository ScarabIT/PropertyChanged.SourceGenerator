using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

namespace PropertyChanged.SourceGenerator.UnitTests.Framework;
public class RemoveBackingFieldsRewriter : CSharpSyntaxRewriter
{
    public static RemoveBackingFieldsRewriter Instance { get; } = new();

    public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        if (node.Declaration.Variables.Any(x => x.Identifier.ValueText.StartsWith("__")))
        {
            return null;
        }

        return base.VisitFieldDeclaration(node);
    }
}
