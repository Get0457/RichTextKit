// This file has been edited and modified from its original version.
// The original version of this file can be found at https://github.com/toptensoftware/RichTextKit/.
using Get.RichTextKit.Editor.DocumentView;
using Get.RichTextKit.Editor.Paragraphs;
using Get.RichTextKit.Editor.Structs;
using Get.RichTextKit.Utils;

namespace Get.RichTextKit.Editor.UndoUnits;

public class UndoReplaceParagraph : UndoUnit<Document, DocumentViewUpdateInfo>
{
    Paragraph _newParagraph;
    public UndoReplaceParagraph(ParagraphIndex paraIndex, Paragraph newParagraph)
    {
        _paraIndex = paraIndex;
        _newParagraph = newParagraph;
    }
    bool addTemporaryParagraph = false;
    public override void Do(Document context)
    {
        _oldParagraph = context.Paragraphs.GetParentAndChild(_paraIndex, out var _parent, out var _index);
        _parent.Paragraphs[_index] = _newParagraph;
        _newParagraph.ParentInfo = new(_parent, _index);
        _oldParagraph.OnParagraphRemoved(context);
        _newParagraph.OnParagraphAdded(context);
        if (addTemporaryParagraph = _parent.Paragraphs[^1] is not TextParagraph)
        {
            var tempPara = new TextParagraph(_newParagraph.EndStyle);
            _parent.Paragraphs.Add(tempPara);
            tempPara.OnParagraphAdded(context);
        }
        context.Layout.Invalidate();
    }
    public override void Redo(Document context)
    {
        base.Redo(context);
        context.Layout.InvalidateAndValid();
        NotifyInfo(new(NewSelection: new(_oldParagraph.GlobalInfo.CodePointIndex, true)));
    }

    public override void Undo(Document context)
    {
        _newParagraph = context.Paragraphs.GetParentAndChild(_paraIndex, out var _parent, out var _index);
        if (addTemporaryParagraph)
        {
            addTemporaryParagraph = false;
            var tempPara = _parent.Paragraphs[^1];
            _parent.Paragraphs.RemoveAt(_parent.Paragraphs.Count - 1);
            tempPara.OnParagraphRemoved(context);
        }
        _parent.Paragraphs[_index] = _oldParagraph;
        _oldParagraph.ParentInfo = new(_parent, _index);
        _newParagraph.OnParagraphRemoved(context);
        _oldParagraph.OnParagraphAdded(context);
        context.Layout.InvalidateAndValid();
        NotifyInfo(new(NewSelection: new(_oldParagraph.GlobalInfo.CodePointIndex + _oldParagraph.CodePointLength)));
        context.Layout.Invalidate();
    }

    ParagraphIndex _paraIndex;
    Paragraph _oldParagraph;
}
