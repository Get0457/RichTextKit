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
    bool DeletePartialImplement(bool doDelete, DeleteInfo delInfo, out TextRange requestedSelection, UndoManager<Document, DocumentViewUpdateInfo> UndoManager)
    {
        var lastCodePoint = CodePointLength - 1;
        var joinWithNext = delInfo.Range.Contains(lastCodePoint);
        if (joinWithNext)
        {
            if (delInfo.Range.IsReversed)
                delInfo.Range = delInfo.Range with { Start = lastCodePoint };
            else
                delInfo.Range = delInfo.Range with { End = lastCodePoint };
        }
        if (IsChildrenReadOnly)
        {
            if (
                (delInfo.Range.Maximum >= EndCaretPosition.CodePointIndex && delInfo.DeleteMode is DeleteModes.Backward) ||
                (delInfo.Range.Minimum <= 0 && delInfo.DeleteMode is DeleteModes.Forward)
            )
            {
                requestedSelection = new TextRange(StartCaretPosition.CodePointIndex, EndCaretPosition.CodePointIndex, altPosition: EndCaretPosition.AltPosition);
                return false;
            }
        }
        if (IsRangeWithinTheSameChildParagraph(delInfo.Range, out var paraIdx, out var newRange))
        {
            if (doDelete)
            {
                var child = Children[paraIdx];
                var savedLength = child.CodePointLength;
                if (child.ShouldDeletAll(delInfo with { Range = newRange }))
                {
                    requestedSelection = newRange;
                    requestedSelection.Start = requestedSelection.End = requestedSelection.Minimum;
                    child.LocalInfo.OffsetFromThis(ref requestedSelection);
                    UndoManager.Do(new UndoDeleteParagraph(child.GlobalParagraphIndex) { ShouldNotifyInfo = false });
                    return true;
                } else
                {
                    var success = child.DeletePartial(delInfo with { Range = newRange }, out var returnRange, UndoManager);
                    child.LocalInfo.OffsetFromThis(ref returnRange);
                    requestedSelection = returnRange;
                    if (success)
                        goto SuccessDelete;
                    else
                        return false;
                }
            }
            else
            {
                if (!Children[paraIdx].CanDeletePartial(delInfo with { Range = newRange }, out requestedSelection))
                    return false;
                // if at the end of previous paragraph, and delete backward
                if (newRange.Maximum + 1 >= Children[paraIdx].CodePointLength && delInfo.DeleteMode is DeleteModes.Backward)
                {
                    if (IsChildrenReadOnly)
                    {
                        requestedSelection = new TextRange(StartCaretPosition.CodePointIndex, EndCaretPosition.CodePointIndex, altPosition: EndCaretPosition.AltPosition);
                        return false;
                    }
                    Debug.Assert(paraIdx + 1 < Children.Count);
                    return Children[paraIdx].CanJoinWith(Children[paraIdx + 1]);
                }// if at the end of this paragraph, and delete forward
                if (newRange.Minimum <= 0 && delInfo.DeleteMode is DeleteModes.Forward)
                {
                    if (IsChildrenReadOnly)
                    {
                        requestedSelection = new TextRange(StartCaretPosition.CodePointIndex, EndCaretPosition.CodePointIndex, altPosition: EndCaretPosition.AltPosition);
                        return false;
                    }
                    Debug.Assert(paraIdx >= 1);
                    return Children[paraIdx - 1].CanJoinWith(Children[paraIdx]);
                }
                goto SuccessDelete;
            }
        }
        else if (IsChildrenReadOnly)
        {
            requestedSelection = new TextRange(StartCaretPosition.CodePointIndex, EndCaretPosition.CodePointIndex, altPosition: EndCaretPosition.AltPosition);
            return false;
        }
        else
        {
            var interactingRanges = GetInteractingRuns(delInfo.Range).ToArray();
            bool isFailed = false;
            var para = interactingRanges[0].Paragraph;
            TextRange range = delInfo.Range;

            // Check if the first paragraph can be deleted properly
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

            // Check if the last paragraph can be deleted properly
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
                if (newRange.Maximum >= para.EndCaretPosition.CodePointIndex && delInfo.DeleteMode is DeleteModes.Backward)
                {
                    Debug.Assert(idx + 1 < Children.Count);
                    if (!Children[idx].CanJoinWith(Children[idx + 1]))
                    {
                        requestedSelection = new TextRange(
                            Children[idx + 1].StartCaretPosition.CodePointIndex,
                            Children[idx + 1].EndCaretPosition.CodePointIndex,
                            altPosition: Children[idx + 1].EndCaretPosition.AltPosition
                        );
                        Children[idx + 1].LocalInfo.OffsetFromThis(ref requestedSelection);
                        return false;
                    }
                }
                // if at the end of this paragraph, and delete forward
                idx = interactingRanges[0].Index;
                para = interactingRanges[0].Paragraph;
                newRange = para.LocalInfo.OffsetToThis(range);
                if (newRange.Maximum >= para.EndCaretPosition.CodePointIndex && delInfo.DeleteMode is DeleteModes.Forward)
                {
                    Debug.Assert(idx + 1 < Children.Count);
                    if (!Children[idx].CanJoinWith(Children[idx + 1]))
                    {
                        requestedSelection = new TextRange(
                            Children[idx + 1].StartCaretPosition.CodePointIndex,
                            Children[idx + 1].EndCaretPosition.CodePointIndex,
                            altPosition: Children[idx + 1].EndCaretPosition.AltPosition
                        );
                        Children[idx + 1].LocalInfo.OffsetFromThis(ref requestedSelection);
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
            if (!doDelete)
            {
                requestedSelection = range;
                return true;
            }

            // Delete Last first
            // Also check if the length > 1, otherwise interactingRanges[^1] == interactingRanges[0]
            // and we don't want to delete the same paragraph twice
            if (interactingRanges.Length > 1)
            {
                if (interactingRanges[^1].Partial)
                {
                    para = interactingRanges[^1].Paragraph;
                    para.DeletePartial(
                        para.LocalInfo.OffsetToThis(delInfo),
                        out _,
                        UndoManager
                    );
                }
                else
                {
                    UndoManager.Do(new UndoDeleteParagraph(interactingRanges[^1].Paragraph.GlobalParagraphIndex));
                }
            }

            // Delete the middle, from back to front
            {
                foreach (var idx in
                    from i in (1..^1).Iterate(length: interactingRanges.Length, step: -1)
                    select interactingRanges[i].Paragraph.GlobalParagraphIndex
                ) UndoManager.Do(new UndoDeleteParagraph(idx) { ShouldNotifyInfo = false });
            }

            // Delete the first
            {
                if (interactingRanges[0].Partial)
                {
                    para = interactingRanges[0].Paragraph;
                    para.DeletePartial(
                        para.LocalInfo.OffsetToThis(delInfo),
                        out _,
                        UndoManager
                    );
                }
                else
                {
                    UndoManager.Do(new UndoDeleteParagraph(interactingRanges[0].Paragraph.GlobalParagraphIndex));
                }
            }

            requestedSelection = new(range.Minimum);
            goto SuccessDelete;
        }
    SuccessDelete:
        if (!doDelete) return true;
        if (joinWithNext) TryJoinWithNextParagraph(UndoManager);
        return true;
    }
    public override bool DeletePartial(DeleteInfo delInfo, out TextRange requestedSelection, UndoManager<Document, DocumentViewUpdateInfo> UndoManager)
    {
        return DeletePartialImplement(true, delInfo, out requestedSelection, UndoManager);
    }
    public override bool CanDeletePartial(DeleteInfo deleteInfo, out TextRange requestedSelection)
    {
        return DeletePartialImplement(false, deleteInfo, out requestedSelection, null);
    }
    public override bool ShouldDeletAll(DeleteInfo deleteInfo)
    {
        return false;
    }
}