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
    [Property(OnChanged = nameof(InvokeLayoutChanged))]
    float _TableDefaultRatioHeightSize = 100;
    TableLayoutInfo _layoutInfo;
    public TableLayoutInfo GetCurrentLayoutInfo()
    {
        Owner?.Layout.EnsureValid();
        return _layoutInfo.Copy();
    }
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
                    child.ParentInfo = new(this, ResolveIndexUnchekced(rowIdx, colIdx));
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
    public record struct TableLayoutInfo(float[] RowsPos, float[] RowsHeight, float[] ColumnsPos, float[] ColumnsWidth)
    {
        public TableLayoutInfo Copy()
        {
            return new(
                (float[])RowsPos.Clone(),
                (float[])RowsHeight.Clone(),
                (float[])ColumnsPos.Clone(),
                (float[])ColumnsWidth.Clone()
            );
        }
    }
    protected override float ContentWidthOverride
        => _layoutInfo.ColumnsPos[^1] + _layoutInfo.ColumnsWidth[^1];

    protected override float ContentHeightOverride
        => _layoutInfo.RowsPos[^1] + _layoutInfo.RowsHeight[^1];
}