using Get.EasyCSharp;
using Get.RichTextKit.Editor.Paragraphs;
using SkiaSharp;
using System;
using System.ComponentModel;

namespace Get.RichTextKit.Editor.DocumentView;

public partial class DocumentView : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<DocumentView>? RedrawRequested;
    public IDocumentViewOwner OwnerView { get; }
    public Document OwnerDocument { get; }
    public DocumentView(IDocumentViewOwner owner, Document Document)
    {
        OwnerView = owner;
        OwnerDocument = Document;
        Selection = new(this);
        Controller = new(this);
        Document.RedrawRequested += RequestRedraw;
        Document.Layout.Updated += UpdateScroll;
    }
    public DocumentViewSelection Selection { get; }
    public DocumentViewController Controller { get; }
    internal Paragraph.LayoutInfo LayoutInfo => new(new(-XScroll, -YScroll), 0, 0, 0);

    [AutoEventNotifyProperty(OnChanged = nameof(RequestRedraw))]
    SKColor _SelectionColor = SKColors.Blue;

    [Event<Action<Document>>(Visibility = GeneratorVisibility.Private)]
    public void RequestRedraw() => RedrawRequested?.Invoke(this);
    public void Paint(SKCanvas canvas, SKColor textDefaultColor)
    {
        OwnerDocument.Paint(canvas, new(XScroll, YScroll, ViewWidth, ViewHeight), new()
        {
            Selection = Selection.Range,
            SelectionColor = _SelectionColor,
            Edging = SKFontEdging.SubpixelAntialias,
            TextDefaultColor = textDefaultColor
        });
    }
    [AutoEventNotifyProperty(OnChanged = nameof(UpdateScroll))]
    float _ViewHeight;
    [AutoEventNotifyProperty(OnChanged = nameof(UpdateScroll))]
    float _ViewWidth;
    [AutoEventNotifyProperty(OnChanged = nameof(UpdateScroll))]
    float _BottomAdditionalScrollHeight;
    [AutoEventNotifyProperty(OnChanged = nameof(UpdateScroll))]
    float _RightAdditionalScrollWidth;
    void UpdateMaximumYScroll()
    {
        MaximumYScroll = OwnerDocument.Layout.MeasuredSize.Height + BottomAdditionalScrollHeight - ViewHeight;
    }
    void UpdateMaximumXScroll()
    {
        MaximumXScroll = OwnerDocument.Layout.MeasuredSize.Width + RightAdditionalScrollWidth - ViewWidth;
    }
    void UpdateScroll() => UpdateScroll(null, null);
    bool UpdateScroll(float? XsetToNewScroll = null, float? YsetToNewScroll = null)
    {
        UpdateMaximumYScroll();
        UpdateMaximumXScroll();
        var Ypos = YsetToNewScroll ?? YScroll;
        var Xpos = XsetToNewScroll ?? XScroll;
        var newYPos = Ypos.Clamp(0, Math.Max(
            MaximumYScroll,
            0)
        );
        var newXPos = Ypos.Clamp(0, Math.Max(
            MaximumXScroll,
            0)
        );
        bool isUpdated = false;
        if (_YScroll != newYPos)
        {
            _YScroll = newYPos;
            PropertyChanged?.Invoke(this, new(nameof(YScroll)));
            YScrollChanged?.Invoke(this, new());
            isUpdated = true;
        }
        if (_XScroll != newXPos)
        {
            _XScroll = newXPos;
            PropertyChanged?.Invoke(this, new(nameof(XScroll)));
            XScrollChanged?.Invoke(this, new());
            isUpdated = true;
        }
        if (isUpdated)
            RequestRedraw();
        return !isUpdated;
    }
    [AutoEventNotifyProperty(SetVisibility = GeneratorVisibility.Private)]
    float _MaximumYScroll;
    public event EventHandler? YScrollChanged;
    float _YScroll;
    public float YScroll
    {
        get => _YScroll;
        set
        {
            UpdateScroll(YsetToNewScroll: value);
        }
    }
    [AutoEventNotifyProperty(SetVisibility = GeneratorVisibility.Private)]
    float _MaximumXScroll;
    public event EventHandler? XScrollChanged;
    float _XScroll;
    public float XScroll
    {
        get => _XScroll;
        set
        {
            UpdateScroll(value);
        }
    }
    public void InvokeUpdateInfo(DocumentViewUpdateInfo info)
    {
        if (info.NewSelection.HasValue)
        {
            Selection.Range = info.NewSelection.Value;
        }
    }
}
static partial class Extension
{
    public static float Clamp(this float value, float min, float max)
    {
        if (max < min) throw new ArgumentOutOfRangeException(nameof(max), "max must be more than min");
        if (value < min)
        {
            return min;
        }
        if (value > max)
        {
            return max;
        }
        return value;
    }
}