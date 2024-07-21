partial class SomeViewModel
{
    #nullable enable annotations
    private string? __nullable;
    public partial string? Nullable
    {
        get => this.__nullable;
        set
        {
            if (!global::System.Collections.Generic.EqualityComparer<string?>.Default.Equals(value, this.__nullable))
            {
                this.__nullable = value;
                this.OnPropertyChanged(global::PropertyChanged.SourceGenerator.Internal.EventArgsCache.PropertyChanged_Nullable);
            }
        }
    }
    #nullable disable
    #nullable enable annotations
    private string __notNullable;
    public partial string NotNullable
    {
        get => this.__notNullable;
        set
        {
            if (!global::System.Collections.Generic.EqualityComparer<string>.Default.Equals(value, this.__notNullable))
            {
                this.__notNullable = value;
                this.OnPropertyChanged(global::PropertyChanged.SourceGenerator.Internal.EventArgsCache.PropertyChanged_NotNullable);
            }
        }
    }
    #nullable disable
    private string __oblivious;
    public partial string Oblivious
    {
        get => this.__oblivious;
        set
        {
            if (!global::System.Collections.Generic.EqualityComparer<string>.Default.Equals(value, this.__oblivious))
            {
                this.__oblivious = value;
                this.OnPropertyChanged(global::PropertyChanged.SourceGenerator.Internal.EventArgsCache.PropertyChanged_Oblivious);
            }
        }
    }
    private int? __nullableValue;
    public partial int? NullableValue
    {
        get => this.__nullableValue;
        set
        {
            if (!global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(value, this.__nullableValue))
            {
                this.__nullableValue = value;
                this.OnPropertyChanged(global::PropertyChanged.SourceGenerator.Internal.EventArgsCache.PropertyChanged_NullableValue);
            }
        }
    }
}