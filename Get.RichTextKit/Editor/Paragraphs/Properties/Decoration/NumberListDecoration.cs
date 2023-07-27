using Get.RichTextKit.Editor.Paragraphs.Panel;
using Get.RichTextKit.Editor.Structs;
using SkiaSharp;
using System.Drawing;

namespace Get.RichTextKit.Editor.Paragraphs.Properties.Decoration;

public class NumberListDecoration : IParagraphDecoration
{
    public SKColor? Color { get; set; }
    public float FrontOffset => 90;

    public string TypeIdentifier => "NumberList";

    public CountMode CountMode { get; set; } = CountMode.Default;
    public VerticalAlignment VerticalAlignment { get; set; }

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
        LineInfo l;
        var centerPos = new PointF(context.AvaliableSpace.Right - 40, VerticalAlignment switch
        {
            VerticalAlignment.Top => (context.AvaliableSpace.Top + context.OwnerParagraph.GetLineInfo(0).Assign(out l).Y + l.Height / 2),
            VerticalAlignment.Center => (context.AvaliableSpace.Top + context.AvaliableSpace.Bottom) / 2,
            VerticalAlignment.Bottom => (context.AvaliableSpace.Top + context.OwnerParagraph.GetLineInfo(^1).Assign(out l).Y + l.Height / 2),
            _ => throw new ArgumentOutOfRangeException()
        });
        TextBlock tb = new();
        tb.AddText($"{context.RepeatingCount + 1}.", new CopyStyle(context.OwnerParagraph.EndStyle) { TextColor = Color ?? context.TextPaintOptions.TextDefaultColor });
        
        tb.Paint(canvas, new SKPoint(centerPos.X - tb.MeasuredWidth / 2, centerPos.Y - tb.MeasuredHeight / 2));
    }

}
