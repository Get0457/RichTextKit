using Get.RichTextKit.Editor.Structs;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Get.RichTextKit.Utils;
using Get.RichTextKit;
using HarfBuzzSharp;
using System.Collections;

namespace Get.RichTextKit.Editor.Paragraphs.Panel;
public abstract partial class PanelParagraph : Paragraph, IParagraphPanel
{
    public Paragraph GlobalChildrenFromCodePointIndex(CaretPosition position, out IParagraphPanel parent, out int paragraphIndex, out int codePointindexInParagraph)
    {
        Paragraph obj = this;
        // to stop C# compiler from screaming that out var may not be assigned
        parent = this;
        paragraphIndex = default;
        codePointindexInParagraph = position.CodePointIndex;
        while (obj is IParagraphPanel panel)
        {
            parent = panel;
            paragraphIndex = LocalChildrenFromCodePointIndexAsIndex(new ReadOnlyListWrapper<Paragraph>(panel.Children), position, out codePointindexInParagraph);
            position.CodePointIndex = codePointindexInParagraph;
            obj = panel.Children[paragraphIndex];
        }
        return obj;
    }
    /// <summary>
    /// Given a code point index relative to the document, return which
    /// paragraph contains that code point and the offset within the paragraph
    /// </summary>
    /// <param name="position">The caret position to locate the paragraph for</param>
    /// <param name="indexInParagraph">Out parameter returning the code point index into the paragraph</param>
    /// <returns>The paragraph</returns>
    public Paragraph LocalChildrenFromCodePointIndex(CaretPosition position, out int indexInParagraph)
    {
        var a = LocalChildrenFromCodePointIndexAsIndex(position, out indexInParagraph);
        return Children[a];
    }
    /// <summary>
    /// Given a code point index relative to the document, return which
    /// paragraph contains that code point and the offset within the paragraph
    /// </summary>
    /// <param name="position">The caret position to locate the paragraph for</param>
    /// <param name="indexInParagraph">Out parameter returning the code point index into the paragraph</param>
    /// <returns>The index of the paragraph</returns>
    public int LocalChildrenFromCodePointIndexAsIndex(CaretPosition position, out int indexInParagraph)
        => LocalChildrenFromCodePointIndexAsIndex(Children, position, out indexInParagraph);
    public static int LocalChildrenFromCodePointIndexAsIndex(IReadOnlyList<Paragraph> Children, CaretPosition position, out int indexInParagraph)
    {
        if (position.CodePointIndex < 0) position.CodePointIndex = 0;

        // Search paragraphs
        int paraIndex = Children.BinarySearch(position.CodePointIndex, (para, a) =>
        {
            if (a < para.LocalInfo.CodePointIndex)
                return 1;
            if (a >= para.LocalInfo.CodePointIndex + para.CodePointLength)
                return -1;
            return 0;
        });
        if (paraIndex < 0)
            paraIndex = ~paraIndex;

        // Clamp to end of document
        if (paraIndex >= Children.Count)
            paraIndex = Children.Count - 1;

        // Work out offset within paragraph
        indexInParagraph = position.CodePointIndex - Children[paraIndex].LocalInfo.CodePointIndex;

        if (indexInParagraph == 0 && position.AltPosition && paraIndex > 0)
        {
            paraIndex--;
            indexInParagraph = Children[paraIndex].CodePointLength;
        }

        // Clamp to end of paragraph
        if (indexInParagraph > Children[paraIndex].CodePointLength)
            indexInParagraph = Children[paraIndex].CodePointLength;

        // Done
        return paraIndex;
    }
    /// <summary>
    /// Given a code point index relative to the document, return which
    /// paragraph contains that code point and the offset within the paragraph
    /// </summary>
    /// <param name="line">The caret position to locate the paragraph for</param>
    /// <param name="lineIndexInParagraph">Out parameter returning the code point index into the paragraph</param>
    /// <returns>The paragraph</returns>
    public Paragraph LocalChildrenFromLineIndex(int line, out int lineIndexInParagraph)
    {
        var a = LocalChildrenFromLineIndexAsIndex(line, out lineIndexInParagraph);
        return Children[a];
    }
    public Paragraph GlobalChildrenFromCodePointIndex(CaretPosition position, out IParagraphPanel[] parents, out int codePointindexInParagraph)
    {
        var paragraphIndex = LocalChildrenFromCodePointIndexAsIndex(position, out codePointindexInParagraph);
        position.CodePointIndex = codePointindexInParagraph;
        Paragraph obj = Children[paragraphIndex];
        int codePointindexInParagraph2 = default;
        IEnumerable<IParagraphPanel> Iterate()
        {
            while (obj is IParagraphPanel panel)
            {
                yield return panel;
                paragraphIndex = LocalChildrenFromCodePointIndexAsIndex(new ReadOnlyListWrapper<Paragraph>(panel.Children), position, out codePointindexInParagraph2);
                position.CodePointIndex = codePointindexInParagraph2;
                obj = panel.Children[paragraphIndex];
            }
        }
        codePointindexInParagraph = codePointindexInParagraph2;
        parents = Iterate().ToArray();
        return obj;
    }
    /// <summary>
    /// Given a code point index relative to the document, return which
    /// paragraph contains that code point and the offset within the paragraph
    /// </summary>
    /// <param name="line">The caret position to locate the paragraph for</param>
    /// <param name="lineIndexInParagraph">Out parameter returning the code point index into the paragraph</param>
    /// <returns>The index of the paragraph</returns>
    public int LocalChildrenFromLineIndexAsIndex(int line, out int lineIndexInParagraph)
    {
        if (line < 0) line = 0;

        // Search paragraphs
        int paraIndex = Children.BinarySearch(line, (para, a) =>
        {
            if (a < para.LocalInfo.LineIndex)
                return 1;
            if (a >= para.LocalInfo.LineIndex + para.LineCount)
                return -1;
            return 0;
        });
        if (paraIndex < 0)
            paraIndex = ~paraIndex;

        // Clamp to end of document
        if (paraIndex >= Children.Count)
            paraIndex = Children.Count - 1;

        // Work out offset within paragraph
        lineIndexInParagraph = line - Children[paraIndex].LocalInfo.LineIndex;

        // Clamp to end of paragraph
        if (lineIndexInParagraph > Children[paraIndex].CodePointLength)
            lineIndexInParagraph = Children[paraIndex].CodePointLength;

        // Done
        return paraIndex;
    }
    /// <summary>
    /// Helper to find the closest paragraph to a y-coordinate 
    /// </summary>
    /// <param name="y">Y-Coord to hit test</param>
    /// <returns>A reference to the closest paragraph</returns>
    protected Paragraph FindClosestY(float y)
    {
        // Search paragraphs
        int paraIndex = Children.BinarySearch(y, (para, a) =>
        {
            if (para.LocalInfo.ContentPosition.Y > a)
                return 1;
            if (para.LocalInfo.ContentPosition.Y + para.ContentHeight < a)
                return -1;
            return 0;
        });

        // If in the vertical margin space between paragraphs, find the 
        // paragraph whose content is closest
        if (paraIndex < 0)
        {
            // Convert the paragraph index
            paraIndex = ~paraIndex;

            // Is it between paragraphs? 
            // (ie: not above the first or below the last paragraph)
            if (paraIndex > 0 && paraIndex < Children.Count)
            {
                // Yes, find which paragraph's content the position is closer too
                var paraPrev = Children[paraIndex - 1];
                var paraNext = Children[paraIndex];
                if (Math.Abs(y - (paraPrev.LocalInfo.ContentPosition.Y + paraPrev.ContentHeight)) <
                    Math.Abs(y - paraNext.LocalInfo.ContentPosition.Y))
                {
                    return paraPrev;
                }
                else
                {
                    return paraNext;
                }
            }
        }

        // Clamp to last paragraph
        if (paraIndex >= Children.Count)
            paraIndex = Children.Count - 1;

        // Return the paragraph
        return Children[paraIndex];
    }
    /// <summary>
    /// Helper to find the closest paragraph to a x-coordinate 
    /// </summary>
    /// <param name="x">X-Coord to hit test</param>
    /// <returns>A reference to the closest paragraph</returns>
    protected Paragraph FindClosestX(float x)
    {
        // Search paragraphs
        int paraIndex = Children.BinarySearch(x, (para, a) =>
        {
            if (para.LocalInfo.ContentPosition.X > a)
                return 1;
            if (para.LocalInfo.ContentPosition.X + para.ContentWidth < a)
                return -1;
            return 0;
        });

        // If in the vertical margin space between paragraphs, find the 
        // paragraph whose content is closest
        if (paraIndex < 0)
        {
            // Convert the paragraph index
            paraIndex = ~paraIndex;

            // Is it between paragraphs? 
            // (ie: not above the first or below the last paragraph)
            if (paraIndex > 0 && paraIndex < Children.Count)
            {
                // Yes, find which paragraph's content the position is closer too
                var paraPrev = Children[paraIndex - 1];
                var paraNext = Children[paraIndex];
                if (Math.Abs(x - (paraPrev.LocalInfo.ContentPosition.X + paraPrev.ContentWidth)) <
                    Math.Abs(x - paraNext.LocalInfo.ContentPosition.X))
                {
                    return paraPrev;
                }
                else
                {
                    return paraNext;
                }
            }
        }

        // Clamp to last paragraph
        if (paraIndex >= Children.Count)
            paraIndex = Children.Count - 1;

        // Return the paragraph
        return Children[paraIndex];
    }
    protected static PointF OffsetMargin(PointF pt, Thickness margin)
    {
        return new(pt.X + margin.Left, pt.Y + margin.Top);
    }
}