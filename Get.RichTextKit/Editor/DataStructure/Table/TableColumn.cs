using System.Collections;
using System.Diagnostics;

namespace Get.RichTextKit.Editor.DataStructure.Table;
[DebuggerDisplay("Column (Count = {Count})")]
public readonly struct TableColumn<T> : IReadOnlyList<T>, ITableOwner<T>
{
    internal TableColumn(ITable<T> instance, int rowIndex)
    {
        Owner = instance;
        Index = rowIndex;
    }
    public ITable<T> Owner { get; }
    public int Index { get; }

    public int Count => Owner.RowCount;

    public TableLength Width { get => Owner.GetTableLengthOfColumn(Index); set => Owner.SetTableLengthOfColumn(Index, value); }

    public T this[int rowIndex]
    {
        get => Owner[rowIndex, Index];
        set => Owner[rowIndex, Index] = value;
    }

    public IEnumerator<T> GetEnumerator()
    {
        foreach (var i in ..Count)
        {
            yield return this[i];
        }
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    internal void Set(IReadOnlyList<T> col)
    {
        if (Count == col.Count)
        {
            foreach (var i in ..Count)
            {
                this[i] = col[i];
            }
        }
    }
    public IEnumerable<(int RowIndex, T RowElement)> WithRowIndex()
    {
        foreach (var i in ..Count)
        {
            yield return (i, this[i]);
        }
    }
}