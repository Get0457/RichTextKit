using Get.RichTextKit.Utils;
using Get.RichTextKit;
using System.Diagnostics;
using System.Xml.Linq;
using Get.RichTextKit.Editor.Paragraphs;
using Get.RichTextKit.Editor.Paragraphs.Panel;
using System.Runtime.InteropServices.ComTypes;
using Get.RichTextKit.Editor.DocumentView;
using Get.RichTextKit.Editor.Structs;

namespace Get.RichTextKit.Editor;
public class UndoParagraphSettingGlobal<T> : UndoUnit<Document, DocumentViewUpdateInfo>
{
    TextRange range;
    Func<Paragraph, T> Getter;
    Func<Paragraph, T, bool> Setter;
    Func<T> NewValueFactory;
    public UndoParagraphSettingGlobal(TextRange range, T newValue, Func<Paragraph, T> Getter, Func<Paragraph, T, bool> Setter)
        : this(range, () => newValue, Getter, Setter)
    { }
    public UndoParagraphSettingGlobal(TextRange range, Func<T> newValue, Func<Paragraph, T> Getter, Func<Paragraph, T, bool> Setter)
    {
        this.range = range.Normalized;
        this.Getter = Getter;
        this.Setter = Setter;
        NewValueFactory = newValue;
    }
    List<(Paragraph, T)>? SavedValue;
    List<(Paragraph, T)>? SavedValueRedo;
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
            if (!ConfirmSetStyle(run.SubRunInfo.Paragraph))
                SetStyles(run.NextLevelInfo);
        }
    }
    public override void Redo(Document context)
    {
        SavedValue = new();
        foreach (var (para, val) in SavedValueRedo!)
        {
            SavedValue.Add((para, Getter(para)));
            Setter.Invoke(para, val);
        }
        SavedValueRedo!.Clear();
        SavedValueRedo = null;
        NotifyInfo(new(NewSelection: range));
        context.RequestRedraw();
    }
    bool ConfirmSetStyle(Paragraph para)
    {
        var val = Getter(para);
        if (Setter(para, NewValueFactory()))
        {
            SavedValue.Add((para, val));
            return true;
        }
        return false;
    }

    public override void Undo(Document context)
    {
        SavedValueRedo = new();
        foreach (var (para, val) in SavedValue!)
        {
            SavedValueRedo.Add((para, Getter(para)));
            Setter.Invoke(para, val);
        }
        SavedValue!.Clear();
        SavedValue = null;
        NotifyInfo(new(NewSelection: range));
        context.RequestRedraw();
    }
}
public class UndoParagraphSetting<T> : UndoUnit<Document, DocumentViewUpdateInfo>
{
    ParagraphIndex paraIdx;
    Paragraph _para;
    Func<Paragraph, T> Getter;
    Func<Paragraph, T, bool> Setter;
    T NewValue;
    public UndoParagraphSetting(ParagraphIndex paraIdx, T newValue, Func<Paragraph, T> Getter, Func<Paragraph, T, bool> Setter)
    {
        this.paraIdx = paraIdx;
        this.Getter = Getter;
        this.Setter = Setter;
        NewValue = newValue;
    }
    T? SavedValue;
    public override void Do(Document context)
    {
        _para = context.Paragraphs[paraIdx];
        ConfirmSetStyle(_para);
        context.RequestRedraw();
    }
    public override void Redo(Document context)
    {
        base.Redo(context);
        NotifyInfo(new(NewSelection: new(_para.GlobalInfo.OffsetFromThis(_para.UserStartCaretPosition))));
    }
    bool ConfirmSetStyle(Paragraph para)
    {
        var val = Getter(para);
        if (Setter(para, NewValue))
        {
            SavedValue = val;
            return true;
        }
        return false;
    }

    public override void Undo(Document context)
    {
        _para = context.Paragraphs[paraIdx];
        Setter.Invoke(_para, SavedValue);
        SavedValue = default;
        NotifyInfo(new(NewSelection: new(_para.GlobalInfo.OffsetFromThis(_para.UserStartCaretPosition))));
        context.RequestRedraw();
    }
}