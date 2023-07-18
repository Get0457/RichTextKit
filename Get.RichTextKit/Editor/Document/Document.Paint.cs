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

using Get.RichTextKit.Editor.Paragraphs;
using Get.RichTextKit.Editor.DocumentView;
using SkiaSharp;
using System.Drawing;
using Get.RichTextKit;
using Get.RichTextKit.Utils;

namespace Get.RichTextKit.Editor;

public partial class Document
{
    /// <summary>
    /// Paint this text block
    /// </summary>
    /// <param name="canvas">The Skia canvas to paint to</param>
    /// <param name="fromYCoord">The top Y-Coord of the visible part of the document</param>
    /// <param name="toYCoord">The bottom Y-Coord of the visible part of the document</param>
    /// <param name="options">Options controlling the paint operation</param>
    public void Paint(SKCanvas canvas, RectangleF bounds, TextPaintOptions? options = null, IDocumentViewOwner? ownerView = null)
    {
        options ??= new();

        // Make sure layout up to date
        Layout.EnsureValid();

        // Find the starting paragraph
        rootParagraph.DrawingContentPosition = new(0, 0 - bounds.Top);

        rootParagraph.Paint(canvas, new() {
            ViewBounds = bounds,
            TextPaintOptions = options,
            viewOwner = ownerView
        });
    }
}
