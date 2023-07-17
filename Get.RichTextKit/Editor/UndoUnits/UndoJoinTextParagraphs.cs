using System.Diagnostics;
using Get.RichTextKit.Editor.DocumentView;
using Get.RichTextKit.Editor.Paragraphs;
using Get.RichTextKit.Utils;

namespace Get.RichTextKit.Editor.UndoUnits;

class UndoJoinTextParagraphs : UndoUnit<Document, DocumentViewUpdateInfo>
{
    public UndoJoinTextParagraphs(int paragraphCodePointIndex)
    {
        _paragraphCodePointIndex = paragraphCodePointIndex;
    }

    public override void Do(Document context)
    {
        var firstPara = context.Paragraphs.GlobalFromCodePointIndex(new(_paragraphCodePointIndex), out var parent, out var _paragraph, out var cpi);
        var secondPara = parent.Paragraphs[_paragraph + 1];

        // Remember what we need to undo
        _splitPoint = firstPara.CodePointLength;
        _firstParaGlobalCPI = firstPara.GlobalInfo.CodePointIndex;
        _removedParagraph = secondPara;

        // Copy all text from the second paragraph
        if (firstPara is ITextParagraph firstTextPara && secondPara is ITextParagraph secondTextPara)
            firstTextPara.TextBlock.AddText(secondTextPara.TextBlock);

        // Remove the joined paragraph
        secondPara.OnParagraphRemoved(context);
        parent.Paragraphs.RemoveAt(_paragraph + 1);
        context.Layout.Invalidate();
    }
    public override void Redo(Document context)
    {
        base.Redo(context);
        NotifyInfo(new(NewSelection: new(_firstParaGlobalCPI + _splitPoint)));
    }

    public override void Undo(Document context)
    {
        // Delete the joined text from the first paragraph
        var firstPara = context.Paragraphs.GlobalFromCodePointIndex(new(_paragraphCodePointIndex), out var parent, out var _paragraph, out var cpi);
        if (firstPara is ITextParagraph firstTextPara)
            firstTextPara.TextBlock.DeleteText(_splitPoint, firstTextPara.TextBlock.Length - _splitPoint);
        else Debugger.Break();
        // Restore the split paragraph
        parent.Paragraphs.Insert(_paragraph + 1, _removedParagraph);
        _removedParagraph.OnParagraphAdded(context);
        context.Layout.Invalidate();
        context.Layout.EnsureValid();
        NotifyInfo(new(NewSelection: new(_removedParagraph.GlobalInfo.CodePointIndex + _removedParagraph.CodePointLength)));
    }

    int _paragraphCodePointIndex;
    int _splitPoint;
    int _firstParaGlobalCPI;
    Paragraph _removedParagraph;
}
