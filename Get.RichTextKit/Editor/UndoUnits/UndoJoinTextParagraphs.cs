using System.Diagnostics;
using Get.RichTextKit.Editor.DocumentView;
using Get.RichTextKit.Editor.Paragraphs;
using Get.RichTextKit.Editor.Structs;
using Get.RichTextKit.Utils;

namespace Get.RichTextKit.Editor.UndoUnits;

public class UndoJoinTextParagraphs : UndoUnit<Document, DocumentViewUpdateInfo>
{
    public UndoJoinTextParagraphs(ParagraphIndex paraIndex)
    {
        _paraIndex = paraIndex;
    }

    public override void Do(Document context)
    {
        var firstPara = context.Paragraphs.GetParentAndChild(_paraIndex, out var parent, out var _paragraph);
        var secondPara = parent.Paragraphs[_paragraph + 1];

        // Remember what we need to undo
        _splitPoint = firstPara.CodePointLength - 1 /* -1 for the paragraph separator */;
        _firstParaGlobalCPI = firstPara.GlobalInfo.CodePointIndex;
        _removedParagraph = secondPara;

        // Copy all text from the second paragraph
        if (firstPara is ITextParagraph firstTextPara && secondPara is ITextParagraph secondTextPara)
        {
            // Delete the paragraph separator
            _paragraphSeparator = firstTextPara.TextBlock.Extract(firstTextPara.TextBlock.Length - 1, 1);
            Debug.Assert(_paragraphSeparator.Length == 1 && _paragraphSeparator.CodePoints[0] is Document.NewParagraphSeparator);
            firstTextPara.EnsureReadyToModify();
            firstTextPara.TextBlock.DeleteText(firstTextPara.TextBlock.Length - 1, 1);
            // Add the second paragraph text
            firstTextPara.TextBlock.AddText(secondTextPara.TextBlock);
            firstTextPara.OnTextBlockChanged();
        }

        // Remove the joined paragraph
        secondPara.OnParagraphRemoved(context);
        parent.Paragraphs.RemoveAt(_paragraph + 1);
        var _index = _paragraph + 1;
        var _parent = parent;
        foreach (var i in _index.._parent.Paragraphs.Count)
        {
            _parent.Paragraphs[i].ParentInfo = _parent.Paragraphs[i].ParentInfo with { Index = i };
        }
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
        var firstPara = context.Paragraphs.GetParentAndChild(_paraIndex, out var parent, out var _paragraph);
        if (firstPara is ITextParagraph firstTextPara)
        {
            firstTextPara.EnsureReadyToModify();
            // Delete the second paragraph text
            firstTextPara.TextBlock.DeleteText(_splitPoint, firstTextPara.TextBlock.Length - _splitPoint);
            // Add the paragraph separator back
            firstTextPara.TextBlock.AddText(_paragraphSeparator);
            firstTextPara.OnTextBlockChanged();
        }
        else Debugger.Break();
        // Restore the split paragraph
        var _index = _paragraph + 1;
        var _parent = parent;
        _parent.Paragraphs.Insert(_index, _removedParagraph);
        foreach (var i in _index.._parent.Paragraphs.Count)
        {
            _parent.Paragraphs[i].ParentInfo = _parent.Paragraphs[i].ParentInfo with { Index = i };
        }
        _removedParagraph.OnParagraphAdded(context);
        context.Layout.Invalidate();
        context.Layout.EnsureValid();
        NotifyInfo(new(NewSelection: new(_removedParagraph.GlobalInfo.CodePointIndex + _removedParagraph.CodePointLength)));
    }
    StyledText _paragraphSeparator;
    ParagraphIndex _paraIndex;
    int _splitPoint;
    int _firstParaGlobalCPI;
    Paragraph _removedParagraph;
}
