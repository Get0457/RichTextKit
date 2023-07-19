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
using Get.RichTextKit.Editor.Structs;

namespace Get.RichTextKit.Editor;

public partial class Document
{
    internal readonly VerticalParagraph rootParagraph;
    public DocumentParagraphs Paragraphs { get; }
}
public class DocumentParagraphs : IParagraphCollection
{
    readonly Document Document;

    public Paragraph this[int index] => Document.rootParagraph.Children[index];
    public int Count => Document.rootParagraph.Children.Count;
    public void Add(Paragraph paragraph) => Document.rootParagraph.Children.Add(paragraph);
    bool IParagraphCollection.IsChildrenReadOnly => false;

    public IList<Paragraph> Paragraphs => Document.rootParagraph.Children;

    internal DocumentParagraphs(Document owner)
    {
        Document = owner;
    }
    /// <inheritdoc cref="PanelParagraph.LocalChildrenFromCodePointIndex(CaretPosition, out int)"/>
    internal Paragraph LocalChildrenFromCodePointIndex(CaretPosition position, out int indexInParagraph)
        => Document.rootParagraph.LocalChildrenFromCodePointIndex(position, out indexInParagraph);

    /// <inheritdoc cref="PanelParagraph.LocalChildrenFromCodePointIndexAsIndex(CaretPosition, out int)"/>
    internal int LocalChildrenFromCodePointIndexAsIndex(CaretPosition position, out int indexInParagraph)
        => Document.rootParagraph.LocalChildrenFromCodePointIndexAsIndex(position, out indexInParagraph);
    public Paragraph GlobalChildrenFromCodePointIndex(CaretPosition position, out IParagraphPanel parent, out int paragraphIndex, out int codePointindexInParagraph)
        => Document.rootParagraph.GlobalChildrenFromCodePointIndex(position, out parent, out paragraphIndex, out codePointindexInParagraph);
    public Paragraph GlobalChildrenFromCodePointIndex(CaretPosition position, out IParagraphPanel[] parents, out int codePointindexInParagraph)
        => Document.rootParagraph.GlobalChildrenFromCodePointIndex(position, out parents, out codePointindexInParagraph);

    /// <summary>
    /// Helper to find the closest paragraph to a y-coordinate 
    /// </summary>
    /// <param name="y">Y-Coord to hit test</param>
    /// <returns>A reference to the closest paragraph</returns>
    internal Paragraph GetParagraphAt(float y)
        => Document.rootParagraph.GetParagraphAt(new(0, y));
    public IEnumerable<SubRunInfo> GetInteractingRuns(TextRange range)
        => Document.Editor.GetSelectionInfo(range).InteractingRuns;
    public IEnumerable<SubRunInfo> GetInteractingRunsRecursive(TextRange range)
        => Document.Editor.GetSelectionInfo(range).RecursiveInteractingRuns;
    public IEnumerable<SubRunBFSInfo> GetBFSInteractingRunsRecursive(TextRange range)
        => Document.Editor.GetSelectionInfo(range).RecursiveBFSInteractingRuns;
    //public IEnumerable<SubRun> GetInterectingRuns(TextRange range)
    //    => from x in this.GetInterectingRuns(range.Start, range.Length) where x.Length >= 0 select x;
    public IEnumerable<Paragraph> GetInteractingParagraphs(TextRange range)
        => from x in GetInteractingParagraphIndices(range) select this[x];
    public IEnumerable<int> GetInteractingParagraphIndices(TextRange range)
        => from x in GetInteractingRuns(range) select x.Index;
    public IEnumerable<StyleRunEx> GetInteractingStlyeRuns(TextRange range)
    {
        foreach (var subrun in GetInteractingRunsRecursive(range))
        {
            if (subrun.Length < 0) continue;
            // Get the paragraph
            var para = subrun.Paragraph;

            foreach (var styleRun in para.GetStyles(subrun.Offset, subrun.Length))
            {
                yield return styleRun;
            }
        }
    }
    public IEnumerable<SubRunInfo> GetIntersectingRunsRecursiveReverse(int offset, int length, bool stopOnFullSelection)
        => GetIntersectingRunsRecursiveReverse(this, offset, length, stopOnFullSelection);
    static IEnumerable<SubRunInfo> GetIntersectingRunsRecursiveReverse(IParagraphCollection parent, int offset, int length, bool stopOnFullSelection)
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
            if (r.LocalInfo.CodePointIndex + r.CodePointLength < a)
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
            if (para.LocalInfo.CodePointIndex + para.CodePointLength <= offset)
                break;
            var srOffset = para.LocalInfo.CodePointIndex > offset ? 0 : offset - para.LocalInfo.CodePointIndex;
            var srLength = Math.Min(para.LocalInfo.CodePointIndex + para.CodePointLength, to) - para.LocalInfo.CodePointIndex - srOffset;
            

            if (para is IParagraphPanel panel && !(stopOnFullSelection && srOffset is 0 && srLength >= para.CodePointLength))
            {
                foreach (var subrun in GetIntersectingRunsRecursiveReverse(panel, srOffset, srLength, stopOnFullSelection))
                {
                    yield return subrun;
                }
            }
            else
            {
                yield return new SubRunInfo(
                    ParentInfo: new(parent, i),
                    Offset: srOffset,
                    Length: srLength,
                    Partial: para.CodePointLength != srLength
                );
            }
        }
    }
}
public readonly record struct SubRunInfo(ParentInfo ParentInfo, int Offset, int Length, bool Partial)
{
    public Paragraph Paragraph => Parent.Paragraphs[Index];
    public IParagraphCollection Parent => ParentInfo.Parent;
    public int Index => ParentInfo.Index;
}
public readonly record struct SubRunBFSInfo(SubRunInfo SubRunInfo, IEnumerable<SubRunBFSInfo> NextLevelInfo)
{

}