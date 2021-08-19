using SharpExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace QQS_UI.Core
{
    public unsafe class CommonCanvas : CanvasBase
    {
        public CommonCanvas(in RenderOptions options) : base(options)
        {
            for (int i = 0; i != 128; ++i)
            {
                keyx[i] = ((i / 12 * 126) + Global.GenKeyX[i % 12]) * width / 1350;
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
                        val = keyx[i + 1] - keyx[i];
                        break;
                    default:
                        val = keyx[i + 2] - keyx[i];
                        break;
                }
                keyw[i] = val;
            }
            keyw[127] = width - keyx[127];

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
                        notew[i] = keyx[i + 1] - keyx[i];
                        break;
                    default:
                        notew[i] = keyx[i + 1] - keyx[i - 1] - keyw[i - 1];
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
                        notex[i] = keyx[i];
                        break;
                    default:
                        notex[i] = keyx[i - 1] + notew[i - 1];
                        break;
                }
            }
            notex[127] = keyx[126] + keyw[126];
            notew[127] = width - notex[127];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetNoteX(int key)
        {
            return notex[key];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetKeyX(int key)
        {
            return keyx[key];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetNoteWidth(int key)
        {
            return notew[key];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetKeyWidth(int key)
        {
            return keyw[key];
        }
        /// <summary>
        /// 绘制所有的琴键.<br/>
        /// Draw all keys.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawKeys()
        {
            int i, j;
            int bh = keyh * 64 / 100;
            int bgr = keyh / 20;
            for (i = 0; i != 75; ++i) // 绘制所有白键. 参见 Global.DrawMap. Draws all white keys.
            {
                j = Global.DrawMap[i];
                FillRectangle(keyx[j], 0, keyw[j], keyh, KeyColors[j]);
                DrawRectangle(keyx[j], 0, keyw[j] + 1, keyh, 0xFF000000); // 绘制琴键之间的分隔线. Draws a seperator between two keys.
                if (!KeyPressed[j]) // 如果当前琴键未被按下
                {
                    DrawRectangle(keyx[j], 0, keyw[j] + 1, bgr, 0xFF000000);
                    FillRectangle(keyx[j] + 1, 1, keyw[j] - 1, bgr - 2, 0xFF999999); // 绘制琴键底部阴影. 感谢 Tweak 对阴影进行改善.
                }
            }
            int diff = keyh - bh;
            for (; i != 128; ++i) // 绘制所有黑键. Draws all black keys.
            {
                j = Global.DrawMap[i];
                FillRectangle(keyx[j], diff, keyw[j], bh, KeyColors[j]); // 重新绘制黑键及其颜色. Draws a black key (See Global.DrawMap).
                DrawRectangle(keyx[j], diff, keyw[j] + 1, bh, 0xFF000000);
            }
            FillRectangle(0, keyh - 2, width, keyh / 15, lineColor);
        }
        public new void Dispose()
        {
            base.Dispose();
            GC.SuppressFinalize(this);
        }
        ~CommonCanvas()
        {
            Dispose();
        }
        /// <summary>
        /// 绘制一个音符.<br/>
        /// Draws a note.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawNote(short key, int y, int height, uint noteColor)
        {
            if (height > 5)
            {
                --height;
            }
            else if (height < 1)
            {
                height = 1;
            }

            //FillRectangle(_KeyX[k] + 1, y, _KeyWidth[k] - 1, h, c);

            if (height >= 3) // 如果高度足够, 加框. If the height is enough, draw a note border.
            {
                FillRectangle(notex[key] + 1, y + 1, notew[key] - 1, height - 1, noteColor);
                DrawRectangle(notex[key], y, notew[key] + 1, height, 0xFF000000); // 绘制音符边框. 感谢 Tweak 对此的贡献.
            }
            else // 高度不足时, 将音符两侧填充上黑色. If the height is not enough, draw black border at the left and right of the note.
            {
                FillRectangle(notex[key] + 1, y, notew[key] - 1, height, noteColor);
                int x = notex[key];
                int xend = x + notew[key];
                for (int yend = y + height; y != yend; ++y)
                {
                    frameIdx[y][x] = 0xFF000000;
                    frameIdx[y][xend] = 0xFF000000;
                }
            }
        }
    }
}
