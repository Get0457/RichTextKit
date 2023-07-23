// This file has been edited and modified from its original version.
// The original version of this file can be found at https://github.com/toptensoftware/RichTextKit/.
using Get.RichTextKit;
using Get.RichTextKit.Editor;
using Get.RichTextKit.Editor.DocumentView;
using Get.RichTextKit.Utils;

namespace Get.RichTextKit.Editor.UndoUnits;

class UndoReplaceTextGroup : UndoGroup<Document, DocumentViewUpdateInfo>
{
    public UndoReplaceTextGroup() : base(null)
    {
    }

    public bool TryExtend(Document context, TextRange range, StyledText text, EditSemantics semantics, int imeCaretOffset, out ReplaceTextStatus status)
    {
        // Extend typing?
        if (semantics == EditSemantics.Typing && _info.Semantics == EditSemantics.Typing)
        {
            // Mustn't be replacing a range and must be at the correct position
            if (range.IsRange || _info.CodePointIndex + _info.NewLength != range.Start)
            {
                status = default;
                return false;
            }

            // Mustn't be inserting any paragraph breaks
            if (text.CodePoints.AsSlice().IndexOf(Document.NewParagraphSeparator) >= 0)
            {
                status = default;
                return false;
            }

            // The last unit in this group must be an insert text unit
            if (!(LastUnit is UndoInsertText insertUnit))
            {
                status = default;
                return false;
            }

            // Check if should extend (will return false on a word boundary 
            // to avoid extending for long spans of typed text)
            if (!insertUnit.ShouldAppend(context, text))
            {
                status = default;
                return false;
            }

            // Update the insert unit
            insertUnit.Append(context, text);

            // Fire notifications
            context.FireDocumentChanging(new DocumentChangeInfo()
            {
                CodePointIndex = _info.CodePointIndex + _info.NewLength,
                OldLength = 0,
                NewLength = text.Length,
                Semantics = semantics,
            });
            context.FireDocumentChanged();

            // Update the group
            _info.NewLength += text.Length;
            status = ReplaceTextStatus.Success with { RequestedNewSelection = new(range.Maximum + text.Length) };
            return true;
        }

        // Extend overtype?
        if (semantics == EditSemantics.Overtype && _info.Semantics == EditSemantics.Overtype)
        {
            // Mustn't be replacing a range and must be at the correct position
            if (_info.CodePointIndex + _info.NewLength != range.Start)
            {
                status = default;
                return false;
            }

            // Mustn't be inserting any paragraph breaks
            if (text.CodePoints.AsSlice().IndexOf(Document.NewParagraphSeparator) >= 0)
            {
                status = default;
                return false;
            }

            // The last unit in this group must be an insert text unit
            if (!(LastUnit is UndoInsertText insertUnit))
            {
                status = default;
                return false;
            }

            // The second last unit before must be a delete text unit.  
            // If we don't have one, create one.  This can happen when starting
            // to type in overtype mode at the very end of a paragraph
            if (Units.Count < 2 || (!(Units[Units.Count - 2] is UndoDeleteText deleteUnit)))
            {
                deleteUnit = new UndoDeleteText(insertUnit.TargetParagraphIndex, insertUnit.Offset, 0);
                this.Insert(Units.Count - 1, deleteUnit);
            }

            // Delete forward if can 
            // (need to do this before insert and doesn't matter if can't)
            int deletedLength = 0;
            if (deleteUnit.ExtendOvertype(context, range.Start - _info.CodePointIndex, range.Length))
                deletedLength = range.Length;

            // Extend insert unit
            insertUnit.Append(context, text);

            // Fire notifications
            context.FireDocumentChanging(new DocumentChangeInfo()
            {
                CodePointIndex = _info.CodePointIndex + _info.NewLength,
                OldLength = deletedLength,
                NewLength = text.Length,
                Semantics = semantics,
            });
            context.FireDocumentChanged();

            // Update the group
            _info.OldLength += deletedLength;
            _info.NewLength += text.Length;
            status = ReplaceTextStatus.Success with { RequestedNewSelection = new(_info.CodePointIndex) };
            return true;
        }

        // Extend backspace?
        if (semantics == EditSemantics.Backspace && _info.Semantics == EditSemantics.Backspace)
        {
            // Get the last delete unit
            var deleteUnit = this.LastUnit as UndoDeleteText;
            if (deleteUnit == null)
            {
                status = default;
                return false;
            }

            // Must be deleting text immediately before
            if (range.End != _info.CodePointIndex)
            {
                status = default;
                return false;
            }

            // Extend the delete unit
            if (!deleteUnit.ExtendBackspace(context, range.Length))
            {
                status = default;
                return false;
            }

            // Fire change events
            context.FireDocumentChanging(new DocumentChangeInfo()
            {
                CodePointIndex = _info.CodePointIndex - range.Length,
                OldLength = range.Length,
                NewLength = 0,
                Semantics = semantics,
            });
            context.FireDocumentChanged();

            // Update self
            _info.CodePointIndex -= range.Length;
            _info.OldLength += range.Length;
            status = ReplaceTextStatus.Success with { RequestedNewSelection = new(_info.CodePointIndex) };
            return true;
        }

        // Extend delete forward?
        if (semantics == EditSemantics.ForwardDelete && _info.Semantics == EditSemantics.ForwardDelete)
        {
            // Get the last delete unit
            var deleteUnit = this.LastUnit as UndoDeleteText;
            if (deleteUnit == null)
            {
                status = default;
                return false;
            }

            // Must be deleting text immediately after
            if (range.Start != _info.CodePointIndex)
            {
                status = default;
                return false;
            }

            // Extend the delete unit
            if (!deleteUnit.ExtendForwardDelete(context, range.Length))
            {
                status = default;
                return false;
            }

            // Update self
            _info.OldLength += range.Length;

            // Fire change events
            context.FireDocumentChanging(new DocumentChangeInfo()
            {
                CodePointIndex = _info.CodePointIndex,
                OldLength = range.Length,
                NewLength = 0,
                Semantics = semantics,
            });
            context.FireDocumentChanged();
            status = ReplaceTextStatus.Success with { RequestedNewSelection = new(range.Minimum) };
            return true;
        }

        // IME Composition
        if (semantics == EditSemantics.ImeComposition && _info.Semantics == EditSemantics.ImeComposition)
        {
            // The last unit in this group must be an insert text unit
            if (!(LastUnit is UndoInsertText insertUnit))
            {
                status = default;
                return false;
            }

            // Replace the inserted text
            insertUnit.Replace(context, text);

            // Fire notifications
            context.FireDocumentChanging(new DocumentChangeInfo()
            {
                CodePointIndex = _info.CodePointIndex,
                OldLength = _info.NewLength,
                NewLength = text.Length,
                Semantics = semantics,
                ImeCaretOffset = imeCaretOffset,
            });
            context.FireDocumentChanged();

            // Update the group
            _info.NewLength = text.Length;
            _info.ImeCaretOffset = imeCaretOffset;

            status = new() { RequestedNewSelection = new(_info.CodePointIndex) };
            return true;
        }


        status = default;
        return false;
    }

    public override void OnClose(Document context)
    {
        base.OnClose(context);
        context.FireDocumentChanging(_info);
    }


    public override void Undo(Document context)
    {
        // Make the change
        base.Undo(context);

        // Fire the undo version of the info by swapping the
        // old and new length and setting the undo flag
        var undoInfo = _info;
        undoInfo.IsUndoing = true;
        SwapHelper.Swap(ref undoInfo.OldLength, ref undoInfo.NewLength);
        context.FireDocumentChanging(undoInfo);
    }

    public override void Redo(Document context)
    {
        // Make the change
        base.Redo(context);

        // Fire event
        context.FireDocumentChanging(_info);
    }

    public void SetDocumentChangeInfo(DocumentChangeInfo info)
    {
        _info = info;
    }

    public DocumentChangeInfo Info => _info;

    DocumentChangeInfo _info;
}
