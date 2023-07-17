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
using Get.RichTextKit.Editor.Paragraphs;
using Get.RichTextKit.Styles;
using Get.RichTextKit.Editor.DocumentView;

namespace Get.RichTextKit.Editor;

/// <summary>
/// Represents a the document part of a Document/View editor
/// </summary>
public partial class Document
{
    public const char NewParagraphSeparator = '\u2029';
    /// <summary>
    /// Constructs a new TextDocument
    /// </summary>
    public Document(IStyle DefaultStyle)
    {
        // Create paragraph list
        UndoManager = new(this);
        Paragraphs = new(this);
        Editor = new(this);
        Layout = new(this);

        // Create our undo manager
        UndoManager = new UndoManager<Document, DocumentViewUpdateInfo>(this);
        UndoManager.EndOperation += FireDocumentChanged;

        // Temporary... add some text to work with
        Paragraphs.Add(new TextParagraph(DefaultStyle));
        
    }

    /// <summary>
    /// Gets the default alignment for paragraphs in this document
    /// </summary>
    public TextAlignment DefaultAlignment
    {
        get => _defaultAlignment;
        set
        {
            _defaultAlignment = value;
        }
    }
    /// <summary>
    /// Get the style of the text at a specified code point index
    /// </summary>
    /// <param name="position">The offset of the code point</param>
    /// <returns>An IStyle</returns>
    public IStyle GetStyleAtPosition(CaretPosition position)
    {
        return
            Paragraphs
            .FromCodePointIndex(position, out var paraCodePointIndex)
            .GetStyleAtPosition(new(paraCodePointIndex));
    }

    

    /// <summary>
    /// Get the undo manager for this document
    /// </summary>
    public UndoManager<Document, DocumentViewUpdateInfo> UndoManager { get; }

    /// <summary>
    /// Get the text for a part of the document
    /// </summary>
    /// <param name="range">The text to retrieve</param>
    /// <returns>The text as a Utf32Buffer</returns>
    public Utf32Buffer GetText(TextRange range)
    {
        // Normalize and clamp range
        range = range.Normalized.Clamp(Layout.Length - 1);

        // Get all subruns
        var buf = new Utf32Buffer();
        foreach (var subrun in Paragraphs.GetInterectingRuns(range.Start, range.Length))
        {
            // Get the paragraph
            var para = Paragraphs[subrun.Index];
            
            // Add the text
            para.GetTextByAppendTextToBuffer(buf, subrun.Offset, subrun.Length);
        }

        // Done!
        return buf;
    }
    TextAlignment _defaultAlignment = TextAlignment.Left;

    public event TextChangedEventHandler? TextChanged;
    internal void InvokeTextChanged(TextRange modifiedRange) => TextChanged?.Invoke(modifiedRange);
}
public delegate void TextChangedEventHandler(TextRange modifiedRange);