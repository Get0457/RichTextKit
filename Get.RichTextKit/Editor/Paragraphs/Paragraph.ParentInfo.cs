using Get.RichTextKit.Editor.Paragraphs.Panel;
using Get.RichTextKit.Editor.Structs;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Get.RichTextKit.Editor.Paragraphs;

partial class Paragraph
{
    protected ParentInfo VirtualizedParentInfo
    {
        get
        {

            var parentInfo = ParentInfo;
            if (parentInfo.Parent is null)
            {
                if (this is not IParagraphCollection paragraphCollection)
                    throw new InvalidOperationException("The root paragraph must be a panel");
                parentInfo = new(paragraphCollection, 0);
            }
            return parentInfo;
        }
    }
    public ParentInfo ParentInfo { get; internal set; }
    public ParagraphIndex GlobalParagraphIndex => new(GetParaIndex(null).ToArray());
    IEnumerable<int> GetParaIndex(IParagraphCollection? stopAtParent)
    {
        var parentInfo = ParentInfo;
        if (parentInfo.Parent is not Paragraph paragraph || paragraph == stopAtParent)
        {
            yield break;
        }
        else
        {
            foreach (var a in paragraph.GetParaIndex(stopAtParent))
                yield return a;
            yield return parentInfo.Index;
        }
    }
}
