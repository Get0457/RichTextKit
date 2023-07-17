using Get.RichTextKit.Editor.Structs;
using Get.RichTextKit.Editor.UndoUnits;
using SkiaSharp;
using System.Collections;
using System.Drawing;
using System.Reflection;
using Get.RichTextKit;
using Get.RichTextKit.Utils;
using Get.RichTextKit.Styles;
using Get.EasyCSharp;
using Get.RichTextKit.Editor.DocumentView;

namespace Get.RichTextKit.Editor.Paragraphs.Panel;

public partial class VerticalParagraph : PanelParagraph
{
    [Property(OnChanged = nameof(InvokeLayoutChanged))]
    int _Spacing = 30;
    void InvokeLayoutChanged() => Owner?.Layout.Invalidate();
    public new List<Paragraph> Children => base.Children;
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
                ContentPosition: OffsetMargin(new(0, YOffset), child.Margin),
                CodePointIndex: cpiOffset,
                DisplayLineIndex: displayLineOffset,
                LineIndex: lineOffset
            );
            YOffset += child.ContentHeight + _Spacing;
            cpiOffset += child.CodePointLength;
            lineOffset += child.LineCount;
            displayLineOffset += child.DisplayLineCount;
        }
    }
    public override int DisplayLineCount => 1;

    public override void DeletePartial(UndoManager<Document, DocumentViewUpdateInfo> UndoManager, SubRunRecursiveInfo range)
    {
        //UndoManager.Do(new UndoDeleteText(_textBlock, range.Offset, range.Length));
    }
    public override bool TryJoin(UndoManager<Document, DocumentViewUpdateInfo> UndoManager, int thisIndex)
    {
        return false;
    }
    public override Paragraph Split(UndoManager<Document, DocumentViewUpdateInfo> UndoManager, int splitIndex)
    {
        return null!;
    }

    protected override float ContentWidthOverride => Children.Max(x => x.ContentWidth);

    protected override float ContentHeightOverride => Children.Sum(x => x.ContentHeight) + _Spacing * Math.Max(0, Children.Count - 1);
}