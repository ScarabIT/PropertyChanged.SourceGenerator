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
                this.__bar = value;
                this.OnPropertyChanged(@"Bar");
            }
        }
    }
}