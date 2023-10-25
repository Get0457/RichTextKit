namespace Get.RichTextKit.Editor.Structs;
public record struct ParagraphIndex(int[] RecursiveIndexArray)
{
    public override string ToString()
    {
        return $"{string.Join(" -> ", new string[] { "[Root]" }.Concat(from x in RecursiveIndexArray select x.ToString()))}";
    }
    //public ParagraphIndex Clone()
    //{
    //    int[] newArr = new int[RecursiveIndexArray.Length];
    //    foreach (var i in ..RecursiveIndexArray.Length)
    //        newArr[i] = RecursiveIndexArray[i];
    //    return new(newArr);
    //}
    public ref int IndexRelativeToParent
        => ref RecursiveIndexArray[^1];
    public readonly ParagraphIndex Parent
    {
        get
        {
            var @new = new int[Math.Max(0, RecursiveIndexArray.Length - 1)];
            foreach (var i in ..@new.Length)
                @new[i] = RecursiveIndexArray[i];
            return new(@new);
        }
    }
}