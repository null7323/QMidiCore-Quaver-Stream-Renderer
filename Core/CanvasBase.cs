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
        protected readonly int[] keyx = new int[128];
        protected readonly int[] notex = new int[128];
        protected readonly int[] keyw = new int[128];
        protected readonly int[] notew = new int[128];
        /// <summary>
        /// 表示一个空帧的全部像素.<br/>
        /// Represents an empty frame.<br/>
        /// </summary>
        /// <remarks>
        /// 空帧可以用来快速清空绘制完的一帧.<br/>
        /// An empty frame is able to clear the canvas quickly.
        /// </remarks>
        protected uint* emptyFrame;
        protected uint* frame;
        /// <summary>
        /// 表示指向帧的索引.<br/>
        /// Represents indexes of the frame.
        /// </summary>
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
                        // 黑键 black keys
                        DefaultKeyColors[i] = 0xFF000000;
                        break;
                    default:
                        // 白键 white keys
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
            // 如果要求使用 png 序列, 那么就加上 -vcodec png 指明编码器; 否则就添加像素格式.
            // if png encoder is required, append '-vcodec png' to specify the encoder; otherwise append pixel format.
            _ = options.PNGEncoder ? ffargs.Append("-vcodec png") : ffargs.Append("-pix_fmt yuv420p -crf ").Append(crf).Append(" -preset ultrafast");
            _ = ffargs.Append(' ').Append(options.AdditionalFFMpegArgument);
            //_ = !options.PreviewMode ? ffargs.Append(" \"").Append(options.Output).Append("\"") : ffargs.Append(" -f sdl2 Preview");
            if (options.PreviewMode)
            {
                Version osVer = Environment.OSVersion.Version;
                if (osVer.Major == 6 && osVer.Minor == 1)
                {
                    _ = ffargs.Append(" -f sdl Preview");
                }
                else
                {
                    _ = ffargs.Append(" -f sdl2 Preview");
                }
            }
            else
            {
                _ = ffargs.Append(" \"").Append(options.Output).Append("\"");
            }
            pipe = new FFMpeg(ffargs.ToString(), width, height);

            frame = (uint*)UnsafeMemory.Allocate(frameSize + ((uint)width * 4ul)); // malloc
            // 使用此方法: Call UnsafeMemory.Set in order to:
            // 清空分配的帧, 将它们全部初始化为0. Clear the newly allocated frame, filling it with 0.
            UnsafeMemory.Set(frame, 0, frameSize); // memset

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

        /// <summary>
        /// 绘制矩形边框.<br/>
        /// Draws a rectangle which is not filled with specified color.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawRectangle(int x, int y, int width, int height, uint color)
        {

            int i;
            //if (x < _Width)
            for (i = y; i < y + height; ++i)
            {
                frameIdx[i][x] = color;
            }
            //if (y < _Height)
            for (i = x; i < x + width; ++i)
            {
                frameIdx[y][i] = color;
            }
            //if (w > 1)
            for (i = y; i < y + height; ++i)
            {
                frameIdx[i][x + width - 1] = color;
            }
            //if (h > 1)
            for (i = x; i < x + width; ++i)
            {
                frameIdx[y + height - 1][i] = color;
            }
        }

        /// <summary>
        /// 用指定的颜色填满指定区域.<br/>
        /// Fill specified area of the frame with given color.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillRectangle(int x, int y, int width, int height, uint color)
        {
            for (int i = x, xend = x + width; i != xend; ++i)
            {
                for (int j = y, yend = y + height; j != yend; ++j)
                {
                    frameIdx[j][i] = color;
                }
            }
        }

        /// <summary>
        /// 清空当前画布.<br/>
        /// Clear the canvas immediately.
        /// </summary>
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
        /// <summary>
        /// 向 FFMpeg 写入当前帧.<br/>
        /// Write current frame to ffmpeg.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFrame()
        {
            pipe.WriteFrame(frame);
        }
    }
}
