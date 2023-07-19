using Get.RichTextKit.Editor.Paragraphs.Panel;
using Get.RichTextKit.Utils;

namespace Get.RichTextKit.Editor.Structs;
public interface IParentOrParagraph
{

}
public readonly record struct SelectionInfo(
    TextRange OriginalRange,
    TextRange? NewSelection,
    CaretInfo StartCaretInfo,
    CaretInfo EndCaretInfo,
    IParentOrParagraph SelectionInfoProvider,
    IEnumerable<SubRunInfo> InteractingRuns,
    IEnumerable<SubRunInfo> RecursiveInteractingRuns,
    IEnumerable<SubRunBFSInfo> RecursiveBFSInteractingRuns
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
