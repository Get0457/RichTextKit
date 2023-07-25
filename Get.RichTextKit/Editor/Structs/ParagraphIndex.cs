namespace Get.RichTextKit.Editor.Structs;
public record struct ParagraphIndex(int[] RecursiveIndexArray)
{
    public override string ToString()
    {
        return $"{string.Join(" -> ", new string[] { "[Root]" }.Concat(from x in RecursiveIndexArray select x.ToString()))}";
    }
}