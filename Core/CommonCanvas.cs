using SharpExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace QQS_UI.Core
{
    public sealed unsafe class CommonCanvas : CanvasBase
    {
        private readonly RGBAColor[][][] NoteGradients = new RGBAColor[128][][]; // 补充: 感谢 Tweak 为渐变音符做出的贡献
        private readonly RGBAColor[][][] BrightenedNoteGradients = new RGBAColor[128][][]; // 用于存储处理过的渐变色
        private readonly RGBAColor[][] PressedWhiteKeyGradients;
        private readonly RGBAColor[] UnpressedWhiteKeyGradients;
        private readonly RGBAColor[] BorderColors;
        private readonly RGBAColor[] DenseNoteColors;
        public readonly ushort[] KeyTracks = new ushort[128];
        private readonly bool enableGradient;
        private readonly bool separator;
        private readonly bool betterBlackKeys;
        private readonly bool whiteKeyShade;
        private readonly bool brighterNotesOnHit;
        private readonly byte pressedNotesShadeDecrement;
        private readonly int borderWidth, borderHeight;
        private readonly HorizontalGradientDirection noteGradientDirection;
        private readonly VerticalGradientDirection separatorGradientDirection, keyboardGradientDirection;
        public CommonCanvas(in RenderOptions options) : base(options)
        {
            enableGradient = options.Gradient;
            separator = options.DrawSeparator;
            noteGradientDirection = options.NoteGradientDirection;
            separatorGradientDirection = options.SeparatorGradientDirection;
            keyboardGradientDirection = options.KeyboardGradientDirection;
            betterBlackKeys = options.BetterBlackKeys;
            whiteKeyShade = options.WhiteKeyShade;
            brighterNotesOnHit = options.BrighterNotesOnHit;
            pressedNotesShadeDecrement = (byte)options.PressedNotesShadeDecrement;

            if (pressedNotesShadeDecrement == 0)
            {
                brighterNotesOnHit = false;
            }

            borderWidth = Global.EnableNoteBorder ? Math.Max((int)Math.Round(0.0006 * Global.NoteBorderWidth * width), 1) : 0;
            borderHeight = (int)Math.Round((double)borderWidth / width * height);

            if (Global.EnableNoteBorder)
            {
                BorderColors = new RGBAColor[Global.KeyColors.Length];
                Array.Copy(Global.KeyColors, BorderColors, Global.KeyColors.Length);
                for (int i = 0; i != BorderColors.Length; ++i)
                {
                    ref RGBAColor col = ref BorderColors[i];
                    col.R = (byte)Math.Round(col.R / Global.NoteBorderShade);
                    col.G = (byte)Math.Round(col.G / Global.NoteBorderShade);
                    col.B = (byte)Math.Round(col.B / Global.NoteBorderShade);
                }
            }
            if (Global.EnableDenseNoteEffect)
            {
                DenseNoteColors = new RGBAColor[Global.KeyColors.Length];
                Array.Copy(Global.KeyColors, DenseNoteColors, Global.KeyColors.Length);
                for (int i = 0; i != DenseNoteColors.Length; ++i)
                {
                    ref RGBAColor col = ref DenseNoteColors[i];
                    col.R = (byte)Math.Round(col.R / Global.DenseNoteShade);
                    col.G = (byte)Math.Round(col.G / Global.DenseNoteShade);
                    col.B = (byte)Math.Round(col.B / Global.DenseNoteShade);
                }
            }

            UnpressedWhiteKeyGradients = new RGBAColor[keyh - (whiteKeyShade ? (keyh / 20) : 0)];
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

            // 音符不透明度与255.0的比值
            double alphaRatio = Global.NoteAlpha / 255.0;
            // 末颜色与初颜色的比值
            double referenceGradientRatio = Math.Pow(Global.NoteGradientScale, 10);
            for (int i = 0; i != 128; ++i)
            {
                // 初始化索引为i的琴键对应的颜色数组.
                NoteGradients[i] = new RGBAColor[Global.KeyColors.Length][];
                if (brighterNotesOnHit)
                {
                    BrightenedNoteGradients[i] = new RGBAColor[Global.KeyColors.Length][];
                }
                for (int j = 0; j != Global.KeyColors.Length; ++j)
                {
                    // 创建大小为音符宽度的数组, 这样就能实现渐变
                    NoteGradients[i][j] = new RGBAColor[notew[i] - (2 * borderWidth)];
                    if (brighterNotesOnHit)
                    {
                        // 同时地, 我们创建一个相同长度的数组, 用于实现当对应音符按下时变化后的颜色.
                        BrightenedNoteGradients[i][j] = new RGBAColor[NoteGradients[i][j].Length];
                    }
                    // cols是一个引用, 没有发生值的复制
                    RGBAColor[] cols = NoteGradients[i][j];
                    // 初颜色
                    RGBAColor gradientStart = Global.KeyColors[j];
                    double r = gradientStart.R, g = gradientStart.G, b = gradientStart.B;
                    // 根据实际音符宽度计算每2个像素之间颜色之比.
                    double actualGradientRatio = Math.Pow(referenceGradientRatio, 1.0 / (notew[i] - 1));

                    for (int k = 0, len = NoteGradients[i][j].Length; k != len; ++k)
                    {
                        // 根据渐变方向改变索引
                        int idx = noteGradientDirection == HorizontalGradientDirection.FromLeftToRight ? k : (len - k - 1);
                        cols[idx] = new RGBAColor((byte)r, (byte)g, (byte)b, gradientStart.A);
                        if (Global.TranslucentNotes) // 如果是半透明音符, 那么调整颜色
                        {
                            ref RGBAColor c = ref cols[idx];
                            c.R = (byte)(background.R + Math.Round((c.R - background.R) * alphaRatio));
                            c.G = (byte)(background.G + Math.Round((c.G - background.G) * alphaRatio));
                            c.B = (byte)(background.B + Math.Round((c.B - background.B) * alphaRatio));
                            c.A = 0xFF;
                        }

                        if (brighterNotesOnHit)
                        {
                            // 进一步在已经得到的颜色上处理.
                            BrightenedNoteGradients[i][j][idx] = RGBAColor.MixColors(cols[idx], RGBAColor.White, pressedNotesShadeDecrement);
                        }

                        r /= actualGradientRatio;
                        g /= actualGradientRatio;
                        b /= actualGradientRatio;
                    }

                }
            }
            if (keyh == 0)
            {
                return;
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
            PressedWhiteKeyGradients = new RGBAColor[Global.KeyColors.Length][];
            referenceGradientRatio = Math.Pow(Math.Pow(Global.PressedWhiteKeyGradientScale, 162), 1.0 / keyh);
            for (int i = 0; i != Global.KeyColors.Length; ++i)
            {
                PressedWhiteKeyGradients[i] = new RGBAColor[keyh];
                RGBAColor[] cols = PressedWhiteKeyGradients[i];
                RGBAColor gradientStart = Global.KeyColors[i];
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
        public void DrawKeys()
        {
            if (keyh == 0)
            {
                return;
            }
            int i, j;
            int bh = (whiteKeyShade || betterBlackKeys) ? keyh * 64 / 100 : keyh * 66 / 100;
            int bgr = keyh / 20;
            for (i = 0; i != 75; ++i) // 绘制所有白键. 参见 Global.DrawMap. Draws all white keys.
            {
                j = Global.DrawMap[i];
                FillRectangle(keyx[j], 0, keyw[j], keyh, KeyColors[j]);
                DrawRectangle(keyx[j], 0, keyw[j] + 1, keyh, 0xFF000000); // 绘制琴键之间的分隔线. Draws a seperator between two keys.
                if (whiteKeyShade && !KeyPressed[j]) // 如果当前琴键未被按下
                {
                    DrawRectangle(keyx[j], 0, keyw[j] + 1, bgr, 0xFF000000);
                    FillRectangle(keyx[j] + 1, 1, keyw[j] - 1, bgr - 2, 0xFF999999); // 绘制琴键底部阴影. 感谢 Tweak 对阴影进行改善.
                }

            }
            int diff = keyh - bh;
            if (!betterBlackKeys)
            {
                for (; i != 128; ++i) // 绘制所有黑键. Draws all black keys.
                {
                    j = Global.DrawMap[i];
                    FillRectangle(keyx[j], diff, keyw[j], bh, KeyColors[j]); // 重新绘制黑键及其颜色. Draws a black key (See Global.DrawMap).
                    DrawRectangle(keyx[j], diff, keyw[j] + 1, bh, 0xFF000000);
                }
            }
            DrawSeperator();
            if (betterBlackKeys)
            {
                int dtHeight = (int)Math.Round(keyh / 45.0);
                int dtWidth = (int)Math.Round(width / 1500.0);
                for (i = 75; i != 128; ++i)
                {
                    j = Global.DrawMap[i];
                    if (KeyPressed[j])
                    {
                        FillRectangle(keyx[j] - dtWidth, diff - dtWidth, keyw[j] + (2 * dtWidth), bh + dtWidth - 2, 0xFF363636);
                        FillRectangle(keyx[j], diff, keyw[j], bh - 2, KeyColors[j]);
                    }
                    else
                    {
                        FillRectangle(keyx[j] - dtWidth, diff - dtWidth, keyw[j] + (2 * dtWidth), bh + dtWidth, 0xFF363636);
                        FillRectangle(keyx[j], diff, keyw[j], bh + dtHeight, 0xFF000000);
                    }
                }
            }
        }
        public void DrawGradientKeys()
        {
            if (keyh == 0)
            {
                return;
            }
            // bh: 黑键的高(不是坐标!)
            int i, j;
            int bh = (whiteKeyShade || betterBlackKeys) ? keyh * 64 / 100 : keyh * 66 / 100;
            int bgr = keyh / 20;
            for (i = 0; i != 75; ++i) // 先画白键
            {
                j = Global.DrawMap[i];
                if (KeyPressed[j])
                {
                    //FillRectangle(keyx[j], 0, keyw[j], keyh, KeyColors[j]);
                    for (int y = 0, yend = keyh; y != yend; ++y)
                    {
                        RGBAColor col = PressedWhiteKeyGradients[KeyTracks[j] % Global.KeyColors.Length][y];
                        for (int x = keyx[j], xend = x + keyw[j]; x != xend; ++x)
                        {
                            frameIdx[y][x] = col;
                        }
                    }
                }
                else
                {
                    for (int y = whiteKeyShade ? bgr : 0, inity = y, yend = keyh; y != yend; ++y)
                    {
                        for (int x = keyx[j], xend = x + keyw[j]; x != xend; ++x)
                        {
                            frameIdx[y][x] = UnpressedWhiteKeyGradients[y - inity];
                        }
                    }
                    if (whiteKeyShade)
                    {
                        DrawRectangle(keyx[j], 0, keyw[j] + 1, bgr, 0xFF000000);
                        FillRectangle(keyx[j] + 1, 1, keyw[j] - 1, bgr - 2, 0xFF999999);
                    }
                }

                DrawRectangle(keyx[j], 0, keyw[j] + 1, keyh, 0xFF000000);
            }
            int diff = keyh - bh;
            if (!betterBlackKeys)
            {
                for (; i != 128; ++i) // 绘制所有黑键. Draws all black keys.
                {
                    j = Global.DrawMap[i];
                    FillRectangle(keyx[j], diff, keyw[j], bh, KeyColors[j]); // 重新绘制黑键及其颜色. Draws a black key (See Global.DrawMap).
                    DrawRectangle(keyx[j], diff, keyw[j] + 1, bh, 0xFF000000);
                }
            }
            DrawSeperator();
            if (betterBlackKeys)
            {
                int dtHeight = (int)Math.Round(keyh / 45.0);
                int dtWidth = (int)Math.Round(width / 1500.0);
                for (i = 75; i != 128; ++i)
                {
                    j = Global.DrawMap[i];
                    if (KeyPressed[j])
                    {
                        FillRectangle(keyx[j] - dtWidth, diff - dtWidth, keyw[j] + (2 * dtWidth), bh + dtWidth - 2, 0xFF363636);
                        FillRectangle(keyx[j], diff, keyw[j], bh - 2, KeyColors[j]);
                    }
                    else
                    {
                        FillRectangle(keyx[j] - dtWidth, diff - dtWidth, keyw[j] + (2 * dtWidth), bh + dtWidth, 0xFF363636);
                        FillRectangle(keyx[j], diff, keyw[j], bh + dtHeight, 0xFF000000); // 重新绘制黑键及其颜色. Draws a black key (See Global.DrawMap).
                    }
                }
            }
            //FillRectangle(0, keyh - 2, width, keyh / 15, lineColor);
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
        public void DrawNote(short key, int colorIndex, int y, int height, uint noteColor, bool pressed)
        {
            if (height < 1)
            {
                height = 1;
            }
            if (brighterNotesOnHit && pressed)
            {
                noteColor = RGBAColor.MixColors(noteColor, RGBAColor.White, pressedNotesShadeDecrement);
            }
            if (Global.EnableNoteBorder)
            {
                DrawBorderedNote(key, colorIndex, y, height, noteColor);
            }
            else
            {
                if (height > 5)
                {
                    --height;
                }
                FillRectangle(notex[key] + 1, y, notew[key] - 1, height, noteColor);
            }
        }
        private void DrawBorderedNote(short key, int colorIndex, int y, int height, uint noteColor)
        {
            RGBAColor borderColor = BorderColors[colorIndex];
            if (height > 2 * borderHeight)
            {
                FillRectangle(notex[key], y, notew[key], height, borderColor);
                if (y + height != this.height)
                {
                    FillRectangle(notex[key] + borderWidth, y + borderHeight, notew[key] - (borderWidth * 2), height - (borderHeight * 2), noteColor);
                }
                else
                {
                    FillRectangle(notex[key] + borderWidth, y + borderHeight, notew[key] - (borderWidth * 2), height - borderHeight, noteColor);
                }
            }
            else
            {
                if (Global.EnableDenseNoteEffect)
                {
                    FillRectangle(notex[key], y, notew[key], height, DenseNoteColors[colorIndex]);
                }
                else
                {
                    FillRectangle(notex[key], y, notew[key], height, borderColor);
                    FillRectangle(notex[key] + borderWidth, y, notew[key] - (borderWidth * 2), height, noteColor);
                }
            }
        }
        private void DrawGradientBorderedNote(short key, int colorIndex, int y, int height, bool pressed)
        {
            RGBAColor[] gradientColors = (pressed && brighterNotesOnHit) ? BrightenedNoteGradients[key][colorIndex] : NoteGradients[key][colorIndex];
            RGBAColor borderColor = Global.KeyColors[colorIndex];
            borderColor.R = (byte)(borderColor.R / Global.NoteBorderShade);
            borderColor.G = (byte)(borderColor.G / Global.NoteBorderShade);
            borderColor.B = (byte)(borderColor.B / Global.NoteBorderShade);
            if (height > 2 * borderHeight)
            {
                if (y + height != this.height)
                {
                    FillRectangle(notex[key], y, notew[key], height, borderColor);
                    for (int x = notex[key] + borderWidth, xend = x + notew[key] - (2 * borderWidth), initx = x; x != xend; ++x)
                    {
                        uint col = gradientColors[x - initx];
                        for (int dy = y + borderHeight, yend = y + height - borderHeight; dy != yend; ++dy)
                        {
                            frameIdx[dy][x] = col;
                        }
                    }
                }
                else
                {
                    FillRectangle(notex[key], y, notew[key], height, borderColor);
                    for (int x = notex[key] + borderWidth, xend = x + notew[key] - (2 * borderWidth), initx = x; x != xend; ++x)
                    {
                        uint col = gradientColors[x - initx];
                        for (int dy = y + borderHeight, yend = y + height; dy != yend; ++dy)
                        {
                            frameIdx[dy][x] = col;
                        }
                    }
                }
            }
            else
            {
                if (Global.EnableDenseNoteEffect) // 不必要做变白的处理, 因为效果并不明显
                {
                    FillRectangle(notex[key], y, notew[key], height, DenseNoteColors[colorIndex]);
                }
                else
                {
                    FillRectangle(notex[key], y, notew[key], height, borderColor);
                    for (int x = notex[key] + borderWidth, xend = x + notew[key] - (2 * borderWidth), initx = x; x != xend; ++x)
                    {
                        uint col = gradientColors[x - initx];
                        for (int dy = y, yend = dy + height; dy != yend; ++dy)
                        {
                            frameIdx[dy][x] = col;
                        }
                    }
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawGradientNote(short key, int colorIndex, int y, int height, bool pressed)
        {
            if (height < 1)
            {
                height = 1;
            }
            DrawGradientBorderedNote(key, colorIndex, y, height, pressed);
        }

        private void DrawSeperator()
        {
            if (!separator)
            {
                return;
            }
            if (enableGradient)
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
