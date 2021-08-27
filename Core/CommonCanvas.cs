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
        private readonly RGBAColor[][][] NoteGradients = new RGBAColor[128][][]; // 补充: 感谢 Tweak 为渐变音符做出的贡献
        private readonly RGBAColor[][] PressedWhiteKeyGradients;
        private readonly RGBAColor[] UnpressedWhiteKeyGradients;
        public readonly ushort[] KeyTracks = new ushort[128];
        private readonly bool gradient;
        private readonly bool separator;
        private readonly HorizontalGradientDirection noteGradientDirection;
        private readonly VerticalGradientDirection separatorGradientDirection, keyboardGradientDirection;
        public CommonCanvas(in RenderOptions options) : base(options)
        {
            gradient = options.Gradient;
            separator = options.DrawSeparator;
            noteGradientDirection = options.NoteGradientDirection;
            separatorGradientDirection = options.SeparatorGradientDirection;
            keyboardGradientDirection = options.KeyboardGradientDirection;

            UnpressedWhiteKeyGradients = new RGBAColor[keyh - (keyh / 20)];
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

            if (options.ThinnerNotes)
            {
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
            else
            {
                Array.Copy(keyx, notex, 128);
                Array.Copy(keyw, notew, 128);
            }

            // 末颜色与初颜色的比值
            double referenceGradientRatio = Math.Pow(Global.NoteGradientScale, 10);
            for (int i = 0; i != 128; ++i)
            {
                // 初始化索引为i的琴键对应的颜色数组.
                NoteGradients[i] = new RGBAColor[Global.Colors.Length][];
                for (int j = 0; j != Global.Colors.Length; ++j)
                {
                    // 创建大小为音符宽度的数组, 这样就能实现渐变
                    NoteGradients[i][j] = new RGBAColor[notew[i]];
                    // cols是一个引用, 没有发生值的复制
                    RGBAColor[] cols = NoteGradients[i][j];
                    // 初颜色
                    RGBAColor gradientStart = Global.Colors[j];
                    double r = gradientStart.R, g = gradientStart.G, b = gradientStart.B;
                    // 根据实际音符宽度计算每2个像素之间颜色之比.
                    double actualGradientRatio = Math.Pow(referenceGradientRatio, 1.0 / (notew[i] - 1));

                    for (int k = 0, len = NoteGradients[i][j].Length; k != len; ++k)
                    {
                        int idx = noteGradientDirection == HorizontalGradientDirection.FromLeftToRight ? k : (len - k - 1);
                        cols[idx] = new RGBAColor((byte)r, (byte)g, (byte)b, gradientStart.A);
                        r /= actualGradientRatio;
                        g /= actualGradientRatio;
                        b /= actualGradientRatio;
                    }

                }
            }
            double keyrgb = 255;
            referenceGradientRatio = Math.Pow(Math.Pow(Global.UnpressedWhiteKeyGradientScale, 154), 1.0 / UnpressedWhiteKeyGradients.Length);
            int yThreshold = keyh - (keyh * 64 / 100);
            int currentY = keyh / 20;
            if (keyboardGradientDirection == VerticalGradientDirection.FromButtomToTop)
            {
                for (int i = 0; i != UnpressedWhiteKeyGradients.Length; ++i)
                {
                    UnpressedWhiteKeyGradients[i] = new RGBAColor((byte)keyrgb, (byte)keyrgb, (byte)keyrgb, 255);
                    if (currentY < yThreshold)
                    {
                        keyrgb /= ((referenceGradientRatio - 1) / 3) + 1;
                    }
                    else
                    {
                        keyrgb /= referenceGradientRatio;
                    }
                    ++currentY;
                }
            }
            else
            {
                currentY = keyh;
                yThreshold = keyh - yThreshold;
                for (int i = UnpressedWhiteKeyGradients.Length - 1; i != -1; --i)
                {
                    UnpressedWhiteKeyGradients[i] = new RGBAColor((byte)keyrgb, (byte)keyrgb, (byte)keyrgb, 255);
                    if (currentY > yThreshold)
                    {
                        keyrgb /= ((referenceGradientRatio - 1) / 3) + 1;
                    }
                    else
                    {
                        keyrgb /= referenceGradientRatio;
                    }
                    --currentY;
                }
            }
            PressedWhiteKeyGradients = new RGBAColor[Global.Colors.Length][];
            referenceGradientRatio = Math.Pow(Math.Pow(Global.PressedWhiteKeyGradientScale, 162), 1.0 / keyh);
            for (int i = 0; i != Global.Colors.Length; ++i)
            {
                PressedWhiteKeyGradients[i] = new RGBAColor[keyh];
                RGBAColor[] cols = PressedWhiteKeyGradients[i];
                RGBAColor gradientStart = Global.Colors[i];
                double r = gradientStart.R,
                    g = gradientStart.G,
                    b = gradientStart.B;
                if (keyboardGradientDirection == VerticalGradientDirection.FromButtomToTop)
                {
                    currentY = 0;
                    for (int j = 0; j != keyh; ++j)
                    {
                        cols[j] = new RGBAColor((byte)r, (byte)g, (byte)b, gradientStart.A);
                        if (currentY < yThreshold)
                        {
                            double gradientRatio = ((referenceGradientRatio - 1) / 1.2) + 1;
                            r /= gradientRatio;
                            g /= gradientRatio;
                            b /= gradientRatio;
                        }
                        else
                        {
                            r /= referenceGradientRatio;
                            g /= referenceGradientRatio;
                            b /= referenceGradientRatio;
                        }
                        ++currentY;
                    }
                }
                else
                {
                    currentY = keyh;
                    for (int j = keyh - 1; j != -1; --j)
                    {
                        cols[j] = new RGBAColor((byte)r, (byte)g, (byte)b, gradientStart.A);
                        if (currentY > yThreshold)
                        {
                            double gradientRatio = ((referenceGradientRatio - 1) / 1.2) + 1;
                            r /= gradientRatio;
                            g /= gradientRatio;
                            b /= gradientRatio;
                        }
                        else
                        {
                            r /= referenceGradientRatio;
                            g /= referenceGradientRatio;
                            b /= referenceGradientRatio;
                        }
                        --currentY;
                    }
                }
            }
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
            if (separator)
            {
                FillRectangle(0, keyh - 2, width, keyh / 15, lineColor);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawGradientKeys()
        {
            int i, j, bh = keyh * 64 / 100, bgr = keyh / 20;
            for (i = 0; i != 75; ++i)
            {
                j = Global.DrawMap[i];
                if (KeyPressed[j])
                {
                    //FillRectangle(keyx[j], 0, keyw[j], keyh, KeyColors[j]);
                    for (int y = 0, yend = keyh; y != yend; ++y)
                    {
                        RGBAColor col = PressedWhiteKeyGradients[KeyTracks[j] % Global.Colors.Length][y];
                        for (int x = keyx[j], xend = x + keyw[j]; x != xend; ++x)
                        {
                            frameIdx[y][x] = col;
                        }
                    }
                }
                else
                {
                    for (int y = bgr, yend = keyh; y != yend; ++y)
                    {
                        for (int x = keyx[j], xend = x + keyw[j]; x != xend; ++x)
                        {
                            frameIdx[y][x] = UnpressedWhiteKeyGradients[y - bgr];
                        }
                    }
                    DrawRectangle(keyx[j], 0, keyw[j] + 1, bgr, 0xFF000000);
                    FillRectangle(keyx[j] + 1, 1, keyw[j] - 1, bgr - 2, 0xFF999999); // 绘制琴键底部阴影. 感谢 Tweak 对阴影进行改善.
                }
                DrawRectangle(keyx[j], 0, keyw[j] + 1, keyh, 0xFF000000);
            }
            int diff = keyh - bh;
            for (; i != 128; ++i) // 绘制所有黑键. Draws all black keys.
            {
                j = Global.DrawMap[i];
                FillRectangle(keyx[j], diff, keyw[j], bh, KeyColors[j]); // 重新绘制黑键及其颜色. Draws a black key (See Global.DrawMap).
                DrawRectangle(keyx[j], diff, keyw[j] + 1, bh, 0xFF000000);
            }
            //FillRectangle(0, keyh - 2, width, keyh / 15, lineColor);
            DrawSeperator();
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
            if (height < 1)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawGradientNote(short key, int colorIndex, int y, int height)
        {
            RGBAColor[] gradientColors = NoteGradients[key][colorIndex];
            if (height < 1)
            {
                height = 1;
            }
            if (height >= 3)
            {
                for (int x = notex[key] + 1, xend = x + notew[key] - 1, initx = x; x != xend; ++x)
                {
                    uint col = gradientColors[x - initx];
                    for (int dy = y + 1, yend = dy + height - 1; dy != yend; ++dy)
                    {
                        frameIdx[dy][x] = col;
                    }
                }
                DrawRectangle(notex[key], y, notew[key] + 1, height, 0xFF000000);
            }
            else
            {
                for (int x = notex[key] + 1, xend = x + notew[key] - 1, initx = x; x != xend; ++x)
                {
                    uint col = gradientColors[x - initx];
                    for (int dy = y, yend = dy + height; dy != yend; ++dy)
                    {
                        frameIdx[dy][x] = col;
                    }
                }
                int borderx = notex[key];
                int borderxEnd = borderx + notew[key];
                for (int yend = y + height; y != yend; ++y)
                {
                    frameIdx[y][borderx] = 0xFF000000;
                    frameIdx[y][borderxEnd] = 0xFF000000;
                }
            }
        }

        private void DrawSeperator()
        {
            if (!separator)
            {
                return;
            }
            if (gradient)
            {
                RGBAColor gradientStart = lineColor;
                double r = gradientStart.R,
                    g = gradientStart.G,
                    b = gradientStart.B;
                double gradientScale = Math.Pow(Math.Pow(Global.SeparatorGradientScale, 162 / 15), 1.0 / (keyh / 15));
                if (separatorGradientDirection == VerticalGradientDirection.FromButtomToTop)
                {
                    for (int y = keyh - 2, yend = y + (keyh / 15); y != yend; ++y)
                    {
                        uint col = (uint)((byte)r | ((byte)g << 8) | ((byte)b << 16) | (gradientStart.A << 24));
                        for (int x = 0; x != width; ++x)
                        {
                            frameIdx[y][x] = col;
                        }
                        r /= gradientScale;
                        g /= gradientScale;
                        b /= gradientScale;
                    }
                }
                else
                {
                    for (int y = keyh - 3 + (keyh / 15), yend = keyh - 3; y != yend; --y)
                    {
                        uint col = (uint)((byte)r | ((byte)g << 8) | ((byte)b << 16) | (gradientStart.A << 24));
                        for (int x = 0; x != width; ++x)
                        {
                            frameIdx[y][x] = col;
                        }
                        r /= gradientScale;
                        g /= gradientScale;
                        b /= gradientScale;
                    }
                }
            }
            else
            {
                FillRectangle(0, keyh - 2, width, keyh / 15, lineColor);
            }
        }
    }
}
