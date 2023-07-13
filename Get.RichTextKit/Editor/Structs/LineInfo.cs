using System;
using System.Collections.Generic;
using System.Text;
using Get.RichTextKit;

namespace Get.RichTextKit.Editor.Structs
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Line"></param>
    /// <param name="Start"></param>
    /// <param name="End"></param>
    /// <param name="PrevLine">The previous line index. If null, going to the previous line should be navigated to the previous paragraph.</param>
    /// <param name="NextLine">The next line index. If null, going to the next line should be navigated to the next paragraph.</param>
    public record struct LineInfo(int Line, CaretPosition Start, CaretPosition End, int? PrevLine, int? NextLine)
    {
    }
}
