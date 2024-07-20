partial class Derived
{
    private string __foo;
    public partial string Foo
    {
        get => this.__foo;
        set
        {
            if (!global::System.Collections.Generic.EqualityComparer<string>.Default.Equals(value, this.__foo))
            {
                this.__foo = value;
                this.OnPropertyChanged(global::PropertyChanged.SourceGenerator.Internal.EventArgsCache.PropertyChanged_Foo);
            }
        }
    }
}