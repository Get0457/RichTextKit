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
    readonly record struct TableIndex(TableParagraph Owner, int Row, int Column)
    {
        public Paragraph Paragraph
        {
            get => Owner.Children[ActualIndex];
            set => Owner.Children[ActualIndex] = value;
        }
        public int ActualIndex
        {
            get
            {
                Owner.EnsureInBounds(Row, Column);
                return Owner.ResolveIndexUnchekced(Row, Column);
            }
        }
        public RectangleF Bounds
        {
            get
            {
                return new(
                    x: Owner._layoutInfo.ColumnsPos[Column],
                    y: Owner._layoutInfo.RowsPos[Row],
                    width: Owner._layoutInfo.ColumnsWidth[Column],
                    height: Owner._layoutInfo.RowsHeight[Row]
                );
            }
        }
        public void Deconstruct(out int row, out int col)
        {
            row = Row;
            col = Column;
        }
        public static TableIndex FromCaretPosition(TableParagraph tableRef, CaretPosition caretPosition)
        {
            return FromActualIndex(tableRef, tableRef.LocalChildrenFromCodePointIndexAsIndex(caretPosition, out _));
        }
        public static TableIndex FromActualIndex(TableParagraph tableRef, int actualIndex)
        {
            var row = actualIndex / tableRef._columnCount;
            var col = actualIndex - row * tableRef._columnCount;
            return new(tableRef, row, col);
        }
    }
}