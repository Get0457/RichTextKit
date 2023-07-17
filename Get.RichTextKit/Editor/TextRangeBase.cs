using SkiaSharp;
using Get.RichTextKit;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel;
using Get.RichTextKit.Editor.Paragraphs;
using System;
using Get.RichTextKit.Styles;

namespace Get.RichTextKit.Editor;

public abstract class TextRangeBase
{
    public const int FontWeightBold = 700;
    public const int FontWeightNormal = 400;
    protected virtual void OnChanged(string FormatName) { }
    public abstract void ApplyStyle(Func<IStyle, IStyle> styleModifier);
    public abstract StyleStatus GetStyleStatus(Func<IStyle, bool> styleModifier);
    public abstract bool GetStyleValue<T>(Func<IStyle, T> statusChecker, [NotNullWhen(true)] out T? value);
    public abstract bool GetParagraphSetting<T>(Func<Paragraph, T?> statusChecker, [NotNullWhen(true)] out T? value);
    public abstract void ApplyParagraphSetting<T>(T newValue, Func<Paragraph, T> Getter, Action<Paragraph, T> Setter);
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
}
public enum StyleStatus
{
    On,
    Off,
    Undefined
}
