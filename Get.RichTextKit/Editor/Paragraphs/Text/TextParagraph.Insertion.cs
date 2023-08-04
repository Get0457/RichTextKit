
using Get.RichTextKit.Editor.DocumentView;
using Get.RichTextKit.Editor.UndoUnits;
using Get.RichTextKit.Utils;

namespace Get.RichTextKit.Editor.Paragraphs;

partial class TextParagraph
{
    protected internal override InsertTextStatus AddNewParagraph(int codePointIndex, UndoManager<Document, DocumentViewUpdateInfo> UndoManager)
    {
        throw new Exception();
    }
    protected internal override (InsertTextStatus Status, StyledText RemainingText) AddText(int codePointIndex, StyledText text, UndoManager<Document, DocumentViewUpdateInfo> UndoManager)
    {
        var (idx, _) = text.CodePoints.WithIndex().FirstOrDefault(x => x.Item is Document.NewParagraphSeparator);
        if (idx is default(int) && text.CodePoints[default] is not Document.NewParagraphSeparator)
        {
            // Simple: There is no new paragraph separator
            UndoManager.Do(new UndoInsertText(GlobalParagraphIndex, codePointIndex, text));
            return (InsertTextStatus.AlreadyAdd, new());
        }
        // Complex: There is paragraph separator

        // Get the text at the point that we will add
        var textAfterNewSeparator =
            codePointIndex < _textBlock.Length - 1 /* -1 for paragraph separator */
            ? _textBlock.Extract(codePointIndex, (_textBlock.Length - 1) - codePointIndex)
            : new();

        // Add all the text before paragraph separator
        UndoManager.Do(new UndoInsertText(GlobalParagraphIndex, codePointIndex, text.Extract(0, Math.Min(idx + 1, text.Length))));

        // Delete the remaining text
        UndoManager.Do(new UndoDeleteText(GlobalParagraphIndex, codePointIndex + text.Length, textAfterNewSeparator.Length + 1));

        // Add the remaining text after paragraph separator and the
        StyledText remainingText = new();
        if (idx + 1  < text.Length)
            remainingText.AddText(text.Extract(idx + 1, text.Length - (idx + 1)));
        remainingText.AddText(textAfterNewSeparator);

        return (InsertTextStatus.AddAfter, remainingText);
    }
}
