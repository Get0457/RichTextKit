using Get.RichTextKit.Editor.DocumentView;
using Get.RichTextKit.Editor.Paragraphs.Properties.Decoration;
using SkiaSharp;
using System.Drawing;

namespace Get.RichTextKit.Editor.Paragraphs;

public class ParagraphProperties
{
    public IParagraphDecoration Decoration { get; set; }
    public void CopyTo(ParagraphProperties another)
    {
        another.Decoration = Decoration.Clone();
    }
}