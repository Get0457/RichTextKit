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
using Get.RichTextKit.Utils;

namespace Get.RichTextKit.Editor;

public partial class Document
{
    bool _suppressDocumentChangeEvents = false;
    public event Action<Document>? RedrawRequested;
    public event Action<Document, DocumentChangeInfo>? Changing;
    public event Action<Document>? Changed;
    public void RequestRedraw() => RedrawRequested?.Invoke(this);
    /// <summary>
    /// Notify all attached views that the document has changed
    /// </summary>
    /// <param name="info">Info about the changes to the document</param>
    internal void FireDocumentChanging(DocumentChangeInfo info)
    {
        if (_suppressDocumentChangeEvents)
            return;

        // Layout is now invalid
        Layout.Invalidate();

        // Notify all views
        Changing?.Invoke(this, info);


    }

    /// <summary>
    /// Notify all attached views that the document has finished changing
    /// </summary>
    internal void FireDocumentChanged()
    {
        if (_suppressDocumentChangeEvents)
            return;

        Layout.Invalidate();

        // Notify all views
        Changed?.Invoke(this);

        RequestRedraw();
    }
}
