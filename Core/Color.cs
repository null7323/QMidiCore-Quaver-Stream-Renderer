using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QQS_UI.Core
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RGBAColor
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;
        public unsafe RGBAColor(uint color)
        {
            fixed (RGBAColor* instance = &this)
            {
                Buffer.MemoryCopy(&color, instance, 4, 4);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe implicit operator uint(RGBAColor color)
        {
            return *(uint*)&color;
        }
        public static unsafe implicit operator RGBAColor(uint color)
        {
            return new RGBAColor(color);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HSLColor
    {
        public float Hue;
        public float Saturation;
        public float Luminance;
        public static unsafe explicit operator HSLColor(RGBAColor color)
        {
            HSLColor hsl = new HSLColor();
            float r = color.R / 255f;
            float g = color.G / 255f;
            float b = color.B / 255f;
            float max = r > g ? r > b ? r : b : r > b ? g : b;
            float min = r < g ? r < b ? r : b : g < b ? g : b;

            hsl.Luminance = (max + min) / 2f; // must >= 0
            if (max == min)
            {
                hsl.Hue = 0;
                hsl.Saturation = 0;
            }
            else
            {
                hsl.Hue = max == r
                    ? (60f * (g - b) / (max - min)) + g >= b ? 0 : 360
                    : max == g ? (60f * (b - r) / (max - min)) + 120 : (60f * (r - g) / (max - min)) + 240;
            }
            hsl.Saturation = hsl.Luminance == 0 ? 0 : hsl.Luminance <= 0.5f ? (max - min) / (max + min) : (max - min) / (2 - (min + max));

            hsl.Hue = hsl.Hue > 360 ? 360 : (hsl.Hue < 0) ? 0 : hsl.Hue;
            hsl.Saturation = hsl.Saturation > 1 ? 1 : hsl.Saturation < 0 ? 0 : hsl.Saturation * 100;
            hsl.Luminance = hsl.Luminance > 1 ? 1 : hsl.Luminance < 0 ? 0 : hsl.Luminance * 100;
            return hsl;
        }
    }
}
