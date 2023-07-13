using SkiaSharp;

namespace Get.RichTextKit.Styles;
public interface INotifyRenderStyle : IStyle
{
    void OnRender(SKRect region);
}