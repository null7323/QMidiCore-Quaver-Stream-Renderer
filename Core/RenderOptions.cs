using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace QQS_UI.Core
{
    /// <summary>
    /// 表示渲染的设置. 请使用 <see cref="CreateRenderOptions"/> 来创建一个 <see cref="RenderOptions"/> 结构.
    /// </summary>
    public struct RenderOptions
    {
        public bool TickBased;
        public bool PNGEncoder;
        public bool TransparentBackground;
        public bool Horizontal;
        public bool PreviewMode;
        public int Width, Height, FPS, CRF, KeyHeight;
        public RGBAColor DivideBarColor;
        public RGBAColor BackgroundColor;
        public double NoteSpeed;
        public string Input;
        public string Output;
        public string AdditionalFFMpegArgument;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RenderOptions CreateRenderOptions()
        {
            return new RenderOptions
            {
                Width = 1920,
                Height = 1080,
                FPS = 60,
                CRF = 17,
                KeyHeight = 162,
                NoteSpeed = 1,
                DivideBarColor = 0xFF000080,
                TickBased = true,
                PNGEncoder = false,
                TransparentBackground = false,
                Horizontal = false,
                PreviewMode = false,
                AdditionalFFMpegArgument = string.Empty,
                BackgroundColor = new RGBAColor
                {
                    A = 0xFF,
                    G = 0,
                    R = 0,
                    B = 0
                }
            };
        }
    }
}
