partial class C
{
    private string __bar;
    public partial string Bar
    {
        get => this.__bar;
        set
        {
            if (!global::System.Collections.Generic.EqualityComparer<string>.Default.Equals(value, this.__bar))
            {
                this.OnPropertyChanging(@"Bar");
                this.__bar = value;
                this.NotifyOfPropertyChange(global::PropertyChanged.SourceGenerator.Internal.EventArgsCache.PropertyChanged_Bar);
            }
        }
    }
}