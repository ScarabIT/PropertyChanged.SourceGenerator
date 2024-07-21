using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using PropertyChanged.SourceGenerator.UnitTests.Framework;

namespace PropertyChanged.SourceGenerator.UnitTests;

[TestFixture]
public class NullableTypeTests : TestsBase
{
    private static readonly CSharpSyntaxVisitor<SyntaxNode?>[] rewriters = new CSharpSyntaxVisitor<SyntaxNode?>[]
    {
        RemovePropertiesRewriter.Instance, RemoveDocumentationRewriter.Instance,
    };

    [Test]
    public void GeneratesNullableEventIfInCompilationNullableContext()
    {
        string input = """
            public partial class SomeViewModel
            {
                [Notify]
                public partial string Foo { get; set; } = "";
            }
            """;

        this.AssertThat(
            input,
            It.HasFile("SomeViewModel", rewriters),
            nullableContextOptions: NullableContextOptions.Enable);
    }

    [Test]
    public void DoesNotGenerateNullableEventIfInFileNullableContext()
    {
        string input = """
            #nullable enable
            public partial class SomeViewModel
            {
                [Notify]
                public partial string Foo { get; set; } = "";
            }
            """;

        this.AssertThat(
            input,
            It.HasFile("SomeViewModel", rewriters),
            nullableContextOptions: NullableContextOptions.Disable);
        ;
    }

    [Test]
    public void GeneratesNullablePropertiesIfInCompilationNullableContext()
    {
        string input = """
            public partial class SomeViewModel
            {
                [Notify]
                public partial string? Nullable { get; set; }
                private string? _nullable;
                [Notify]
                public partial string NotNullable { get; set; } = "";
            #nullable disable
                [Notify]
                public partial string Oblivious { get; set; }
            #nullable restore
                [Notify]
                public partial int? NullableValue { get; set; }
            }
            """;

        this.AssertThat(
            input,
            It.HasFile("SomeViewModel", RemoveInpcMembersRewriter.All),
            nullableContextOptions: NullableContextOptions.Enable);
    }

    [Test]
    public void GeneratesNullablePropertiesIfInFileNullableContext()
    {
        string input = """
            #nullable enable
            public partial class SomeViewModel
            {
                [Notify]
                public partial string? Nullable { get; set; }
                [Notify]
                public partial string NotNullable { get; set; } = "";
            #nullable disable
                [Notify]
                public partial string Oblivious { get; set; }
            #nullable restore
                [Notify]
                public partial int? NullableValue { get; set; }
            }
            """;

        this.AssertThat(
            input,
            It.HasFile("SomeViewModel", RemoveInpcMembersRewriter.All),
            nullableContextOptions: NullableContextOptions.Disable);
    }
}
