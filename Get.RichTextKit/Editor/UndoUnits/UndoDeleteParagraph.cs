// This file has been edited and modified from its original version.
// The original version of this file can be found at https://github.com/toptensoftware/RichTextKit/.
using Get.RichTextKit.Editor.DocumentView;
using Get.RichTextKit.Editor.Paragraphs;
using Get.RichTextKit.Editor.Paragraphs.Panel;
using Get.RichTextKit.Utils;

namespace Get.RichTextKit.Editor.UndoUnits;

class UndoDeleteParagraph : UndoUnit<Document, DocumentViewUpdateInfo>
{
    IParagraphCollection _parent;
    public UndoDeleteParagraph(IParagraphCollection parent, int index)
    {
        _parent = parent;
        _index = index;
    }

    public override void Do(Document context)
    {
        _paragraph = _parent.Paragraphs[_index];
        _parent.Paragraphs.RemoveAt(_index);
        _paragraph.OnParagraphRemoved(context);
    }
    public override void Redo(Document context)
    {
        base.Redo(context);
        NotifyInfo(new(NewSelection: new(_paragraph.GlobalInfo.CodePointIndex, true)));
    }

    public override void Undo(Document context)
    {
        _parent.Paragraphs.Insert(_index, _paragraph);
        _paragraph.OnParagraphAdded(context);
        NotifyInfo(new(NewSelection: new(_paragraph.GlobalInfo.CodePointIndex + _paragraph.CodePointLength)));
    }

    int _index;
    Paragraph _paragraph;
}
