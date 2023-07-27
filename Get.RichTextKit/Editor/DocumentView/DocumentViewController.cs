using Paragraph = Get.RichTextKit.Editor.Paragraphs.Paragraph;
using Get.RichTextKit.Editor.UndoUnits;
using System.Drawing;
using Get.RichTextKit.Styles;
using static System.Net.Mime.MediaTypeNames;

namespace Get.RichTextKit.Editor.DocumentView;

public partial class DocumentViewController
{
    DocumentView DocumentView;
    internal DocumentViewController(DocumentView docview)
    {
        DocumentView = docview;
    }
    public void Type(string text, bool insertMode = true) => Type(text.AsSpan(), insertMode);
    public void Type(ReadOnlySpan<char> text, bool insertMode = true)
    {
        var selection = DocumentView.Selection.Range;
        var currentStyle = DocumentView.Selection.CurrentPositionStyle;
        if (currentStyle is IDoNotCombineStyle d)
            currentStyle = new CopyStyle(d);
        DocumentView.OwnerDocument.Editor.ReplaceText(
            insertMode || selection.IsRange ?
            selection : new(selection.Start, selection.End + text.Length, altPosition: false),
            text,
            insertMode ? EditSemantics.Typing : EditSemantics.Overtype,
            styleToUse: currentStyle
        );
        MoveCaret(new CaretPosition(selection.Minimum + text.Length, false));
    }
    public void Type(ReadOnlySpan<int> text, bool insertMode = true)
    {
        var selection = DocumentView.Selection.Range;
        var currentStyle = DocumentView.Selection.CurrentPositionStyle;
        if (currentStyle is IDoNotCombineStyle d)
            currentStyle = new CopyStyle(d);
        DocumentView.OwnerDocument.Editor.ReplaceText(
            insertMode || selection.IsRange ?
            selection : new(selection.Start, selection.End + text.Length, altPosition: false),
            text,
            insertMode ? EditSemantics.Typing : EditSemantics.Overtype,
            styleToUse: currentStyle
        );
        MoveCaret(new CaretPosition(selection.Minimum + text.Length, false));
    }
    public void Type(StyledText text, bool insertMode = true)
    {
        var selection = DocumentView.Selection.Range;
        DocumentView.OwnerDocument.Editor.ReplaceText(
            insertMode || selection.IsRange ?
            selection : new(selection.Start, selection.End + text.Length, altPosition: false),
            text,
            insertMode ? EditSemantics.Typing : EditSemantics.Overtype
        );
        MoveCaret(new CaretPosition(selection.Minimum + text.Length, false));
    }
    public void Delete(bool deleteFront = false, int charCount = 1)
    {
        var selection = DocumentView.Selection.Range;
        if (selection.Start == 0 && !selection.IsRange && !deleteFront)
        {
            DocumentView.OwnerDocument.Paragraphs.GlobalChildrenFromCodePointIndex(
                selection.StartCaretPosition,
                out _,
                out _
            ).DeleteFront(DocumentView.OwnerDocument.UndoManager);
            // delete front or don't delete
            return;
        }
        var status = DocumentView.OwnerDocument.Editor.ReplaceText(
            selection.IsRange ? selection : new(
                selection.Start,
                deleteFront ? selection.Start + charCount : selection.Start - charCount,
                altPosition: false
            ),
            "".AsSpan(),
            deleteFront ? EditSemantics.ForwardDelete : EditSemantics.Backspace,
            isNonSelectionDeletion: !selection.IsRange
        );
        Select(status.RequestedNewSelection);
    }
    float? _ghostXCoordinate;
    public void MoveCaret(CaretPosition position) => Select(new(position));
    public void Select(CaretPosition position, SelectionKind kind) => Select(DocumentView.OwnerDocument.GetSelectionRange(position, kind));
    public void Select(TextRange textRange)
    {
        _ghostXCoordinate = null;
        DocumentView.Selection.Range = textRange;
    }
    public void MoveCaret(Direction direction, bool wholeWord = false, bool selectionMode = false)
    {
        var originalRange = DocumentView.Selection.Range;
        // If selecting but not wanting to select
        if (!selectionMode && originalRange.IsRange)
        {
            if (direction is Direction.Left or Direction.Right)
            {
                var OrderedRange = originalRange.Start > originalRange.End
                ? originalRange.Reversed : originalRange;
                var newCaretPos = direction is Direction.Left
                    ? OrderedRange.Start : OrderedRange.End;
                DocumentView.Selection.Range = new(newCaretPos);
                return;
            }
            else
            {
                // Up or down: Just use the end position
                originalRange = new(originalRange.End, originalRange.AltPosition);
                // TODO: Make this like other text editor engine.
            }
        }
        if (direction is Direction.Left or Direction.Right)
        {
            DocumentView.Selection.Range = DocumentView.OwnerDocument.Editor.Navigate(
                originalRange,
                wholeWord ? Paragraphs.NavigationSnap.Word : Paragraphs.NavigationSnap.Character,
                direction is Direction.Left ? Paragraphs.NavigationDirection.Backward : Paragraphs.NavigationDirection.Forward,
                selectionMode,
                ref _ghostXCoordinate
            );
        }
        else
        {
            DocumentView.Selection.Range = DocumentView.OwnerDocument.Editor.Navigate(
                originalRange,
                wholeWord ? Paragraphs.NavigationSnap.Word : Paragraphs.NavigationSnap.Character,
                direction is Direction.Down ? Paragraphs.NavigationDirection.Down : Paragraphs.NavigationDirection.Up,
                selectionMode,
                ref _ghostXCoordinate
            );
        }
    }
    public void MoveCaret(SpacialCaretMovement mode, bool selectionMode = false)
    {
        var originalRange = DocumentView.Selection.Range;
        // If selecting but not wanting to select
        if (!selectionMode && originalRange.IsRange)
        {
            switch (mode)
            {
                case SpacialCaretMovement.LineHome:
                case SpacialCaretMovement.PageUp:
                    originalRange = new(originalRange.GetMinimumCaretPosition());
                    break;
                case SpacialCaretMovement.LineEnd:
                case SpacialCaretMovement.PageDown:
                    originalRange = new(originalRange.GetMaximumCaretPosition());
                    break;
                default:
                    // DocumentHome and DocumentEnd will already be handled correctly
                    break;
            }
        }
        var newPos = DocumentView.OwnerDocument.Editor.Navigate(
            mode switch
            {
                SpacialCaretMovement.LineHome or
                SpacialCaretMovement.PageUp or
                SpacialCaretMovement.DocumentHome
                => originalRange.GetMinimumCaretPosition(),
                SpacialCaretMovement.LineEnd or
                SpacialCaretMovement.PageDown or
                SpacialCaretMovement.DocumentEnd
                => originalRange.GetMaximumCaretPosition(),
                _ => throw new ArgumentOutOfRangeException(nameof(mode))
            },
            mode switch
            {
                SpacialCaretMovement.LineHome => NavigationKind.LineHome,
                SpacialCaretMovement.LineEnd => NavigationKind.LineEnd,
                SpacialCaretMovement.DocumentHome => NavigationKind.DocumentHome,
                SpacialCaretMovement.DocumentEnd => NavigationKind.DocumentEnd,
                SpacialCaretMovement.PageUp => NavigationKind.PageUp,
                SpacialCaretMovement.PageDown => NavigationKind.PageDown,
                _ => throw new ArgumentOutOfRangeException(nameof(mode))
            },
            DocumentView.ViewHeight,
            ref _ghostXCoordinate
        );
        if (mode is SpacialCaretMovement.PageDown or SpacialCaretMovement.DocumentHome)
        {
            var info = DocumentView.OwnerDocument.Editor.GetCaretInfo(newPos);
            DocumentView.YScroll = info.CaretRectangle.Top;
        }
        else if (mode is SpacialCaretMovement.PageUp or SpacialCaretMovement.DocumentEnd)
        {
            var info = DocumentView.OwnerDocument.Editor.GetCaretInfo(newPos);
            DocumentView.YScroll = info.CaretRectangle.Bottom - DocumentView.ViewHeight;
        }
        if (selectionMode)
        {
            DocumentView.Selection.Range = new(originalRange.Start, newPos.CodePointIndex);
        }
        else
        {
            DocumentView.Selection.Range = new(newPos);
        }
    }
    public void InsertNewParagraph(Paragraph paragraph)
    {
        using (DocumentView.OwnerDocument.UndoManager.OpenGroup("Insert New Paragraph"))
        {
            Type(Document.NewParagraphSeparator.ToString());
            var selection = DocumentView.Selection.Range;
            DocumentView.OwnerDocument.Paragraphs.GlobalChildrenFromCodePointIndex(DocumentView.Selection.Range.EndCaretPosition, out var parent, out var paragraphIndex, out _);
            DocumentView.OwnerDocument.UndoManager.Do(new UndoInsertParagraph(parent, paragraphIndex, paragraph));
            DocumentView.OwnerDocument.Layout.Invalidate();
            DocumentView.OwnerDocument.Layout.EnsureValid();
            DocumentView.OwnerDocument.InvokeTextChanged(new(selection.Minimum, selection.Minimum + Math.Max(selection.Normalized.Length, paragraph.CodePointLength)));
            DocumentView.OwnerDocument.RequestRedraw();
        }
    }
    /// <summary>
    /// Hit Test relative to the view
    /// </summary>
    /// <param name="pt">The hit test point relative to the view</param>
    /// <returns></returns>
    public HitTestResult HitTest(PointF pt)
        => DocumentView.OwnerDocument.Editor.HitTest(
            new(pt.X, pt.Y + DocumentView.YScroll)
        );
    public CaretInfo GetCaretInfo(CaretPosition position)
    {
        var info = DocumentView.OwnerDocument.Editor.GetCaretInfo(position);
        info.CaretRectangle = new(
            info.CaretRectangle.Left,
            info.CaretRectangle.Top - DocumentView.YScroll,
            info.CaretRectangle.Right,
            info.CaretRectangle.Bottom - DocumentView.YScroll
        );
        return info;
    }
}
public enum Direction
{
    Left,
    Right,
    Up,
    Down
}
public enum SpacialCaretMovement
{
    Home,
    LineHome = Home,
    End,
    LineEnd = End,
    CtrlHome,
    DocumentHome = CtrlHome,
    CtrlEnd,
    DocumentEnd = CtrlEnd,
    PageUp,
    PageDown
}
static partial class Extension
{
    public static CaretPosition GetMinimumCaretPosition(this TextRange r)
    {
        if (r.End > r.Start) r = r.Reversed;
        return r.EndCaretPosition;
    }
    public static CaretPosition GetMaximumCaretPosition(this TextRange r)
    {
        if (r.End < r.Start) r = r.Reversed;
        return r.EndCaretPosition;
    }
    public static TextRange CreateRangeTo(this CaretPosition a, CaretPosition b)
    {
        return new(a.CodePointIndex, b.CodePointIndex, b.AltPosition);
    }
}