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
                this.__foo = value;
                string new_Foo = this.__foo;
                this.NotifyPropertyChanged(@"Foo", old_Foo, new_Foo);
            }
        }
    }
}