using Get.RichTextKit.Utils;
using Get.RichTextKit;
using System.Diagnostics;
using System.Xml.Linq;
using Get.RichTextKit.Editor.Paragraphs;
namespace Get.RichTextKit.Editor;
public class UndoParagraphSetting<T> : UndoUnit<Document>
{
    TextRange range;
    Func<Paragraph, T> Getter;
    Action<Paragraph, T> Setter;
    T NewValue;
    public UndoParagraphSetting(TextRange range, T newValue, Func<Paragraph, T> Getter, Action<Paragraph, T> Setter)
    {
        this.range = range;
        this.Getter = Getter;
        this.Setter = Setter;
        NewValue = newValue;
    }
    List<T>? SavedValue;
    public override void Do(Document context)
    {
        SavedValue = new();
        foreach (var para in context.Paragraphs.GetInterectingParagraphs(range))
        {
            SavedValue.Add(Getter.Invoke(para));
            Setter.Invoke(para, NewValue);
        }
        context.RequestRedraw();
    }

    public override void Undo(Document context)
    {
        foreach (var (para, val) in context.Paragraphs.GetInterectingParagraphs(range).Zip(SavedValue!, (x, y) => (x, y)))
        {
            Setter.Invoke(para, val);
        }
        SavedValue!.Clear();
        SavedValue = null;
        context.RequestRedraw();
    }
}