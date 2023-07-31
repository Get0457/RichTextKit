using Get.RichTextKit.Utils;
using Get.RichTextKit;
using System.Diagnostics.CodeAnalysis;
using Get.RichTextKit.Editor;
using System.ComponentModel;
using Get.EasyCSharp;
using Get.RichTextKit.Editor.Paragraphs;
using Get.RichTextKit.Styles;
using Get.RichTextKit.Editor.Paragraphs.Panel;
using Get.RichTextKit.Editor.Structs;

namespace Get.RichTextKit.Editor.DocumentView;

public partial class DocumentViewSelection : TextRangeBase, INotifyPropertyChanged
{
    DocumentView DocumentView;
    internal DocumentViewSelection(DocumentView textDocument)
    {
        DocumentView = textDocument;
        DocumentView.RedrawRequested += UpdateCaretInfoAndNotify;
        DocumentView.OwnerDocument.Layout.Updated += UpdateCaretInfoAndNotify;
        DocumentView.YScrollChanged += UpdateCaretInfoAndNotify;
        DocumentView.XScrollChanged += UpdateCaretInfoAndNotify;
    }
    [AutoEventProperty(OnChanged = nameof(OnRangeChanged))]
    TextRange _Range;
    public CaretInfo StartCaretInfo { get; private set; }
    public CaretInfo EndCaretInfo { get; private set; }
    /// <summary>
    /// Gets the cache style at the current caret position. If the selection is a range, the style is null
    /// </summary>
    internal IStyle? CurrentPositionStyle { get; private set; }
    public IStyle CurrentCaretStyle => CurrentPositionStyle ?? DocumentView.OwnerDocument.GetStyleAtPosition(Range.EndCaretPosition);
    bool wasNotSelection = false;
    void OnRangeChanged()
    {
        if (!Range.IsRange)
        {
            CurrentPositionStyle = DocumentView.OwnerDocument.GetStyleAtPosition(Range.EndCaretPosition);
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
            UpdateCaretInfo();
        }
        else
        {
            CurrentPositionStyle = null;
            var selectInfo = DocumentView.OwnerDocument.Editor.GetSelectionInfo(Range);
            DocumentView.LayoutInfo.OffsetToThis(ref selectInfo);
            _Range = selectInfo.FinalSelection;
            StartCaretInfo = selectInfo.StartCaretInfo;
            EndCaretInfo = selectInfo.EndCaretInfo;
            Info = selectInfo;
        }
        //var start = DocumentView.Controller.HitTest(new(0, 0));
        //var end = DocumentView.Controller.HitTest(new(0, DocumentView.ViewHeight));
        bool change = true;
        if (EndCaretInfo.CaretRectangle.Top < 0)
            DocumentView.YScroll += EndCaretInfo.CaretRectangle.Top;
        else if (EndCaretInfo.CaretRectangle.Bottom - DocumentView.ViewHeight > 0)
            DocumentView.YScroll += EndCaretInfo.CaretRectangle.Bottom - DocumentView.ViewHeight;
        else change = false;
        if (change) UpdateCaretInfo();
        if (!(wasNotSelection && !Range.IsRange))
            DocumentView.RequestRedraw();
        wasNotSelection = !Range.IsRange;
        PropertyChanged?.Invoke(this, new(null)); // update all, not just range
    }
    [Event<Action<DocumentView>>]
    [Event<EventHandler>]
    void UpdateCaretInfoAndNotify()
    {
        UpdateCaretInfo();
        PropertyChanged?.Invoke(this, new(nameof(StartCaretInfo)));
        PropertyChanged?.Invoke(this, new(nameof(EndCaretInfo)));
    }
    void UpdateCaretInfo()
    {
        if (Range.IsRange)
        {
            var selectInfo = DocumentView.OwnerDocument.Editor.GetSelectionInfo(Range);
            DocumentView.LayoutInfo.OffsetToThis(ref selectInfo);
            Info = selectInfo;
            StartCaretInfo = selectInfo.StartCaretInfo;
            EndCaretInfo = selectInfo.EndCaretInfo;
        } else
        {
            Info = null;
            StartCaretInfo = EndCaretInfo = DocumentView.Controller.GetCaretInfo(Range.EndCaretPosition);
        }

    }
    public event PropertyChangedEventHandler? PropertyChanged;
    protected override void OnChanged(string FormatName)
    {
        base.OnChanged(FormatName);
        PropertyChanged?.Invoke(this, new(FormatName));
    }
    public SelectionInfo? Info { get; private set; }

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
    /// <summary>
    /// Get Currently Interacting Paragraph.
    /// If the selection spans multiple paragraphs, it will not be included.
    /// The enumerable returns the most inner paragraph.
    /// </summary>
    /// <remarks>
    /// The caller should only go to the next item if there is no handler for the current
    /// paragraph. When a paragraph is handled, the enumerable should not ask for the next value and stop immedietly.
    /// </remarks>
    public IEnumerable<Paragraph> CurrentlyInteractingParagraph
    {
        get
        {
            var info = DocumentView.OwnerDocument.Paragraphs.GetBFSInteractingRunsRecursive(Range).ToArray();
            if (info.Length is 1)
            {
                yield return info[0].SubRunInfo.Paragraph;
            }
            if (info.Length > 0)
            {
                var parent = info[0].SubRunInfo.ParentInfo.Parent;
                while (true)
                {
                    if (parent is Paragraph para)
                    {
                        yield return para;
                        parent = para.ParentInfo.Parent;
                    }
                    else yield break;
                }
            }
        }
    }
    public override bool GetParagraphSetting<T>(Func<Paragraph, T?> statusChecker, [NotNullWhen(true)] out T? value) where T : default
        => DocumentView.OwnerDocument.GetParagraphSetting(Range, statusChecker, out value);

    public override void ApplyParagraphSetting<T>(T newValue, Func<Paragraph, T> Getter, Func<Paragraph, T, bool> Setter)
        => DocumentView.OwnerDocument.ApplyParagraphSetting(Range, newValue, Getter, Setter);

    public override void ApplyParagraphSettingViaFactory<T>(Func<T> newValueFactory, Func<Paragraph, T> Getter, Func<Paragraph, T, bool> Setter)
        => DocumentView.OwnerDocument.ApplyParagraphSettingViaFactory(Range, newValueFactory, Getter, Setter);
}