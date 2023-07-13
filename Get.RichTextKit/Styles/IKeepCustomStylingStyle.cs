namespace Get.RichTextKit.Styles;

public interface IKeepCustomStylingStyle : IWriteableStyle
{
    void OnDelete();
}