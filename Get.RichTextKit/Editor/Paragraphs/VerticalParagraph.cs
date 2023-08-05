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
using System.Diagnostics;

namespace Get.RichTextKit.Editor.Paragraphs.Panel;
[DebuggerDisplay("Vertical {Children.Count} ({string.Join(\", \", Children)})")]
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

    public override Paragraph GetParagraphAt(PointF pt)
        => FindClosestY(pt.Y);
    protected override void LayoutOverride(LayoutParentInfo owner)
    {
        var parentInfo = new LayoutParentInfo(owner.AvaliableWidth, owner.LineWrap, owner.LineNumberMode);
        float YOffset = 0;
        int cpiOffset = 0;
        int displayLineOffset = 0;
        int lineOffset = 0;
        foreach (var (idx, child) in Children.WithIndex())
        {
            child.ParentInfo = new(this, idx);
            child.Layout(parentInfo);
            child.LocalInfo = new(
                ContentPosition: OffsetMargin(new(child.Properties.Decoration?.FrontOffset ?? 0, YOffset), child.Margin),
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

    public override bool TryJoinWithNextParagraph(UndoManager<Document, DocumentViewUpdateInfo> UndoManager)
    {
        return false;
    }

    protected override float ContentWidthOverride => Children.Count is 0 ? 0: Children.Max(x => x.ContentWidth);

    protected override float ContentHeightOverride => Children.Count is 0 ? 0 : Children.Sum(x => x.ContentHeight) + _Spacing * Math.Max(0, Children.Count - 1);
    public override bool IsChildrenReadOnly => false;
}