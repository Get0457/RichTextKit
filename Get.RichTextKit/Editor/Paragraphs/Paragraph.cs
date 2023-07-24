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
    /// <summary>
    /// Constructs a new Paragraph
    /// </summary>
    protected Paragraph() { }
    public abstract IStyle StartStyle { get; }
    public abstract IStyle EndStyle { get; }
    
    /// <inheritdoc cref="Layout(LayoutParentInfo)"/>
    protected abstract void LayoutOverride(LayoutParentInfo owner);
    /// <summary>
    /// Layout the content of this paragraph
    /// </summary>
    /// <param name="owner">The TextDocument that owns this paragraph</param>
    public void Layout(LayoutParentInfo owner)
        => LayoutOverride(owner with { AvaliableWidth = owner.AvaliableWidth - Margin.Left - Margin.Right });

    public virtual CaretPosition StartCaretPosition => new(0, altPosition: false);
    public virtual CaretPosition EndCaretPosition => new(Math.Max(0, CodePointLength - 2), altPosition: false);
    public record struct PaintOptions(RectangleF ViewBounds, TextPaintOptions TextPaintOptions, IDocumentViewOwner? viewOwner);
    /// <summary>
    /// Paint this paragraph
    /// </summary>
    /// <param name="canvas">The canvas to paint to</param>
    /// <param name="options">Paint options</param>
    public abstract void Paint(SKCanvas canvas, PaintOptions options);

    // So protected members can access the method
    protected static IEnumerable<SubRunInfo> GetInteractingRuns(Paragraph para, TextRange selection)
        => para.GetInteractingRuns(selection);
    protected virtual IEnumerable<SubRunInfo> GetInteractingRuns(TextRange selection)
    {
        yield return new(
            ParentInfo, selection.Minimum, Math.Abs(selection.Length),
            !(selection.Minimum <= 0 && selection.Maximum >= CodePointLength)
        );
    }
    protected static IEnumerable<SubRunInfo> GetInteractingRunsRecursive(Paragraph para, TextRange selection)
        => para.GetInteractingRunsRecursive(selection);
    protected virtual IEnumerable<SubRunInfo> GetInteractingRunsRecursive(TextRange selection)
        => GetInteractingRuns(selection);
    public IEnumerable<SubRunBFSInfo> GetBFSInteractingRuns(TextRange selection)
    {
        foreach (var subRun in GetInteractingRuns(selection))
        {
            // We can do this because IEnumerable is lazy so the function will actually not evaluate here
            var childEnumerable =
                subRun.Paragraph == this ? Enumerable.Empty<SubRunBFSInfo>() :
                subRun.Paragraph.GetBFSInteractingRuns(
                    new(subRun.Offset, subRun.Offset + subRun.Length)
                );
            if (subRun.Partial)
            {
                foreach (var sr2 in childEnumerable)
                    yield return sr2;
            }
            else
            {
                yield return new SubRunBFSInfo(subRun, childEnumerable);
            }
        }
    }

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