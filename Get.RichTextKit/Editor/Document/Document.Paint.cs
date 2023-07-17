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
        int startParaIndex = Paragraphs.BinarySearch(bounds.Top, (para, a) =>
        {
            if (para.GlobalInfo.ContentPosition.Y > a)
                return 1;
            if (para.GlobalInfo.ContentPosition.Y + para.ContentHeight < a)
                return -1;
            return 0;
        });
        if (startParaIndex < 0)
            startParaIndex = ~startParaIndex;

        // Offset the selection range to be relative to the first paragraph
        // that will be painted
        if (options.Selection != null)
        {
            if (startParaIndex == Paragraphs.Count)
            {
                options.Selection = options.Selection.Value.Offset(-Layout.Length);
            }
            else
            {
                options.Selection = options.Selection.Value.Offset(-Paragraphs[startParaIndex].GlobalInfo.CodePointIndex);
            }
        }

        // Paint...  
        for (int i = startParaIndex; i < Paragraphs.Count; i++)
        {
            // Get the paragraph
            var p = Paragraphs[i];

            // Quit if past the region to be painted?
            if (p.GlobalInfo.ContentPosition.Y > bounds.Bottom)
                break;

            p.DrawingContentPosition = p.GlobalInfo.ContentPosition with { Y = p.GlobalInfo.ContentPosition.Y - bounds.Top };
            // Paint it
            p.Paint(canvas, new(bounds, options, ownerView));

            // Update the selection range for the next paragraph
            if (options.Selection != null)
            {
                options.Selection = options.Selection.Value.Offset(-p.CodePointLength);
            }
        }
    }
}
