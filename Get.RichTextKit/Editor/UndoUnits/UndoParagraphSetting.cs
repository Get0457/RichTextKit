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
        var bfsRuns = context.Paragraphs.GetBFSInteractingRunsRecursive(range);
        SetStyles(bfsRuns);
        context.RequestRedraw();
    }
    void SetStyles(IEnumerable<SubRunBFSInfo> bfsRuns)
    {
        foreach (var run in bfsRuns)
        {
            Debug.WriteLine(run.SubRunInfo);
            if (!ConfirmSetStyle(run.SubRunInfo.Paragraph))
                SetStyles(run.NextLevelInfo);
        }
    }
    public override void Redo(Document context)
    {
        base.Redo(context);
        NotifyInfo(new(NewSelection: range));
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