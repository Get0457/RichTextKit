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

using Get.RichTextKit.Editor.Structs;
using Get.EasyCSharp;
using System.Drawing;
using System.ComponentModel;
using Get.RichTextKit.Editor.Paragraphs;
using Get.RichTextKit.Editor.Paragraphs.Panel;

namespace Get.RichTextKit.Editor;

public partial class Document
{
    public DocumentLayout Layout { get; }
}
public partial class DocumentLayout : INotifyPropertyChanged
{
    bool _layoutValid;
    Document Document;

    public event Action? Updating;
    public event Action? Updated;

    [Property(OnChanged = nameof(Invalidate))]
    int _Spacing = 30;

    internal DocumentLayout(Document owner) => Document = owner;
    public void InvalidateAndValid()
    {
        Invalidate();
        EnsureValid();
    }
    /// <summary>
    /// Mark the document as needing layout update
    /// </summary>
    public void Invalidate()
    {
        _layoutValid = false;
    }

    /// <summary>
    /// Update the layout of the document
    /// </summary>
    public void EnsureValid()
    {
        // Already valid?
        if (_layoutValid)
            return;

        Updating?.Invoke();

        _layoutValid = true;

        // Work out the starting code point index and y-coord and starting margin
        float yCoord = 0;
        float prevYMargin = Margin.Top;
        int codePointIndex = 0;
        int lineIndex = 0;
        int displayLineIndex = 0;

        _measuredWidth = 0;

        // Layout paragraphs
        for (int i = 0; i < Document.Paragraphs.Count; i++)
        {
            // Get the paragraph
            var para = Document.Paragraphs[i];

            // Layout
            para.Layout(new ParentInfo(
                PageWidth - 
                Margin.Left -
                Margin.Right,
                LineWrap,
                LineNumberMode: false
            ));

            // Position
            para.GlobalInfo = para.LocalInfo = new(
                ContentPosition: new(Margin.Left + para.Margin.Left, yCoord + Math.Max(para.Margin.Top, prevYMargin)),
                CodePointIndex: codePointIndex,
                LineIndex: lineIndex,
                DisplayLineIndex: displayLineIndex
            );

            RecursiveUpdateChild(para);
            static void RecursiveUpdateChild(Paragraph para)
            {
                if (para is IParagraphPanel panel)
                {
                    foreach (var child in panel.Children)
                    {
                        child.GlobalInfo = child.LocalInfo.OffsetToGlobal(para.GlobalInfo);
                        RecursiveUpdateChild(child);
                    }
                }
            }

            // Width
            var paraWidth = para.ContentWidth + para.Margin.Left + para.Margin.Top;
            if (paraWidth > _measuredWidth)
                _measuredWidth = paraWidth;

            // Update positions
            yCoord = para.GlobalInfo.ContentPosition.Y + para.ContentHeight + _Spacing;
            prevYMargin = para.Margin.Bottom;
            codePointIndex += para.CodePointLength;
            lineIndex += para.LineCount;
            displayLineIndex += para.DisplayLineCount;
        }

        // Update the totals
        _measuredWidth += Margin.Left + Margin.Right;
        _measuredHeight = yCoord + Math.Max(prevYMargin, Margin.Bottom);
        PropertyChanged?.Invoke(this, new(nameof(MeasuredSize)));
        _totalLength = codePointIndex;
        _totalLines = lineIndex;
        _totalDisplayLines = displayLineIndex;

        Updated?.Invoke();
    }
    /// <summary>
    /// Indicates if text should be wrapped
    /// </summary>
    [AutoEventNotifyProperty(OnChanged = nameof(Invalidate))]
    bool _LineWrap;

    /// <summary>
    /// Specifies the page width of the document
    /// </summary>
    [AutoEventNotifyProperty(OnChanged = nameof(Invalidate))]
    float _PageWidth = 1000;

    /// <summary>
    /// The document's margin
    /// </summary>
    [AutoEventNotifyProperty(OnChanged = nameof(OnMarginChanged))]
    Thickness _Margin = new(3);
    void OnMarginChanged()
    {
        Invalidate();
        Document.RequestRedraw();
    }
    float _measuredWidth;
    float _measuredHeight;
    /// <summary>
    /// The total size of the document
    /// </summary>
    public SizeF MeasuredSize
    {
        get
        {
            EnsureValid();
            return new(LineWrap ? PageWidth : _measuredWidth, _measuredHeight);
        }
    }
    int _totalLength;
    int _totalLines;
    int _totalDisplayLines;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the total length of the document in code points
    /// </summary>
    public int Length
    {
        get
        {
            EnsureValid();
            return _totalLength;
        }
    }
    /// <summary>
    /// Gets the total length of the document
    /// </summary>
    public int LineCount
    {
        get
        {
            EnsureValid();
            return _totalLines;
        }
    }
    /// <summary>
    /// Gets the total display line count of the document
    /// </summary>
    public int DisplayLineCount
    {
        get
        {
            EnsureValid();
            return _totalDisplayLines;
        }
    }
}