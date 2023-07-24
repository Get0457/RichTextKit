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
using System.Diagnostics;

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
    public override int CodePointLength => Children.Sum(x => x.CodePointLength);// + 1;

    public override int LineCount => Children.Sum(x => x.LineCount);

    public override int DisplayLineCount => Children.Sum(x => x.DisplayLineCount);

    IList<Paragraph> IParagraphPanel.Children => Children;
    IList<Paragraph> IParagraphCollection.Paragraphs => Children;

    public abstract bool IsChildrenReadOnly { get; }

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
    public override SelectionInfo GetSelectionInfo(ParentInfo parentInfo, TextRange selection)
    {
        if (IsRangeWithinTheSameChildParagraph(selection, out var paraIndex, out var newRange))
        {
            return
                Children[paraIndex].LocalInfo.OffsetFromThis(
                Children[paraIndex].GetSelectionInfo(new(this, paraIndex), newRange)
            );
        }
        else
            return base.GetSelectionInfo(parentInfo, selection);
    }
    protected virtual IEnumerable<SubRun> GetLocalChildrenInteractingRange(TextRange selection)
        => Children.AsIReadOnlyList().LocalGetInterectingRuns(selection.Minimum, selection.Length);
    protected override IEnumerable<SubRunInfo> GetInteractingRuns(ParentInfo parentInfo, TextRange selection)
    {
        var paraIdx1 = LocalChildrenFromCodePointIndexAsIndex(selection.StartCaretPosition, out int cpi1);
        if (IsRangeWithinTheSameChildParagraph(selection, out var paraIndex, out var newRange))
            foreach (var subRun in GetInteractingRuns(Children[paraIndex], new(this, paraIndex), newRange))
                yield return subRun;
        else
            foreach (var subRun in GetLocalChildrenInteractingRange(selection))
                yield return new SubRunInfo(new(this, subRun.Index), subRun.Offset, subRun.Length, subRun.Partial);
    }
    protected override IEnumerable<SubRunInfo> GetInteractingRunsRecursive(ParentInfo parentInfo, TextRange selection)
    {
        if (IsRangeWithinTheSameChildParagraph(selection, out var paraIndex, out var newRange))
            foreach (var subRunRecursive in GetInteractingRunsRecursive(Children[paraIndex], new(this, paraIndex), newRange))
                yield return subRunRecursive;
        else
            foreach (var subRun in GetLocalChildrenInteractingRange(selection))
            {
                foreach (var subRunRecursive in GetInteractingRunsRecursive(Children[subRun.Index], parentInfo, new(subRun.Offset, subRun.Offset + subRun.Length)))
                    yield return subRunRecursive;
            }
    }
    protected bool IsRangeWithinTheSameChildParagraph(TextRange range, out int paraIndex, out TextRange newRange)
    {
        var paraIdx1 = LocalChildrenFromCodePointIndexAsIndex(range.StartCaretPosition, out int cpi1);
        var paraIdx2 = LocalChildrenFromCodePointIndexAsIndex(range.EndCaretPosition, out int cpi2);
        if (paraIdx1 == paraIdx2)
        {
            newRange = new(cpi1, cpi2, altPosition: range.AltPosition);
            paraIndex = paraIdx1;
            return true;
        }
        paraIndex = default;
        newRange = default;
        return false;
    }
    public override CaretPosition StartCaretPosition => Children[0].LocalInfo.OffsetFromThis(Children[0].StartCaretPosition);
    public override CaretPosition EndCaretPosition => Children[^1].LocalInfo.OffsetFromThis(Children[^1].EndCaretPosition);
    protected override NavigationStatus NavigateOverride(TextRange selection, NavigationSnap snap, NavigationDirection direction, bool keepSelection, ref float? ghostXCoord, out TextRange newSelection)
    {
        if (direction is NavigationDirection.Backward or NavigationDirection.Forward)
            ghostXCoord = null;
        switch (NavigateOverrideHeadImpl(selection, snap, direction, keepSelection, ref ghostXCoord, out newSelection, out var paraIdx))
        {
            case NavigationStatus.Success:
                return NavigationStatus.Success;
            case NavigationStatus.MoveAfter:
                if (paraIdx + 1 < Children.Count)
                {
                    if (direction is NavigationDirection.Forward or NavigationDirection.Backward)
                    {
                        Children[paraIdx + 1].LocalInfo.OffsetToThis(ref selection);
                        selection.EndCaretPosition = Children[paraIdx + 1].StartCaretPosition;
                        if (!keepSelection) selection.Start = selection.End;
                        Children[paraIdx + 1].LocalInfo.OffsetFromThis(ref selection);
                        newSelection = selection;
                    }
                    else
                    {
                        return VerticalNavigateUsingLineInfo(selection, snap, direction, keepSelection, ref ghostXCoord, out newSelection);
                    }
                    return NavigationStatus.Success;
                }
                return NavigationStatus.MoveAfter;
            case NavigationStatus.MoveBefore:
                if (paraIdx - 1 >= 0)
                {
                    if (direction is NavigationDirection.Forward or NavigationDirection.Backward)
                    {
                        Children[paraIdx - 1].LocalInfo.OffsetToThis(ref selection);
                        selection.EndCaretPosition = Children[paraIdx - 1].EndCaretPosition;
                        if (!keepSelection) selection.Start = selection.End;
                        Children[paraIdx - 1].LocalInfo.OffsetFromThis(ref selection);
                        newSelection = selection;
                        return NavigationStatus.Success;
                    } else
                    {
                        return VerticalNavigateUsingLineInfo(selection, snap, direction, keepSelection, ref ghostXCoord, out newSelection);
                    }
                }
                return NavigationStatus.MoveBefore;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    protected NavigationStatus NavigateOverrideHeadImpl(TextRange selection, NavigationSnap snap, NavigationDirection direction, bool keepSelection, ref float? ghostXCoord, out TextRange newSelection, out int paraIdx)
    {
        if (!keepSelection && selection.IsRange)
        {
            if (direction is NavigationDirection.Forward or NavigationDirection.Backward)
            {
                newSelection = direction is NavigationDirection.Backward ? new(selection.MinimumCaretPosition) : new(selection.MaximumCaretPosition);
                paraIdx = default;
                return NavigationStatus.Success;
            }
        }
        paraIdx = LocalChildrenFromCodePointIndexAsIndex(selection.EndCaretPosition, out _);
        var output = Children[paraIdx].Navigate(Children[paraIdx].LocalInfo.OffsetToThis(selection), snap, direction, keepSelection, ref ghostXCoord, out newSelection);
        Children[paraIdx].LocalInfo.OffsetFromThis(ref newSelection);
        return output;
    }
    public override TextRange GetSelectionRange(CaretPosition position, ParagraphSelectionKind kind)
        => LocalChildrenFromCodePointIndex(position, out var idx).GetSelectionRange(
            new(idx, position.AltPosition), kind
        );
}