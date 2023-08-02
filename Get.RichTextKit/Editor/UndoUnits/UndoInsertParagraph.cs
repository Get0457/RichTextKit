// This file has been edited and modified from its original version.
// The original version of this file can be found at https://github.com/toptensoftware/RichTextKit/.
// Original copyright notice is below.
using Get.RichTextKit.Editor.DocumentView;
using Get.RichTextKit.Editor.Paragraphs;
using Get.RichTextKit.Editor.Paragraphs.Panel;
using Get.RichTextKit.Utils;

namespace Get.RichTextKit.Editor.UndoUnits;

public class UndoInsertParagraph : UndoUnit<Document, DocumentViewUpdateInfo>
{
    IParagraphCollection _parent;
    public UndoInsertParagraph(IParagraphCollection parent, int index, Paragraph paragraph)
    {
        _parent = parent;
        _index = index;
        _paragraph = paragraph;
    }

    public override void Do(Document context)
    {
        _parent.Paragraphs.Insert(_index, _paragraph);
        foreach (var i in _index.._parent.Paragraphs.Count)
        {
            _parent.Paragraphs[i].ParentInfo = _parent.Paragraphs[i].ParentInfo with { Index = i };
        }
        _paragraph.OnParagraphAdded(context);
        context.Layout.Invalidate();
    }
    public override void Redo(Document context)
    {
        base.Redo(context);
        context.Layout.Invalidate();
        context.Layout.EnsureValid();
        NotifyInfo(new(NewSelection: new(_paragraph.GlobalInfo.CodePointIndex + _paragraph.CodePointLength)));
    }

    public override void Undo(Document context)
    {
        _parent.Paragraphs.RemoveAt(_index);
        foreach (var i in _index.._parent.Paragraphs.Count)
        {
            _parent.Paragraphs[i].ParentInfo = _parent.Paragraphs[i].ParentInfo with { Index = i };
        }
        _paragraph.OnParagraphRemoved(context);
        context.Layout.Invalidate();
        context.Layout.EnsureValid();
        NotifyInfo(new(NewSelection: new(_paragraph.GlobalInfo.CodePointIndex, true)));
    }

    int _index;
    Paragraph _paragraph;
}
