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
using Get.RichTextKit.Editor;
using Get.RichTextKit.Utils;
using System;

namespace Get.RichTextKit.Editor;

public partial class Document
{
    /// <summary>
    /// Given a caret position, find an enclosing selection range for the
    /// current word, line, paragraph or document
    /// </summary>
    /// <param name="position">The caret position to select from</param>
    /// <param name="kind">The kind of selection to return</param>
    /// <returns></returns>
    public TextRange GetSelectionRange(CaretPosition position, SelectionKind kind)
    {
        switch (kind)
        {
            case SelectionKind.None:
                return new TextRange(position.CodePointIndex, position.CodePointIndex, position.AltPosition);

            case SelectionKind.Word:
                return getWordRange();

            case SelectionKind.Line:
                return getLineRange();

            case SelectionKind.Paragraph:
                return getParagraphRange();

            case SelectionKind.Document:
                return new TextRange(0, Layout.Length, true);

            default:
                throw new ArgumentException("Unknown navigation kind");
        }

        // Helper to get a word range
        TextRange getWordRange()
        {
            // Get the paragraph and position in paragraph
            var para = Paragraphs.GlobalFromCodePointIndex(position, out _, out _, out var paraCodePointIndex);
            
            // Find the word boundaries for this paragraph and find 
            // the current word
            var indicies = para.WordBoundaryIndicies;
            var ii = indicies.BinarySearch(paraCodePointIndex);
            if (ii < 0)
                ii = (~ii - 1);
            if (ii >= indicies.Count)
                ii = indicies.Count - 1;

            if (ii + 1 >= indicies.Count)
            {
                // Point is past end of paragraph
                return new TextRange(
                    para.GlobalInfo.CodePointIndex + indicies[ii],
                    para.GlobalInfo.CodePointIndex + indicies[ii],
                    true
                );
            }

            // Create text range covering the entire word
            return new TextRange(
                para.GlobalInfo.CodePointIndex + indicies[ii],
                para.GlobalInfo.CodePointIndex + indicies[ii + 1],
                true
            );
        }

        // Helper to get a line range
        TextRange getLineRange()
        {
            // Get the paragraph and position in paragraph
            var para = Paragraphs.FromCodePointIndex(position, out var paraCodePointIndex);

            // Get the line number the caret is on
            var ci = para.GetCaretInfo(new CaretPosition(paraCodePointIndex, position.AltPosition));

            // Handle out of range (should never happen)
            if (ci.LineIndex < 0)
                ci.LineIndex = 0;
            if (ci.LineIndex >= para.LineCount)
                ci.LineIndex = para.LineCount - 1;

            var lineInfo = para.GetLineInfo(ci.LineIndex);

            // Return the line range
            return new TextRange(lineInfo.Start.CodePointIndex, lineInfo.End.CodePointIndex, lineInfo.End.AltPosition);
        }

        // Helper to get a paragraph range
        TextRange getParagraphRange()
        {
            // Get the paragraph and position in paragraph
            var para = Paragraphs.FromCodePointIndex(position, out var paraCodePointIndex);

            // Create text range covering the entire paragraph
            return new TextRange(
                para.GlobalInfo.CodePointIndex,
                para.GlobalInfo.CodePointIndex + para.Length - 1,
                true
            );
        }
    }
}
