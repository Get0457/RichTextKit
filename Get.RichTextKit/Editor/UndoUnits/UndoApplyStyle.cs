using Get.RichTextKit.Utils;
using Get.RichTextKit;
using System.Diagnostics;
using Get.RichTextKit.Styles;
using Get.RichTextKit.Editor.DocumentView;

namespace Get.RichTextKit.Editor.UndoUnits;
public class UndoApplyStyle : UndoUnit<Document, DocumentViewUpdateInfo>
{
    readonly Func<IStyle, IStyle> ModifyStyleFunc;
    TextRange range;
    public UndoApplyStyle(TextRange range, Func<IStyle, IStyle> modifyStyleFunc)
    {
        if (modifyStyleFunc is null) throw new ArgumentNullException();
        ModifyStyleFunc = modifyStyleFunc;
        this.range = range;
    }
    List<List<IStyle>>? SavedStyles;
    public override void Do(Document context)
    {
        SavedStyles = new();
        foreach (var subrun in context.Paragraphs.GetInterectingRuns(range.Start, range.Length))
        {
            // Get the paragraph
            var para = context.Paragraphs[subrun.Index];

            var _SavedStyles = new List<IStyle>();
            foreach (var styleRun in para.GetStyles(subrun.Offset, subrun.Length))
            {
                _SavedStyles.Add(styleRun.Style);
                para.ApplyStyle(ModifyStyleFunc.Invoke(styleRun.Style), subrun.Offset + styleRun.Start, styleRun.Length);
            }
            SavedStyles.Add(_SavedStyles);
        }
        context.RequestRedraw();
    }
    public override void Redo(Document context)
    {
        base.Redo(context);
        NotifyInfo(new(NewSelection: range));
    }
    public override void Undo(Document context)
    {
        if (SavedStyles is null)
        {
            Debugger.Break();
            throw new InvalidOperationException();
        }
        var enumerator = SavedStyles.GetEnumerator();
        foreach (var subrun in context.Paragraphs.GetInterectingRuns(range.Start, range.Length))
        {
            // Get the paragraph
            var para = context.Paragraphs[subrun.Index];
            enumerator.MoveNext();
            foreach (var (styleRun, oldStyle) in para.GetStyles(subrun.Offset, subrun.Length).Zip(enumerator.Current, static (a, b) => (a, b)))
            {
                para.ApplyStyle(oldStyle, subrun.Offset + styleRun.Start, styleRun.Length);
            }
        }
        SavedStyles.Clear();
        SavedStyles = null;
        context.RequestRedraw();
        NotifyInfo(new(NewSelection: range));
    }
}