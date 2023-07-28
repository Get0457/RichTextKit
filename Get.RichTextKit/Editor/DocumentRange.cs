using Get.RichTextKit.Editor.Paragraphs;
using System.Diagnostics.CodeAnalysis;
using Get.RichTextKit;
using System;
using Get.RichTextKit.Styles;

namespace Get.RichTextKit.Editor;

public class DocumentRange : TextRangeBase
{
    readonly Document TextDocument;
    TextRange _range;
    internal DocumentRange(Document textDocument, TextRange range)
    {
        TextDocument = textDocument;
        _range = range;
    }

    public override void ApplyStyle(Func<IStyle, IStyle> styleModifier)
        => TextDocument.ApplyStyle(_range, styleModifier);

    public override bool GetParagraphSetting<T>(Func<Paragraph, T?> statusChecker, [NotNullWhen(true)] out T? value) where T : default
        => TextDocument.GetParagraphSetting(_range, statusChecker, out value);

    public override void ApplyParagraphSetting<T>(T newValue, Func<Paragraph, T> Getter, Func<Paragraph, T, bool> Setter)
        => TextDocument.ApplyParagraphSetting(_range, newValue, Getter, Setter);

    public override StyleStatus GetStyleStatus(Func<IStyle, bool> styleModifier)
        => TextDocument.GetStyleStatus(_range, styleModifier);
    public override bool GetStyleValue<T>(Func<IStyle, T> statusChecker, [NotNullWhen(true)] out T? value) where T : default
        => TextDocument.GetStyleValue(_range, statusChecker, out value);

    public override void ApplyParagraphSettingViaFactory<T>(Func<T> newValueFactory, Func<Paragraph, T> Getter, Func<Paragraph, T, bool> Setter)
        => TextDocument.ApplyParagraphSettingViaFactory(_range, newValueFactory, Getter, Setter);
}