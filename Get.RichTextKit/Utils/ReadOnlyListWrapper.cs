using System.Collections;

namespace Get.RichTextKit.Utils;


readonly record struct ReadOnlyListWrapper<T>(IList<T> List) : IReadOnlyList<T>
{
    public T this[int index] => List[index];

    public int Count => List.Count;

    public IEnumerator<T> GetEnumerator() => List.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
public static partial class Extension
{
    public static IReadOnlyList<T> AsReadOnly<T>(this IList<T> list) => new ReadOnlyListWrapper<T>(list);
}