using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QQS_UI.Core
{
    /// <summary>
    /// 表示一个RGBA颜色结构.<br/>
    /// Represents a color structure of RGBA.
    /// </summary>
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
                // 直接初始化. Initialize 'this' directly.
                // 可以直接复制是因为 uint 是以小端序存储. It is ok to assign in this way, for unsigned int is little endian.
                Buffer.MemoryCopy(&color, instance, 4, 4);
            }
        }
        public RGBAColor(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
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
        public static unsafe explicit operator YUVColor(RGBAColor col)
        {
            return new YUVColor(col);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct YUVColor
    {
        public float Y;
        public float U;
        public float V;
        public YUVColor(RGBAColor rgba) // alpha is ignored.
        {
            Y = (0.299F * rgba.R) + (0.587F * rgba.G) + (0.114F * rgba.B);
            U = (-0.147F * rgba.R) + (-0.289F * rgba.G) + (-0.436F * rgba.B);
            V = (0.615F * rgba.R) + (-0.515f * rgba.G) + (-0.1f * rgba.B);
        }
        public static explicit operator RGBAColor(in YUVColor yuv)
        {
            return new RGBAColor
            {
                A = 255,
                R = (byte)(yuv.Y + (1.140F * yuv.U)),
                G = (byte)(yuv.Y - (0.395F * yuv.U) - (0.581F * yuv.V)),
                B = (byte)(yuv.Y + (2.032F * yuv.U))
            };
        }
    }
}
