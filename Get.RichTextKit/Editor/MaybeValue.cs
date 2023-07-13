namespace Get.RichTextKit.Editor;

public readonly struct MaybeValue<T>
{
    public MaybeValue() { HasValue = false; }
    public MaybeValue(T value)
    {
        HasValue = true;
        Value = value;
    }
    public bool HasValue { get; }
    public T? Value { get; }
    public static implicit operator MaybeValue<T>(T value) => new(value);
}

