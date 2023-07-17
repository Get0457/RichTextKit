namespace Get.RichTextKit.Editor.DataStructure.Table;
public enum TableLengthMode {
    Pixel,
    Ratio,
    Auto
}
public readonly record struct TableLength(float Length, TableLengthMode Mode)
{
    public bool IsRatioMode
    {
        get => Mode is TableLengthMode.Ratio;
        init => Mode = TableLengthMode.Ratio;
    }
    public bool IsPixelMode
    {
        get => Mode is TableLengthMode.Pixel;
        init => Mode = TableLengthMode.Pixel;
    }
    public bool IsAutoMode
    {
        get => Mode is TableLengthMode.Auto;
        init => Mode = TableLengthMode.Auto;
    }
    public static TableLength Auto { get; } = new(default, TableLengthMode.Auto);
    public static TableLength OneRatio { get; } = new(1, TableLengthMode.Ratio);
}