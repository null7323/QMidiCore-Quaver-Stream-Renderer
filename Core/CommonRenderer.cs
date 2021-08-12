using SharpExtension;
using SharpExtension.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QQS_UI.Core
{
    public class CommonRenderer : RendererBase
    {
        private readonly CommonCanvas canvas;
        public CommonRenderer(RenderFile file, in RenderOptions options) : base(file, options)
        {
            canvas = new CommonCanvas(options);
        }

        public override void Render()
        {
            Render_TickBased();
        }

        private unsafe void Render_TickBased()
        {
            // Pixels per beat.
            // eval "spd":
            // spd = 1000000.0 * ppq / [tempo] / fps

            Global.CurrentRenderTick = 0;
            Global.RealtimeFPS = 0;
            Global.RenderedFrameCount = 0;

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
            for (; tick < fileTick; tick += spd)
            {
                Global.CurrentRenderTick = tick;
                frameWatch.Restart();
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
                _ = Parallel.For(0, 128, (i) =>
                {
                    if (noteBegins[i] == null)
                    {
                        return; // no notes available
                    }
                    uint j, k, l;
                    bool flag = false;
                    Note* noteptr = noteBegins[i];
                    while (noteptr->Start < tickup)
                    {
                        if (noteptr == end[i])
                        {
                            break;
                        }
                        if (noteptr->End >= tick)
                        {
                            l = Global.Colors[noteptr->Track % Global.Colors.Length];
                            if (!flag && (flag = true))
                            {
                                noteBegins[i] = noteptr;
                            }
                            if (noteptr->Start < tick)
                            {
                                k = keyHeight;
                                j = (uint)((float)(noteptr->End - tick) * ppb);
                                canvas.KeyColors[i] = l;
                                canvas.KeyPressed[i] = true;
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
                            canvas.DrawNote((short)i, (int)k, (int)j, l); // each key is individual
                        }
                        ++noteptr;
                    }
                });
                canvas.DrawKeys();
                canvas.WriteFrame();
                if (isPreview && Global.LimitPreviewFPS)
                {
                    while (frameWatch.ElapsedTicks < frameLen)
                    {

                    }
                }
                Global.RealtimeFPS = 10000000 / frameWatch.ElapsedTicks;
                ++Global.RenderedFrameCount;
            }
            canvas.Clear();
            canvas.DrawKeys();
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
