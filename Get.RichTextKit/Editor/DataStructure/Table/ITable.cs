using Get.RichTextKit.Editor.Paragraphs.Panel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Get.RichTextKit.Editor.DataStructure.Table;

public interface ITable<T>
{
    int RowCount { get; }
    int ColumnCount { get; }
    T this[int rowIndex, int colIndex] { get; set; }
    void InsertRow(int rowIndex, IReadOnlyList<T> item, TableLength length);
    void InsertColumn(int colIndex, IReadOnlyList<T> item, TableLength length);
    TableLength GetTableLengthOfRow(int rowIndex);
    TableLength GetTableLengthOfColumn(int colIndex);
    void SetTableLengthOfRow(int rowIndex, TableLength length);
    void SetTableLengthOfColumn(int colIndex, TableLength length);
    void Clear();
}