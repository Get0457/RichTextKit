using System.Collections;
using System.Drawing;
using Get.RichTextKit.Utils;
using Get.RichTextKit.Styles;
using Get.RichTextKit.Editor.DataStructure.Table;
using System.Diagnostics;

namespace Get.RichTextKit.Editor.Paragraphs.Panel;
[DebuggerDisplay("Table Paragraph ({Rows.Count} Rows x {Columns.Count} Columns)")]
public partial class TableParagraph : PanelParagraph, ITable<Paragraph>
{
    public TableRowManager<Paragraph> Rows { get; }
    public TableColumnManager<Paragraph> Columns { get; }

    public Paragraph this[int row, int col]
    {
        get
        {
            return Children[ResolveIndex(row, col)];
        }
        set
        {
            Children[ResolveIndex(row, col)] = value;
            Owner?.Layout.Invalidate();
        }
    }
    (int row, int col) ResolveIndex(int index)
    {
        var row = index / _columnCount;
        var col = index - row * _columnCount;
        return (row, col);
    }
    int ResolveIndex(int row, int col)
    {
        EnsureInBounds(row, col);
        return ResolveIndexUnchekced(row, col);
    }
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

    void ITable<Paragraph>.InsertRow(int rowIndex, IReadOnlyList<Paragraph> item, TableLength length)
    {
        EnsureInBoundsP1(row: rowIndex);
        if (item.Count != _columnCount)
            throw new ArgumentException(nameof(item));
        if (item is ITableOwner<Paragraph> to && to.Owner == Owner)
        {
            throw new ArgumentException("Cannot add an instance of list that reference current table. Please clone the list first before adding", nameof(item));
        }
        _rowCount++;
        var idxOffset = ResolveIndexUnchekced(rowIndex, 0);
        foreach (var i in .._columnCount)
        {
            Children.Insert(i + idxOffset, item[i]);
        }
        ColumnLengths.Insert(rowIndex, length);
        Owner?.Layout.Invalidate();
    }

    void ITable<Paragraph>.InsertColumn(int colIndex, IReadOnlyList<Paragraph> item, TableLength length)
    {
        EnsureInBoundsP1(col: colIndex);
        if (item.Count != _rowCount)
            throw new ArgumentException(nameof(item));
        if (item is ITableOwner<Paragraph> to && to.Owner == Owner)
        {
            throw new ArgumentException("Cannot add an instance of list that reference current table. Please clone the list first before adding", nameof(item));
        }
        _columnCount++;
        foreach (var i in .._columnCount)
        {
            Children.Insert(ResolveIndexUnchekced(i, colIndex), item[i]);
        }
        ColumnLengths.Insert(colIndex, length);
        Owner?.Layout.Invalidate();
    }

    void ITable<Paragraph>.Clear()
    {
        _rowCount = 0;
        _columnCount = 0;
        Children.Clear();
    }

    TableLength ITable<Paragraph>.GetTableLengthOfRow(int rowIndex)
        => RowLengths[rowIndex];

    TableLength ITable<Paragraph>.GetTableLengthOfColumn(int colIndex)
        => ColumnLengths[colIndex];

    void ITable<Paragraph>.SetTableLengthOfRow(int rowIndex, TableLength length)
    {
        RowLengths[rowIndex] = length;
        Owner?.Layout.Invalidate();
    }

    void ITable<Paragraph>.SetTableLengthOfColumn(int colIndex, TableLength length)
    {
        ColumnLengths[colIndex] = length;
        Owner?.Layout.Invalidate();
    }

    readonly List<TableLength> RowLengths = new(), ColumnLengths = new();

    int _rowCount, _columnCount;
    int ITable<Paragraph>.RowCount { get => _rowCount; }
    int ITable<Paragraph>.ColumnCount { get => _columnCount; }

}