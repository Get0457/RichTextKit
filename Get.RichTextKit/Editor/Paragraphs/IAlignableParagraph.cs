using Get.RichTextKit;

namespace Get.RichTextKit.Editor.Paragraphs;

public interface IAlignableParagraph
{
    TextAlignment Alignment { get; set; }
}
