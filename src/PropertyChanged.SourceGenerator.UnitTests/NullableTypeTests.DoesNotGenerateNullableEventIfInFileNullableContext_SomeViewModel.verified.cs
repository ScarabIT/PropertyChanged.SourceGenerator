﻿partial class SomeViewModel : global::System.ComponentModel.INotifyPropertyChanged
{
    public event global::System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
    #nullable enable annotations
    private string __foo;
    public partial string Foo { get; set; }
    #nullable disable
    protected virtual void OnPropertyChanged(global::System.ComponentModel.PropertyChangedEventArgs eventArgs)
    {
        this.PropertyChanged?.Invoke(this, eventArgs);
    }
}