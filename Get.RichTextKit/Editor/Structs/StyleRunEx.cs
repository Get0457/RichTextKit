using Get.RichTextKit.Styles;
using Get.RichTextKit.Utils;

namespace Get.RichTextKit.Editor.Structs;

public record struct StyleRunEx(int Start, int Length, IStyle Style) : IRun
{
    public StyleRunEx(StyleRun styleRun) : this(styleRun.Start, styleRun.Length, styleRun.Style)
    {

    }
    readonly int IRun.Offset => Start;
}