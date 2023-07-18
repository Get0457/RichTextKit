using Get.RichTextKit.Editor.Structs;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Get.RichTextKit.Utils;
using Get.RichTextKit;
using HarfBuzzSharp;

namespace Get.RichTextKit.Editor.Paragraphs.Panel;

public abstract partial class PanelParagraph : Paragraph, IParagraphPanel
{
    public abstract Paragraph GetParagraphAt(PointF pt);
}