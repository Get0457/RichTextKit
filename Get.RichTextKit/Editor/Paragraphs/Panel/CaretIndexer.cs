using System;
using System.Collections;

namespace Get.RichTextKit.Editor.Paragraphs.Panel;

class CaretIndexer : IReadOnlyList<int>
{
    public IList<Paragraph> Paragraphs { get; }
    readonly Func<Paragraph, IReadOnlyList<int>> Getter;
    public CaretIndexer(IList<Paragraph> Paragraphs, Func<Paragraph, IReadOnlyList<int>> Path)
    {
        this.Paragraphs = Paragraphs;
        this.Getter = Path;
    }

    public int this[int index]
    {
        get
        {
            int offset = 0;
            foreach (var para in Paragraphs)
            {
                var list = Getter(para);
                var count = list.Count;
                if (index >= count)
                {
                    offset += list[^1] + 1;
                    index -= count;
                    continue;
                }
                return list[index] + offset;
            }
            throw new IndexOutOfRangeException();
        }
    }

    public int Count => Paragraphs.Select(Getter).Sum(x => x.Count);

    public IEnumerator<int> GetEnumerator()
    {
        int offset = 0;
        foreach (var para in Paragraphs)
        {
            var list = Getter(para);
            var count = list.Count;
            foreach (var item in list)
                yield return item + offset;
            offset += list[^1];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

