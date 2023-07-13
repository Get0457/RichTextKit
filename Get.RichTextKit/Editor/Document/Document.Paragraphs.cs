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

using Get.RichTextKit;
using Get.RichTextKit.Utils;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Get.RichTextKit.Editor.Paragraphs;
using Get.RichTextKit.Editor.Paragraphs.Panel;
using System.Diagnostics;
using Get.RichTextKit.Styles;

namespace Get.RichTextKit.Editor;

public partial class Document
{
    public DocumentParagraphs Paragraphs { get; }
}
public class DocumentParagraphs : ObservableCollection<Paragraph>, IParagraphCollection
{
    readonly Document Document;

    IList<Paragraph> IParagraphCollection.Paragraphs => this;
    bool initialized;
    internal DocumentParagraphs(Document owner)
    {
        Document = owner;
    }
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        Document.Layout.EnsureValid();
        base.OnCollectionChanged(e);
    }
    /// <summary>
    /// Given a code point index relative to the document, return which
    /// paragraph contains that code point and the offset within the paragraph
    /// </summary>
    /// <param name="position">The caret position to locate the paragraph for</param>
    /// <param name="indexInParagraph">Out parameter returning the code point index into the paragraph</param>
    /// <returns>The index of the paragraph</returns>
    internal Paragraph FromCodePointIndex(CaretPosition position, out int indexInParagraph)
    {
        var a = FromCodePointIndexAsIndex(position, out indexInParagraph);
        return this[a];
    }
    /// <summary>
    /// Given a code point index relative to the document, return which
    /// paragraph contains that code point and the offset within the paragraph
    /// </summary>
    /// <param name="position">The caret position to locate the paragraph for</param>
    /// <param name="indexInParagraph">Out parameter returning the code point index into the paragraph</param>
    /// <returns>The index of the paragraph</returns>
    internal int FromCodePointIndexAsIndex(CaretPosition position, out int indexInParagraph)
    {
        if (position.CodePointIndex < 0) position.CodePointIndex = 0;
        // Ensure layout is valid
        Document.Layout.EnsureValid();

        // Search paragraphs
        int paraIndex = this.BinarySearch(position.CodePointIndex, (para, a) =>
        {
            if (a < para.GlobalInfo.CodePointIndex)
                return 1;
            if (a >= para.GlobalInfo.CodePointIndex + para.Length)
                return -1;
            return 0;
        });
        if (paraIndex < 0)
            paraIndex = ~paraIndex;

        // Clamp to end of document
        if (paraIndex >= Count)
            paraIndex = Count - 1;

        // Work out offset within paragraph
        indexInParagraph = position.CodePointIndex - this[paraIndex].GlobalInfo.CodePointIndex;

        if (indexInParagraph == 0 && position.AltPosition && paraIndex > 0)
        {
            paraIndex--;
            indexInParagraph = this[paraIndex].Length;
        }

        // Clamp to end of paragraph
        if (indexInParagraph > this[paraIndex].Length)
            indexInParagraph = this[paraIndex].Length;

       System.Diagnostics.Debug.Assert(indexInParagraph >= 0);

        // Done
        return paraIndex;
    }
    public Paragraph GlobalFromCodePointIndex(CaretPosition position, out IParagraphCollection parent, out int paragraphIndex, out int codePointindexInParagraph)
    {
        paragraphIndex = FromCodePointIndexAsIndex(position, out codePointindexInParagraph);
        position.CodePointIndex = codePointindexInParagraph;
        Paragraph obj = this[paragraphIndex];
        parent = this;
        while (obj is IParagraphPanel panel)
        {
            parent = panel;
            paragraphIndex = PanelParagraph.LocalChildrenFromCodePointIndexAsIndex(new ReadOnlyListWrapper<Paragraph>(panel.Children), position, out codePointindexInParagraph);
            position.CodePointIndex = codePointindexInParagraph;
            obj = panel.Children[paragraphIndex];
        }
        return obj;
    }

    /// <summary>
    /// Helper to find the closest paragraph to a y-coordinate 
    /// </summary>
    /// <param name="y">Y-Coord to hit test</param>
    /// <returns>A reference to the closest paragraph</returns>
    internal Paragraph FindClosest(float y)
    {
        // Ensure layout is valid
        Document.Layout.EnsureValid();

        // Search paragraphs
        int paraIndex = this.BinarySearch(y, (para, a) =>
        {
            if (para.GlobalInfo.ContentPosition.Y > a)
                return 1;
            if (para.GlobalInfo.ContentPosition.Y + para.ContentHeight < a)
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
            if (paraIndex > 0 && paraIndex < Count)
            {
                // Yes, find which paragraph's content the position is closer too
                var paraPrev = this[paraIndex - 1];
                var paraNext = this[paraIndex];
                if (Math.Abs(y - (paraPrev.GlobalInfo.ContentPosition.Y + paraPrev.ContentHeight)) <
                    Math.Abs(y - paraNext.GlobalInfo.ContentPosition.Y))
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
        if (paraIndex >= Count)
            paraIndex = Count - 1;

        // Return the paragraph
        return this[paraIndex];
    }
    public IEnumerable<SubRun> GetInterectingRuns(TextRange range)
        => from x in this.GetInterectingRuns(range.Start, range.Length) where x.Length >= 0 select x;
    public IEnumerable<Paragraph> GetInterectingParagraphs(TextRange range)
        => from x in GetInterectingParagraphIndices(range) select this[x];
    public IEnumerable<int> GetInterectingParagraphIndices(TextRange range)
        => from x in GetInterectingRuns(range) select x.Index;
    public IEnumerable<StyleRunEx> GetInterectingStlyeRuns(TextRange range)
    {
        foreach (var subrun in GetInterectingRuns(range))
        {
            if (subrun.Length < 0) continue;
            // Get the paragraph
            var para = this[subrun.Index];

            foreach (var styleRun in para.GetStyles(subrun.Offset, subrun.Length))
            {
                yield return styleRun;
            }
        }
    }
    public IEnumerable<SubRunRecursiveInfo> GetIntersectingRunsRecursiveReverse(int offset, int length)
        => GetIntersectingRunsRecursiveReverse(this, offset, length);
    static IEnumerable<SubRunRecursiveInfo> GetIntersectingRunsRecursiveReverse(IParagraphCollection parent, int offset, int length)
    {
        // Check list is consistent
        var paragraphs = new ReadOnlyListWrapper<Paragraph>(parent.Paragraphs);

        // Calculate end position
        int to = offset + length;

        // Find the start run
        int endRunIndex = paragraphs.BinarySearch(to, (r, a) =>
        {
            if (r.LocalInfo.CodePointIndex >= a)
                return 1;
            if (r.LocalInfo.CodePointIndex + r.Length < a)
                return -1;
            return 0;
        });
        Debug.Assert(endRunIndex >= 0);
        Debug.Assert(endRunIndex < paragraphs.Count);

        // Iterate over all runs
        for (int i = endRunIndex; i >= 0; i--)
        {
            var para = paragraphs[i];

            // Quit if past requested run
            if (para.LocalInfo.CodePointIndex + para.Length <= offset)
                break;
            var srOffset = para.LocalInfo.CodePointIndex > offset ? 0 : offset - para.LocalInfo.CodePointIndex;
            var srLength = Math.Min(para.LocalInfo.CodePointIndex + para.Length, to) - para.LocalInfo.CodePointIndex - srOffset;
            

            if (para is IParagraphPanel panel)
            {
                foreach (var subrun in GetIntersectingRunsRecursiveReverse(panel, srOffset, srLength))
                {
                    yield return subrun;
                }
            }
            else
            {
                yield return new SubRunRecursiveInfo(
                    Parent: parent,
                    Index: i,
                    Offset: srOffset,
                    Length: srLength,
                    Partial: para.Length != srLength
                );
            }
        }
    }
}
public readonly record struct SubRunRecursiveInfo(IParagraphCollection Parent, int Index, int Offset, int Length, bool Partial)
{
    public Paragraph Paragraph => Parent.Paragraphs[Index];
}