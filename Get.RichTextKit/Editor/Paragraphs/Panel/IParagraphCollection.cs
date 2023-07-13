namespace Get.RichTextKit.Editor.Paragraphs.Panel;

public interface IParagraphCollection
{
    IList<Paragraph> Paragraphs { get; }
}
public interface IParagraphPanel : IParagraphCollection
{
    IList<Paragraph> Children { get; }
}
