// This file has been edited and modified from its original version.
// The original version of this file can be found at https://github.com/toptensoftware/RichTextKit/.
using System.Reflection;
using Get.RichTextKit;
using Get.RichTextKit.Editor.DocumentView;
using Get.RichTextKit.Editor.Paragraphs;
using Get.RichTextKit.Utils;

namespace Get.RichTextKit.Editor.UndoUnits;

class UndoInsertText : UndoUnit<Document, DocumentViewUpdateInfo>
{
    public UndoInsertText(ParagraphIndex paraIndex, int offset, StyledText text)
    {
        _paraIndex = paraIndex;
        _offset = offset;
        _length = text.Length;
        _text = text;
    }

    public ParagraphIndex TargetParagraphIndex => _paraIndex;
    public int Offset => _offset;
    public int Length => _length;

    public bool ShouldAppend(Document context, StyledText text)
    {
        if (context.Paragraphs[_paraIndex] is not ITextParagraph tp) return false;
        var _textBlock = tp.TextBlock;
        // If this is a word boundary then don't extend this unit
        return !WordBoundaryAlgorithm.IsWordBoundary(_textBlock.CodePoints.SubSlice(0, _offset + _length), text.CodePoints.AsSlice());
    }
    
    public void Append(Document context, StyledText text)
    {
        Paragraph para;
        if ((para = context.Paragraphs[_paraIndex]) is not ITextParagraph tp) return;
        var _textBlock = tp.TextBlock;
        // Insert into the text block
        _textBlock.InsertText(_offset + _length, text);

        // Update length
        _length += text.Length;
        NotifyInfo(new(NewSelection: new(para.GlobalInfo.CodePointIndex + _offset + _length)));
    }

    public void Replace(Document context, StyledText text)
    {
        Paragraph para;
        if ((para = context.Paragraphs[_paraIndex]) is not ITextParagraph tp) return;
        var _textBlock = tp.TextBlock;

        // Insert into the text block
        _textBlock.DeleteText(_offset, _length);
        _textBlock.InsertText(_offset, text);

        // Update length
        _length = text.Length;

        NotifyInfo(new(NewSelection: new(para.GlobalInfo.CodePointIndex + _offset + _length)));
    }
    int _notifyTextLength = 0;
    public override void Do(Document context)
    {
        if (context.Paragraphs[_paraIndex] is not ITextParagraph tp) return;
        var _textBlock = tp.TextBlock;

        // Insert the text into the text block
        _textBlock.InsertText(_offset, _text);

        // Release our copy of the text
        _notifyTextLength = _text.Length;
        _text = null;
    }
    public override void Redo(Document context)
    {
        base.Redo(context);
        context.Layout.EnsureValid();
        var para = context.Paragraphs[_paraIndex];
        NotifyInfo(new(NewSelection: new(para.GlobalInfo.CodePointIndex + _offset + _notifyTextLength)));
    }

    public override void Undo(Document context)
    {
        Paragraph para;
        if ((para = context.Paragraphs[_paraIndex]) is not ITextParagraph tp) return;
        var _textBlock = tp.TextBlock;
        // Save a copy of the text being deleted
        _text = _textBlock.Extract(_offset, _length);

        // Delete it
        _textBlock.DeleteText(_offset, _length);
        context.Layout.EnsureValid();
        NotifyInfo(new(NewSelection: new(para.GlobalInfo.CodePointIndex + _offset)));
    }

    ParagraphIndex _paraIndex;
    int _offset;
    int _length;
    StyledText _text;
}
