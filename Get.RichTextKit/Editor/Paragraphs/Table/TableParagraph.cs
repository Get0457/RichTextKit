using System.Collections;
using System.Drawing;
using Get.RichTextKit.Utils;
using Get.RichTextKit.Styles;
using Get.RichTextKit.Editor.DataStructure.Table;
using Get.EasyCSharp;
using Get.RichTextKit.Editor.Structs;
using System.ComponentModel.Design;
using SkiaSharp;
using Get.RichTextKit.Editor.DocumentView;
using System.IO;
using System.Net.Mail;
using System.Reflection;

namespace Get.RichTextKit.Editor.Paragraphs.Panel;

public partial class TableParagraph : PanelParagraph, ITable<Paragraph>
{
    public const float AutoMinCellHeight = 30;
    public const float AutoMinCellWidth = 30;
    public TableParagraph(IStyle style, int initialRows, int initialCols) : base(style)
    {
        _rowCount = initialRows;
        _columnCount = initialCols;
        foreach (var _ in ..(initialRows * initialCols))
        {
            Children.Add(new VerticalParagraph(style) { Margin = new(10) });
        }
        foreach (var _ in ..initialRows)
        {
            RowLengths.Add(TableLength.Auto);
        }
        foreach (var _ in ..initialCols)
        {
            ColumnLengths.Add(TableLength.OneRatio);
        }
        Rows = new(this);
        Columns = new(this);
    }
    public override int DisplayLineCount => 1;

    public override bool TryJoinWithNextParagraph(UndoManager<Document, DocumentViewUpdateInfo> UndoManager)
    {
        return false;
    }
    public override Paragraph Split(UndoManager<Document, DocumentViewUpdateInfo> UndoManager, int splitIndex)
    {
        return null!;
    }

    public override LineInfo GetLineInfo(int line)
    {
        var idx = LocalChildrenFromLineIndexAsIndex(line, out var newLineIdx);
        var para = Children[idx];
        var info = para.GetLineInfo(newLineIdx);
        var (row, col) = TableIndex.FromActualIndex(this, idx);
        para.LocalInfo.OffsetFromThis(ref info);
        if (info.PrevLine is null && row > 0)
        {
            var newPara = this[row - 1, col];
            info.PrevLine = newPara.LocalInfo.LineIndex + (newPara.LineCount - 1);
        }
        if (info.NextLine is null && row + 1 < _rowCount)
        {
            var newPara = this[row + 1, col];
            info.NextLine = newPara.LocalInfo.LineIndex;
        }
        return info;
    }
    public override HitTestResult HitTestLine(int lineIndex, float x)
    {
        if (lineIndex > 0 && lineIndex < LineCount - 1) return base.HitTestLine(lineIndex, x);
        var para = this[lineIndex is 0 ? 0 : _rowCount - 1, GetColIndex(x)];
        var info = para.HitTestLine(lineIndex is 0 ? 0 : para.LineCount - 1, para.LocalInfo.OffsetXToThis(x));
        para.LocalInfo.OffsetFromThis(ref info);

        return info;
    }
    public override Paragraph GetParagraphAt(PointF pt)
    {
        int colIndex = GetColIndex(pt.X);
        int rowIndex = GetRowIndex(pt.Y);
        return this[rowIndex, colIndex];
    }
    public override void Paint(SKCanvas canvas, PaintOptions options)
    {
        var width = ContentWidth;
        var height = ContentHeight;
        if (options.TextPaintOptions.Selection.HasValue)
        {
            var selection = options.TextPaintOptions.Selection.Value;
            var sel = TableSelection.FromRange(this, selection);

            if (sel.IsValid)
            {
                options.TextPaintOptions = options.TextPaintOptions.Clone();
                options.TextPaintOptions.Selection = null;
                canvas.Save();
                canvas.Translate(DrawingContentPosition.X, DrawingContentPosition.Y);
                using SKPaint selectionPaint = new() { Color = options.TextPaintOptions.SelectionColor };
                var bounds = sel.Bounds;
                canvas.DrawRect(
                    x: bounds.X,
                    y: bounds.Y,
                    w: bounds.Width,
                    h: bounds.Height,
                    selectionPaint
                );
                canvas.Restore();
            }
        }
        base.Paint(canvas, options);
        using SKPaint paint = new() { Color = options.TextPaintOptions.TextDefaultColor };
        canvas.Save();
        canvas.Translate(DrawingContentPosition.X, DrawingContentPosition.Y);
        foreach (var rowPos in _layoutInfo.RowsPos)
        {
            canvas.DrawLine(x0: 0, x1: width, y0: rowPos, y1: rowPos, paint: paint);
        }
        var rowEnd = height;
        canvas.DrawLine(x0: 0, x1: width, y0: rowEnd, y1: rowEnd, paint: paint);
        foreach (var colPos in _layoutInfo.ColumnsPos)
        {
            canvas.DrawLine(y0: 0, y1: height, x0: colPos, x1: colPos, paint: paint);
        }
        var colEnd = width;
        canvas.DrawLine(y0: 0, y1: height, x0: colEnd, x1: colEnd, paint: paint);
        canvas.Restore();
    }
    public override SelectionInfo GetSelectionInfo(TextRange selection)
    {
        var sel = TableSelection.FromRange(this, selection);
        if (!sel.IsValid)
            return base.GetSelectionInfo(selection);
        static SKRect GetCaretBounds(bool isAtRight, TableIndex idx, out RectangleF bounds)
        {
            if (isAtRight)
                return idx.Bounds.Apply(static x => x with { Width = 0, X = x.Right }).Assign(out var endBounds).ToSKRect();
            else
                return (idx.Bounds.Assign(out bounds) with { Width = 0 }).ToSKRect();
        }
        return new SelectionInfo(
            selection,
            sel.TextRange.Assign(out var selTextRange),
            new()
            {
                CaretRectangle = GetCaretBounds(sel.Start.Column > sel.End.Column, sel.Start, out var startBounds),
                CodePointIndex = selTextRange.Start,
                LineIndex = sel.Start.Row,
                CaretXCoord = startBounds.X
            },
            new()
            {
                CaretRectangle = GetCaretBounds(sel.Start.Column <= sel.End.Column, sel.End, out var endBounds),
                CodePointIndex = selTextRange.End,
                LineIndex = sel.End.Row,
                CaretXCoord = endBounds.X
            },
            this,
            GetInteractingRuns(selTextRange),
            GetInteractingRunsRecursive(selTextRange),
            GetBFSInteractingRuns(selTextRange)
        );
    }
    protected override IEnumerable<SubRun> GetLocalChildrenInteractingRange(TextRange selection)
    {
        var sel = TableSelection.FromRange(this, selection);
        if (!sel.IsValid)
            foreach (var lcir in base.GetLocalChildrenInteractingRange(selection))
                yield return lcir;
        else
        {
            foreach (int row in (sel.Minimum.Row..sel.Maximum.Row).Iterate(endInclusive: true))
            {
                foreach (int col in (sel.Minimum.Column..sel.Maximum.Column).Iterate(endInclusive: true))
                {
                    var idx = ResolveIndexUnchekced(row, col);
                    var para = Children[idx];
                    yield return new SubRun(idx, 0, para.CodePointLength, false);
                }
            }
        }
    }
    protected override NavigationStatus NavigateOverride(TextRange selection, NavigationSnap snap, NavigationDirection direction, bool keepSelection, ref float? ghostXCoord, out TextRange newSelection)
    {
        var sel = TableSelection.FromRange(this, selection);
        if (!sel.IsValid)
            return base.NavigateOverride(selection, snap, direction, keepSelection, ref ghostXCoord, out newSelection);

        switch (direction)
        {
            case NavigationDirection.Forward:
                sel = sel with { End = sel.End with { Column = sel.End.Column + 1 } };
                break;
            case NavigationDirection.Backward:
                sel = sel with { End = sel.End with { Column = sel.End.Column - 1 } };
                break;
            case NavigationDirection.Up:
                sel = sel with { End = sel.End with { Row = sel.End.Row - 1 } };
                break;
            case NavigationDirection.Down:
                sel = sel with { End = sel.End with { Row = sel.End.Row + 1 } };
                break;
        }
        if (sel.End.Row < 0)
        {
            newSelection = default;
            return NavigationStatus.MoveBefore;
        }
        if (sel.End.Row >= _rowCount)
        {
            newSelection = default;
            return NavigationStatus.MoveAfter;
        }
        if (sel.End.Column < 0)
            sel = sel with { End = sel.End with { Column = 0 } };
        if (sel.End.Column >= _columnCount)
            sel = sel with { End = sel.End with { Column = _columnCount - 1 } };
        newSelection = sel.TextRange;
        return NavigationStatus.Success;
    }
    public override bool ShouldDeleteAll(DeleteInfo deleteInfo)
    {
        var sel = TableSelection.FromRange(this, deleteInfo.Range);
        var (r1, c1) = sel.Minimum;
        var (r2, c2) = sel.Maximum;
        return r1 is 0 && c1 is 0 && r2 == _rowCount - 1 && c2 == _columnCount - 1;
    }
}
static partial class Extension
{
    public static SKRect ToSKRect(this RectangleF rectF)
        => new(rectF.Left, rectF.Top, rectF.Right, rectF.Bottom);
    public static T2 Apply<T, T2>(this T item, Func<T, T2> func)
        => func(item);
    public static T Assign<T>(this T item, out T sameItem)
    {
        sameItem = item;
        return item;
    }
}