using Get.RichTextKit.Editor.Paragraphs.Panel;
using Get.RichTextKit.Editor.Structs;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Get.RichTextKit.Editor.Paragraphs;

partial class Paragraph
{
    /// <summary>
    /// Calculate the selection range from the navigation
    /// </summary>
    /// <param name="selection">The current selection</param>
    /// <param name="snap">The place to snap the new selection</param>
    /// <param name="direction">The direction to navigate</param>
    /// <param name="keepSelection">Whether to keep the selection</param>
    /// <param name="ghostXCoord">The ghost X Coordinate for up/down navigation</param>
    /// <param name="newSelection">The new selection; The value is only valid if the function reports success</param>
    /// <returns>The navigation status</returns>
    /// <remarks>
    /// newSelection is only valid if the navigation status is successful
    /// </remarks>
    public NavigationStatus Navigate(TextRange selection, NavigationSnap snap, NavigationDirection direction, bool keepSelection, ref float? ghostXCoord, out TextRange newSelection)
    {
        return NavigateOverride(selection, snap, direction, keepSelection, ref ghostXCoord, out newSelection);
    }
    protected abstract NavigationStatus NavigateOverride(TextRange selection, NavigationSnap snap, NavigationDirection direction, bool keepSelection, ref float? ghostXCoord, out TextRange newSelection);
    protected NavigationStatus VerticalNavigateUsingLineInfo(TextRange selection, NavigationSnap snap, NavigationDirection direction, bool keepSelection, ref float? ghostXCoord, out TextRange newSelection)
    {
        if (direction is not (NavigationDirection.Up or NavigationDirection.Down))
        {
            throw new ArgumentOutOfRangeException(nameof(direction));
        }

        // Get the line number the caret is on
        var ci = GetCaretInfo(new CaretPosition(selection.End, selection.AltPosition));

        // Resolve the xcoord
        ghostXCoord ??= ci.CaretXCoord + GlobalInfo.ContentPosition.X;

        // Work out which line to hit test
        var lineInfo = GetLineInfo(ci.LineIndex);
        var toLine = direction is NavigationDirection.Down ? lineInfo.NextLine : lineInfo.PrevLine;

        // Exceed paragraph?
        if (toLine is null)
        {
            newSelection = default;
            if (direction is NavigationDirection.Up)
                return NavigationStatus.MoveBefore;
            else
                return NavigationStatus.MoveAfter;
        }


        // Hit test the line
        var htr = HitTestLine(toLine.Value, ghostXCoord.Value - GlobalInfo.ContentPosition.X);
        selection.EndCaretPosition = new CaretPosition(htr.ClosestCodePointIndex, htr.AltCaretPosition);
        if (!keepSelection)
            selection.Start = selection.End;
        newSelection = selection;
        return NavigationStatus.Success;
    }
}
