// This file has been edited and modified from its original version.
// The original version of this file can be found at https://github.com/toptensoftware/RichTextKit/.
// Original copyright notice is below.
// RichTextKit
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

using Get.RichTextKit.Editor.Structs;
using Get.RichTextKit.Editor.DocumentView;
using SkiaSharp;
using System.Drawing;
using Get.RichTextKit;
using Get.RichTextKit.Utils;
using Get.RichTextKit.Styles;
using Get.RichTextKit.Editor.Paragraphs.Panel;

namespace Get.RichTextKit.Editor.Paragraphs;
public enum NavigationStatus
{
    Success,
    MoveBefore,
    MoveAfter
}
public enum NavigationSnap
{
    Character,
    Word
}
public enum ParagraphSelectionKind
{
    None,
    Word
}
public enum NavigationDirection : byte
{
    Backward = default,
    Forward = 1,
    Down = 2,
    Up = 3
}
/// <summary>
/// Abstract base class for all TextDocument paragraphs
/// </summary>
public abstract partial class Paragraph : IRun, IParentOrParagraph
{
    public abstract IStyle StartStyle { get; }
    public abstract IStyle EndStyle { get; }
    /// <summary>
    /// Constructs a new Paragraph
    /// </summary>
    protected Paragraph()
    {

    }
    /// <inheritdoc cref="Layout(LayoutParentInfo)"/>
    protected abstract void LayoutOverride(LayoutParentInfo owner);
    /// <summary>
    /// Layout the content of this paragraph
    /// </summary>
    /// <param name="owner">The TextDocument that owns this paragraph</param>
    public void Layout(LayoutParentInfo owner)
        => LayoutOverride(owner with { AvaliableWidth = owner.AvaliableWidth - Margin.Left - Margin.Right });
    /// <summary>
    /// Calculate the selection range from the navigation
    /// </summary>
    /// <param name="selection">The current selection</param>
    /// <param name="snap">The place to snap the new selection</param>
    /// <param name="direction">The direction to navigate</param>
    /// <param name="keepSelection">Whether to keep the selection</param>
    /// <param name="ghostXCoord">The ghost X Coordinate for up/down navigation</param>
    /// <param name="newSelection">The new selection; The value is only valid if the function reports success</param>
    /// <returns>The navigation status</returns>
    /// <remarks>
    /// newSelection is only valid if the navigation status is successful
    /// </remarks>
    public NavigationStatus Navigate(TextRange selection, NavigationSnap snap, NavigationDirection direction, bool keepSelection, ref float? ghostXCoord, out TextRange newSelection)
    {
        return NavigateOverride(selection, snap, direction, keepSelection, ref ghostXCoord, out newSelection);
    }
    public virtual CaretPosition StartCaretPosition => new(0, altPosition: false);
    public virtual CaretPosition EndCaretPosition => new(Math.Max(0, CodePointLength - 2), altPosition: false);
    protected abstract NavigationStatus NavigateOverride(TextRange selection, NavigationSnap snap, NavigationDirection direction, bool keepSelection, ref float? ghostXCoord, out TextRange newSelection);
    protected NavigationStatus VerticalNavigateUsingLineInfo(TextRange selection, NavigationSnap snap, NavigationDirection direction, bool keepSelection, ref float? ghostXCoord, out TextRange newSelection)
    {
        if (direction is not (NavigationDirection.Up or NavigationDirection.Down))
        {
            throw new ArgumentOutOfRangeException(nameof(direction));
        }

        // Get the line number the caret is on
        var ci = GetCaretInfo(new CaretPosition(selection.End, selection.AltPosition));

        // Resolve the xcoord
        ghostXCoord ??= ci.CaretXCoord + GlobalInfo.ContentPosition.X;

        // Work out which line to hit test
        var lineInfo = GetLineInfo(ci.LineIndex);
        var toLine = direction is NavigationDirection.Down ? lineInfo.NextLine : lineInfo.PrevLine;

        // Exceed paragraph?
        if (toLine is null)
        {
            newSelection = default;
            if (direction is NavigationDirection.Up)
                return NavigationStatus.MoveBefore;
            else
                return NavigationStatus.MoveAfter;
        }


        // Hit test the line
        var htr = HitTestLine(toLine.Value, ghostXCoord.Value - GlobalInfo.ContentPosition.X);
        selection.EndCaretPosition = new CaretPosition(htr.ClosestCodePointIndex, htr.AltCaretPosition);
        if (!keepSelection)
            selection.Start = selection.End;
        newSelection = selection;
        return NavigationStatus.Success;
    }
    public record struct PaintOptions(RectangleF ViewBounds, TextPaintOptions TextPaintOptions, IDocumentViewOwner? viewOwner)
    {
        
    }
    /// <summary>
    /// Paint this paragraph
    /// </summary>
    /// <param name="canvas">The canvas to paint to</param>
    /// <param name="options">Paint options</param>
    public abstract void Paint(SKCanvas canvas, PaintOptions options);

    /// <summary>
    /// Get caret position information
    /// </summary>
    /// <remarks>
    /// The returned caret info should be relative to the paragraph's content
    /// </remarks>
    /// <param name="position">The caret position</param>
    /// <returns>A CaretInfo struct, or CaretInfo.None</returns>
    public abstract CaretInfo GetCaretInfo(CaretPosition position);

    /// <summary>
    /// Get line position information
    /// </summary>
    /// <remarks>
    /// The returned caret info should be relative to the paragraph's content
    /// </remarks>
    /// <param name="line">The line number</param>
    /// <returns>A LineInfo struct</returns>
    public abstract LineInfo GetLineInfo(int line);
    internal LineInfo GetLineInfo(Index idx) => GetLineInfo(idx.GetOffset(LineCount));
    public LineInfo GetLineInfo(int idx, bool fromEnd) => GetLineInfo(new Index(idx, fromEnd));

    /// <summary>
    /// Hit test this paragraph
    /// </summary>
    /// <param name="pt">The coordinate relative to top left of the paragraph content</param>
    /// <returns>A HitTestResult</returns>
    public abstract HitTestResult HitTest(PointF pt);

    /// <summary>
    /// Hit test a line in this paragraph
    /// </summary>
    /// <remarks>
    /// The number of lines can be determined from LineIndicies.Count.
    /// </remarks>
    /// <param name="lineIndex">The line number to be tested</param>
    /// <param name="x">The x-coordinate relative to left of the paragraph content</param>
    /// <returns>A HitTestResult</returns>
    public abstract HitTestResult HitTestLine(int lineIndex, float x);

    public virtual SelectionInfo GetSelectionInfo(ParentInfo parentInfo, TextRange selection) => new(
        selection, null, GetCaretInfo(selection.StartCaretPosition), GetCaretInfo(selection.EndCaretPosition),
        this,
        GetInteractingRuns(parentInfo, selection),
        GetInteractingRunsRecursive(parentInfo, selection),
        GetBFSInteractingRuns(parentInfo, selection)
    );
    // So protected members can access the method
    protected static IEnumerable<SubRunInfo> GetInteractingRuns(Paragraph para, ParentInfo parentInfo, TextRange selection)
        => para.GetInteractingRuns(parentInfo, selection);
    protected virtual IEnumerable<SubRunInfo> GetInteractingRuns(ParentInfo parentInfo, TextRange selection)
    {
        yield return new(
            parentInfo, selection.Minimum, Math.Abs(selection.Length),
            !(selection.Minimum <= 0 && selection.Maximum >= CodePointLength)
        );
    }
    protected static IEnumerable<SubRunInfo> GetInteractingRunsRecursive(Paragraph para, ParentInfo parentInfo, TextRange selection)
        => para.GetInteractingRunsRecursive(parentInfo, selection);
    protected virtual IEnumerable<SubRunInfo> GetInteractingRunsRecursive(ParentInfo parentInfo, TextRange selection)
        => GetInteractingRuns(parentInfo, selection);
    public IEnumerable<SubRunBFSInfo> GetBFSInteractingRuns(ParentInfo parentInfo, TextRange selection)
    {
        foreach (var subRun in GetInteractingRuns(parentInfo, selection))
        {
            // We can do this because IEnumerable is lazy so the function will actually not evaluate here
            var childEnumerable =
                subRun.Paragraph == this ? Enumerable.Empty<SubRunBFSInfo>() :
                subRun.Paragraph.GetBFSInteractingRuns(
                    subRun.ParentInfo, new(subRun.Offset, subRun.Offset + subRun.Length)
                );
            if (subRun.Partial)
            {
                foreach (var sr2 in childEnumerable)
                    yield return sr2;
            } else
            {
                yield return new SubRunBFSInfo(subRun, childEnumerable);
            }
        }
    }
    public abstract TextRange GetSelectionRange(CaretPosition position, ParagraphSelectionKind kind);

    /// <summary>
    /// Gets the length of this paragraph in code points
    /// </summary>
    /// <remarks>
    /// All paragraphs must have a non-zero length and text paragraphs
    /// should include the end of paragraph marker in the length.
    /// </remarks>
    public abstract int CodePointLength { get; }

    /// <summary>
    /// Gets the line count of this paragraph in code points
    /// </summary>
    /// <remarks>
    /// All paragraphs must have a non-zero line count
    /// </remarks>
    public abstract int LineCount { get; }
    /// <summary>
    /// Gets the line count of this paragraph in code points
    /// </summary>
    /// <remarks>
    /// All paragraphs must have a non-zero line count
    /// </remarks>
    public abstract int DisplayLineCount { get; }

    /// <summary>
    /// Qureries the height of this paragraph, excluding margins
    /// </summary>
    protected abstract float ContentHeightOverride { get; }
    /// <summary>
    /// Qureries the height of this paragraph
    /// </summary>
    public float ContentHeight => ContentHeightOverride + Margin.Top + Margin.Bottom;

    /// <summary>
    /// Queries the width of this paragraph, excluding margins
    /// </summary>
    protected abstract float ContentWidthOverride { get; }
    /// <summary>
    /// Queries the width of this paragraph
    /// </summary>
    public float ContentWidth => ContentWidthOverride + Margin.Left + Margin.Right;
    protected internal LayoutInfo GlobalInfo { get; internal set; }
    protected internal LayoutInfo LocalInfo { get; internal set; }

    /// <summary>
    /// The coordinate of this paragraph's content (ie: after applying margin)
    /// </summary>
    /// <remarks>
    /// This property is calculated and assigned by the TextDocument
    /// </remarks>
    protected internal PointF DrawingContentPosition { get; internal set; }
    
    /// <summary>
    /// The margin
    /// </summary>
    public Thickness Margin { get; internal set; }

    // Explicit implementation of IRun so we can use RunExtensions
    // with the paragraphs collection.
    int IRun.Offset => GlobalInfo.CodePointIndex;
    int IRun.Length => CodePointLength;

    public abstract void DeletePartial(UndoManager<Document, DocumentViewUpdateInfo> UndoManager, SubRunInfo range);
    public virtual bool CanJoinWith(Paragraph other) { return false; }
    public virtual bool TryJoin(UndoManager<Document, DocumentViewUpdateInfo> UndoManager, int thisIndex) { return false; }
    public abstract Paragraph Split(UndoManager<Document, DocumentViewUpdateInfo> UndoManager, int splitIndex);
    public abstract void GetTextByAppendTextToBuffer(Utf32Buffer buffer, int position, int length);
    public Utf32Buffer GetText(int position, int length)
    {
        var buf = new Utf32Buffer();
        GetTextByAppendTextToBuffer(buf, position, length);
        return buf;
    }
    /// <summary>
    /// Get the style of the text at a specified code point index
    /// </summary>
    /// <param name="position">The offset of the code point</param>
    /// <returns>An IStyle</returns>
    public abstract IStyle GetStyleAtPosition(CaretPosition position);
    public abstract IReadOnlyList<StyleRunEx> GetStyles(int position, int length);
    public abstract void ApplyStyle(IStyle style, int position, int length);
    protected internal Document? Owner { get; private set; }
    protected internal virtual void OnParagraphAdded(Document owner) { Owner = owner; }
    protected internal virtual void OnParagraphRemoved(Document owner) { Owner = null; }
}
public record struct StyleRunEx(int Start, int Length, IStyle Style) : IRun
{
    public StyleRunEx(StyleRun styleRun) : this(styleRun.Start, styleRun.Length, styleRun.Style)
    {

    }
    int IRun.Offset => Start;
}