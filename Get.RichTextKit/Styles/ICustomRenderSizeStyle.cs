using SkiaSharp;

namespace Get.RichTextKit.Styles;
public enum TextVerticalAlignment
{
    Top, Center, Bottom
}
public interface ICustomRenderSizeStyle : IStyle
{
    SKSize CustomRenderSize { get; }

    TextVerticalAlignment TextVerticalAlignment { get; }
}