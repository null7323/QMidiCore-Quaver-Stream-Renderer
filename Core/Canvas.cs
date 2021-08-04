using SharpExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace QQS_UI.Core
{
    public unsafe class Canvas : IDisposable
    {
        private readonly int width;
        private readonly int height;
        private readonly int fps;
        private readonly int crf;
        private readonly int keyh;
        private readonly uint lineColor;
        private readonly ulong frameSize;
        private FFMpeg pipe;
        private readonly int[] keyX = new int[128];
        private readonly int[] noteX = new int[128];
        private readonly int[] keyw = new int[128];
        private readonly int[] notew = new int[128];
        private uint* emptyFrame;
        private uint* frame;
        private uint** frameIdx;
        private static readonly uint[] DefaultKeyColors = new uint[128];
        public readonly uint[] KeyColors = new uint[128];
        public readonly bool[] KeyPressed = new bool[128];
        static Canvas()
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
        public Canvas(in RenderOptions options)
        {
            width = options.Width;
            height = options.Height;
            fps = options.FPS;
            crf = options.CRF;
            keyh = options.KeyHeight;
            lineColor = options.LineColor;
            frameSize = (ulong)width * (ulong)height * 4ul;
            StringBuilder ffargs = new StringBuilder();
            _ = ffargs.Append("-y -hide_banner -f rawvideo -pix_fmt rgba -s ").Append(width).Append('x')
                .Append(height).Append(" -r ").Append(fps).Append(" -i - -preset ultrafast ");
            _ = options.PNGEncoder ? ffargs.Append("-vcodec png") : ffargs.Append("-pix_fmt yuv420p");
            _ = ffargs.Append(" -crf ").Append(crf).Append(" \"").Append(options.Output).Append("\"");
            pipe = new FFMpeg(ffargs.ToString(), width, height);

            frame = (uint*)UnsafeMemory.Allocate(frameSize + ((uint)width * 4ul));
            UnsafeMemory.Set(frame, 0, frameSize);

            frameIdx = (uint**)UnsafeMemory.Allocate((ulong)height * (ulong)sizeof(void*));
            for (int i = 0; i != height; ++i)
            {
                frameIdx[i] = frame + ((height - i - 1) * width);
            }

            for (int i = 0; i != 128; ++i)
            {
                keyX[i] = ((i / 12 * 126) + Global.GenKeyX[i % 12]) * width / 1350;
            }
            for (int i = 0; i != 127; ++i)
            {
                int val;
                switch (i % 12)
                {
                    case 1:
                    case 3:
                    case 6:
                    case 8:
                    case 10:
                        val = width * 9 / 1350;
                        break;
                    case 4:
                    case 11:
                        val = keyX[i + 1] - keyX[i];
                        break;
                    default:
                        val = keyX[i + 2] - keyX[i];
                        break;
                }
                keyw[i] = val;
            }
            keyw[127] = width - keyX[127];

            for (int i = 0; i != 127; ++i)
            {
                switch (i % 12)
                {
                    case 1:
                    case 3:
                    case 6:
                    case 8:
                    case 10:
                        notew[i] = keyw[i];
                        break;
                    case 0:
                    case 5:
                        notew[i] = keyX[i + 1] - keyX[i];
                        break;
                    default:
                        notew[i] = keyX[i + 1] - keyX[i - 1] - keyw[i - 1];
                        break;
                }
            }
            for (int i = 0; i != 127; ++i)
            {
                switch (i % 12)
                {
                    case 0:
                    case 5:
                    case 1:
                    case 3:
                    case 6:
                    case 8:
                    case 10:
                        noteX[i] = keyX[i];
                        break;
                    default:
                        noteX[i] = keyX[i - 1] + notew[i - 1];
                        break;
                }
            }
            noteX[127] = keyX[126] + keyw[126];
            notew[127] = width - noteX[127];

            emptyFrame = (uint*)UnsafeMemory.Allocate(frameSize);
            uint fillInColor = options.TransparentBackground ? 0x00000000 : 0xFF000000;
            for (uint i = 0, loop = (uint)frameSize / 4; i != loop; ++i)
            {
                frame[i] = fillInColor;
            }
            UnsafeMemory.Copy(emptyFrame, frame, frameSize);
            fixed (bool* kp = KeyPressed)
            {
                UnsafeMemory.Set(kp, 0, 128);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawKeys()
        {
            int i, j;
            int bh = keyh * 64 / 100;
            int bgr = keyh / 15;
            for (i = 0; i != 75; ++i)
            {
                j = Global.DrawMap[i];
                FillRectangle(keyX[j], 0, keyw[j], keyh, KeyColors[j]);
                DrawRectangle(keyX[j], 0, keyw[j] + 1, keyh, 0xFF000000); // 绘制琴键之间的分隔线
                if (!KeyPressed[j])
                {
                    DrawRectangle(keyX[j], 0, keyw[j] + 1, bgr, 0xFF000000);
                    FillRectangle(keyX[j] + 1, 1, keyw[j] - 1, bgr - 2, 0xFF999999); // 绘制琴键底部阴影. 感谢 Tweak 对阴影进行改善.
                }
            }
            int diff = keyh - bh;
            for (; i != 128; ++i)
            {
                j = Global.DrawMap[i];
                FillRectangle(keyX[j], diff, keyw[j], bh, KeyColors[j]); // 重新绘制黑键及其颜色
                DrawRectangle(keyX[j], diff, keyw[j] + 1, bh, 0xFF000000);
            }
            FillRectangle(0, keyh - 2, width, keyh / 15, lineColor);
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
            GC.SuppressFinalize(this);
        }
        ~Canvas()
        {
            Dispose();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawNote(short k, int y, int h, uint c)
        {
            if (h > 5)
            {
                --h;
            }

            if (h < 1)
            {
                h = 1;
            }
            //FillRectangle(_KeyX[k] + 1, y, _KeyWidth[k] - 1, h, c);
            FillRectangle(noteX[k] + 1, y, notew[k] - 1, h, c);
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
    }
}
