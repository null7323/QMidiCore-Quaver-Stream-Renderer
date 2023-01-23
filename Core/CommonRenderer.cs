using SharpExtension;
using SharpExtension.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QQS_UI.Core
{
    public sealed class CommonRenderer : RendererBase
    {
        private readonly CommonCanvas canvas;
        private readonly bool drawMiddleSquare;
        private readonly bool gradientNotes;
        private readonly bool thinnerNotes;
        private readonly bool whiteKeyShade;
        private readonly double delayStart;
        private readonly ParallelOptions parallelOptions;
        public CommonRenderer(RenderFile file, in RenderOptions options) : base(file, options)
        {
            canvas = new CommonCanvas(options);
            drawMiddleSquare = options.DrawGreySquare;
            gradientNotes = options.Gradient;
            thinnerNotes = options.ThinnerNotes;
            delayStart = options.DelayStartSeconds;
            whiteKeyShade = options.WhiteKeyShade;

            parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Global.MaxRenderConcurrency
            };
        }

        public override void Render()
        {
            if (thinnerNotes)
            {
                Render_Thinner();
            }
            else
            {
                Render_Wider();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Render_Thinner()
        {
            // Pixels per beat.
            // eval "spd":
            // spd = 1000000.0 * ppq / [tempo] / fps
            double ppb = 520.0 / ppq * noteSpeed;
            double fileTick = renderFile.MidiTime;
            double tick = 0, tickup, spd = ppq * 2.0 / fps;

            long spdidx = 0;
            long spdCount = tempos.Count;
            double deltaTicks = (height - keyHeight) / ppb;

            Note** noteBegins = stackalloc Note*[128];
            Note** end = stackalloc Note*[128];

            Stopwatch frameWatch = new Stopwatch();
            double frameLen = 10000000.0 / fps;

            int middleCx = canvas.GetKeyX(60);
            int middleCwidth = canvas.GetKeyWidth(60);
            int greySquareY = (int)keyHeight * 2 / 15;
            int greySquareLeft = middleCx + (middleCwidth / 4);
            int greySquareWidth = middleCwidth * 2 / 4;
            for (int i = 0; i != 128; ++i)
            {
                if (noteMap[i].Count != 0)
                {
                    noteBegins[i] = (Note*)UnsafeMemory.GetActualAddressOf(ref noteMap[i][0]);
                    end[i] = noteBegins[i] + noteMap[i].Count;
                }
                else
                {
                    noteBegins[i] = end[i] = null;
                }
            }
            int colorLen = Global.KeyColors.Length;

            int delayFrames = (int)(delayStart * fps);
            canvas.Clear();
            if (gradientNotes)
            {
                canvas.DrawGradientKeys();
            }
            else
            {
                canvas.DrawKeys();
            }
            for (int i = 0; i != delayFrames; ++i)
            {
                canvas.WriteFrame();
            }
            for (; tick < fileTick; tick += spd)
            {
                frameWatch.Restart();
                while (isPreview && Global.PreviewPaused && !Interrupt)
                {
                    canvas.WriteFrame();
                    while (Global.LimitPreviewFPS && frameWatch.ElapsedTicks < frameLen)
                    {

                    }
                    frameWatch.Restart();
                }
                canvas.Clear();
                tickup = tick + deltaTicks;
                while (spdidx != spdCount && tempos[spdidx].Tick < tick)
                {
                    spd = 1e6 / tempos[spdidx].Value * ppq / fps;
                    ++spdidx;
                }
                if (Interrupt)
                {
                    break;
                }
                // 使用并行 for 循环提高性能.
                _ = Parallel.For(0, 128, parallelOptions,
                    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
                    (i) =>
                {
                    if (noteBegins[i] == null)
                    {
                        return; // no notes available
                    }
                    uint j, k, l;
                    bool flag = false;
                    Note* noteptr = noteBegins[i];
                    bool isCurrentNotePressed;
                    while (noteptr->Start < tickup)
                    {
                        isCurrentNotePressed = false;
                        if (noteptr == end[i])
                        {
                            break;
                        }
                        if (noteptr->End >= tick)
                        {
                            l = Global.KeyColors[noteptr->Track % colorLen];
                            if (!flag && (flag = true))
                            {
                                noteBegins[i] = noteptr;
                            }
                            if (noteptr->Start < tick)
                            {
                                k = keyHeight;
                                j = (uint)((noteptr->End - tick) * ppb);
                                canvas.KeyColors[i] = l;
                                canvas.KeyPressed[i] = true;
                                canvas.KeyTracks[i] = noteptr->Track;
                                isCurrentNotePressed = true;
                            }
                            else
                            {
                                k = (uint)(((noteptr->Start - tick) * ppb) + keyHeight);
                                j = (uint)((noteptr->End - noteptr->Start) * ppb);
                            }
                            if (j + k > height)
                            {
                                j = height - k;
                            }
                            l = Global.NoteColors[noteptr->Track % colorLen];
                            if (gradientNotes)
                            {
                                canvas.DrawGradientNote((short)i, noteptr->Track % colorLen, (int)k, (int)j, isCurrentNotePressed);
                            }
                            else
                            {
                                canvas.DrawNote((short)i, noteptr->Track % colorLen, (int)k, (int)j, l, isCurrentNotePressed); // each key is individual
                            }
                        }
                        ++noteptr;
                    }
                });
                if (gradientNotes)
                {
                    canvas.DrawGradientKeys();
                }
                else
                {
                    canvas.DrawKeys();
                }
                if (drawMiddleSquare)
                {
                    RGBAColor col = canvas.KeyColors[60];
                    col.R = (byte)Math.Round(col.R * 0.62745);
                    col.G = (byte)Math.Round(col.G * 0.62745);
                    col.B = (byte)Math.Round(col.B * 0.62745);
                    if (whiteKeyShade && canvas.KeyPressed[60])
                    {
                        canvas.FillRectangle(greySquareLeft, greySquareY - ((int)keyHeight / 50), greySquareWidth, greySquareWidth, col);
                    }
                    else
                    {
                        canvas.FillRectangle(greySquareLeft, greySquareY, greySquareWidth, greySquareWidth, col);
                    }
                }
                canvas.WriteFrame();
                if (isPreview && Global.LimitPreviewFPS)
                {
                    while (frameWatch.ElapsedTicks < frameLen)
                    {

                    }
                }
            }
            canvas.Clear();
            if (gradientNotes)
            {
                canvas.DrawGradientKeys();
            }
            else
            {
                canvas.DrawKeys();
            }
            for (int i = 0; i != 3 * fps; i++)
            {
                if (Interrupt)
                {
                    break;
                }
                canvas.WriteFrame();
            }
            canvas.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void Render_Wider()
        {
            // Pixels per beat.
            // eval "spd":
            // spd = 1000000.0 * ppq / [tempo] / fps
            double ppb = 520.0 / ppq * noteSpeed;
            double fileTick = renderFile.MidiTime;
            double tick = 0, tickup, spd = ppq * 2.0 / fps;

            long spdidx = 0;
            long spdCount = tempos.Count;
            double deltaTicks = (height - keyHeight) / ppb;

            Note** noteBegins = stackalloc Note*[128];
            Note** end = stackalloc Note*[128];

            Stopwatch frameWatch = new Stopwatch();
            double frameLen = 10000000.0 / fps;

            int middleCx = canvas.GetKeyX(60);
            int middleCwidth = canvas.GetKeyWidth(60);
            int greySquareY = (int)keyHeight * 2 / 15;
            int greySquareLeft = middleCx + (middleCwidth / 4);
            int greySquareWidth = middleCwidth * 2 / 4;
            for (int i = 0; i != 128; ++i)
            {
                if (noteMap[i].Count != 0)
                {
                    noteBegins[i] = (Note*)UnsafeMemory.GetActualAddressOf(ref noteMap[i][0]);
                    end[i] = noteBegins[i] + noteMap[i].Count;
                }
                else
                {
                    noteBegins[i] = end[i] = null;
                }
            }
            int colorLen = Global.KeyColors.Length;
            int delayFrames = (int)delayStart * fps;
            canvas.Clear();
            if (gradientNotes)
            {
                canvas.DrawGradientKeys();
            }
            else
            {
                canvas.DrawKeys();
            }
            for (int i = 0; i != delayFrames; ++i)
            {
                canvas.WriteFrame();
            }
            for (; tick < fileTick; tick += spd)
            {
                frameWatch.Restart();
                while (isPreview && Global.PreviewPaused && !Interrupt)
                {
                    canvas.WriteFrame();
                    while (Global.LimitPreviewFPS && frameWatch.ElapsedTicks < frameLen)
                    {

                    }
                    frameWatch.Restart();
                }
                canvas.Clear();
                tickup = tick + deltaTicks;
                while (spdidx != spdCount && tempos[spdidx].Tick < tick)
                {
                    spd = 1e6 / tempos[spdidx].Value * ppq / fps;
                    ++spdidx;
                }
                if (Interrupt)
                {
                    break;
                }
                // 使用并行 for 循环提高性能.
                _ = Parallel.For(0, 75, parallelOptions, [MethodImpl(MethodImplOptions.AggressiveOptimization)] (i) =>
                {
                    i = Global.DrawMap[i];
                    if (noteBegins[i] == null)
                    {
                        return; // no notes available
                    }
                    uint j, k, l;
                    bool flag = false;
                    bool isCurrentNotePressed;
                    Note* noteptr = noteBegins[i];
                    while (noteptr->Start < tickup)
                    {
                        isCurrentNotePressed = false;
                        if (noteptr == end[i])
                        {
                            break;
                        }
                        if (noteptr->End >= tick)
                        {
                            l = Global.KeyColors[noteptr->Track % colorLen];
                            if (!flag && (flag = true))
                            {
                                noteBegins[i] = noteptr;
                            }
                            if (noteptr->Start < tick)
                            {
                                k = keyHeight;
                                j = (uint)((noteptr->End - tick) * ppb);
                                canvas.KeyColors[i] = l;
                                canvas.KeyPressed[i] = true;
                                canvas.KeyTracks[i] = noteptr->Track;
                                isCurrentNotePressed = true;
                            }
                            else
                            {
                                k = (uint)(((noteptr->Start - tick) * ppb) + keyHeight);
                                j = (uint)((noteptr->End - noteptr->Start) * ppb);
                            }
                            if (j + k > height)
                            {
                                j = height - k;
                            }
                            if (gradientNotes)
                            {
                                canvas.DrawGradientNote((short)i, noteptr->Track % colorLen, (int)k, (int)j, isCurrentNotePressed);
                            }
                            else
                            {
                                canvas.DrawNote((short)i, noteptr->Track % colorLen, (int)k, (int)j, l, isCurrentNotePressed); // each key is individual
                            }
                        }
                        ++noteptr;
                    }
                });
                _ = Parallel.For(75, 128, parallelOptions, [MethodImpl(MethodImplOptions.AggressiveOptimization)] (i) =>
                {
                    i = Global.DrawMap[i];
                    if (noteBegins[i] == null)
                    {
                        return; // no notes available
                    }
                    uint j, k, l;
                    bool flag = false;
                    bool isCurrentNotePressed;
                    Note* noteptr = noteBegins[i];
                    while (noteptr->Start < tickup)
                    {
                        isCurrentNotePressed = false;
                        if (noteptr == end[i])
                        {
                            break;
                        }
                        if (noteptr->End >= tick)
                        {
                            l = Global.KeyColors[noteptr->Track % colorLen];
                            if (!flag && (flag = true))
                            {
                                noteBegins[i] = noteptr;
                            }
                            if (noteptr->Start < tick)
                            {
                                k = keyHeight;
                                j = (uint)((noteptr->End - tick) * ppb);
                                canvas.KeyColors[i] = l;
                                canvas.KeyPressed[i] = true;
                                canvas.KeyTracks[i] = noteptr->Track;
                                isCurrentNotePressed = true;
                            }
                            else
                            {
                                k = (uint)(((noteptr->Start - tick) * ppb) + keyHeight);
                                j = (uint)((noteptr->End - noteptr->Start) * ppb);
                            }
                            if (j + k > height)
                            {
                                j = height - k;
                            }
                            if (gradientNotes)
                            {
                                canvas.DrawGradientNote((short)i, noteptr->Track % colorLen, (int)k, (int)j, isCurrentNotePressed);
                            }
                            else
                            {
                                canvas.DrawNote((short)i, noteptr->Track % colorLen, (int)k, (int)j, l, isCurrentNotePressed); // each key is individual
                            }
                        }
                        ++noteptr;
                    }
                });
                if (gradientNotes)
                {
                    canvas.DrawGradientKeys();
                }
                else
                {
                    canvas.DrawKeys();
                }
                if (drawMiddleSquare)
                {
                    RGBAColor col = canvas.KeyColors[60];
                    col.R = (byte)Math.Round(col.R * 0.62745);
                    col.G = (byte)Math.Round(col.G * 0.62745);
                    col.B = (byte)Math.Round(col.B * 0.62745);
                    if (whiteKeyShade && canvas.KeyPressed[60])
                    {
                        canvas.FillRectangle(greySquareLeft, greySquareY - ((int)keyHeight / 50), greySquareWidth, greySquareWidth, col);
                    }
                    else
                    {
                        canvas.FillRectangle(greySquareLeft, greySquareY, greySquareWidth, greySquareWidth, col);
                    }
                }
                canvas.WriteFrame();
                if (isPreview && Global.LimitPreviewFPS)
                {
                    while (frameWatch.ElapsedTicks < frameLen)
                    {

                    }
                }
            }
            canvas.Clear();
            if (gradientNotes)
            {
                canvas.DrawGradientKeys();
            }
            else
            {
                canvas.DrawKeys();
            }
            for (int i = 0; i != 3 * fps; i++)
            {
                if (Interrupt)
                {
                    break;
                }
                canvas.WriteFrame();
            }
            canvas.Dispose();
        }

        public void Dispose()
        {
            canvas.Dispose();
            GC.SuppressFinalize(this);
        }

        ~CommonRenderer()
        {
            Dispose();
        }
    }
}
