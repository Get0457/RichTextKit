using Get.RichTextKit.Editor.Paragraphs;
using Get.RichTextKit.Editor.UndoUnits;
using System.Diagnostics.CodeAnalysis;
using Get.RichTextKit;
using System;
using Get.RichTextKit.Styles;

namespace Get.RichTextKit.Editor;

public partial class Document
{
    public DocumentRange this[TextRange textRange] => new(this, textRange);

    public void ApplyStyle(TextRange range, Func<IStyle, IStyle> styleModifier)
    {
        UndoManager.Do(new UndoApplyStyle(range.Normalized, styleModifier));
    }
    public StyleStatus GetStyleStatus(TextRange range, Func<IStyle, bool> statusChecker)
    {
        bool? HasStyle = null;
        bool? NotHasStyle = null;
        foreach (var subrun in Paragraphs.GetInteractingRunsRecursive(range.Normalized))
        {
            // Get the paragraph
            var para = subrun.Paragraph;

            foreach (var styleRun in para.GetStyles(subrun.Offset, subrun.Length))
            {
                if (statusChecker.Invoke(styleRun.Style))
                {
                    HasStyle = true;
                }
                else
                {
                    NotHasStyle = true;
                }
                if (HasStyle.HasValue && NotHasStyle.HasValue) goto End;
            }
        }
        if (!HasStyle.HasValue) HasStyle = false;
        if (!NotHasStyle.HasValue) NotHasStyle = false;
        End:
        if (HasStyle == NotHasStyle) return StyleStatus.Undefined;
        if (HasStyle.Value) return StyleStatus.On;
        /* NotHasStyle must be true */
        return StyleStatus.Off;
    }
    public bool GetStyleValue<T>(TextRange range, Func<IStyle, T> statusChecker, [NotNullWhen(true)] out T? value)
    {
        var a = Paragraphs.GetInteractingStlyeRuns(range.Normalized);
        var enumerator = a.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            value = default;
            return false;
        }
        T firstValue = statusChecker.Invoke(enumerator.Current.Style);
        while (enumerator.MoveNext())
        {
            if (!EqualityComparer<T>.Default.Equals(
                firstValue,
                statusChecker.Invoke(enumerator.Current.Style)
            ))
            {
                value = default;
                return false;
            }
        }
        value = firstValue!;
        return true;
    }
    public bool GetParagraphSetting<T>(TextRange range, Func<Paragraph, T?> statusChecker, [NotNullWhen(true)] out T? value)
    {
        var a = Paragraphs.GetInteractingParagraphs(range);

        var enumerator = a.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            value = default;
            return false;
        }
        T? firstValue = statusChecker.Invoke(enumerator.Current);
        if (EqualityComparer<T?>.Default.Equals(
                firstValue,
                default
        ))
        {
            value = default;
            return false;
        }
        // firstValue is not null
        while (enumerator.MoveNext())
        {
            if (!EqualityComparer<T?>.Default.Equals(
                firstValue,
                statusChecker.Invoke(enumerator.Current)
            ))
            {
                value = default;
                return false;
            }
        }
        value = firstValue!;
        return true;
    }
    public void ApplyParagraphSetting<T>(TextRange range, T newValue, Func<Paragraph, T> Getter, Func<Paragraph, T, bool> Setter)
    {
        UndoManager.Do(new UndoParagraphSetting<T>(range, newValue, Getter, Setter));
    }

}
