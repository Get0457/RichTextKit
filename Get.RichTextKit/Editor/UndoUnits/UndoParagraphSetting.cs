using Get.RichTextKit.Utils;
using Get.RichTextKit;
using System.Diagnostics;
using System.Xml.Linq;
using Get.RichTextKit.Editor.Paragraphs;
using Get.RichTextKit.Editor.Paragraphs.Panel;
using System.Runtime.InteropServices.ComTypes;
using Get.RichTextKit.Editor.DocumentView;

namespace Get.RichTextKit.Editor;
public class UndoParagraphSetting<T> : UndoUnit<Document, DocumentViewUpdateInfo>
{
    TextRange range;
    Func<Paragraph, T> Getter;
    Func<Paragraph, T, bool> Setter;
    T NewValue;
    public UndoParagraphSetting(TextRange range, T newValue, Func<Paragraph, T> Getter, Func<Paragraph, T, bool> Setter)
    {
        this.range = range.Normalized;
        this.Getter = Getter;
        this.Setter = Setter;
        NewValue = newValue;
    }
    List<(Paragraph, T)>? SavedValue;
    public override void Do(Document context)
    {
        SavedValue = new();
        SetStyles(context.Paragraphs, range);
        context.RequestRedraw();
    }
    public override void Redo(Document context)
    {
        base.Redo(context);
        NotifyInfo(new(NewSelection: range));
    }
    bool SetStyles(Paragraph para, TextRange range)
    {
        if (para is IParagraphCollection collection)
        {
            return SetStyles(collection, range);
        }
        else return ConfirmSetStyle(para);
    }
    bool SetStyles(IParagraphCollection parent, TextRange range)
    {
        if (parent is Paragraph para && range.Start <= 0 && range.End >= para.CodePointLength)
        {
            if (ConfirmSetStyle(para)) return true;
        }
        var idx1 = PanelParagraph.LocalChildrenFromCodePointIndexAsIndex(parent.Paragraphs.AsReadOnly(), range.StartCaretPosition, out int cpi1);
        var idx2 = PanelParagraph.LocalChildrenFromCodePointIndexAsIndex(parent.Paragraphs.AsReadOnly(), range.EndCaretPosition, out int cpi2);
        bool success = false;
        success = SetStyles(parent.Paragraphs[idx1], new(cpi1, parent.Paragraphs[idx1].CodePointLength)) || success;
        for (int i = idx1 + 1; i < idx2; i++)
        {
            success = SetStyles(parent.Paragraphs[i], new(0, parent.Paragraphs[i].CodePointLength)) || success;
        }
        success = SetStyles(parent.Paragraphs[idx2], new(0, cpi2)) || success;
        if (success) return true;

        if (parent is Paragraph para2)
            return ConfirmSetStyle(para2);
        return false;
    }
    bool ConfirmSetStyle(Paragraph para)
    {
        var val = Getter(para);
        if (Setter(para, NewValue))
        {
            SavedValue.Add((para, val));
            return true;
        }
        return false;
    }

    public override void Undo(Document context)
    {
        foreach (var (para, val) in SavedValue!)
        {
            Setter.Invoke(para, val);
        }
        SavedValue!.Clear();
        SavedValue = null;
        NotifyInfo(new(NewSelection: range));
        context.RequestRedraw();
    }
}