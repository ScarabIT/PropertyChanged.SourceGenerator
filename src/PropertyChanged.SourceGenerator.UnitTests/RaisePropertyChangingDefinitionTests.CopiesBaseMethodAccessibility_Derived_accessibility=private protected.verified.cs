partial class Derived
{
    public partial string Bar { get; set; }
    private protected override void OnPropertyChanging(string propertyName)
    {
        base.OnPropertyChanging(propertyName);
        switch (propertyName)
        {
            case @"Foo":
            {
                this.OnPropertyChanging(@"Bar");
            }
            break;
        }
    }
}