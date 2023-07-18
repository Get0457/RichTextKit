using Get.RichTextKit.Editor.Paragraphs.Panel;

namespace Get.RichTextKit.Editor.Structs;

public readonly record struct SelectionInfo(
    TextRange OriginalRange,
    TextRange? NewSelection,
    CaretInfo StartCaretInfo,
    CaretInfo EndCaretInfo,
    IParagraphCollection SelectionInfoProvider,
    IEnumerable<SubRunRecursiveInfo> InteractingRuns
)
{
    public bool IsSelectionChanged => NewSelection.HasValue;
    public TextRange FinalSelection
    {
        get => NewSelection ?? OriginalRange;
        init => NewSelection = value == NewSelection ? null : value;
    }
    public CaretInfo MaximumCaretInfo => FinalSelection.End > FinalSelection.Start ? EndCaretInfo : StartCaretInfo;
    public CaretInfo MinimumCaretInfo => FinalSelection.End < FinalSelection.Start ? EndCaretInfo : StartCaretInfo;
}
