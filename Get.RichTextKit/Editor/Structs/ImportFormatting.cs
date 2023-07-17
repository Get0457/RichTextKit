namespace Get.RichTextKit.Editor;

public readonly struct ImportFormatting
{
    public bool AllowFontColorChange { get; init; }
    public bool AllowFontSizeChange { get; init; }
    public bool AllowFontFamilyChange { get; init; }
    public bool AllowBold { get; init; }
    public bool AllowItalic { get; init; }
    public bool AllowUnderline { get; init; }
    public bool AllowSuperScript { get; init; }
    public bool AllowSubScript { get; init; }
    public bool AllowStrikethrough { get; init; }
}