using System.Collections;
using System.Diagnostics;

namespace Get.RichTextKit.Editor.DataStructure.Table;
[DebuggerDisplay("Count = {Count}")]
public class TableRowManager<T> : IReadOnlyList<TableRow<T>>, ITableOwner<T>
{
    public ITable<T> Owner { get; }
    internal TableRowManager(ITable<T> instance)
    {
        Owner = instance;
    }
    public static TableRowManager<T> ForTable(ITable<T> table) => new(table);
    public TableRow<T> this[int index] { get => new(Owner, index); set => this[index].Set(value); }
    public int Count => Owner.RowCount;

    public void Add(IReadOnlyList<T> item, TableLength length) => Insert(Count, item, length);

    public void Insert(int index, IReadOnlyList<T> item, TableLength length) => Owner.InsertRow(index, item, length);
    public IEnumerable<(int RowIndex, TableRow<T> Row)> WithRowIndex()
    {
        foreach (var i in ..Count)
        {
            yield return (i, this[i]);
        }
    }
    public IEnumerator<TableRow<T>> GetEnumerator()
    {
        foreach (var i in ..Count)
        {
            yield return this[i];
        }
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}