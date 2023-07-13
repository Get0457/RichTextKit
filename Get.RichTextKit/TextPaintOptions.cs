// This file has been edited and modified from its original version.
// The original version of this file can be found at https://github.com/toptensoftware/RichTextKit/.
// Original copyright notice is below.
// RichTextKit
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

using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Get.RichTextKit
{
    /// <summary>
    /// Options to control how TextBlock is rendered.
    /// </summary>
    public class TextPaintOptions
    {
        /// <summary>
        /// Constructs a new text paint options
        /// </summary>
        public TextPaintOptions()
        {
        }


        /// <summary>
        /// Creates a clone of this object
        /// </summary>
        /// <returns>The closed object</returns>
        public TextPaintOptions Clone()
        {
            return (TextPaintOptions)this.MemberwiseClone();
        }

        /// <summary>
        /// An optional TextRange to painted as selected.
        /// </summary>
        public TextRange? Selection
        {
            get;
            set;
        }

        /// <summary>
        /// The color to be used for the selection background.
        /// </summary>
        public SKColor SelectionColor
        {
            get;
            set;
        }

        /// <summary>
        /// The color to be used for the default foreground.
        /// </summary>
        public SKColor TextDefaultColor { get; set; } = SKColors.Black;

        /// <summary>
        /// The color to be used for touch screen selection handles
        /// </summary>
        public SKColor SelectionHandleColor
        {
            get;
            set;
        }

        /// <summary>
        /// Scaling of the touch screen selection handles
        /// </summary>
        /// <remarks>
        /// Sets the scaling of the selection handles.  This can be used
        /// to keep the selection handle size consistent even if zooming in
        /// on rendered text.  Set to zero to disable selection handles
        /// </remarks>
        public float SelectionHandleScale
        {
            get;
            set;
        } = 0;

        /// <summary>
        /// Controls how font edges are drawn
        /// </summary>
        public SKFontEdging Edging
        {
            get;
            set;
        } = SKFontEdging.Antialias;


        /// <summary>
        /// Requests text be drawn at sub-pixel offsets
        /// </summary>
        public bool SubpixelPositioning
        {
            get;
            set;
        } = true;

        /// <summary>
        /// Controls whether text is rendered with anti-aliasing.
        /// </summary>
        [Obsolete("Use Edging property instead of IsAntialias")]
        public bool IsAntialias
        {
            get => Edging != SKFontEdging.Alias;
            set => Edging = value ? SKFontEdging.Antialias : SKFontEdging.Alias;
        }

        /// <summary>
        /// Controls whether text is rendered using LCD sub-pixel rendering.
        /// </summary>
        [Obsolete("Use Edging property instead of LcdRenderText")]
        public bool LcdRenderText
        {
            get => Edging == SKFontEdging.SubpixelAntialias;
            set => Edging = value ? SKFontEdging.SubpixelAntialias : SKFontEdging.Antialias;
        }


        /// <summary>
        /// Controls the font hint used when rendering text
        /// </summary>
        public SKFontHinting Hinting
        {
            get;
            set;
        } = SKFontHinting.Normal;

        /// <summary>
        /// A default set of paint options that renders text blocks without 
        /// a selection range.
        /// </summary>
        public static TextPaintOptions Default = new TextPaintOptions();

        public override bool Equals(object obj)
        {
            if (!(obj is TextPaintOptions options)) return base.Equals(obj);
            return Equals(options);
        }
        public bool Equals(TextPaintOptions other)
        {
            if (other == null) return false;
            if (Edging != other.Edging) return false;
            if (Hinting != other.Hinting) return false;
            if (EqualityComparer<TextRange?>.Default.Equals(Selection, other.Selection)) return false;
            if (SelectionColor != other.SelectionColor) return false;
            if (SelectionHandleColor != other.SelectionHandleColor) return false;
            if (SelectionHandleScale != other.SelectionHandleScale) return false;
            if (SubpixelPositioning != other.SubpixelPositioning) return false;
            if (TextDefaultColor != other.TextDefaultColor) return false;
            return true;
        }
    }
}
