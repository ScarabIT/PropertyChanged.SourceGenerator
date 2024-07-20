partial class B
{
    private string __foo;
    public partial string Foo
    {
        get => this.__foo;
        set
        {
            if (!global::System.Collections.Generic.EqualityComparer<string>.Default.Equals(value, this.__foo))
            {
                this.NotifyOfPropertyChanging(global::PropertyChanged.SourceGenerator.Internal.EventArgsCache.PropertyChanging_Foo);
                this.__foo = value;
                this.NotifyOfPropertyChange(global::PropertyChanged.SourceGenerator.Internal.EventArgsCache.PropertyChanged_Foo);
            }
        }
    }
}