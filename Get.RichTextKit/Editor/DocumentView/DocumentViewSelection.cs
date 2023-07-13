using Get.RichTextKit.Utils;
using Get.RichTextKit;
using System.Diagnostics.CodeAnalysis;
using Get.RichTextKit.Editor;
using System.ComponentModel;
using Get.EasyCSharp;
using Get.RichTextKit.Editor.Paragraphs;
using Get.RichTextKit.Styles;

namespace Get.RichTextKit.Editor.DocumentView;

public partial class DocumentViewSelection : TextRangeBase, INotifyPropertyChanged
{
    DocumentView DocumentView;
    internal DocumentViewSelection(DocumentView textDocument)
    {
        DocumentView = textDocument;
    }
    [AutoEventProperty(OnChanged = nameof(OnRangeChanged))]
    TextRange _Range;

    public CaretInfo StartCaretInfo { get; private set; }
    public CaretInfo EndCaretInfo { get; private set; }
    /// <summary>
    /// Gets the cache style at the current caret position. If the selection is a range, the style is null
    /// </summary>
    public IStyle? CurrentPositionStyle { get; private set; }
    bool wasNotSelection = false;
    void OnRangeChanged()
    {
        if (!Range.IsRange)
        {
            CurrentPositionStyle = DocumentView.OwnerDocument.GetStyleAtPosition(Range.CaretPosition);
            if (Range.AltPosition is true)
            {
                var thestr = DocumentView.OwnerDocument.GetText(new(Range.Start - 1, Range.Start)).ToString();
                if (thestr.Length > 0 && thestr[0] is Document.NewParagraphSeparator)
                    // no I do not want alt position as a cursor
                    _Range = new(_Range.Start - 1);
            }
            if (DocumentView.OwnerDocument.Layout.Length == Range.End)
            {
                // no I do not want cursor at the end
                _Range = new(_Range.Start - 1);
            }
        }
        else
            CurrentPositionStyle = null;
        //var start = DocumentView.Controller.HitTest(new(0, 0));
        //var end = DocumentView.Controller.HitTest(new(0, DocumentView.ViewHeight));
        StartCaretInfo = DocumentView.Controller.GetCaretInfo(Range.StartCaretPosition);
        EndCaretInfo = DocumentView.Controller.GetCaretInfo(Range.CaretPosition);
        bool change = true;
        if (EndCaretInfo.CaretRectangle.Top < 0)
            DocumentView.YScroll += EndCaretInfo.CaretRectangle.Top;
        else if (EndCaretInfo.CaretRectangle.Bottom - DocumentView.ViewHeight > 0)
            DocumentView.YScroll += EndCaretInfo.CaretRectangle.Bottom - DocumentView.ViewHeight;
        else change = false;
        if (change)
        {
            StartCaretInfo = DocumentView.Controller.GetCaretInfo(Range.StartCaretPosition);
            EndCaretInfo = DocumentView.Controller.GetCaretInfo(Range.CaretPosition);
        }
        if (!(wasNotSelection && !Range.IsRange))
            DocumentView.RequestRedraw();
        wasNotSelection = !Range.IsRange;
        PropertyChanged?.Invoke(this, new(null)); // update all, not just range
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    protected override void OnChanged(string FormatName)
    {
        base.OnChanged(FormatName);
        PropertyChanged?.Invoke(this, new(FormatName));
    }
    public IEnumerable<SubRun> InterectingRuns => DocumentView.OwnerDocument.Paragraphs.GetInterectingRuns(Range);

    public override void ApplyStyle(Func<IStyle, IStyle> styleModifier)
    {
        if (Range.IsRange || CurrentPositionStyle is null)
            DocumentView.OwnerDocument.ApplyStyle(Range, styleModifier);
        else
            CurrentPositionStyle = styleModifier(CurrentPositionStyle);
    }

    public override StyleStatus GetStyleStatus(Func<IStyle, bool> styleChecker)
    {
        if (Range.IsRange || CurrentPositionStyle is null)
            return DocumentView.OwnerDocument.GetStyleStatus(Range, styleChecker);
        else
            return styleChecker(CurrentPositionStyle) ? StyleStatus.On : StyleStatus.Off;
    }
    public override bool GetStyleValue<T>(Func<IStyle, T> statusChecker, [NotNullWhen(true)] out T? value) where T : default
    {
        if (Range.IsRange || CurrentPositionStyle is null)
            return DocumentView.OwnerDocument.GetStyleValue(Range, statusChecker, out value);

        value = statusChecker(CurrentPositionStyle);
        return value is not null;
    }

    public override bool GetParagraphSetting<T>(Func<Paragraph, T?> statusChecker, [NotNullWhen(true)] out T? value) where T : default
        => DocumentView.OwnerDocument.GetParagraphSetting(Range, statusChecker, out value);

    public override void ApplyParagraphSetting<T>(T newValue, Func<Paragraph, T> Getter, Action<Paragraph, T> Setter)
        => DocumentView.OwnerDocument.ApplyParagraphSetting(Range, newValue, Getter, Setter);
}