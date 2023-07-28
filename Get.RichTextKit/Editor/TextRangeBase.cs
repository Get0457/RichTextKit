using SkiaSharp;
using Get.RichTextKit;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel;
using Get.RichTextKit.Editor.Paragraphs;
using System;
using Get.RichTextKit.Styles;
using Get.RichTextKit.Editor.Paragraphs.Properties.Decoration;

namespace Get.RichTextKit.Editor;

public abstract class TextRangeBase
{
    public TextRangeBase()
    {
        ParagraphSettings = new(this);
    }
    public const int FontWeightBold = 700;
    public const int FontWeightNormal = 400;
    protected virtual void OnChanged(string FormatName) { }
    public abstract void ApplyStyle(Func<IStyle, IStyle> styleModifier);
    public abstract StyleStatus GetStyleStatus(Func<IStyle, bool> styleModifier);
    public abstract bool GetStyleValue<T>(Func<IStyle, T> statusChecker, [NotNullWhen(true)] out T? value);
    public abstract bool GetParagraphSetting<T>(Func<Paragraph, T?> statusChecker, [NotNullWhen(true)] out T? value);
    public abstract void ApplyParagraphSetting<T>(T newValue, Func<Paragraph, T> Getter, Func<Paragraph, T, bool> Setter);
    public abstract void ApplyParagraphSettingViaFactory<T>(Func<T> newValueFactory, Func<Paragraph, T> Getter, Func<Paragraph, T, bool> Setter);
    public void ApplyParagraphSetting<T>(T newValue, Func<Paragraph, T> Getter, Action<Paragraph, T> Setter)
        => ApplyParagraphSetting(newValue, Getter, (p, x) =>
        {
            Setter(p, x);
            return true;
        });
    public StyleStatus Bold
    {
        get
        {
            if (GetStyleValue(x => x.FontWeight, out var a))
            {
                if (a is FontWeightBold) return StyleStatus.On;
                if (a is FontWeightNormal) return StyleStatus.Off;
            }
            return StyleStatus.Undefined;
        }
        set
        {
            if (value is StyleStatus.On)
            {
                ApplyStyle(static x => new CopyStyle(x) { FontWeight = FontWeightBold });
            }
            else if (value is StyleStatus.Off)
            {
                ApplyStyle(static x => new CopyStyle(x) { FontWeight = FontWeightNormal });
            }
            OnChanged(nameof(Bold));
        }
    }
    public StyleStatus Italic
    {
        get => GetStyleStatus(static x => x.FontItalic);
        set
        {
            if (value is StyleStatus.On)
            {
                ApplyStyle(static x => new CopyStyle(x) { FontItalic = true });
            }
            else if (value is StyleStatus.Off)
            {
                ApplyStyle(static x => new CopyStyle(x) { FontItalic = false });
            }
            OnChanged(nameof(Italic));
        }
    }
    public StyleStatus Underline
    {
        get => GetStyleStatus(static x => x.Underline is UnderlineStyle.Solid);
        set
        {
            if (value is StyleStatus.On)
            {
                ApplyStyle(static x => new CopyStyle(x) { Underline = UnderlineStyle.Solid });
            }
            else if (value is StyleStatus.Off)
            {
                ApplyStyle(static x => new CopyStyle(x) { Underline = UnderlineStyle.None });
            }
            OnChanged(nameof(Underline));
        }
    }
    public StyleStatus SuperScript
    {
        get => GetStyleStatus(static x => x.FontVariant is FontVariant.SuperScript);
        set
        {
            if (value is StyleStatus.On)
            {
                ApplyStyle(static x => new CopyStyle(x) { FontVariant = FontVariant.SuperScript });
            }
            else if (value is StyleStatus.Off)
            {
                ApplyStyle(static x => new CopyStyle(x) { FontVariant = x.FontVariant is FontVariant.SuperScript ? FontVariant.Normal : x.FontVariant });
            }
            OnChanged(nameof(SuperScript));
        }
    }
    public StyleStatus SubScript
    {
        get => GetStyleStatus(static x => x.FontVariant is FontVariant.SubScript);
        set
        {
            if (value is StyleStatus.On)
            {
                ApplyStyle(static x => new CopyStyle(x) { FontVariant = FontVariant.SubScript });
            }
            else if (value is StyleStatus.Off)
            {
                ApplyStyle(static x => new CopyStyle(x) { FontVariant = x.FontVariant is FontVariant.SubScript ? FontVariant.Normal : x.FontVariant });
            }
            OnChanged(nameof(SubScript));
        }
    }
    public StyleStatus Strikethrough
    {
        get => GetStyleStatus(static x => x.StrikeThrough is StrikeThroughStyle.Solid);
        set
        {
            if (value is StyleStatus.On)
            {
                ApplyStyle(static x => new CopyStyle(x) { StrikeThrough = StrikeThroughStyle.Solid });
            }
            else if (value is StyleStatus.Off)
            {
                ApplyStyle(static x => new CopyStyle(x) { StrikeThrough = StrikeThroughStyle.None });
            }
            OnChanged(nameof(SubScript));
        }
    }
    public string? FontFamily
    {
        get
        {
            if (GetStyleValue(x => x.FontFamily, out var a))
            {
                return a;
            }
            return null;
        }
        set
        {
            if (value is not null)
            {
                ApplyStyle(x => new CopyStyle(x) { FontFamily = value });
            }
            OnChanged(nameof(FontFamily));
        }
    }
    public float? FontSize
    {
        get
        {
            if (GetStyleValue(x => x.FontSize, out var a))
            {
                return a;
            }
            return null;
        }
        set
        {
            if (value is not null)
            {
                ApplyStyle(x => new CopyStyle(x) { FontSize = value.Value });
            }
            OnChanged(nameof(FontSize));
        }
    }
    public MaybeValue<SKColor?> TextColor
    {
        get
        {
            if (GetStyleValue(x => x.TextColor, out var a))
            {
                return new(a);
            }
            return new();
        }
        set
        {
            if (value.HasValue)
            {
                ApplyStyle(x => new CopyStyle(x) { TextColor = value.Value });
            }
            OnChanged(nameof(TextColor));
        }
    }
    public SKColor? BackgroundColor
    {
        get
        {
            if (GetStyleValue(x => x.BackgroundColor, out var a))
            {
                return a;
            }
            return null;
        }
        set
        {
            if (value is not null)
            {
                ApplyStyle(x => new CopyStyle(x) { BackgroundColor = value.Value });
            }
            OnChanged(nameof(BackgroundColor));
        }
    }
    public ParagraphSetting ParagraphSettings { get; }
    public class ParagraphSetting
    {
        readonly TextRangeBase TextRangeBase;
        internal ParagraphSetting(TextRangeBase trb)
        {
            TextRangeBase = trb;
        }
        public TextAlignment? TextAlignment
        {
            get
            {
                if (TextRangeBase.GetParagraphSetting<TextAlignment?>(x => x is IAlignableParagraph a ? a.Alignment : null, out var setting))
                {
                    return setting;
                }
                return null;
            }
            set
            {
                if (value is not null)
                {
                    TextRangeBase.ApplyParagraphSetting(value.Value, AlignmentGetter, AlignmentSetter);
                }
                TextRangeBase.OnChanged(nameof(TextAlignment));
            }
        }
        public StyleStatus HasBulletListDecoration
        {
            get => HasParagraphDecorationOfType<BulletDecoration>();
            set
            {
                if (value is StyleStatus.On)
                {
                    SetParagraphDecorationToType<BulletDecoration>();
                }
                else if (value is StyleStatus.Off)
                {
                    RemoveParagraphDecorationToType<BulletDecoration>();
                }
            }
        }
        public StyleStatus HasNumberListDecoration
        {
            get => HasParagraphDecorationOfType<NumberListDecoration>();
            set
            {
                if (value is StyleStatus.On)
                {
                    SetParagraphDecorationToType<NumberListDecoration>();
                }
                else if (value is StyleStatus.Off)
                {
                    RemoveParagraphDecorationToType<NumberListDecoration>();
                }
            }
        }
        public StyleStatus HasParagraphDecorationOfType<T>() where T : IParagraphDecoration
        {
            if (TextRangeBase.GetParagraphSetting(x => x.Properties.Decoration is T, out var output))
                return output ? StyleStatus.On : StyleStatus.Off;
            return StyleStatus.Undefined;
        }
        public void SetParagraphDecorationToType<T>() where T : IParagraphDecoration, new()
        {
            TextRangeBase.ApplyParagraphSetting(new T(), static x => x.Properties.Decoration, static (x, y) =>
            {
                if (y is T && x.Properties.Decoration is T)
                    return false;
                var oldDeco = x.Properties.Decoration;
                x.Properties.Decoration = y;
                oldDeco?.RemovedFromLayout();
                return true;
            });
        }
        public void SetParagraphDecorationToType<T>(StyleStatus status) where T : IParagraphDecoration, new()
        {
            if (status is StyleStatus.On)
            {
                SetParagraphDecorationToType<T>();
            }
            else if (status is StyleStatus.Off)
            {
                RemoveParagraphDecorationToType<T>();
            }
        }
        public VerticalAlignment? DecorationVerticalAlignment
        {
            get
            {
                if (TextRangeBase.GetParagraphSetting(x => x.Properties.Decoration?.VerticalAlignment, out var output))
                    return output;
                return null;
            }
            set
            {
                if (value is not null)
                    TextRangeBase.ApplyParagraphSetting(value.Value, static x => x.Properties.Decoration?.VerticalAlignment, static (x, y) =>
                    {
                        var deco = x.Properties.Decoration;
                        if (deco is not null && y is not null && deco.VerticalAlignment != y)
                        {
                            deco.VerticalAlignment = y.Value;
                            return true;
                        }
                        return false;
                    });
            }
        }
        public CountMode? DecorationCountMode
        {
            get
            {
                if (TextRangeBase.GetParagraphSetting(x => x.Properties.Decoration?.CountMode, out var output))
                    return output;
                return null;
            }
            set
            {
                if (value is not null)
                    TextRangeBase.ApplyParagraphSetting(value.Value, static x => x.Properties.Decoration?.CountMode, static (x, y) =>
                    {
                        if (y is not null && x.Properties.Decoration is IParagraphDecorationCountModifiable deco && deco.CountMode != y)
                        {
                            deco.CountMode = y.Value;
                            return true;
                        }
                        return false;
                    });
            }
        }
        public void SetParagraphDecorationToType<T>(StyleStatus status, Func<T> Factory) where T : IParagraphDecoration
        {
            if (status is StyleStatus.On)
            {
                SetParagraphDecorationToType<T>(Factory);
            }
            else if (status is StyleStatus.Off)
            {
                RemoveParagraphDecorationToType<T>();
            }
        }
        public void SetParagraphDecorationToType<T>(Func<T> Factory) where T : IParagraphDecoration
        {
            TextRangeBase.ApplyParagraphSettingViaFactory<IParagraphDecoration>(() => Factory(), static x => x.Properties.Decoration, static (x, y) =>
            {
                if (y is T && x.Properties.Decoration is T)
                    return false;
                var oldDeco = x.Properties.Decoration;
                x.Properties.Decoration = y;
                oldDeco?.RemovedFromLayout();
                return true;
            });
        }
        public void RemoveParagraphDecorationToType<T>() where T : IParagraphDecoration
        {
            TextRangeBase.ApplyParagraphSetting(null, static x => x.Properties.Decoration, static (x, y) =>
            {
                if (x.Properties.Decoration is not T)
                    return false;
                var oldDeco = x.Properties.Decoration;
                x.Properties.Decoration = y;
                oldDeco?.RemovedFromLayout();
                return true;
            });
        }
        static TextAlignment AlignmentGetter(Paragraph p)
            => p is IAlignableParagraph ap ? ap.Alignment : default;
        static bool AlignmentSetter(Paragraph p, TextAlignment x)
        {
            if (p is IAlignableParagraph ap)
            {
                ap.Alignment = x;
                return true;
            }
            return false;
        }
    }
}
public enum StyleStatus
{
    On,
    Off,
    Undefined
}
