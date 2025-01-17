﻿// This file has been edited and modified from its original version.
// The original version of this file can be found at https://github.com/toptensoftware/RichTextKit/.
#nullable enable
using Get.RichTextKit.Editor.DocumentView;
using Get.RichTextKit.Editor.Paragraphs;
using Get.RichTextKit.Editor.Structs;
using Get.RichTextKit.Utils;

namespace Get.RichTextKit.Editor.UndoUnits;

public class UndoDeleteText : UndoUnit<Document, DocumentViewUpdateInfo>
{
    public UndoDeleteText(ParagraphIndex paraIndex, int offset, int length)
    {
        _paraIndex = paraIndex;
        _offset = offset;
        _length = length;
    }

    public override void Do(Document context)
    {
        if (context.Paragraphs[_paraIndex] is not ITextParagraph tp) return;
        var _textBlock = tp.TextBlock;
        _savedText = _length > 0 ? _textBlock.Extract(_offset, _length) : null;
        tp.EnsureReadyToModify();
        _textBlock.DeleteText(_offset, _length);
        tp.OnTextBlockChanged();
    }
    public override void Redo(Document context)
    {
        base.Redo(context);
        context.Layout.Invalidate();
        context.Layout.EnsureValid();
        var para = context.Paragraphs[_paraIndex];
        NotifyInfo(new(NewSelection: new(para.GlobalInfo.CodePointIndex + _offset)));
    }
    public override void Undo(Document context)
    {
        Paragraph para;
        if ((para = context.Paragraphs[_paraIndex]) is not ITextParagraph tp) return;
        var _textBlock = tp.TextBlock;
        if (_savedText != null)
        {
            tp.EnsureReadyToModify();
            _textBlock.InsertText(_offset, _savedText);
            tp.OnTextBlockChanged();
        }
        var length = _savedText?.Length ?? 0;
        _savedText = null;
        NotifyInfo(new(NewSelection: new(para.GlobalInfo.CodePointIndex + _offset + length)));
    }

    public bool ExtendBackspace(Document context, int length)
    {
        Paragraph para;
        if ((para = context.Paragraphs[_paraIndex]) is not ITextParagraph tp) return false;
        var _textBlock = tp.TextBlock;
        // Don't extend across paragraph boundaries
        if (_offset - length < 0)
            return false;

        // Copy the additional text
        var temp = _textBlock.Extract(_offset - length, length);
        (_savedText ??= new()).InsertText(0, temp);
        tp.EnsureReadyToModify();
        _textBlock.DeleteText(_offset - length, length);
        tp.OnTextBlockChanged();

        // Update position
        _offset -= length;
        _length += length;
        NotifyInfo(new(NewSelection: new(para.GlobalInfo.CodePointIndex + _offset)));

        return true;
    }

    public bool ExtendForwardDelete(Document context, int length)
    {
        if (context.Paragraphs[_paraIndex] is not ITextParagraph tp) return false;
        var _textBlock = tp.TextBlock;
        // Don't extend across paragraph boundaries
        if (_offset + length > _textBlock.Length - 1)
            return false;

        // Copy the additional text
        var temp = _textBlock.Extract(_offset, length);
        (_savedText ??= new()).InsertText(_length, temp);
        tp.EnsureReadyToModify();
        _textBlock.DeleteText(_offset, length);
        tp.OnTextBlockChanged();

        // Update position
        _length += length;

        return true;
    }

    public bool ExtendOvertype(Document context, int offset, int length)
    {
        if (context.Paragraphs[_paraIndex] is not ITextParagraph tp) return false;
        var _textBlock = tp.TextBlock;
        // Don't extend across paragraph boundaries
        if (_offset + offset + length > _textBlock.Length - 1)
            return false;

        // This can happen when a DeleteText unit is retroactively
        // constructed when typing in overtype mode at the end of a 
        // paragraph
        _savedText ??= new StyledText();

        // Copy the additional text
        var temp = _textBlock.Extract(_offset + offset, length);
        _savedText.InsertText(_length, temp);
        tp.EnsureReadyToModify();
        _textBlock.DeleteText(_offset + offset, length);
        tp.OnTextBlockChanged();

        // Update position
        _length += length;

        return true;
    }

    ParagraphIndex _paraIndex;
    int _offset;
    int _length;
    StyledText? _savedText;
}
