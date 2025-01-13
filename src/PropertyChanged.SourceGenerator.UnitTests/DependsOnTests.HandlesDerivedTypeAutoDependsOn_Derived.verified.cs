partial class Derived
{
    public int Baz { get; set; }
    protected override void OnPropertyChanged(global::System.ComponentModel.PropertyChangedEventArgs eventArgs)
    {
        base.OnPropertyChanged(eventArgs);
        switch (eventArgs.PropertyName)
        {
            case @"Selected":
            {
                this.OnPropertyChanged(global::PropertyChanged.SourceGenerator.Internal.EventArgsCache.PropertyChanged_Test);
            }
            break;
        }
    }
}