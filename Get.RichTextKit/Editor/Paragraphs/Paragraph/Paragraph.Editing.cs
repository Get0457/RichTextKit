using Get.RichTextKit.Editor.DocumentView;
using Get.RichTextKit.Editor.Paragraphs.Properties.Decoration;
using Get.RichTextKit.Editor.Structs;
using Get.RichTextKit.Utils;

namespace Get.RichTextKit.Editor.Paragraphs;

partial class Paragraph
{
    protected internal enum InsertTextStatus
    {
        AddBefore,
        AddAfter,
        AlreadyAdd
    }
    protected internal abstract (InsertTextStatus Status, StyledText RemainingText) AddText(int codePointIndex, StyledText text, UndoManager<Document, DocumentViewUpdateInfo> UndoManager);
    /// <summary>
    /// Notify the paragraph that the user tries to delete the front of the paragraph
    /// </summary>
    /// <returns>Whether the method handles the delete front option. If handled, paragraph joining with previous paragraph should not happen</returns>
    public virtual bool DeleteFront(UndoManager<Document, DocumentViewUpdateInfo> UndoManager)
    {
        if (Properties.Decoration is not null)
        {
            UndoManager.Do(new UndoParagraphSetting<IParagraphDecoration>(GlobalParagraphIndex, null, x => x.Properties.Decoration, (x, y) =>
            {
                var oldDecoration = x.Properties.Decoration;
                x.Properties.Decoration = y;
                oldDecoration?.RemovedFromLayout();
                return true;
            }));
            return true;
        }
        return false;
    }
    /// <summary>
    /// Notify the paragraph that the user tries to delete the front of the paragraph
    /// </summary>
    /// <returns>Whether the method handles the delete front option. If handled, paragraph joining with previous paragraph should not happen</returns>
    public virtual bool CanDeleteFront()
    {
        if (Properties.Decoration is not null)
        {
            return true;
        }
        return false;
    }
    public abstract bool ShouldDeleteAll(DeleteInfo deleteInfo);
    public abstract bool CanDeletePartial(DeleteInfo deleteInfo, out TextRange requestedSelection);
    public abstract bool DeletePartial(DeleteInfo deleteInfo, out TextRange requestedSelection, UndoManager<Document, DocumentViewUpdateInfo> UndoManager);
    public virtual bool CanJoinWith(Paragraph other) { return false; }
    public virtual bool TryJoinWithNextParagraph(UndoManager<Document, DocumentViewUpdateInfo> UndoManager) { return false; }
}
