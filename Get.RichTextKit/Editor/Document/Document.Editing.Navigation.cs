﻿// This file has been edited and modified from its original version.
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

using Get.RichTextKit.Editor.Structs;
using Get.RichTextKit.Editor.UndoUnits;
using Get.EasyCSharp;
using SkiaSharp;
using System.Diagnostics;
using Get.RichTextKit;
using Get.RichTextKit.Utils;
using System.Collections.ObjectModel;
using Get.RichTextKit.Editor.Paragraphs;
using Get.RichTextKit.Editor;
using Paragraph = Get.RichTextKit.Editor.Paragraphs.Paragraph;

namespace Get.RichTextKit.Editor;

public partial class DocumentEditor
{
    DocumentParagraphs Paragraphs => Document.Paragraphs;
    int Length => Document.Layout.Length;
    Thickness Margin => Document.Layout.Margin;

    /// <summary>
    /// Handles keyboard navigation events
    /// </summary>
    /// <param name="position">The current caret position</param>
    /// <param name="kind">The direction and type of caret movement</param>
    /// <param name="pageSize">Specifies the page size for page up/down navigation</param>
    /// <param name="ghostXCoord">Transient storage for XCoord of caret during vertical navigation</param>
    /// <returns>The new caret position</returns>
    public CaretPosition Navigate(CaretPosition position, NavigationKind kind, float pageSize, ref float? ghostXCoord)
    {
        switch (kind)
        {
            case NavigationKind.None:
                ghostXCoord = null;
                return position;

            case NavigationKind.CharacterLeft:
                ghostXCoord = null;
                return navigateIndicies(-1, p => p.CaretIndicies);

            case NavigationKind.CharacterRight:
                ghostXCoord = null;
                return navigateIndicies(1, p => p.CaretIndicies);

            case NavigationKind.LineUp:
                return navigateLine(-1, ref ghostXCoord);

            case NavigationKind.LineDown:
                return navigateLine(1, ref ghostXCoord);

            case NavigationKind.WordLeft:
                ghostXCoord = null;
                return navigateIndicies(-1, p => p.WordBoundaryIndicies);

            case NavigationKind.WordRight:
                ghostXCoord = null;
                return navigateIndicies(1, p => p.WordBoundaryIndicies);

            case NavigationKind.PageUp:
                return navigatePage(-1, ref ghostXCoord);

            case NavigationKind.PageDown:
                return navigatePage(1, ref ghostXCoord);

            case NavigationKind.LineHome:
                ghostXCoord = null;
                return navigateLineEnd(-1);

            case NavigationKind.LineEnd:
                ghostXCoord = null;
                return navigateLineEnd(1);

            case NavigationKind.DocumentHome:
                ghostXCoord = null;
                return new CaretPosition(0);

            case NavigationKind.DocumentEnd:
                ghostXCoord = null;
                return new CaretPosition(Document.Layout.Length, true);

            default:
                throw new ArgumentException("Unknown navigation kind");
        }

        // Helper for character/word left/right
        CaretPosition navigateIndicies(int direction, Func<Paragraph, IReadOnlyList<int>> getIndicies)
        {
            // Get the paragraph and position in paragraph
            var para = Paragraphs.GlobalFromCodePointIndex(position, out var parent, out int paraIndex, out var paraCodePointIndex);
            
            // Find the current caret index
            var indicies = getIndicies(para);
            var ii = indicies.BinarySearch(paraCodePointIndex);

            // Work out the new position
            if (ii < 0)
            {
                ii = (~ii);
                if (direction > 0)
                    ii--;
            }
            ii += direction;


            if (ii < 0)
            {
                // Move to end of previous paragraph
                if (paraIndex > 0)
                    return new CaretPosition(parent.Paragraphs[paraIndex - 1].GlobalInfo.CodePointIndex + parent.Paragraphs[paraIndex - 1].Length - 1);
                else
                    // Move to the position before this paragraph or the start of the document if there's nothing before
                    return new CaretPosition(Math.Max(0, parent.Paragraphs[paraIndex].GlobalInfo.CodePointIndex - 1));
            }

            if (ii >= indicies.Count)
            {
                // Move to start of next paragraph
                if (paraIndex + 1 < parent.Paragraphs.Count)
                    return new CaretPosition(parent.Paragraphs[paraIndex + 1].GlobalInfo.CodePointIndex);
                else
                    // Move to the position after this paragraph or the end of the document if there's no more
                    return new CaretPosition(
                        Math.Min(parent.Paragraphs[paraIndex].GlobalInfo.CodePointIndex + parent.Paragraphs[paraIndex + 1].Length,
                        Length - 1
                    ));
            }

            // Move to new position in this paragraph
            var pos = para.GlobalInfo.CodePointIndex + indicies[ii];
            if (pos >= Length)
                pos = Length - 1;
            return new CaretPosition(pos);
        }

        // Helper for line up/down
        CaretPosition navigateLine(int direction, ref float? xCoord)
        {
            // Get the paragraph and position in paragraph
            int paraIndex = Paragraphs.FromCodePointIndexAsIndex(position, out var paraCodePointIndex);
            var para = Paragraphs[paraIndex];

            // Get the line number the caret is on
            var ci = para.GetCaretInfo(new CaretPosition(paraCodePointIndex, position.AltPosition));

            // Resolve the xcoord
            xCoord ??= ci.CaretXCoord + Margin.Left + para.Margin.Left;

            // Work out which line to hit test
            var lineInfo = para.GetLineInfo(ci.LineIndex);
            var toLine = direction > 0 ? lineInfo.NextLine : lineInfo.PrevLine;
            
            // Exceed paragraph?
            if (toLine is null)
            {
                if (direction < 0)
                {
                    // Top of document?
                    if (paraIndex == 0)
                        return position;

                    // Move to last line of previous paragraph
                    para = Paragraphs[paraIndex - 1];
                    toLine = para.GetLineInfo(^1).Line;
                } else
                {
                    // End of document?
                    if (paraIndex + 1 >= Paragraphs.Count)
                        return position;

                    // Move to first line of next paragraph
                    para = Paragraphs[paraIndex + 1];
                    toLine = para.GetLineInfo(0).Line;
                }
            }
            

            // Hit test the line
            var htr = para.HitTestLine(toLine.Value, xCoord.Value - Margin.Left - para.Margin.Left);
            return new CaretPosition(para.GlobalInfo.CodePointIndex + htr.ClosestCodePointIndex, htr.AltCaretPosition);
        }

        // Helper for line home/end
        CaretPosition navigateLineEnd(int direction)
        {
            // Get the paragraph and position in paragraph
            int paraIndex = Paragraphs.FromCodePointIndexAsIndex(position, out var paraCodePointIndex);
            var para = Paragraphs[paraIndex];

            // Get the line number the caret is on
            var ci = para.GetCaretInfo(new CaretPosition(paraCodePointIndex, position.AltPosition));

            // Handle out of range
            if (ci.LineIndex < 0)
                return new CaretPosition(para.GlobalInfo.CodePointIndex);


            if (direction < 0)
            {
                var info = para.GetLineInfo(ci.LineIndex);
                // Return code point index of this line
                return info.End;
            }
            else
            {
                // Last unwrapped line?
                if (ci.LineIndex + 1 >= para.LineCount)
                    return new CaretPosition(para.GlobalInfo.CodePointIndex + para.Length - 1);

                // Return code point index of the next line, but with alternate caret position
                // so caret appears at the end of this line
                var info = para.GetLineInfo(ci.LineIndex + 1);
                return info.End;
            }
        }

        // Helper for page up/down
        CaretPosition navigatePage(int direction, ref float? xCoord)
        {
            // Get current caret position
            var ci = this.GetCaretInfo(position);

            // Work out which XCoord to use
            if (xCoord == null)
                xCoord = ci.CaretXCoord;

            // Hit test one page up
            var htr = HitTest(new(xCoord.Value, ci.CaretRectangle.MidY + pageSize * direction));

            // Convert to caret position
            return htr.CaretPosition;
        }

    }
}