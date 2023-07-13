using SkiaSharp;

namespace Get.RichTextKit.Styles;
public interface INotifyInvalidateStyle : IStyle
{
    event Action Invalidate;
}