using Get.RichTextKit.Editor.Structs;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Get.RichTextKit.Utils;
using Get.RichTextKit;
using HarfBuzzSharp;
using System.Reflection;
using Get.RichTextKit.Styles;
using System.Diagnostics;
using Get.RichTextKit.Editor.DocumentView;
using Get.RichTextKit.Editor.UndoUnits;

namespace Get.RichTextKit.Editor.Paragraphs.Panel;

public abstract partial class PanelParagraph : Paragraph, IParagraphPanel
{
    protected internal override (InsertTextStatus Status, StyledText RemainingText) AddText(int codePointIndex, StyledText text, UndoManager<Document, DocumentViewUpdateInfo> UndoManager)
    {
        int nextIdx;
        Paragraph textPara;

        // Setup
        nextIdx = LocalChildrenFromCodePointIndexAsIndex(new(codePointIndex, false), out int idxInside);
        textPara = Children[nextIdx];

        // Do the job
        var (status, remainingText) = textPara.AddText(idxInside, text, UndoManager);
        if (IsChildrenReadOnly) return (status, remainingText);

        while (remainingText.Length >= 0)
        {
            // Update Next
            switch (status)
            {
                case InsertTextStatus.AddBefore:
                    // add before current para = insert same index
                    break;
                case InsertTextStatus.AddAfter:
                    // add after current para = insert next index
                    nextIdx++;
                    break;
                case InsertTextStatus.AlreadyAdd:
                    return (status, remainingText);
                default:
                    throw new ArgumentOutOfRangeException(nameof(status));
            }

            // Setup
            textPara = new TextParagraph(remainingText.GetStyleAtOffset(0) ??
                (status is InsertTextStatus.AddAfter ? textPara.EndStyle : textPara.StartStyle)
                ) { ParentInfo = new(this, nextIdx) };

            // Do the job
            UndoManager.Do(new UndoInsertParagraph(this, nextIdx, textPara));
            if (remainingText.Length is 0) return (InsertTextStatus.AlreadyAdd, new());
            (status, remainingText) = textPara.AddText(0, remainingText, UndoManager);
        }
        return (InsertTextStatus.AlreadyAdd, new());
    }
}