using SkiaSharp;
using System.Drawing;

namespace Get.RichTextKit.Editor.Paragraphs.Decoration;

public class NumberListDecoration : IParagraphDecoration
{
    public SKColor? Color { get; set; }
    public float FrontOffset => 90;

    public string TypeIdentifier => "NumberList";

    public CountMode CountMode { get; set; } = CountMode.Default;

    public IParagraphDecoration Clone()
    {
        return new NumberListDecoration() { Color = Color };
    }

    public void NotifyGoingOffscreen(DecorationOffscreenNotifyContext context)
    {

    }
    public void RemovedFromLayout()
    {
        
    }

    public void Paint(SKCanvas canvas, DecorationPaintContext context)
    {
        var centerPos = new PointF(context.AvaliableSpace.Right - 40, (context.AvaliableSpace.Top + context.AvaliableSpace.Bottom) / 2);
        TextBlock tb = new();
        tb.AddText($"{context.RepeatingCount + 1}.", new Style() { TextColor = Color ?? context.TextPaintOptions.TextDefaultColor });
        
        tb.Paint(canvas, new SKPoint(centerPos.X - tb.MeasuredWidth / 2, centerPos.Y - tb.MeasuredHeight / 2));
    }

}
