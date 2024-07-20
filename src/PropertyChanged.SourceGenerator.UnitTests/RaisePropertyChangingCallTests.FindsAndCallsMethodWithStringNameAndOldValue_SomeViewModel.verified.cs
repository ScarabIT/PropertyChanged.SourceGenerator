partial class SomeViewModel
{
    private string __foo;
    public partial string Foo
    {
        get => this.__foo;
        set
        {
            if (!global::System.Collections.Generic.EqualityComparer<string>.Default.Equals(value, this.__foo))
            {
                string old_Foo = this.__foo;
                this.NotifyPropertyChanging(@"Foo", old_Foo);
                this.__foo = value;
                this.NotifyPropertyChanged(global::PropertyChanged.SourceGenerator.Internal.EventArgsCache.PropertyChanged_Foo);
            }
        }
    }
}