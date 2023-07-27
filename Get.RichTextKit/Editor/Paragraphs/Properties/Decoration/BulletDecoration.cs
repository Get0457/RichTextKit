using Get.RichTextKit.Editor.Paragraphs.Panel;
using Get.RichTextKit.Editor.Structs;
using SkiaSharp;
using System.Drawing;

namespace Get.RichTextKit.Editor.Paragraphs.Properties.Decoration;

public class BulletDecoration : IParagraphDecoration
{
    public SKColor? Color { get; set; }
    public float FrontOffset => 90;

    public string TypeIdentifier => "Bullet";

    public CountMode CountMode => CountMode.Default;

    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Top;

    public IParagraphDecoration Clone()
    {
        return new BulletDecoration() { Color = Color };
    }

    public void NotifyGoingOffscreen(DecorationOffscreenNotifyContext context)
    {

    }
    public void RemovedFromLayout()
    {
        
    }

    public void Paint(SKCanvas canvas, DecorationPaintContext context)
    {
        const int BulletSize = 10;
        LineInfo l;
        var bulletPos = new PointF(context.AvaliableSpace.Right - 40, VerticalAlignment switch {
            VerticalAlignment.Top => (context.AvaliableSpace.Top + context.OwnerParagraph.GetLineInfo(0).Assign(out l).Y + l.Height / 2),
            VerticalAlignment.Center => (context.AvaliableSpace.Top + context.AvaliableSpace.Bottom) / 2,
            VerticalAlignment.Bottom => (context.AvaliableSpace.Top + context.OwnerParagraph.GetLineInfo(^1).Assign(out l).Y + l.Height / 2),
            _ => throw new ArgumentOutOfRangeException()
        });
        using var paint = new SKPaint() { Color = Color ?? context.TextPaintOptions.TextDefaultColor, IsAntialias = true };
        canvas.DrawCircle(bulletPos.X, bulletPos.Y, BulletSize / 2, paint);
    }

}
