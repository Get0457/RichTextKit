using Get.RichTextKit.Editor.Paragraphs.Panel;
using Get.RichTextKit.Styles;
using System;
using System.Collections.Generic;
using System.Text;

namespace Get.RichTextKit.Editor.Paragraphs;

internal class DocumentRootParagraph : VerticalParagraph
{
    public DocumentRootParagraph(IStyle style) : base(style)
    {
        ParentInfo = new(null, 0);
    }
}
