using System.Collections;

namespace Get.RichTextKit.Editor.DataStructure.Table;

public class TableColumnManager<T> : IReadOnlyList<TableColumn<T>>, ITableOwner<T>
{
    public ITable<T> Owner { get; }
    internal TableColumnManager(ITable<T> instance)
    {
        Owner = instance;
    }
    public static TableColumnManager<T> ForTable(ITable<T> table) => new(table);
    public TableColumn<T> this[int index] { get => new(Owner, index); set => this[index].Set(value); }
    public int Count => Owner.ColumnCount;

    public void Add(IReadOnlyList<T> item, TableLength length) => Insert(Count, item, length);

    public void Insert(int index, IReadOnlyList<T> item, TableLength length)
        => Owner.InsertColumn(index, item, length);
    public IEnumerable<(int ColumnIndex, TableColumn<T> Column)> WithColumnIndex()
    {
        foreach (var i in ..Count)
        {
            yield return (i, this[i]);
        }
    }
    public IEnumerator<TableColumn<T>> GetEnumerator()
    {
        foreach (var i in ..Count)
        {
            yield return this[i];
        }
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}