partial class SomeViewModel
{
    public partial bool IsChanged
    {
        get => this.__isChanged;
        set
        {
            if (!global::System.Collections.Generic.EqualityComparer<bool>.Default.Equals(value, this.__isChanged))
            {
                this.__isChanged = value;
                this.OnPropertyChanged(global::PropertyChanged.SourceGenerator.Internal.EventArgsCache.PropertyChanged_IsChanged);
            }
        }
    }
    public partial string Foo
    {
        get => this.__foo;
        set
        {
            if (!global::System.Collections.Generic.EqualityComparer<string>.Default.Equals(value, this.__foo))
            {
                this.__foo = value;
                this.OnPropertyChanged(global::PropertyChanged.SourceGenerator.Internal.EventArgsCache.PropertyChanged_Foo);
                this.IsChanged = true;
            }
        }
    }
}