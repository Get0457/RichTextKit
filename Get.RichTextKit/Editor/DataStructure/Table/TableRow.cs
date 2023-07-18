using System.Collections;
using System.Diagnostics;

namespace Get.RichTextKit.Editor.DataStructure.Table;
[DebuggerDisplay("Row (Count = {Count})")]
public readonly struct TableRow<T> : IReadOnlyList<T>, ITableOwner<T>
{
    internal TableRow(ITable<T> instance, int rowIndex)
    {
        Owner = instance;
        Index = rowIndex;
    }
    public ITable<T> Owner { get; }
    public int Index { get; }

    public int Count => Owner.ColumnCount;

    public T this[int colIndex]
    {
        get => Owner[Index, colIndex];
        set => Owner[Index, colIndex] = value;
    }
    public TableLength Height { get => Owner.GetTableLengthOfRow(Index); set => Owner.SetTableLengthOfColumn(Index, value); }
    
    public IEnumerator<T> GetEnumerator()
    {
        foreach (var i in ..Count)
        {
            yield return this[i];
        }
    }
    public IEnumerable<(int ColumnIndex, T ColumnElement)> WithColumnIndex()
    {
        foreach (var i in ..Count)
        {
            yield return (i, this[i]);
        }
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    internal void Set(IReadOnlyList<T> row)
    {
        if (Count == row.Count)
        {
            foreach (var i in ..Count)
            {
                this[i] = row[i];
            }
        }
    }
}
