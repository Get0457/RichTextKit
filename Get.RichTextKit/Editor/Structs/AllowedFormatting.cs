namespace Get.RichTextKit.Editor;

public readonly struct AllowedFormatting
{
    public AllowedFormatting(bool defaultValue = false)
    {
        TextColor = defaultValue;
        FontSize = defaultValue;
        FontFamily = defaultValue;
        Bold = defaultValue;
        Italic = defaultValue;
        Underline = defaultValue;
        SuperScript = defaultValue;
        SubScript = defaultValue;
        Strikethrough = defaultValue;
        Alignment = defaultValue;
    }

    public bool TextColor { get; init; }
    public bool FontSize { get; init; }
    public bool FontFamily { get; init; }
    public bool Bold { get; init; }
    public bool Italic { get; init; }
    public bool Underline { get; init; }
    public bool SuperScript { get; init; }
    public bool SubScript { get; init; }
    public bool Strikethrough { get; init; }
    public bool Alignment { get; init; }
}