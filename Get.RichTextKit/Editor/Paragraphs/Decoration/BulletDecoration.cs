using SkiaSharp;
using System.Drawing;

namespace Get.RichTextKit.Editor.Paragraphs.Decoration;

public class BulletDecoration : IParagraphDecoration
{
    public SKColor? Color { get; set; }
    public float FrontOffset => 90;

    public string TypeIdentifier => "Bullet";

    public CountMode CountMode => CountMode.Default;

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
        var bulletPos = new PointF(context.AvaliableSpace.Right - 40, (context.AvaliableSpace.Top + context.AvaliableSpace.Bottom) / 2);
        using var paint = new SKPaint() { Color = Color ?? context.TextPaintOptions.TextDefaultColor, IsAntialias = true };
        canvas.DrawCircle(bulletPos.X, bulletPos.Y, BulletSize / 2, paint);
    }

}
