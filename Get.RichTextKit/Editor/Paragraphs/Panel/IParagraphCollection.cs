using Get.RichTextKit.Editor.Structs;

namespace Get.RichTextKit.Editor.Paragraphs.Panel;

public interface IParagraphCollection : IParentOrParagraph
{
    bool IsChildrenReadOnly { get; }
    IList<Paragraph> Paragraphs { get; }
}
public interface IParagraphPanel : IParagraphCollection
{
    IList<Paragraph> Children { get; }
}
