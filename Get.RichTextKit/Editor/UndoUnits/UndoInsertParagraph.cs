// This file has been edited and modified from its original version.
// The original version of this file can be found at https://github.com/toptensoftware/RichTextKit/.
// Original copyright notice is below.
using Get.RichTextKit.Editor.Paragraphs;
using Get.RichTextKit.Editor.Paragraphs.Panel;
using Get.RichTextKit.Utils;

namespace Get.RichTextKit.Editor.UndoUnits;

class UndoInsertParagraph : UndoUnit<Document>
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
        _paragraph.OnParagraphAdded(context);
    }

    public override void Undo(Document context)
    {
        _parent.Paragraphs.RemoveAt(_index);
        _paragraph.OnParagraphRemoved(context);
    }

    int _index;
    Paragraph _paragraph;
}
