using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using PropertyChanged.SourceGenerator.UnitTests.Framework;

namespace PropertyChanged.SourceGenerator.UnitTests;

[TestFixture]
public class IsChangedTests : TestsBase
{
    private static readonly CSharpSyntaxVisitor<SyntaxNode?>[] rewriters = new CSharpSyntaxVisitor<SyntaxNode?>[]
    {
        RemoveInpcMembersRewriter.All, RemoveBackingFieldsRewriter.Instance
    };

    [Test]
    public void GeneratesIsChangedToProperty()
    {
        string input = """
            public partial class SomeViewModel
            {
                [IsChanged]
                public bool IsChanged { get; set; }
                [Notify]
                public partial string Foo { get; set; }
            }
            """;

        this.AssertThat(input, It.HasFile("SomeViewModel", rewriters));
    }

    [Test]
    public void GeneratesIsChangedToGeneratedProperty()
    {
        string input = """
            public partial class SomeViewModel
            {
                [Notify, IsChanged]
                public partial bool IsChanged { get; set; }
                [Notify]
                public partial string Foo { get; set; }
            }
            """;

        this.AssertThat(input, It.HasFile("SomeViewModel", rewriters));
    }

    [Test]
    public void GeneratesIsChangedOnBaseClass()
    {
        string input = """
            public class Base
            {
                [IsChanged] public bool IsChanged { get; set; }
            }
            public partial class Derived : Base
            {
                [Notify] public partial string Foo { get; set; }
            }
            """;

        this.AssertThat(input, It.HasFile("Derived", rewriters));
    }

    [Test]
    public void RaisesIfMultipleIsChangedProperties()
    {
        string input = """
            public partial class SomeViewModel
            {
                [IsChanged] public bool Foo { get; set; }
                [IsChanged] public bool Bar { get; set; }
            }
            """;
        this.AssertThat(input, It.HasDiagnostics(
            // (4,6): Warning INPC014: Found multiple [IsChanged] properties, but only one is allowed. Ignoring this one, and using 'Foo'
            // IsChanged
            Diagnostic("INPC014", @"IsChanged").WithLocation(4, 6)
        ));
    }

    [Test]
    public void RaisesIfMultipleIsChangedPropertiesOnBaseTypes()
    {
        string input = """
            public class A
            {
                [IsChanged] public bool IsChangedA { get; set; }
            }
            public class B : A { }
            public partial class C : B
            {
                [IsChanged] public bool IsChangedC { get; set; }
            }
            """;
        this.AssertThat(input, It.HasDiagnostics(
            // (8,6): Warning INPC014: Found multiple [IsChanged] properties, but only one is allowed. Ignoring this one, and using 'IsChangedA'
            // IsChanged
            Diagnostic("INPC014", @"IsChanged").WithLocation(8, 6)
        ));
    }

    [Test]
    public void RaisesIfIsChangedDoesNotReturnBoolean()
    {
        string input = """
            public partial class SomeViewModel
            {
                [IsChanged] public string IsChanged { get; set; }
            }
            """;
        this.AssertThat(input, It.HasDiagnostics(
            // (3,31): Warning INPC015: [IsChanged] property 'IsChanged' does not return a bool. Skipping
            // IsChanged
            Diagnostic("INPC015", @"IsChanged").WithLocation(3, 31)
        ));
    }

    [Test]
    public void RaisesIfIsChangedHasNoSetter()
    {
        string input = """
            public partial class SomeViewModel
            {
                [IsChanged]
                public bool IsChanged { get; }
                [Notify]
                public partial int Bar { get; set; }
            }
            """;

        this.AssertThat(input, It.HasDiagnostics(
            // (4,17): Warning INPC016: [IsChanged] property 'IsChanged' does not have a setter. Skipping
            // IsChanged
            Diagnostic("INPC016", @"IsChanged").WithLocation(4, 17)
        ));
    }

    [Test]
    public void RaisesIfPartialIsChangedHasNoSetter()
    {
        string input = """
            public partial class SomeViewModel
            {
                [IsChanged]
                public partial bool IsChanged { get; }
                [Notify]
                public partial int Bar { get; set; }
            }
            """;

        this.AssertThat(input, It.HasDiagnostics(
            // (4,25): Warning INPC016: [IsChanged] property 'IsChanged' does not have a setter. Skipping
            // IsChanged
            Diagnostic("INPC016", @"IsChanged").WithLocation(4, 25)
        ));
    }

    [Test]
    public void IgnoresIfIsChangedPropertyOnBaseClassHasPrivateSetter()
    {
        string input = """
            public class Base
            {
                [IsChanged] public bool IsChanged { get; private set; }
            }
            public partial class Derived : Base
            {
                [Notify] public partial int? Foo { get; set; }
            }
            """;

        this.AssertThat(input, It.HasFile("Derived", rewriters));
    }

    [Test]
    public void IgnoresIfGeneratedIsChangedPropertyOnBaseClassHasPrivateSetter()
    {
        string input = """
            public partial class Base
            {
                [IsChanged, Notify] public partial bool IsChanged { get; private set; }
            }
            public partial class Derived : Base
            {
                [Notify] public partial int? Foo { get; set; }
            }
            """;

        this.AssertThat(input, It.HasFile("Derived", rewriters));
    }
}
