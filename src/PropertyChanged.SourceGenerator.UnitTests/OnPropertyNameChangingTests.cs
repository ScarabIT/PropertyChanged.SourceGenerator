﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using PropertyChanged.SourceGenerator.UnitTests.Framework;

namespace PropertyChanged.SourceGenerator.UnitTests;

[TestFixture]
public class OnPropertyNameChangingTests : TestsBase
{
    private static readonly CSharpSyntaxVisitor<SyntaxNode?>[] rewriters = new CSharpSyntaxVisitor<SyntaxNode?>[]
    {
        RemoveInpcMembersRewriter.All, RemoveBackingFieldsRewriter.Instance,
    };

    [Test]
    public void GenerateParameterlessRaise()
    {
        string input = """
            using System.ComponentModel;
            public partial class SomeViewModel : INotifyPropertyChanging
            {
                [Notify]
                public partial string Foo { get; set; }
                public void OnFooChanging() { }
            }
            """;

        this.AssertThat(input, It.HasFile("SomeViewModel", rewriters));
    }

    [Test]
    public void GeneratesOldWithMatchingDataType()
    {
        string input = """
            using System.ComponentModel;
            public partial class SomeViewModel : INotifyPropertyChanging
            {
                [Notify]
                public partial string Foo { get; set; }
                public void OnFooChanging(string oldValue) { }
            }
            """;

        this.AssertThat(input, It.HasFile("SomeViewModel", rewriters));
    }

    [Test]
    public void GeneratesOldAndNewWithParentDataType()
    {
        string input = """
            using System.ComponentModel;
            public partial class SomeViewModel : INotifyPropertyChanging
            {
                [Notify]
                public partial string Foo { get; set; }
                public void OnFooChanging(object oldValue) { }
            }
            """;

        this.AssertThat(input, It.HasFile("SomeViewModel", rewriters));
    }

    [Test]
    public void GeneratesAlsoNotifyCallableParameterless()
    {
        string input = """
            using System.ComponentModel;
            public partial class SomeViewModel : INotifyPropertyChanging
            {
                [Notify, AlsoNotify("Bar")]
                public partial int Foo { get; set; }
                public int Bar { get; }
                private void OnBarChanging() { }
            }
            """;

        this.AssertThat(input, It.HasFile("SomeViewModel", rewriters));
    }

    [Test]
    public void GeneratesAlsoNotifyCallableParameters()
    {
        string input = """
            using System.ComponentModel;
            public partial class SomeViewModel : INotifyPropertyChanging
            {
                [Notify, AlsoNotify("Bar")]
                public partial int Foo { get; set; }
                public int Bar { get; }
                private void OnBarChanging(int oldValue) { }
            }
            """;

        this.AssertThat(input, It.HasFile("SomeViewModel", rewriters));
    }

    [Test]
    public void GeneratesAlsoNotifyOnBaseClass()
    {
        string input = """
            using System.ComponentModel;
            public partial class Base : INotifyPropertyChanging
            {
                [Notify]
                public partial int Bar { get; set; }
                protected void OnBarChanging(int oldValue) { }
            }
            public partial class Derived : Base
            {
                [Notify, AlsoNotify("Bar")]
                public partial int Foo { get; set; }
            }
            """;

        this.AssertThat(input, It.HasFile("Derived", rewriters));
    }

    [Test]
    public void DoesNotGenerateAlsoNotifyWithPropertyOnBaseClassAndMethodOnDerived()
    {
        string input = """
            using System.ComponentModel;
            public partial class Base : INotifyPropertyChanging
            {
                [Notify]
                public partial int Bar { get; set; }
            }
            public partial class Derived : Base
            {
                [Notify, AlsoNotify("Bar")]
                public partial int Foo { get; set; }
                private void OnBarChanging(int oldValue) { }
            }
            """;

        this.AssertThat(input, It.HasFile("Derived", rewriters));
    }

    [Test]
    public void DoesNotCallInaccessibleAlsoNotifyOnBaseClass()
    {
        string input = """
            using System.ComponentModel;
            public partial class Base
            {
                public int Bar { get; set; }
                private void OnBarChanging(int oldValue) { }
            }
            public partial class Derived : Base, INotifyPropertyChanging
            {
                [Notify, AlsoNotify("Bar")]
                public partial int Foo { get; set; }
            }
            """;

        this.AssertThat(input, It.HasFile("Derived", rewriters)
            .HasDiagnostics(
                // (5,18): Warning INPC030: Found one or more On{PropertyName}Changing methods called 'OnBarChanging' for property 'Bar', but none had the correct signature, or were inaccessible. Skipping
                // OnBarChanging
                Diagnostic("INPC030", @"OnBarChanging").WithLocation(5, 18)
            ));
    }

    [Test]
    public void GeneratesDependsOn()
    {
        string input = """
            using System.ComponentModel;
            public partial class SomeViewModel : INotifyPropertyChanging
            {
                [Notify]
                public partial int Foo { get; set; }
                [DependsOn("Foo")]
                public int Bar { get; }
                private void OnBarChanging(int oldValue) { }
            }
            """;

        this.AssertThat(input, It.HasFile("SomeViewModel", rewriters));
    }

    [Test]
    public void GeneratesAutoDependsOn()
    {
        string input = """
            using System.ComponentModel;
            public partial class SomeViewModel : INotifyPropertyChanging
            {
                [Notify]
                public partial int Foo { get; set; }
                public int Bar => this.Foo + 2;
                public string Baz => $"Test: {Bar}";
                private void OnBarChanging(int oldValue) { }
                private void OnBazChanging(string oldValue) { }
            }
            """;

        this.AssertThat(input, It.HasFile("SomeViewModel", rewriters));
    }
}
