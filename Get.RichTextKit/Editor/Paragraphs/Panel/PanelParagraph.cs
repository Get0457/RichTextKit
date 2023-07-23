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
using Get.RichTextKit.Editor.DocumentView;
using Get.RichTextKit.Editor.UndoUnits;

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
    public override SelectionInfo GetSelectionInfo(TextRange selection)
    {
        if (IsRangeWithinTheSameChildParagraph(selection, out var paraIndex, out var newRange))
            return Children[paraIndex].GetSelectionInfo(newRange);
        else
            return base.GetSelectionInfo(selection);
    }
    protected virtual IEnumerable<SubRun> GetLocalChildrenInteractingRange(TextRange selection)
        => Children.AsIReadOnlyList().LocalGetInterectingRuns(selection);
    protected override IEnumerable<SubRunInfo> GetInteractingRuns(TextRange selection)
    {
        var paraIdx1 = LocalChildrenFromCodePointIndexAsIndex(selection.StartCaretPosition, out int cpi1);
        if (IsRangeWithinTheSameChildParagraph(selection, out var paraIndex, out var newRange))
            foreach (var subRun in GetInteractingRuns(Children[paraIndex], newRange))
                yield return subRun;
        else
            foreach (var subRun in GetLocalChildrenInteractingRange(selection))
                yield return new SubRunInfo(new(this, subRun.Index), subRun.Offset, subRun.Length, subRun.Partial);
    }
    protected override IEnumerable<SubRunInfo> GetInteractingRunsRecursive(TextRange selection)
    {
        if (IsRangeWithinTheSameChildParagraph(selection, out var paraIndex, out var newRange))
            foreach (var subRunRecursive in GetInteractingRunsRecursive(Children[paraIndex], newRange))
                yield return subRunRecursive;
        else
            foreach (var subRun in GetLocalChildrenInteractingRange(selection))
            {
                foreach (var subRunRecursive in GetInteractingRunsRecursive(Children[subRun.Index], new(subRun.Offset, subRun.Offset + subRun.Length)))
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
    bool DeletePartialImplement(bool doDelete, DeleteInfo delInfo, out TextRange requestedSelection, UndoManager<Document, DocumentViewUpdateInfo> UndoManager)
    {
        if (IsChildrenReadOnly)
        {
            if (
                (delInfo.Range.Maximum >= CodePointLength && delInfo.DeleteMode is DeleteModes.Backward) ||
                (delInfo.Range.Minimum <= 0 && delInfo.DeleteMode is DeleteModes.Forward)
            )
            {
                requestedSelection = new TextRange(0, CodePointLength, altPosition: true);
                return false;
            }
        }
        if (IsRangeWithinTheSameChildParagraph(delInfo.Range, out var paraIdx, out var newRange))
        {
            if (doDelete)
            {
                var child = Children[paraIdx];
                var savedLength = child.CodePointLength;
                var success = child.DeletePartial(delInfo with { Range = newRange }, out var returnRange, UndoManager);
                child.LocalInfo.OffsetFromThis(ref returnRange);
                requestedSelection = returnRange;
                return true;
            } else
            {
                if (!Children[paraIdx].CanDeletePartial(delInfo with { Range = newRange }, out requestedSelection))
                    return false;
                // if at the end of previous paragraph, and delete backward
                if (newRange.Maximum + 1 >= Children[paraIdx].CodePointLength && delInfo.DeleteMode is DeleteModes.Backward)
                {
                    Debug.Assert(paraIdx + 1 < Children.Count);
                    return Children[paraIdx].CanJoinWith(Children[paraIdx + 1]);
                }// if at the end of this paragraph, and delete forward
                if (newRange.Minimum <= 0 && delInfo.DeleteMode is DeleteModes.Forward)
                {
                    Debug.Assert(paraIdx >= 1);
                    return Children[paraIdx - 1].CanJoinWith(Children[paraIdx]);
                }
                return true;
            }
        }
        else if (IsChildrenReadOnly)
        {
            requestedSelection = new TextRange(0, CodePointLength, altPosition: true);
            return false;
        }
        else
        {
            var interactingRanges = GetInteractingRuns(delInfo.Range).ToArray();
            bool isFailed = false;
            var para = interactingRanges[0].Paragraph;
            TextRange range = delInfo.Range;
            if (interactingRanges[0].Partial && !para.CanDeletePartial(
                para.LocalInfo.OffsetToThis(delInfo), out var rq1
            ))
            {
                isFailed = true;
                para.LocalInfo.OffsetFromThis(ref rq1);
                range = range.Normalized;
                range.Start = Math.Min(range.Start, rq1.Minimum);
                range.End = Math.Max(range.End, rq1.Maximum);
            }
            para = interactingRanges[^1].Paragraph;
            if (interactingRanges[^1].Partial && !para.CanDeletePartial(
                para.LocalInfo.OffsetToThis(delInfo), out var rq2
            ))
            {
                isFailed = true;
                para.LocalInfo.OffsetFromThis(ref rq2);
                range = range.Normalized;
                range.Start = Math.Min(range.Start, rq2.Minimum);
                range.End = Math.Max(range.End, rq2.Maximum);
            }
            // Check backspace deletion
            {
                var idx = interactingRanges[^1].Index;
                para = interactingRanges[^1].Paragraph;
                newRange = para.LocalInfo.OffsetToThis(range);
                if (newRange.Minimum <= 0 && delInfo.DeleteMode is DeleteModes.Backward)
                {
                    Debug.Assert(idx >= 1);
                    if (!Children[idx].CanJoinWith(Children[idx - 1]))
                    {
                        requestedSelection = new TextRange(Children[idx + 1].LocalInfo.CodePointIndex,
                            Children[idx + 1].LocalInfo.CodePointIndex + Children[idx + 1].CodePointLength,
                            altPosition: true
                        );
                        return false;
                    }
                }
                // if at the end of this paragraph, and delete forward
                idx = interactingRanges[0].Index;
                para = interactingRanges[0].Paragraph;
                newRange = para.LocalInfo.OffsetToThis(range);
                if (newRange.Minimum + 1 >= para.CodePointLength && delInfo.DeleteMode is DeleteModes.Forward)
                {
                    Debug.Assert(paraIdx >= 1);
                    if (!Children[idx - 1].CanJoinWith(Children[idx]))
                    {
                        requestedSelection = new TextRange(Children[idx + 1].LocalInfo.CodePointIndex,
                            Children[idx + 1].LocalInfo.CodePointIndex + Children[idx + 1].CodePointLength,
                            altPosition: true
                        );
                        return false;
                    }
                }
            }
            if (isFailed)
            {
                requestedSelection = range;
                return false;
            }
            // If not actually deleting, no need to do anytihng else
            if (doDelete)
            {
                requestedSelection = range;
                return true;
            }
            para = interactingRanges[0].Paragraph;
            if (interactingRanges[0].Partial)
                interactingRanges[0].Paragraph.DeletePartial(para.LocalInfo.OffsetToThis(delInfo), out _, UndoManager);
            foreach (var idx in
                from i in (1..^1).Iterate(length: interactingRanges.Length, step: -1)
                select interactingRanges[i].Index
            ) UndoManager.Do(new UndoDeleteParagraph(this, idx));
            // interactingRange at index 1 and above is no longer valid. Use with caution
            var firstIdx = interactingRanges[0].Index;
            if (!interactingRanges[0].Partial)
                // It's the same
                UndoManager.Do(new UndoDeleteParagraph(this, interactingRanges[0].Index));
            if (interactingRanges.Length > 1)
            {
                // Previous Index ^1 is now at index IR[0] + 1
                if (interactingRanges[^1].Partial)
                {
                    para = Children[interactingRanges[0].Index + 1];
                    para.ParentInfo = para.ParentInfo with { Index = interactingRanges[0].Index + 1 };
                    para.DeletePartial(
                        para.LocalInfo.OffsetToThis(delInfo),
                        out _,
                        UndoManager
                    );
                }
                else
                {
                    UndoManager.Do(new UndoDeleteParagraph(this, interactingRanges[0].Index + 1));
                }
            }
            if (
                // Join with next paragraph if delete the end of the paragraph
                interactingRanges[0].Partial && interactingRanges[0].Offset + interactingRanges[0].Length
                >= interactingRanges[0].Paragraph.CodePointLength
            )
                if (!Children[firstIdx].TryJoin(UndoManager, firstIdx))
                    EnsureParagraphEnding(Children[firstIdx], UndoManager);
            requestedSelection = new(range.Minimum);
            return true;
        }
    }
    public override bool DeletePartial(DeleteInfo delInfo, out TextRange requestedSelection, UndoManager<Document, DocumentViewUpdateInfo> UndoManager)
    {
        return DeletePartialImplement(true, delInfo, out requestedSelection, UndoManager);
    }
    public override bool CanDeletePartial(DeleteInfo deleteInfo, out TextRange requestedSelection)
    {
        return DeletePartialImplement(false, deleteInfo, out requestedSelection, null);
    }
}