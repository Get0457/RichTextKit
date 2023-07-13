// This file has been edited and modified from its original version.
// Original Class Name: TopTen.RichTextKit.Editor.TextDocument
// The original version of this file can be found at https://github.com/toptensoftware/RichTextKit/.
// Original copyright notice is below.
// Copyright © 2019-2020 Topten Software. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may 
// not use this product except in compliance with the License. You may obtain 
// a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
// License for the specific language governing permissions and limitations 
// under the License.

using Get.RichTextKit.Editor.Paragraphs;
using Get.RichTextKit.Editor.UndoUnits;
using SkiaSharp;
using System.Diagnostics;
using System.Drawing;
using Get.RichTextKit;
using Get.RichTextKit.Editor;
using Get.RichTextKit.Utils;
using Get.RichTextKit.Styles;

namespace Get.RichTextKit.Editor;

public partial class Document
{
    public DocumentEditor Editor { get; }
}
public partial class DocumentEditor
{
    Document Document;
    internal DocumentEditor(Document owner)
    {
        Document = owner;
    }
    /// <summary>
    /// Replaces a range of text with the specified text
    /// </summary>
    /// <param name="view">The view initiating the operation</param>
    /// <param name="range">The range to be replaced</param>
    /// <param name="text">The text to replace with</param>
    /// <param name="semantics">Controls how undo operations are coalesced and view selections updated</param>"
    /// <param name="styleToUse">The style to use for the added text (optional)</param>
    public void ReplaceText(TextRange range, ReadOnlySpan<char> text, EditSemantics semantics, IStyle? styleToUse = null)
    {
        // Convert text to utf32
        Slice<int> codePoints;
        if (!text.IsEmpty)
        {
            codePoints = new Utf32Buffer(text).AsSlice();
        }
        else
        {
            codePoints = Slice<int>.Empty;
        }

        // Do the work
        ReplaceText(range, codePoints, semantics, styleToUse);
    }

    /// <summary>
    /// Replaces a range of text with the specified text
    /// </summary>
    /// <param name="view">The view initiating the operation</param>
    /// <param name="range">The range to be replaced</param>
    /// <param name="text">The text to replace with</param>
    /// <param name="semantics">Controls how undo operations are coalesced and view selections updated</param>"
    /// <param name="styleToUse">The style to use for the added text (optional)</param>
    public void ReplaceText(TextRange range, ReadOnlySpan<int> text, EditSemantics semantics, IStyle? styleToUse = null)
    {
        // Convert text to utf32
        Slice<int> codePoints;
        if (!text.IsEmpty)
        {
            codePoints = new Utf32Buffer(text).AsSlice();
        }
        else
        {
            codePoints = Slice<int>.Empty;
        }

        // Do the work
        ReplaceText(range, codePoints, semantics, styleToUse);
    }

    /// <summary>
    /// Replaces a range of text with the specified text
    /// </summary>
    /// <param name="view">The view initiating the operation</param>
    /// <param name="range">The range to be replaced</param>
    /// <param name="codePoints">The text to replace with</param>
    /// <param name="semantics">Controls how undo operations are coalesced and view selections updated</param>"
    /// <param name="styleToUse">The style to use for the added text (optional)</param>
    public void ReplaceText(TextRange range, Slice<int> codePoints, EditSemantics semantics, IStyle? styleToUse = null)
    {
        Document.Layout.EnsureValid();
        // Check range is valid
        if (range.Minimum < 0 || range.Maximum > Document.Layout.Length)
            throw new ArgumentException("Invalid range", nameof(range));

        if (IsImeComposing)
            FinishImeComposition();

        if (styleToUse is null)
        {
            styleToUse = Document.GetStyleAtPosition(range.CaretPosition);
            if (styleToUse is IDoNotCombineStyle)
            {
                styleToUse = new CopyStyle(styleToUse);
            }
        }

        var styledText = new StyledText(codePoints);
        styledText.ApplyStyle(0, styledText.Length, styleToUse);
        ReplaceTextInternal(range, styledText, semantics, -1);
        Document.InvokeTextChanged(range);
    }

    /// <summary>
    /// Replaces a range of text with the specified text
    /// </summary>
    /// <param name="view">The view initiating the operation</param>
    /// <param name="range">The range to be replaced</param>
    /// <param name="codePoints">The text to replace with</param>
    /// <param name="semantics">Controls how undo operations are coalesced and view selections updated</param>"
    /// <param name="styleToUse">The style to use for the added text (optional)</param>
    public void ReplaceText(TextRange range, StyledText styledText, EditSemantics semantics)
    {
        // Check range is valid
        if (range.Minimum < 0 || range.Maximum > Document.Layout.Length)
            throw new ArgumentException("Invalid range", nameof(range));

        if (IsImeComposing)
            FinishImeComposition();

        ReplaceTextInternal(range, styledText, semantics, -1);
    }
    /// <summary>
    /// Internal helper to replace text creating an undo unit
    /// </summary>
    /// <param name="range">The range of text to be replaced</param>
    /// <param name="text">The replacement text</param>
    /// <param name="semantics">The edit semantics of the change</param>
    /// <param name="imeCaretOffset">The position of the IME caret relative to the start of the range</param>
    void ReplaceTextInternal(TextRange range, StyledText text, EditSemantics semantics, int imeCaretOffset)
    {
        // Quit if redundant
        if (!range.IsRange && text.Length == 0)
            return;

        // Make sure layout is up to date
        Document.Layout.EnsureValid();

        // Normalize the range
        range = range.Normalized;

        // Update range to include the following character if overtyping
        // and no current selection
        if (semantics == EditSemantics.Overtype && !range.IsRange)
            range = GetOvertypeRange(range);

        // Try to extend the last undo operation
        if (Document.UndoManager.GetUnsealedUnit() is UndoReplaceTextGroup group &&
            group.TryExtend(Document, range, text, semantics, imeCaretOffset))
            return;

        // Wrap all edits in an undo group.  Note this is a custom
        // undo group that also fires the DocumentChanged notification
        // to views.
        group = new UndoReplaceTextGroup();
        using (Document.UndoManager.OpenGroup(group))
        {
            // Delete range (if any)
            if (range.Length != 0)
            {
                DeleteInternal(range);
            }

            // Insert text (if any)
            if (text.Length != 0)
            {
                InsertInternal(range.Minimum, text);
            }

            // Setup document change info on the group
            group.SetDocumentChangeInfo(new DocumentChangeInfo()
            {
                CodePointIndex = range.Minimum,
                OldLength = range.Normalized.Length,
                NewLength = text.Length,
                Semantics = semantics,
                ImeCaretOffset = imeCaretOffset,
            });
        }
    }

    /// <summary>
    /// Delete a section of the document
    /// </summary>
    /// <remarks>
    /// Returns the index of the first paragraph affected
    /// </remarks>
    /// <param name="range">The range to be deleted</param>
    int DeleteInternal(TextRange range)
    {
        // Iterate over the sections to be deleted
        int joinParagraph = -1;
        int firstParagraph = -1;
        foreach (var subRun in Document.Paragraphs.GetIntersectingRunsRecursiveReverse(range.Start, range.Length))
        {
            Debug.Assert(joinParagraph == -1);

            firstParagraph = subRun.Index;

            // Is it a partial paragraph deletion?
            if (subRun.Partial)
            {
                // Yes...

                // Get the paragraph
                var para = subRun.Paragraph;

                // If we're deleting paragraph separator at the the end 
                // of this paragraph then remember this paragraph needs to 
                // be joined with the next paragraph after any intervening 
                // paragraphs have been deleted
                if (subRun.Offset + subRun.Length >= para.Length)
                {
                    // If deleting paragraph at the end, do not delete
                    if (!(subRun.Index >= 0 && subRun.Index + 1 < subRun.Parent.Paragraphs.Count))
                    {
                        continue;
                    }
                    Debug.Assert(joinParagraph == -1);
                    joinParagraph = subRun.Index;
                }

                // Delete the text
                para.DeletePartial(Document.UndoManager, subRun);
            }
            else
            {
                if (Document.Paragraphs.Count > 1)
                {
                    // Remove entire paragraph
                    Document.UndoManager.Do(new UndoDeleteParagraph(subRun.Parent, subRun.Index));
                }
                else
                {
                    var para = new TextParagraph(Paragraphs[0].EndStyle);
                    // Remove entire paragraph
                    Document.UndoManager.Do(new UndoDeleteParagraph(subRun.Parent, subRun.Index));
                    // Add a dummy paragraph
                    Document.UndoManager.Do(new UndoInsertParagraph(subRun.Parent, 0, para));
                }
            }
        }

        // If the deletion started mid paragraph and crossed into
        // subsequent paragraphs then we need to join the paragraphs
        if (joinParagraph >= 0 && joinParagraph + 1 < Document.Paragraphs.Count)
        {
            // Get both paragraphs
            var firstPara = Document.Paragraphs[joinParagraph];
            var secondPara = Document.Paragraphs[joinParagraph + 1];

            firstPara.TryJoin(Document.UndoManager, joinParagraph);
        }

        // Layout is now invalid
        Document.Layout.EnsureValid();

        return firstParagraph;
    }

    //void NewDeleteInternal(TextRange range)
    //{
    //    while (range.IsRange)
    //    {
    //        var paraIdx = Paragraphs.FromCodePointIndexAsIndex(range.CaretPosition, out var endIdx);
    //        var para = Paragraphs[paraIdx];
    //        var startIdx = Math.Max(range.Start - para.GlobalInfo.CodePointIndex, 0);
    //        var isEnd = endIdx + 1 == para.Length;
    //        var isStart = startIdx is 0;
    //        if (isEnd && isStart)
    //        {
    //            // Delete Entire Paragraph
    //            Paragraphs.Remove(para);
    //        }
    //        else
    //        {
    //            bool containsTheEnd = endIdx + 1 == para.Length;
    //            if (containsTheEnd) endIdx--;
    //            para.DeletePartial(Document.UndoManager, new(Parent: , Index: 0, Offset: startIdx, Length: endIdx - startIdx, Partial: true));
    //            if (containsTheEnd) para.TryJoin(Document.UndoManager, paraIdx);
    //        }
    //        range = new(range.Start, startIdx + para.GlobalInfo.CodePointIndex, true);
    //    }
    //}

    /// <summary>
    /// Insert text into the document
    /// </summary>
    /// <param name="position">The position to insert the text at</param>
    /// <param name="text">The text to insert</param>
    /// <returns>The index of the first paragraph affected</returns>
    int InsertInternal(int position, StyledText text)
    {
        // Find the position in the document
        var para = Document.Paragraphs.GlobalFromCodePointIndex(new CaretPosition(position), out var parent, out var paraIndex, out var indexInParagraph);
        
        // Is it a text paragraph?
        if (para is not ITextParagraph)
        {
            // TODO:
#if DEBUG
            Debugger.Break();
#endif
            throw new NotImplementedException();
        }

        // Split the passed text into paragraphs
        var parts = text.CodePoints.GetRanges(Document.NewParagraphSeparator).ToArray();
        if (parts.Length > 1)
        {
            // Split the paragraph at the insertion point into paragraphs A and B
            var paraA = para;
            var paraB = para.Split(Document.UndoManager, indexInParagraph);
            int startingAppendingIdx = 1;
            // Append the first part of the inserted text to the end of paragraph A
            var firstPart = parts[0];
            if (firstPart.Length != 0)
            {
                if (paraA is ITextParagraph tp)
                    Document.UndoManager.Do(new UndoInsertText(
                        tp.TextBlock,
                        indexInParagraph,
                        text.Extract(firstPart.Offset, firstPart.Length)
                    ));
                else startingAppendingIdx = 0;
            }

            int endingAppendingIdx = parts.Length - 1;
            // Prepend the last text part of the inserted text to the start paragraph B
            var lastPart = parts[parts.Length - 1];
            if (lastPart.Length != 0)
            {
                if (paraB is ITextParagraph tp)
                    Document.UndoManager.Do(new UndoInsertText(tp.TextBlock, 0, text.Extract(lastPart.Offset, lastPart.Length)));
                else endingAppendingIdx = parts.Length;
            }

            // We could do this above, but by doing it after the above InsertText operations
            // we prevent subsequent typing from be coalesced into this unit.
            Document.UndoManager.Do(new UndoInsertParagraph(parent, paraIndex + 1, paraB));

            // Create new paragraphs for parts [1..N-1] of the inserted text and insert them
            // betweeen paragraphs A and B.
            for (int i = startingAppendingIdx; i < endingAppendingIdx; i++)
            {
                var betweenPara = new TextParagraph(para.EndStyle);
                betweenPara.SetStyleContinuingFrom(para);
                var part = parts[i];
                betweenPara.TextBlock.InsertText(0, text.Extract(part.Offset, part.Length));
                Document.UndoManager.Do(new UndoInsertParagraph(parent, paraIndex + i, betweenPara));
            }
        }
        else
        {
            if (para is ITextParagraph tp)
            {
                if (tp.TextBlock.Length == indexInParagraph)
                    indexInParagraph--;
                Document.UndoManager.Do(new UndoInsertText(tp.TextBlock, indexInParagraph, text));
            }
            else
            {
                var newPara = new TextParagraph(para.EndStyle);
                newPara.SetStyleContinuingFrom(para);
                newPara.TextBlock.AddText(text);
                Document.UndoManager.Do(new UndoInsertParagraph(parent, paraIndex + 1, newPara));
            }
        }

        return paraIndex;
    }
    /// <summary>
    /// Gets the range of text that will be overwritten by overtype mode
    /// at a particular location in the document
    /// </summary>
    /// <param name="range">The current selection range</param>
    /// <returns>The range that will be replaced by overtyping</returns>
    public TextRange GetOvertypeRange(TextRange range)
    {
        if (range.IsRange)
            return range;

        float? unused = null;
        var nextPos = Navigate(range.CaretPosition, NavigationKind.CharacterRight, default, ref unused);

        var paraThis = Document.Paragraphs.FromCodePointIndexAsIndex(range.CaretPosition, out var _);
        var paraNext = Document.Paragraphs.FromCodePointIndexAsIndex(nextPos, out var _);

        if (paraThis == paraNext && nextPos.CodePointIndex < Document.Layout.Length)
            range.End = nextPos.CodePointIndex;

        return range;
    }
    /// <summary>
    /// Hit test this string
    /// </summary>
    /// <param name="x">The x-coordinate relative to top-left of the document</param>
    /// <param name="y">The y-coordinate relative to top-left of the document</param>
    /// <returns>A HitTestResult</returns>
    public HitTestResult HitTest(PointF pt)
    {
        var para = Document.Paragraphs.FindClosest(y: pt.Y);
        para.GlobalInfo.OffsetToThis(ref pt);
        var htr = para.HitTest(pt);
        para.GlobalInfo.OffsetFromThis(ref htr);
        return htr;
    }
    /// <summary>
    /// Calculates useful information for displaying a caret
    /// </summary>
    /// <param name="position">The caret position</param>
    /// <returns>A CaretInfo struct, or CaretInfo.None</returns>
    public CaretInfo GetCaretInfo(CaretPosition position)
    {
        // Make sure layout up to date
        Document.Layout.EnsureValid();

        // Find the paragraph
        if (position.CodePointIndex < 0) position.CodePointIndex = 0;
        var para = Document.Paragraphs.FromCodePointIndex(position, out var indexInParagraph);

        // Get caret info
        var ci = para.GetCaretInfo(new CaretPosition(indexInParagraph, position.AltPosition));

        // Adjust caret info to be relative to document
        para.GlobalInfo.OffsetFromThis(ref ci);

        // Done
        return ci;
    }
}
