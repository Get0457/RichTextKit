// This file has been edited and modified from its original version.
// The original version of this file can be found at https://github.com/toptensoftware/RichTextKit/.
using System;
using System.Collections.Generic;
using System.Text;

namespace Get.RichTextKit
{
    /// <summary>
    /// Helper class to swap two values
    /// </summary>
    public static class SwapHelper
    {
        /// <summary>
        /// Swaps two values
        /// </summary>
        /// <typeparam name="T">The value type</typeparam>
        /// <param name="a">The first value</param>
        /// <param name="b">The second value</param>
        public static void Swap<T>(ref T a, ref T b)
        {
            var temp = a;
            a = b;
            b = temp;
        }
    }
}
