namespace Get.RichTextKit.Editor.DataStructure.Table;

public partial class TableController<T>
{
    public T this[int rowIndex, int colIndex] { get => Target[rowIndex, colIndex]; set => Target[rowIndex, colIndex] = value; }
    public TableRow<T> this[int rowIndex] { get => Rows[rowIndex]; set => Rows[rowIndex] = value; }
    public TableRowManager<T> Rows { get; }
    public TableRowManager<T> Columns { get; }
    public partial void Clear();
}

partial class TableController<T> : ITable<T>, ITableOwner<T>
{
    readonly ITable<T> Target;
    internal TableController(ITable<T> target)
    {
        Target = target;
        Rows = new(this);
        Columns = new(this);
    }
    ITable<T> ITableOwner<T>.Owner => Target;

    int ITable<T>.RowCount => Target.RowCount;

    int ITable<T>.ColumnCount => Target.ColumnCount;

    TableLength ITable<T>.GetTableLengthOfColumn(int colIndex)
        => Target.GetTableLengthOfColumn(colIndex);

    TableLength ITable<T>.GetTableLengthOfRow(int colIndex)
        => Target.GetTableLengthOfRow(colIndex);

    void ITable<T>.InsertColumn(int colIndex, IReadOnlyList<T> item, TableLength length)
        => Target.InsertColumn(colIndex, item, length);

    void ITable<T>.InsertRow(int rowIndex, IReadOnlyList<T> item, TableLength length)
        => Target.InsertRow(rowIndex, item, length);

    void ITable<T>.SetTableLengthOfColumn(int colIndex, TableLength length)
        => Target.SetTableLengthOfColumn(colIndex, length);

    void ITable<T>.SetTableLengthOfRow(int rowIndex, TableLength length)
        => Target.SetTableLengthOfRow(rowIndex, length);
    public partial void Clear() => Target.Clear();
}