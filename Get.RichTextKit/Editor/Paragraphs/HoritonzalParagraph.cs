using Get.RichTextKit.Editor.Structs;
using Get.RichTextKit.Editor.UndoUnits;
using SkiaSharp;
using System.Collections;
using System.Drawing;
using System.Reflection;
using Get.RichTextKit;
using Get.RichTextKit.Utils;
using Get.RichTextKit.Styles;
using Get.RichTextKit.Editor.DocumentView;

namespace Get.RichTextKit.Editor.Paragraphs.Panel;

public class HorizontalParagraph : PanelParagraph
{
    float _childWidth;
    Thickness Padding = new(0, 0, 10, 0);
    public HorizontalParagraph(IStyle style, int amount = 2) : base(style)
    {
        foreach (var i in ..amount)
        {
            Children.Add(new VerticalParagraph(style));
        }
    }
    public new List<Paragraph> Children => base.Children;

    public override Paragraph GetParagraphAt(PointF pt)
        => FindClosestX(pt.X);
    protected override void LayoutOverride(LayoutParentInfo owner)
    {
        var totalWidth =
                owner.AvaliableWidth;
        _childWidth = totalWidth / Children.Count;
        var parentInfo = new LayoutParentInfo(_childWidth - Padding.Left - Padding.Right, owner.LineWrap, owner.LineNumberMode);
        float XOffset = 0;
        int cpiOffset = 0;
        int lineOffset = 0;
        foreach (var child in Children)
        {
            child.Layout(parentInfo);
            child.LocalInfo = new(
                ContentPosition: OffsetMargin(new(XOffset + Padding.Left, Padding.Top), child.Margin),
                CodePointIndex: cpiOffset,
                DisplayLineIndex: 0,
                LineIndex: lineOffset
            );
            XOffset += _childWidth;
            cpiOffset += child.CodePointLength;
            lineOffset += child.LineCount;
        }
    }
    public override int DisplayLineCount => 1;

    public override void DeletePartial(UndoManager<Document, DocumentViewUpdateInfo> UndoManager, SubRunInfo range)
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
    public override HitTestResult HitTestLine(int lineIndex, float x)
    {
        if (lineIndex > 0 && lineIndex < LineCount - 1) return base.HitTestLine(lineIndex, x);
        var para = FindClosestX(x);
        var info = para.HitTestLine(lineIndex is 0 ? 0 : para.LineCount - 1, para.LocalInfo.OffsetXToThis(x));
        para.LocalInfo.OffsetFromThis(ref info);

        return info;
    }
    public override LineInfo GetLineInfo(int line)
    {
        var para = LocalChildrenFromLineIndex(line, out var newLineIdx);
        var info = para.GetLineInfo(newLineIdx);
        para.LocalInfo.OffsetFromThis(ref info);
        return info;
    }
    protected override float ContentWidthOverride => Children.Sum(x => x.ContentWidth + Padding.Left + Padding.Right);

    protected override float ContentHeightOverride => Children.Max(x => x.ContentHeight) + Padding.Bottom + Padding.Top;

    public override bool IsChildrenReadOnly => true;

    public override void Paint(SKCanvas canvas, PaintOptions options)
    {
        base.Paint(canvas, options);
        var height = ContentHeight;
        using var paint = new SKPaint() { Color = options.TextPaintOptions.TextDefaultColor };
        foreach (var idx in 1..Children.Count)
        {
            var x = DrawingContentPosition.X + idx * _childWidth;
            canvas.DrawLine(
                x, DrawingContentPosition.Y,
                x, DrawingContentPosition.Y + height,
                paint
            );
        }
    }
}