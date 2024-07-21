partial class Derived : global::System.ComponentModel.INotifyPropertyChanged
{
    public partial int Foo { get; set; }
    protected override void OnPropertyChanged(global::System.ComponentModel.PropertyChangedEventArgs eventArgs)
    {
        this.OnAnyPropertyChanged(eventArgs.PropertyName, (object)null, (object)null);
        base.OnPropertyChanged(eventArgs);
    }
}