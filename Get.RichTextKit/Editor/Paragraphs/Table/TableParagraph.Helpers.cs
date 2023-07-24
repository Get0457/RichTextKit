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
    void InvokeLayoutChanged() => Owner?.Layout.Invalidate();
    void EnsureInBounds(int? row = null, int? col = null)
    {
        if (row.HasValue)
            if (row < 0 || row >= _rowCount)
                throw new IndexOutOfRangeException($"Row out of range (expected 0 <= row < {_rowCount}, but got {row})");
        if (col.HasValue)
            if (col < 0 || col >= _columnCount)
                throw new IndexOutOfRangeException($"Columnout of range (expected 0 <= col < {_columnCount}, but got {col})");
    }
    void EnsureInBoundsP1(int? row = null, int? col = null)
    {
        if (row.HasValue)
            if (row < 0 || row > _rowCount)
                throw new IndexOutOfRangeException($"Row out of range (expected 0 <= row < {_rowCount}, but got {row})");
        if (col.HasValue)
            if (col < 0 || col > _columnCount)
                throw new IndexOutOfRangeException($"Columnout of range (expected 0 <= col < {_columnCount}, but got {col})");
    }
    int ResolveIndexUnchekced(int row, int col)
    {
        return row * _columnCount + col;
    }
}