partial class SomeViewModel
{
    public event global::System.ComponentModel.PropertyChangingEventHandler PropertyChanging;
    public partial string Foo { get; set; }
    protected virtual void OnPropertyChanging(global::System.ComponentModel.PropertyChangingEventArgs eventArgs)
    {
        this.PropertyChanging?.Invoke(this, eventArgs);
    }
}