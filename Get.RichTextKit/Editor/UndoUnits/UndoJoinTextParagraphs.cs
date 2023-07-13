using System.Diagnostics;
using Get.RichTextKit.Editor.Paragraphs;
using Get.RichTextKit.Utils;

namespace Get.RichTextKit.Editor.UndoUnits;

class UndoJoinTextParagraphs : UndoUnit<Document>
{
    public UndoJoinTextParagraphs(int paragraphIndex)
    {
        _paragraphIndex = paragraphIndex;
    }

    public override void Do(Document context)
    {
        var firstPara = context.Paragraphs[_paragraphIndex];
        var secondPara = context.Paragraphs[_paragraphIndex + 1];

        // Remember what we need to undo
        _splitPoint = firstPara.Length;
        _removedParagraph = secondPara;

        // Copy all text from the second paragraph
        if (firstPara is ITextParagraph firstTextPara && secondPara is ITextParagraph secondTextPara)
            firstTextPara.TextBlock.AddText(secondTextPara.TextBlock);

        // Remove the joined paragraph
        secondPara.OnParagraphRemoved(context);
        context.Paragraphs.RemoveAt(_paragraphIndex + 1);
    }

    public override void Undo(Document context)
    {
        // Delete the joined text from the first paragraph
        var firstPara = context.Paragraphs[_paragraphIndex];
        if (firstPara is ITextParagraph firstTextPara)
            firstTextPara.TextBlock.DeleteText(_splitPoint, firstTextPara.TextBlock.Length - _splitPoint);
        else Debugger.Break();
        // Restore the split paragraph
        context.Paragraphs.Insert(_paragraphIndex + 1, _removedParagraph);
        _removedParagraph.OnParagraphAdded(context);
    }

    int _paragraphIndex;
    int _splitPoint;
    Paragraph _removedParagraph;
}
