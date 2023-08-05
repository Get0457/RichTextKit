using Get.RichTextKit;

namespace Get.RichTextKit.Editor.Paragraphs;

public interface ITextParagraph
{
    /// <summary>
    /// Gets the TextBlock associated with this paragraph
    /// </summary>
    TextBlock TextBlock { get; }

    void EnsureReadyToModify();
    void OnTextBlockChanged();
}
