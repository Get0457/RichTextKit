using SkiaSharp;

namespace Get.RichTextKit.Styles;
public interface IWriteableStyle : IStyle
{
    new string FontFamily { set; }
    new float FontSize { set; }
    new int FontWeight { set; }
    new SKFontStyleWidth FontWidth { set; }
    new bool FontItalic { set; }
    new UnderlineStyle Underline { set; }
    new StrikeThroughStyle StrikeThrough { set; }
    new float LineHeight { set; }
    new SKColor? TextColor { set; }
    new SKColor? BackgroundColor { set; }
    new SKColor HaloColor { set; }
    new float HaloWidth { set; }
    new float HaloBlur { set; }
    new float LetterSpacing { set; }
    new FontVariant FontVariant { set; }
    new TextDirection TextDirection { set; }
    new char ReplacementCharacter { set; }
}