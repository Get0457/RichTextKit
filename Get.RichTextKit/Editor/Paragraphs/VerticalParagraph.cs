using Get.RichTextKit.Editor.Structs;
using Get.RichTextKit.Editor.UndoUnits;
using SkiaSharp;
using System.Collections;
using System.Drawing;
using System.Reflection;
using Get.RichTextKit;
using Get.RichTextKit.Utils;
using Get.RichTextKit.Styles;

namespace Get.RichTextKit.Editor.Paragraphs.Panel;

public class VerticalParagraph : PanelParagraph
{

    public VerticalParagraph(IStyle style, int amount = 1) : base(style)
    {
        foreach (var _ in ..amount)
        {
            Children.Add(new TextParagraph(style));
        }
    }

    protected override Paragraph GetParagraphAt(PointF pt)
        => FindClosestY(pt.Y);
    protected override void LayoutOverride(ParentInfo owner)
    {
        var parentInfo = new ParentInfo(owner.AvaliableWidth, owner.LineWrap, owner.LineNumberMode);
        float YOffset = 0;
        int cpiOffset = 0;
        int displayLineOffset = 0;
        int lineOffset = 0;
        foreach (var child in Children)
        {
            child.Layout(parentInfo);
            child.LocalInfo = new(
                ContentPosition: new(0, YOffset),
                CodePointIndex: cpiOffset,
                DisplayLineIndex: displayLineOffset,
                LineIndex: lineOffset
            );
            YOffset += child.ContentHeight;
            cpiOffset += child.Length;
            lineOffset += child.LineCount;
            displayLineOffset += child.DisplayLineCount;
        }
    }
    public override int DisplayLineCount => 1;

    public override void DeletePartial(UndoManager<Document> UndoManager, SubRunRecursiveInfo range)
    {
        //UndoManager.Do(new UndoDeleteText(_textBlock, range.Offset, range.Length));
    }
    public override bool TryJoin(UndoManager<Document> UndoManager, int thisIndex)
    {
        return false;
    }
    public override Paragraph Split(UndoManager<Document> UndoManager, int splitIndex)
    {
        return null!;
    }

    public override void SetStyleContinuingFrom(Paragraph other)
    {

    }
    /// <inheritdoc />
    public override float ContentWidth => Children.Max(x => x.ContentWidth);

    /// <inheritdoc />
    public override float ContentHeight => Children.Sum(x => x.ContentHeight);
}