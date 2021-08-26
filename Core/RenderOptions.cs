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
        /// <summary>
        /// 此成员没有被使用过.<br/>
        /// This member is wasted.
        /// </summary>
        public bool TickBased;
        public bool PNGEncoder;
        public bool TransparentBackground;
        public bool ThinnerNotes;
        public bool DrawSeparator;
        public bool PreviewMode;
        public bool DrawGreySquare;
        public bool Gradient;
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
                DivideBarColor = 0xFF0000A0,
                TickBased = true,
                PNGEncoder = false,
                TransparentBackground = false,
                DrawSeparator = true,
                PreviewMode = false,
                AdditionalFFMpegArgument = string.Empty,
                DrawGreySquare = false,
                Gradient = false,
                ThinnerNotes = true,
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
