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
using Get.RichTextKit.Editor.UndoUnits;
using SkiaSharp;
using System.Drawing;
using Get.RichTextKit.Utils;
using Get.RichTextKit.Styles;
using Get.RichTextKit.Editor.DocumentView;
using System.Diagnostics;

namespace Get.RichTextKit.Editor.Paragraphs;

/// <summary>
/// Implements a text paragraph
/// </summary>
[DebuggerDisplay("{TextBlock}")]
public partial class TextParagraph : Paragraph, ITextParagraph, IAlignableParagraph
{
    const int NumberLineOffset = 60;
    const int NumberRightMargin = 10;
    public override IStyle StartStyle => _textBlock.GetStyleAtOffset(0);
    public override IStyle EndStyle => _textBlock.GetStyleAtOffset(_textBlock.Length);
    /// <summary>
    /// Constructs a new TextParagraph
    /// </summary>
    public TextParagraph(IStyle style)
    {
        _textBlock = new TextBlock();
        _textBlock.AddText(Document.NewParagraphSeparator.ToString(), style);
    }
    private TextParagraph(TextBlock tb)
    {
        _textBlock = tb;
    }

    // Create a new textblock by copying the content of another
    public TextParagraph(TextParagraph source, int from, int length)
    {
        // Copy the text block
        _textBlock = source.TextBlock.Copy(from, length);

        // Copy styles
        SetStyleContinuingFrom(source);
    }
    bool LineNumberMode = false;

    /// <inheritdoc />
    protected override void LayoutOverride(LayoutParentInfo owner)
    {
        LineNumberMode = owner.LineNumberMode;
        _textBlock.RenderWidth = owner.AvaliableWidth - (LineNumberMode ? NumberLineOffset : 0);

        // For layout just need to set the appropriate layout width on the text block
        if (owner.LineWrap)
        {
            _textBlock.MaxWidth = _textBlock.RenderWidth;
        }
        else
            _textBlock.MaxWidth = null;
    }

    /// <inheritdoc />
    public override void Paint(SKCanvas canvas, PaintOptions options)
    {
        _textBlock.Layout();
        _textBlock.Paint(
            canvas,
            new(DrawingContentPosition.X + (LineNumberMode ? NumberLineOffset : 0),
            DrawingContentPosition.Y),
            options.TextPaintOptions
        );
        int lineNumberOffset = 1 + GlobalInfo.DisplayLineIndex;
        if (LineNumberMode)
        {
            var style = new Style() { FontFamily = "Segoe UI", FontSize = 16, TextColor = options.TextPaintOptions.TextDefaultColor };
            var tb = new TextBlock();
            for (int i = 0; i < _textBlock.Lines.Count; i++)
            {
                var line = _textBlock.Lines[i];
                if (DrawingContentPosition.Y + line.YCoord > options.ViewBounds.Bottom)
                    // The next line is probably 
                    break;
                //if (line.)
                tb.AddText((i + lineNumberOffset).ToString(), style);
                var height = line.Height - tb.MeasuredHeight;
                tb.Paint(canvas, new SKPoint(
                    DrawingContentPosition.X + NumberLineOffset - NumberRightMargin - tb.MeasuredWidth,
                    DrawingContentPosition.Y + line.YCoord + height / 2));
                tb.Clear();
            }
        }
    }

    /// <inheritdoc />
    public override CaretInfo GetCaretInfo(CaretPosition position)
    {
        var info = _textBlock.GetCaretInfo(position);
        if (LineNumberMode)
        {
            info.CaretXCoord += NumberLineOffset;
            info.CaretRectangle = new(info.CaretRectangle.Left + NumberLineOffset, info.CaretRectangle.Top, info.CaretRectangle.Right + NumberLineOffset, info.CaretRectangle.Bottom);
        }
        return info;
    }

    public override LineInfo GetLineInfo(int line)
    {
        if (line >= _textBlock.LineIndicies.Count || line < 0) throw new ArgumentOutOfRangeException(nameof(line));
        bool isFirstLine = line is 0;
        bool isLastLine = line + 1 == _textBlock.LineIndicies.Count;
        return new LineInfo(
            Line: line,
            Y: _textBlock.Lines[line].YCoord,
            Height: _textBlock.Lines[line].Height,
            Start: new(_textBlock.LineIndicies[line]),
            End: new(
                isLastLine ? _textBlock.Length - 1 : _textBlock.LineIndicies[line + 1],
                true
            ),
            PrevLine: isFirstLine ? null : line - 1,
            NextLine: isLastLine ? null : line + 1
        );
    }

    /// <inheritdoc />
    public override HitTestResult HitTest(PointF pt) => _textBlock.HitTest(LineNumberMode ? Math.Max(0, pt.X - NumberLineOffset) : pt.X, pt.Y);

    /// <inheritdoc />
    public override HitTestResult HitTestLine(int lineIndex, float x) => _textBlock.HitTestLine(lineIndex, LineNumberMode ? Math.Max(0, x - NumberLineOffset) : x);

    /// <inheritdoc />
    public override int CodePointLength => _textBlock.Length;

    /// <inheritdoc />
    protected override float ContentWidthOverride => _textBlock.MeasuredWidth;

    /// <inheritdoc />
    protected override float ContentHeightOverride => _textBlock.MeasuredHeight;

    /// <inheritdoc />
    public TextBlock TextBlock => _textBlock;

    public TextAlignment Alignment { get => _textBlock.Alignment; set => _textBlock.Alignment = value; }

    public override int LineCount => _textBlock.Lines.Count;
    public override int DisplayLineCount => _textBlock.Lines.Count;

    // Private attributes
    readonly TextBlock _textBlock;


    IStyle _savedParaEndingStyle;
    public override bool TryJoinWithNextParagraph(UndoManager<Document, DocumentViewUpdateInfo> UndoManager)
    {
        if (ParentInfo.Index + 1 >= ParentInfo.Parent.Paragraphs.Count)
            return false;
        if (ParentInfo.Parent.Paragraphs[ParentInfo.Index + 1] is not ITextParagraph)
            return false;
        UndoManager.Do(new UndoJoinTextParagraphs(GlobalParagraphIndex));
        return true;
    }

    public void SetStyleContinuingFrom(Paragraph other)
    {
        Margin = other.Margin;
        if (other is ITextParagraph tp)
        {
            TextBlock.Alignment = tp.TextBlock.Alignment;
            TextBlock.BaseDirection = tp.TextBlock.BaseDirection;
        }
    }
    public override void GetTextByAppendTextToBuffer(Utf32Buffer bufToAdd, int position, int length)
    {
        bufToAdd.Add(TextBlock.CodePoints.SubSlice(position, length));
    }
    public override IStyle GetStyleAtPosition(CaretPosition position)
    {
        return TextBlock.GetStyleAtOffset(position.CodePointIndex);
    }
    public override IReadOnlyList<StyleRunEx> GetStyles(int position, int length)
    {
        if (length > 0)
            return TextBlock.Extract(position, length).StyleRuns.Select(x => new StyleRunEx(x)).ToArray();
        return new StyleRunEx[] { new(position, 0, TextBlock.GetStyleAtOffset(position)) };
    }

    public override void ApplyStyle(IStyle style, int position, int length)
    {
        TextBlock.ApplyStyle(position, length, style);
    }
    public override CaretPosition EndCaretPosition => new(TextBlock.Length - 1, false);
}
