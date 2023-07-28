using Get.RichTextKit.Editor.DocumentView;
using SkiaSharp;
using System.Drawing;

namespace Get.RichTextKit.Editor.Paragraphs.Properties.Decoration;

public record struct DecorationPaintContext(IDocumentViewOwner ViewOwner, int RepeatingCount, RectangleF AvaliableSpace, TextPaintOptions TextPaintOptions, Paragraph OwnerParagraph);
public record struct DecorationOffscreenNotifyContext(IDocumentViewOwner ViewOwner);
public enum CountMode : byte
{
    Default = default,
    ContinueNumbering,
    ResetNumbering
}
public enum VerticalAlignment : byte
{
    Top = default,
    Center,
    Bottom
}
public interface IParagraphDecorationCountModifiable : IParagraphDecoration
{
    new CountMode CountMode { get; set; }
}
public interface IParagraphDecoration
{
    string TypeIdentifier { get; }
    float FrontOffset { get; }
    CountMode CountMode { get; }
    VerticalAlignment VerticalAlignment { get; set; }
    void Paint(SKCanvas canvas, DecorationPaintContext context);
    void NotifyGoingOffscreen(DecorationOffscreenNotifyContext context);
    void RemovedFromLayout();

    IParagraphDecoration Clone();
}
