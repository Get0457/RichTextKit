using Get.RichTextKit.Editor.Paragraphs.Panel;
using Get.RichTextKit.Editor.Structs;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Get.RichTextKit.Editor.Paragraphs;

partial class Paragraph
{

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

    public virtual SelectionInfo GetSelectionInfo(TextRange selection) => new(
        selection, null, GetCaretInfo(selection.StartCaretPosition), GetCaretInfo(selection.EndCaretPosition),
        this,
        GetInteractingRuns(selection),
        GetInteractingRunsRecursive(selection),
        GetBFSInteractingRuns(selection)
    );
    public abstract TextRange GetSelectionRange(CaretPosition position, ParagraphSelectionKind kind);
}
