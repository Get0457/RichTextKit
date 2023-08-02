// This file has been edited and modified from its original version.
// The original version of this file can be found at https://github.com/toptensoftware/RichTextKit/.
using Get.RichTextKit.Editor.DocumentView;
using Get.RichTextKit.Editor.Paragraphs;
using Get.RichTextKit.Editor.Paragraphs.Panel;
using Get.RichTextKit.Editor.Structs;
using Get.RichTextKit.Utils;
using System.Diagnostics;

namespace Get.RichTextKit.Editor.UndoUnits;

public class UndoDeleteParagraph : UndoUnit<Document, DocumentViewUpdateInfo>
{
    public UndoDeleteParagraph(ParagraphIndex paraIndex)
    {
        _paraIndex = paraIndex;
    }

    public override void Do(Document context)
    {
        _paragraph = context.Paragraphs.GetParentAndChild(_paraIndex, out var _parent, out var _index);
        _parent.Paragraphs.RemoveAt(_index);
        foreach (var i in _index.._parent.Paragraphs.Count)
        {
            _parent.Paragraphs[i].ParentInfo = _parent.Paragraphs[i].ParentInfo with { Index = i };
        }
        _paragraph.OnParagraphRemoved(context);
        context.Layout.Invalidate();
    }
    public override void Redo(Document context)
    {
        base.Redo(context);
        context.Layout.InvalidateAndValid();
        NotifyInfo(new(NewSelection: new(_paragraph.GlobalInfo.CodePointIndex, true)));
    }

    public override void Undo(Document context)
    {
        context.Paragraphs.GetParentAndChild(_paraIndex, out var _parent, out var _index);
        _parent.Paragraphs.Insert(_index, _paragraph);
        foreach (var i in _index.._parent.Paragraphs.Count)
        {
            _parent.Paragraphs[i].ParentInfo = _parent.Paragraphs[i].ParentInfo with { Index = i };
        }
        _paragraph.OnParagraphAdded(context);
        context.Layout.InvalidateAndValid();
        NotifyInfo(new(NewSelection: new(_paragraph.GlobalInfo.CodePointIndex + _paragraph.CodePointLength)));
        context.Layout.Invalidate();
    }

    ParagraphIndex _paraIndex;
    Paragraph _paragraph;
}
