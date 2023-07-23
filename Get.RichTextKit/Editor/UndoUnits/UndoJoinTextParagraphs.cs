using System.Diagnostics;
using System.Reflection;
using Get.RichTextKit.Editor.DocumentView;
using Get.RichTextKit.Editor.Paragraphs;
using Get.RichTextKit.Utils;

namespace Get.RichTextKit.Editor.UndoUnits;

class UndoJoinTextParagraphs : UndoUnit<Document, DocumentViewUpdateInfo>
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
        _splitPoint = firstPara.CodePointLength;
        _firstParaGlobalCPI = firstPara.GlobalInfo.CodePointIndex;
        _removedParagraph = secondPara;

        // Copy all text from the second paragraph
        if (firstPara is ITextParagraph firstTextPara && secondPara is ITextParagraph secondTextPara)
            firstTextPara.TextBlock.AddText(secondTextPara.TextBlock);

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
            firstTextPara.TextBlock.DeleteText(_splitPoint, firstTextPara.TextBlock.Length - _splitPoint);
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

    ParagraphIndex _paraIndex;
    int _splitPoint;
    int _firstParaGlobalCPI;
    Paragraph _removedParagraph;
}
