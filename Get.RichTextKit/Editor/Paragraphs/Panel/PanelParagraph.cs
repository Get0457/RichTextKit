using Get.RichTextKit.Editor.Structs;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Get.RichTextKit.Utils;
using Get.RichTextKit;
using HarfBuzzSharp;
using System.Reflection;
using Get.RichTextKit.Styles;

namespace Get.RichTextKit.Editor.Paragraphs.Panel;

public abstract partial class PanelParagraph : Paragraph, IParagraphPanel
{
    const int NumberLineOffset = 60;
    const int NumberRightMargin = 10;
    public override IStyle StartStyle => Children[0].StartStyle;
    public override IStyle EndStyle => Children[^1].StartStyle;
    protected List<Paragraph> Children { get; }

    /// <summary>
    /// Constructs a new TextParagraph
    /// </summary>
    public PanelParagraph(IStyle style)
    {
        Children = new();
        CaretIndicies = new CaretIndexer(Children, x => x.CaretIndicies);
        WordBoundaryIndicies = new CaretIndexer(Children, x => x.WordBoundaryIndicies);
    }


    /// <inheritdoc />
    public override void Paint(SKCanvas canvas, PaintOptions options)
    {
        options = options with { TextPaintOptions = options.TextPaintOptions.Clone() };

        foreach (var para in Children)
        {
            var drawingPos = para.GlobalInfo.ContentPosition;
            drawingPos.X -= options.ViewBounds.X;
            drawingPos.Y -= options.ViewBounds.Y;
            para.DrawingContentPosition = drawingPos;
            if (drawingPos.X + para.ContentWidth < 0) goto AfterPaint;
            if (drawingPos.Y + para.ContentHeight < 0) goto AfterPaint;
            if (drawingPos.X > options.ViewBounds.Right) goto AfterPaint;
            if (drawingPos.Y > options.ViewBounds.Bottom) goto AfterPaint;
            para.Paint(canvas, options);
        AfterPaint:
            if (options.TextPaintOptions.Selection is not null)
            {
                options.TextPaintOptions.Selection = options.TextPaintOptions.Selection.Value.Offset(-para.CodePointLength);
            }
        }
    }

    public override CaretInfo GetCaretInfo(CaretPosition position)
    {
        // Find the paragraph
        if (position.CodePointIndex < 0) position.CodePointIndex = 0;
        var para = LocalChildrenFromCodePointIndex(position, out var indexInParagraph);

        // Get caret info
        var ci = para.GetCaretInfo(new CaretPosition(indexInParagraph, position.AltPosition));

        // Adjust caret info to be relative to document
        para.LocalInfo.OffsetFromThis(ref ci);

        // Done
        return ci;
    }

    public override HitTestResult HitTest(PointF pt)
    {
        var para = GetParagraphAt(pt);
        
        para.LocalInfo.OffsetToThis(ref pt);
        
        var htr = para.HitTest(pt);

        para.LocalInfo.OffsetFromThis(ref htr);

        return htr;
    }

    public override HitTestResult HitTestLine(int lineIndex, float x)
    {
        var para = LocalChildrenFromLineIndex(lineIndex, out var newLineIdx);
        var htr = para.HitTestLine(newLineIdx, para.LocalInfo.OffsetXToThis(x));
        para.LocalInfo.OffsetFromThis(ref htr);
        return htr;
    }

    /// <inheritdoc />
    public override IReadOnlyList<int> CaretIndicies { get; }

    /// <inheritdoc />
    public override IReadOnlyList<int> WordBoundaryIndicies { get; }
    
    public override LineInfo GetLineInfo(int line)
    {
        var paraIndex = LocalChildrenFromLineIndexAsIndex(line, out var newLineIdx);
        var para = Children[paraIndex];
        var info = para.GetLineInfo(newLineIdx);
        if (info.PrevLine is null && paraIndex > 0)
            info.PrevLine = -1;
        if (info.NextLine is null && paraIndex + 1 < Children.Count)
            info.NextLine = para.LineCount;
        para.LocalInfo.OffsetFromThis(ref info);
        return info;
    }

    /// <inheritdoc />
    public override int CodePointLength => Children.Sum(x => x.CodePointLength);

    public override int LineCount => Children.Sum(x => x.LineCount);

    public override int DisplayLineCount => Children.Sum(x => x.DisplayLineCount);

    IList<Paragraph> IParagraphPanel.Children => Children;
    IList<Paragraph> IParagraphCollection.Paragraphs => Children;

    public override void GetTextByAppendTextToBuffer(Utf32Buffer bufToAdd, int position, int length)
    {
        Range r = position..(position + length);
        foreach (var para in Children)
        {
            if (r.Start.Equals(r.End)) return;
            if (r.Start.Value > para.CodePointLength)
            {
                r = (r.Start.Value - para.CodePointLength)..(r.End.Value - para.CodePointLength);
                continue;
            }
            var len = Math.Min(r.End.Value - r.Start.Value, para.CodePointLength - r.Start.Value);
            para.GetTextByAppendTextToBuffer(bufToAdd, r.Start.Value, len);
            r = 0..(r.End.Value - r.Start.Value - len);
        }
    }
    public override IStyle GetStyleAtPosition(CaretPosition position)
    {
        return LocalChildrenFromCodePointIndex(position, out var idx)
            .GetStyleAtPosition(new(idx, position.AltPosition));
    }
    public override IReadOnlyList<StyleRunEx> GetStyles(int position, int length)
        => GetStylesHelper(position, length).ToArray();
    IEnumerable<StyleRunEx> GetStylesHelper(int position, int length)
    {
        Range r = position..(position + length);
        foreach (var para in Children)
        {
            if (r.Start.Equals(r.End)) yield break;
            if (r.Start.Value > para.CodePointLength)
            {
                r = (r.Start.Value - para.CodePointLength)..(r.End.Value - para.CodePointLength);
                continue;
            }
            var len = Math.Min(r.End.Value - r.Start.Value, para.CodePointLength);
            para.GetStyles(r.Start.Value, len);
            r = 0..(r.End.Value - r.Start.Value - len);
        }
    }

    public override void ApplyStyle(IStyle style, int position, int length)
    {
        Range r = position..(position + length);
        foreach (var para in Children)
        {
            if (r.Start.Equals(r.End)) return;
            if (r.Start.Value > para.CodePointLength)
            {
                r = (r.Start.Value - para.CodePointLength)..(r.End.Value - para.CodePointLength);
                continue;
            }
            var len = Math.Min(r.End.Value - r.Start.Value, para.CodePointLength);
            para.ApplyStyle(style, r.Start.Value, len);
            r = 0..(r.End.Value - r.Start.Value - len);
        }
    }
}