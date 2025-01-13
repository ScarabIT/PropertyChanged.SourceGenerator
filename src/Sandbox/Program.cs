using PropertyChanged.SourceGenerator;
using System;
using System.ComponentModel;

namespace Sandbox;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Hello World!");
        var vm = new Derived();
    }
}

internal partial class Base
{
    [Notify]
    private bool _selected;
}

internal partial class Derived : Base
{
    // [DependsOn(nameof(Selected))]
    public string Test => Selected ? "foo" : "bar";

    [Notify]
    private int _baz;
}
