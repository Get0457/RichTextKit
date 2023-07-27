using Get.RichTextKit.Editor.DocumentView;
using SkiaSharp;
using System.Drawing;

namespace Get.RichTextKit.Editor.Paragraphs;

public class ParagraphProperties
{
    public IParagraphDecoration Decoration { get; set; }
    public void CopyTo(ParagraphProperties another)
    {
        another.Decoration = Decoration.Clone();
    }
}
public record struct DecorationPaintContext(IDocumentViewOwner ViewOwner, int RepeatingCount, RectangleF AvaliableSpace, TextPaintOptions TextPaintOptions, Paragraph OwnerParagraph);
public record struct DecorationOffscreenNotifyContext(IDocumentViewOwner ViewOwner);
public enum CountMode : byte
{
    Default = default,
    ContinueNumbering,
    ResetNumbering
}
public interface IParagraphDecoration
{
    string TypeIdentifier { get; }
    float FrontOffset { get; }
    CountMode CountMode { get; }
    void Paint(SKCanvas canvas, DecorationPaintContext context);
    void NotifyGoingOffscreen(DecorationOffscreenNotifyContext context);
    void RemovedFromLayout();

    IParagraphDecoration Clone();
}