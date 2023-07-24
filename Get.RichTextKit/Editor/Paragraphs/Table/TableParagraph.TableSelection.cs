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
    readonly record struct TableSelection(TableParagraph Owner, TableIndex Start, TableIndex End)
    {
        public static TableSelection FromRange(TableParagraph tableRef, TextRange range)
        {
            range = range.Clamp(tableRef.EndCaretPosition.CodePointIndex);
            var idx1 = TableIndex.FromCaretPosition(tableRef, range.Start < 0 ? tableRef.StartCaretPosition : range.StartCaretPosition);
            var idx2 = TableIndex.FromCaretPosition(tableRef, range.End >= tableRef.CodePointLength ? tableRef.EndCaretPosition : range.EndCaretPosition);
            return new(tableRef, idx1, idx2);
        }
        public bool IsValid => Start != End;
        public TableIndex Minimum => new(Owner, Math.Min(Start.Row, End.Row), Math.Min(Start.Column, End.Column));
        public TableIndex Maximum => new(Owner, Math.Max(Start.Row, End.Row), Math.Max(Start.Column, End.Column));
        public TextRange TextRange
        {
            get
            {
                var p1Idx = Start.ActualIndex;
                var p2Idx = End.ActualIndex;
                var p1 = Owner.Children[p1Idx];
                var p2 = Owner.Children[p2Idx];
                if (p1Idx > p2Idx)
                    return new(
                        p1.LocalInfo.OffsetFromThis(p1.EndCaretPosition).CodePointIndex,
                        p2.LocalInfo.OffsetFromThis(p2.StartCaretPosition).CodePointIndex,
                        false
                    );
                else
                    return new(
                        p1.LocalInfo.OffsetFromThis(p1.StartCaretPosition).CodePointIndex,
                        p2.LocalInfo.OffsetFromThis(p2.EndCaretPosition).CodePointIndex,
                        false
                    );
            }
        }
        public RectangleF Bounds
        {
            get
            {
                // Cache the values
                var Minimum = this.Minimum;
                var Maximum = this.Maximum;
                return new(
                    x: Owner._layoutInfo.ColumnsPos[Minimum.Column],
                    y: Owner._layoutInfo.RowsPos[Minimum.Row],
                    width: Owner._layoutInfo.ColumnsPos[Maximum.Column] - Owner._layoutInfo.ColumnsPos[Minimum.Column] + Owner._layoutInfo.ColumnsWidth[Maximum.Column],
                    height: Owner._layoutInfo.RowsPos[Maximum.Row] - Owner._layoutInfo.RowsPos[Minimum.Row] + Owner._layoutInfo.RowsHeight[Maximum.Row]
                );
            }
        }
    }
}