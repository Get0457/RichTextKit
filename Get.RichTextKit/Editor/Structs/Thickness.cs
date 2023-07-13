using System;
using System.Collections.Generic;
using System.Text;

namespace Get.RichTextKit.Editor.Structs;

public record struct Thickness(float Left, float Top, float Right, float Bottom)
{
    public Thickness(float uniform) : this(uniform, uniform, uniform, uniform) { }
}
