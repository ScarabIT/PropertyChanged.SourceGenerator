partial class Derived
{
    public partial string Bar { get; set; }
    public override void OnPropertyChanged(string propertyName)
    {
        base.OnPropertyChanged(propertyName);
        switch (propertyName)
        {
            case @"Foo":
            {
                this.OnPropertyChanged(@"Bar");
            }
            break;
        }
    }
}