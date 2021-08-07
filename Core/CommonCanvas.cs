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
        public new void Dispose()
        {
            base.Dispose();
            GC.SuppressFinalize(this);
        }
        ~CommonCanvas()
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
    }
}
