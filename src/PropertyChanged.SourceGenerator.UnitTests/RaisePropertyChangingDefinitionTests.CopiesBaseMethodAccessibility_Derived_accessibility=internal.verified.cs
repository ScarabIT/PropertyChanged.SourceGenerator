partial class Derived
{
    public partial string Bar { get; set; }
    internal override void OnPropertyChanging(string propertyName)
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