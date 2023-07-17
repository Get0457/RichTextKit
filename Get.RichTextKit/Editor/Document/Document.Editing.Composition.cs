// This file has been edited and modified from its original version.
// Original Class Name: TopTen.RichTextKit.Editor.TextDocument
// The original version of this file can be found at https://github.com/toptensoftware/RichTextKit/.
// Original copyright notice is below.
// Copyright © 2019-2020 Topten Software. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may 
// not use this product except in compliance with the License. You may obtain 
// a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
// License for the specific language governing permissions and limitations 
// under the License.

using Get.RichTextKit;
using Get.RichTextKit.Editor;

namespace Get.RichTextKit.Editor;

public partial class DocumentEditor
{
    TextRange _imeInitialSelection;
    /// <summary>
    /// Indicates if an IME composition is currently in progress
    /// </summary>
    public bool IsImeComposing { get; private set; }


    /// <summary>
    /// Get the code point offset position of the current IME composition
    /// </summary>
    public int ImeCompositionOffset
    {
        get => IsImeComposing ? _imeInitialSelection.Minimum : -1;
    }

    /// <summary>
    /// Starts and IME composition action
    /// </summary>
    /// <param name="initialSelection">The initial text selection</param>
    public void StartImeComposition(TextRange initialSelection)
    {
        // Finish last composition
        if (IsImeComposing) FinishImeComposition();

        // Store until first call
        IsImeComposing = true;
        _imeInitialSelection = initialSelection;
    }

    /// <summary>
    /// Update a pending IME composition
    /// </summary>
    /// <param name="text">The composition text</param>
    /// <param name="caretOffset">The caret offset relative to the composition text</param>
    public void UpdateImeComposition(StyledText text, int caretOffset)
    {
        if (!IsImeComposing)
            return;

        ReplaceTextInternal(_imeInitialSelection, text, EditSemantics.ImeComposition, caretOffset);
    }

    /// <summary>
    /// Complete an IME composition
    /// </summary>
    public void FinishImeComposition()
    {
        Document.UndoManager.Undo(delegate { });
    }
}
