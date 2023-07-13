using Get.RichTextKit.Styles;
using SkiaSharp;


namespace Get.RichTextKit
{
    public class CopyStyle : IWriteableStyle
    {
        public CopyStyle(IStyle style)
        {
            Copy(style, this);
        }
        public static void Copy(IStyle From, IWriteableStyle To)
        {
            To.FontFamily = From.FontFamily;
            To.FontSize = From.FontSize;
            To.FontWeight = From.FontWeight;
            To.FontWidth = From.FontWidth;
            To.FontItalic = From.FontItalic;
            To.Underline = From.Underline;
            To.StrikeThrough = From.StrikeThrough;
            To.LineHeight = From.LineHeight;
            To.TextColor = From.TextColor;
            To.BackgroundColor = From.BackgroundColor;
            To.HaloColor = From.HaloColor;
            To.HaloWidth = From.HaloWidth;
            To.HaloBlur = From.HaloBlur;
            To.LetterSpacing = From.LetterSpacing;
            To.FontVariant = From.FontVariant;
            To.TextDirection = From.TextDirection;
            To.ReplacementCharacter = From.ReplacementCharacter;
        }
        public string FontFamily { get; set; }
        public float FontSize { get; set; }
        public int FontWeight { get; set; }
        public SKFontStyleWidth FontWidth { get; set; }
        public bool FontItalic { get; set; }
        public UnderlineStyle Underline { get; set; }
        public StrikeThroughStyle StrikeThrough { get; set; }
        public float LineHeight { get; set; }
        public SKColor? TextColor { get; set; }
        public SKColor BackgroundColor { get; set; }
        public SKColor HaloColor { get; set; }
        public float HaloWidth { get; set; }
        public float HaloBlur { get; set; }
        public float LetterSpacing { get; set; }
        public FontVariant FontVariant { get; set; }
        public TextDirection TextDirection { get; set; }
        public char ReplacementCharacter { get; set; }

    }
}