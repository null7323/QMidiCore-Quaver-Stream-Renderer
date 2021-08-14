using SharpExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace QQS_UI.Core
{
    public abstract unsafe class CanvasBase : IDisposable
    {
        protected readonly int width;
        protected readonly int height;
        protected readonly int fps;
        protected readonly int crf;
        protected readonly int keyh;
        protected readonly uint lineColor;
        protected readonly ulong frameSize;
        protected FFMpeg pipe;
        protected readonly int[] keyX = new int[128];
        protected readonly int[] noteX = new int[128];
        protected readonly int[] keyw = new int[128];
        protected readonly int[] notew = new int[128];
        protected uint* emptyFrame;
        protected uint* frame;
        protected uint** frameIdx;
        protected static readonly uint[] DefaultKeyColors = new uint[128];
        public readonly uint[] KeyColors = new uint[128];
        public readonly bool[] KeyPressed = new bool[128];
        static CanvasBase()
        {
            for (int i = 0; i != 128; ++i)
            {
                switch (i % 12)
                {
                    case 1:
                    case 3:
                    case 6:
                    case 8:
                    case 10:
                        DefaultKeyColors[i] = 0xFF000000;
                        break;
                    default:
                        DefaultKeyColors[i] = 0xFFFFFFFF;
                        break;
                }
            }
        }
        public CanvasBase(in RenderOptions options)
        {
            width = options.Width;
            height = options.Height;
            fps = options.FPS;
            crf = options.CRF;
            keyh = options.KeyHeight;
            lineColor = options.DivideBarColor;
            frameSize = (ulong)width * (ulong)height * 4ul;

            StringBuilder ffargs = new StringBuilder();
            _ = ffargs.Append("-y -hide_banner -f rawvideo -pix_fmt rgba -s ").Append(width).Append('x')
                .Append(height).Append(" -r ").Append(fps).Append(" -i - ");
            // if png encoder is required, append '-vcodec png' to specify the encoder; otherwise append pixel format.
            _ = options.PNGEncoder ? ffargs.Append("-vcodec png") : ffargs.Append("-pix_fmt yuv420p -crf ").Append(crf).Append(" -preset ultrafast");
            _ = ffargs.Append(' ').Append(options.AdditionalFFMpegArgument);
            _ = !options.PreviewMode ? ffargs.Append(" \"").Append(options.Output).Append("\"") : ffargs.Append(" -f sdl2 Preview");
            pipe = new FFMpeg(ffargs.ToString(), width, height);

            frame = (uint*)UnsafeMemory.Allocate(frameSize + ((uint)width * 4ul));
            UnsafeMemory.Set(frame, 0, frameSize);

            frameIdx = (uint**)UnsafeMemory.Allocate((ulong)height * (ulong)sizeof(void*));
            for (int i = 0; i != height; ++i)
            {
                frameIdx[i] = frame + ((height - i - 1) * width);
            }
            emptyFrame = (uint*)UnsafeMemory.Allocate(frameSize);
            uint backgroundColor = options.TransparentBackground ? (options.BackgroundColor & 0x00FFFFFF) : (uint)options.BackgroundColor;
            for (uint i = 0, loop = (uint)frameSize / 4; i != loop; ++i)
            {
                frame[i] = backgroundColor;
            }
            UnsafeMemory.Copy(emptyFrame, frame, frameSize);
            fixed (bool* kp = KeyPressed)
            {
                UnsafeMemory.Set(kp, 0, 128);
            }
        }

        public void Dispose()
        {
            pipe.Dispose();
            if (frame != null)
            {
                UnsafeMemory.Free(frame);
            }
            if (emptyFrame != null)
            {
                UnsafeMemory.Free(emptyFrame);
            }
            if (frameIdx != null)
            {
                UnsafeMemory.Free(frameIdx);
            }
            frame = null;
            emptyFrame = null;
            frameIdx = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawRectangle(int x, int y, int w, int h, uint c)
        {

            int i;
            //if (x < _Width)
            for (i = y; i < y + h; ++i)
            {
                frameIdx[i][x] = c;
            }
            //if (y < _Height)
            for (i = x; i < x + w; ++i)
            {
                frameIdx[y][i] = c;
            }
            //if (w > 1)
            for (i = y; i < y + h; ++i)
            {
                frameIdx[i][x + w - 1] = c;
            }
            //if (h > 1)
            for (i = x; i < x + w; ++i)
            {
                frameIdx[y + h - 1][i] = c;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillRectangle(int x, int y, int w, int h, uint c)
        {
            for (int i = x, xend = x + w; i != xend; ++i)
            {
                for (int j = y, yend = y + h; j != yend; ++j)
                {
                    frameIdx[j][i] = c;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            fixed (uint* dst = KeyColors, src = DefaultKeyColors)
            {
                UnsafeMemory.Copy(dst, src, 512);
            }
            UnsafeMemory.Copy(frame, emptyFrame, frameSize);
            fixed (bool* kp = KeyPressed)
            {
                UnsafeMemory.Set(kp, 0, 128);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFrame()
        {
            pipe.WriteFrame(frame);
        }
    }
}
