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

namespace Get.RichTextKit.Editor.Paragraphs.Panel;

public partial class TableParagraph : PanelParagraph, ITable<Paragraph>
{
    public const float AutoMinCellHeight = 30;
    public const float AutoMinCellWidth = 30;
    record struct TableLayoutInfo(float[] RowsPos, float[] RowsHeight, float[] ColumnsPos, float[] ColumnsWidth)
    {

    }
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
    [Property(OnChanged = nameof(InvokeLayoutChanged))]
    float _TableDefaultRatioHeightSize = 100;
    void InvokeLayoutChanged() => Owner?.Layout.Invalidate();
    TableLayoutInfo _layoutInfo;
    protected override void LayoutOverride(LayoutParentInfo owner)
    {
        var parentInfo = new LayoutParentInfo(default, owner.LineWrap, owner.LineNumberMode);
        int cpiOffset = 0;
        int[] DisplayLineOffset = new int[_columnCount];
        int lineOffset = 0;
        var colsRequestedWidth = new float?[_columnCount];
        var rowsRequestedHeight = new float?[_rowCount];
        float remainingWidth = owner.AvaliableWidth;
        foreach (var (colIdx, col) in Columns.WithColumnIndex())
        {
            if (col.Width.IsAutoMode)
            {
                foreach (var child in col)
                {
                    child.Layout(parentInfo with { AvaliableWidth = remainingWidth });
                    SetIfGreater(ref colsRequestedWidth[colIdx], Math.Max(child.ContentWidth, AutoMinCellWidth));
                }
                remainingWidth -= SetIfGreater(ref colsRequestedWidth[colIdx], AutoMinCellWidth);
            }
        }
        var (ColumnsPos, ColumnsWidth) = CalculateColumnInfo(owner.AvaliableWidth, colsRequestedWidth);

        foreach (var (rowIdx, row) in Rows.WithRowIndex())
        {
            if (row.Height.IsAutoMode)
            {
                foreach (var (colIdx, child) in row.WithColumnIndex())
                {
                    // Skips layout for those columns we already layout
                    if (!colsRequestedWidth[colIdx].HasValue)
                        child.Layout(parentInfo with { AvaliableWidth = ColumnsWidth[colIdx] });

                    SetIfGreater(ref rowsRequestedHeight[rowIdx], Math.Max(child.ContentHeight, AutoMinCellHeight));
                }
                SetIfGreater(ref rowsRequestedHeight[rowIdx], AutoMinCellHeight);
            }
        }
        var (RowsPos, RowsHeight) = CalculateRowInfo(rowsRequestedHeight);
        _layoutInfo = new(RowsPos, RowsHeight, ColumnsPos, ColumnsWidth);

        foreach (var (rowIdx, row) in Rows.WithRowIndex())
        {
            foreach (var (colIdx, child) in row.WithColumnIndex())
            {
                // Skips layout for those rows and columns we already layout
                if (!(colsRequestedWidth[colIdx].HasValue || rowsRequestedHeight[rowIdx].HasValue))
                    child.Layout(parentInfo with { AvaliableWidth = ColumnsWidth[colIdx] });

                child.LocalInfo = new(
                    ContentPosition: OffsetMargin(new(ColumnsPos[colIdx], RowsPos[rowIdx]), child.Margin),
                    CodePointIndex: cpiOffset,
                    DisplayLineIndex: DisplayLineOffset[colIdx],
                    LineIndex: lineOffset
                );
                cpiOffset += child.CodePointLength;
                lineOffset += child.LineCount;
                DisplayLineOffset[colIdx] += child.DisplayLineCount;
            }
        }
    }
    static float SetIfGreater(ref float? f1, float f2)
    {
        if (!f1.HasValue)
        {
            f1 = f2;
            return f1.Value;
        }
        if (f2 > f1) f1 = f2;
        return f1.Value;
    }
    (float[] ColumnsPos, float[] ColumnsWidth) CalculateColumnInfo(float avaliableWidth, float?[] requestedWidth)
    {
        var totalXLenth = Columns.Select(x => x.Width.IsPixelMode ? x.Width.Length : 0).Sum();
        var ratioXUnit = Math.Max(0, avaliableWidth - requestedWidth.Sum(x => x ?? 0) - totalXLenth) / Columns.Select(x => x.Width.IsRatioMode ? x.Width.Length : 0).Sum();
        float[] ColumnsPos = new float[_columnCount];
        float[] ColumnsWidth = new float[_columnCount];
        float XOffset = 0;
        foreach (int i in .._columnCount)
        {
            ColumnsPos[i] = XOffset;
            var width = Columns[i].Width;
            if (width.IsAutoMode)
                XOffset += ColumnsWidth[i] = requestedWidth[i] ?? AutoMinCellWidth;
            else if (width.IsRatioMode)
                XOffset += ColumnsWidth[i] = ratioXUnit * width.Length;
            else if (width.IsPixelMode)
                XOffset += ColumnsWidth[i] = width.Length;
            else
                throw new ArgumentOutOfRangeException(nameof(width.Mode));
        }
        return (ColumnsPos, ColumnsWidth);
    }
    (float[] RowsPos, float[] RowsWidth) CalculateRowInfo(float?[] requestedHeight)
    {
        var totalXLenth = Columns.Select(x => x.Width.IsPixelMode ? x.Width.Length : 0).Sum();
        var ratioYUnit = _TableDefaultRatioHeightSize;
        float[] RowsPos = new float[_rowCount];
        float[] RowsWidth = new float[_rowCount];
        float YOffset = 0;
        foreach (int i in .._rowCount)
        {
            RowsPos[i] = YOffset;
            var height = Rows[i].Height;
            if (height.IsAutoMode)
                YOffset += RowsWidth[i] = requestedHeight[i] ?? AutoMinCellHeight;
            else if (height.IsRatioMode)
                YOffset += RowsWidth[i] = ratioYUnit * height.Length;
            else if (height.IsPixelMode)
                YOffset += RowsWidth[i] = height.Length;
            else
                throw new ArgumentOutOfRangeException(nameof(height.Mode));
        }
        return (RowsPos, RowsWidth);
    }
    public override int DisplayLineCount => 1;

    public override void DeletePartial(UndoManager<Document, DocumentViewUpdateInfo> UndoManager, SubRunInfo range)
    {

    }
    public override bool TryJoin(UndoManager<Document, DocumentViewUpdateInfo> UndoManager, int thisIndex)
    {
        return false;
    }
    public override Paragraph Split(UndoManager<Document, DocumentViewUpdateInfo> UndoManager, int splitIndex)
    {
        return null!;
    }

    protected override float ContentWidthOverride
        => _layoutInfo.ColumnsPos[^1] + _layoutInfo.ColumnsWidth[^1];

    protected override float ContentHeightOverride
        => _layoutInfo.RowsPos[^1] + _layoutInfo.RowsHeight[^1];
    public override LineInfo GetLineInfo(int line)
    {
        var idx = LocalChildrenFromLineIndexAsIndex(line, out var newLineIdx);
        var para = Children[idx];
        var info = para.GetLineInfo(newLineIdx);
        var (row, col) = ResolveIndex(idx);
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
    int GetColIndex(float x)
    {
        var colIndex = BinarySearch(0, _columnCount, (idx) =>
        {
            if (_layoutInfo.ColumnsPos[idx] >= x)
                return 1;
            if (_layoutInfo.ColumnsPos[idx] + _layoutInfo.ColumnsWidth[idx] < x)
                return -1;
            return 0;
        });
        if (colIndex < 0) colIndex = _columnCount - 1;
        return colIndex;
    }
    int GetRowIndex(float y)
    {
        var rowIndex = BinarySearch(0, _rowCount, (idx) =>
        {
            if (_layoutInfo.RowsPos[idx] >= y)
                return 1;
            if (_layoutInfo.RowsPos[idx] + _layoutInfo.RowsHeight[idx] < y)
                return -1;
            return 0;
        });
        if (rowIndex < 0) rowIndex = _rowCount - 1;
        return rowIndex;
    }
    public override Paragraph GetParagraphAt(PointF pt)
    {
        int colIndex = GetColIndex(pt.X);
        int rowIndex = GetRowIndex(pt.Y);
        return this[rowIndex, colIndex];
    }
    /// Based on <see cref="BinarySearchExtension.BinarySearch{T, U}(IReadOnlyList{T}, int, int, U, Func{T, U, int})"/>
    static int BinarySearch(int index, int length, Func<int, int> compare)
    {
        int lo = index;
        int hi = index + length - 1;
        while (lo <= hi)
        {
            int i = BinarySearchExtension.GetMedian(lo, hi);
            int c = compare(i);
            if (c == 0) return i;
            if (c < 0)
            {
                lo = i + 1;
            }
            else
            {
                hi = i - 1;
            }
        }
        return ~lo;
    }
    public override void Paint(SKCanvas canvas, PaintOptions options)
    {
        var width = ContentWidth;
        var height = ContentHeight;
        if (options.TextPaintOptions.Selection.HasValue)
        {
            var selection = options.TextPaintOptions.Selection.Value;
            var altPos = selection.AltPosition;
            selection = new(Math.Max(selection.Minimum, 0), Math.Min(selection.Maximum + (altPos ? 0 : 1), Math.Max(0, CodePointLength)), true);
            var idx1 = LocalChildrenFromCodePointIndexAsIndex(selection.StartCaretPosition, out _);
            var idx2 = LocalChildrenFromCodePointIndexAsIndex(selection.EndCaretPosition, out _);

            if (idx1 != idx2)
            {
                var (r1, c1) = ResolveIndex(idx1);
                var (r2, c2) = ResolveIndex(idx2);
                options.TextPaintOptions = options.TextPaintOptions.Clone();
                options.TextPaintOptions.Selection = null;
                canvas.Save();
                canvas.Translate(DrawingContentPosition.X, DrawingContentPosition.Y);
                using SKPaint selectionPaint = new() { Color = options.TextPaintOptions.SelectionColor };
                canvas.DrawRect(
                    x: _layoutInfo.ColumnsPos[c1],
                    y: _layoutInfo.RowsPos[r1],
                    w: _layoutInfo.ColumnsPos[c2] - _layoutInfo.ColumnsPos[c1] + _layoutInfo.ColumnsWidth[c2],
                    h: _layoutInfo.RowsPos[r2] - _layoutInfo.RowsPos[r1] + _layoutInfo.RowsHeight[r2],
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
    public override bool IsChildrenReadOnly => true;
    public override SelectionInfo GetSelectionInfo(ParentInfo parentInfo, TextRange selection)
    {
        if (IsRangeWithinTheSameChildParagraph(selection, out _, out _))
            return base.GetSelectionInfo(parentInfo, selection);
        else
        {
            var p1 = LocalChildrenFromCodePointIndex(selection.MinimumCaretPosition, out _);
            var p2 = LocalChildrenFromCodePointIndex(selection.MaximumCaretPosition, out _);
            TextRange newSelection = new(p1.LocalInfo.CodePointIndex, p2.LocalInfo.CodePointIndex + p2.CodePointLength, true);
            return base.GetSelectionInfo(parentInfo, newSelection) with { OriginalRange = selection };
        }
    }
    protected override IEnumerable<SubRun> GetLocalChildrenInteractingRange(TextRange selection)
    {
        if (IsRangeWithinTheSameChildParagraph(selection, out _, out _))
            foreach (var lcir in base.GetLocalChildrenInteractingRange(selection))
                yield return lcir;
        else
        {
            var (row1, col1) = ResolveIndex(LocalChildrenFromCodePointIndexAsIndex(selection.MinimumCaretPosition, out _));
            var (row2, col2) = ResolveIndex(LocalChildrenFromCodePointIndexAsIndex(selection.MaximumCaretPosition, out _)); 
            foreach (int row in (row1..row2).IncludeEnd())
            {
                foreach (int col in (col1..col2).IncludeEnd())
                {
                    var idx = ResolveIndexUnchekced(row, col);
                    var para = Children[idx];
                    yield return new SubRun(idx, 0, para.CodePointLength, false);
                }
            }
        }
    }
}