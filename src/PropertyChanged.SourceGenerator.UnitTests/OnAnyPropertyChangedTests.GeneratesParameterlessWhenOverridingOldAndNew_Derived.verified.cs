partial class Derived : global::System.ComponentModel.INotifyPropertyChanged
{
    public partial int Foo { get; set; }
    protected override void OnPropertyChanged(string propertyName, object oldValue, object newValue)
    {
        this.OnAnyPropertyChanged(propertyName);
        base.OnPropertyChanged(propertyName, oldValue, newValue);
    }
}