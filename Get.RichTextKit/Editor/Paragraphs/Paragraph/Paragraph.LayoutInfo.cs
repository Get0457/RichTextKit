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
    /// 
    /// </summary>
    /// <param name="ContentPosition">
    /// The coordinate of this paragraph's content
    /// </param>
    /// <param name="CodePointIndex">
    /// This code point index of this paragraph relative to the start
    /// </param>
    /// <param name="LineIndex">
    /// This line index of this paragraph relative to the start
    /// </param>
    /// <param name="DisplayLineIndex"></param>
    public readonly record struct LayoutInfo
    (PointF ContentPosition, int CodePointIndex, int LineIndex, int DisplayLineIndex)
    {
        public LayoutInfo OffsetToGlobal(LayoutInfo parentGlobalInfo)
            => new(
                new(
                    ContentPosition.X + parentGlobalInfo.ContentPosition.X,
                    ContentPosition.Y + parentGlobalInfo.ContentPosition.Y
                ),
                CodePointIndex + parentGlobalInfo.CodePointIndex,
                LineIndex + parentGlobalInfo.LineIndex,
                DisplayLineIndex + parentGlobalInfo.DisplayLineIndex
            );
        public void OffsetFromThis(ref CaretInfo ci)
        {
            ci.CodePointIndex += CodePointIndex;
            ci.CaretXCoord += ContentPosition.X;
            ci.CaretRectangle.Offset(new SKPoint(ContentPosition.X, ContentPosition.Y));
            ci.LineIndex += LineIndex;
        }
        public void OffsetToThis(ref CaretInfo ci)
        {
            ci.CodePointIndex -= CodePointIndex;
            ci.CaretXCoord -= ContentPosition.X;
            ci.CaretRectangle.Offset(new SKPoint(-ContentPosition.X, -ContentPosition.Y));
            ci.LineIndex -= LineIndex;
        }
        public void OffsetFromThis(ref SelectionInfo si)
        {
            si = new(
                OffsetFromThis(si.OriginalRange),
                si.NewSelection.HasValue ? OffsetFromThis(si.NewSelection.Value) : null,
                OffsetFromThis(si.StartCaretInfo),
                OffsetFromThis(si.EndCaretInfo),
                si.SelectionInfoProvider,
                si.InteractingRuns,
                si.RecursiveInteractingRuns,
                si.RecursiveBFSInteractingRuns
            );
        }
        public void OffsetToThis(ref SelectionInfo si)
        {
            si = new(
                OffsetToThis(si.OriginalRange),
                si.NewSelection.HasValue ? OffsetToThis(si.NewSelection.Value) : null,
                OffsetToThis(si.StartCaretInfo),
                OffsetToThis(si.EndCaretInfo),
                si.SelectionInfoProvider,
                si.InteractingRuns,
                si.RecursiveInteractingRuns,
                si.RecursiveBFSInteractingRuns
            );
        }
        public SelectionInfo OffsetFromThis(SelectionInfo si)
        {
            OffsetFromThis(ref si);
            return si;
        }
        public CaretInfo OffsetFromThis(CaretInfo ci)
        {
            OffsetFromThis(ref ci);
            return ci;
        }
        public CaretInfo OffsetToThis(CaretInfo ci)
        {
            OffsetToThis(ref ci);
            return ci;
        }
        public HitTestResult OffsetFromThis(HitTestResult htr)
        {
            OffsetFromThis(ref htr);
            return htr;
        }
        public void OffsetFromThis(ref HitTestResult htr)
        {

            if (htr.ClosestLine is not -1)
                htr.ClosestLine += LineIndex;
            if (htr.OverLine is not -1)
                htr.OverLine += LineIndex;
            if (htr.ClosestCodePointIndex is not -1)
                htr.ClosestCodePointIndex += CodePointIndex;
            if (htr.OverCodePointIndex is not -1)
                htr.OverCodePointIndex += CodePointIndex;
        }
        public LineInfo OffsetFromThis(LineInfo info)
        {
            OffsetFromThis(ref info);
            return info;
        }
        public void OffsetFromThis(ref LineInfo info)
        {
            info.Line += LineIndex;
            info.Start = new(info.Start.CodePointIndex + CodePointIndex, info.Start.AltPosition);
            info.End = new(info.End.CodePointIndex + CodePointIndex, info.End.AltPosition);
            if (info.NextLine is not null)
                info.NextLine += LineIndex;
            if (info.PrevLine is not null)
                info.PrevLine += LineIndex;
        }
        public CaretPosition OffsetFromThis(CaretPosition caretPosition)
        {
            OffsetFromThis(ref caretPosition);
            return caretPosition;
        }
        public void OffsetFromThis(ref CaretPosition caretPosition)
        {
            caretPosition = new(caretPosition.CodePointIndex + CodePointIndex, caretPosition.AltPosition);
        }
        public CaretPosition OffsetToThis(CaretPosition caretPosition)
        {
            OffsetToThis(ref caretPosition);
            return caretPosition;
        }
        public void OffsetToThis(ref CaretPosition caretPosition)
        {
            caretPosition = new(caretPosition.CodePointIndex - CodePointIndex, caretPosition.AltPosition);
        }
        public TextRange OffsetFromThis(TextRange textRange)
        {
            OffsetFromThis(ref textRange);
            return textRange;
        }
        public void OffsetFromThis(ref TextRange textRange)
        {
            textRange = new(textRange.Start + CodePointIndex, textRange.End + CodePointIndex, textRange.AltPosition);
        }
        public void OffsetToThis(ref TextRange textRange)
        {
            textRange = new(textRange.Start - CodePointIndex, textRange.End - CodePointIndex, textRange.AltPosition);
        }
        public TextRange OffsetToThis(TextRange textRange)
        {
            OffsetToThis(ref textRange);
            return textRange;
        }
        public void OffsetToThis(ref PointF pt)
        {
            pt.X -= ContentPosition.X;
            pt.Y -= ContentPosition.Y;
        }
        public PointF OffsetToThis(PointF pt)
        {
            OffsetToThis(ref pt);
            return pt;
        }
        public void OffsetFromThis(ref PointF pt)
        {
            pt.X += ContentPosition.X;
            pt.Y += ContentPosition.Y;
        }
        public PointF OffsetFromThis(PointF pt)
        {
            OffsetFromThis(ref pt);
            return pt;
        }
        public void OffsetXToThis(ref float x)
        {
            x -= ContentPosition.X;
        }
        public float OffsetXToThis(float x)
        {
            OffsetXToThis(ref x);
            return x;
        }
        public void OffsetYToThis(ref float y)
        {
            y -= ContentPosition.Y;
        }
        public float OffsetYToThis(float y)
        {
            OffsetYToThis(ref y);
            return y;
        }
        public void OffsetToThis(ref DeleteInfo info)
        {
            info = info with { Range = OffsetToThis(info.Range) };
        }
        public DeleteInfo OffsetToThis(DeleteInfo info)
        {
            OffsetToThis(ref info);
            return info;
        }
    }
}
