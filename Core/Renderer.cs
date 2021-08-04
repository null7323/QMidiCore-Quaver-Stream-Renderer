using SharpExtension;
using SharpExtension.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QQS_UI.Core
{
    public class Renderer
    {
        private readonly RenderFile renderFile;
        private readonly UnmanagedList<Note>[] noteMap;
        private readonly Canvas canvas;
        private readonly UnmanagedList<Tempo> tempos;

        private readonly ushort ppq;
        private readonly double noteSpeed;
        private readonly int fps;
        private readonly uint height;
        private readonly uint keyHeight;
        private readonly bool isTickBased;

        public bool Interrupt = false;
        public Renderer(RenderFile file, in RenderOptions options)
        {
            renderFile = file;
            noteMap = file.Notes;
            canvas = new Canvas(options);
            tempos = file.Tempos;

            ppq = file.Division;
            noteSpeed = options.NoteSpeed;
            fps = options.FPS;
            height = (uint)options.Height;
            keyHeight = (uint)options.KeyHeight;
            isTickBased = options.TickBased;

            Console.WriteLine("正在对 Midi 文件进行 OR 处理.");
            _ = Parallel.For(0, 128, (i) =>
            {
                UnmanagedList<Note> nl = noteMap[i];
                if (nl.Count < 10)
                {
                    return;
                }
                for (long index = 0, len = nl.Count - 2; index != len;)
                {
                    ref Note curr = ref nl[index++];
                    ref Note next = ref nl[index];
                    if (curr.Start < next.Start && curr.End > next.Start && curr.End < next.End)
                    {
                        curr.End = next.Start;
                    }
                    else if (curr.Start == next.Start && curr.End <= next.End)
                    {
                        curr.End = curr.Start;
                    }
                }
            });
            Console.WriteLine("OR 处理完成.");
        }

        public void Render()
        {
            Render_TickBased();
        }

        private unsafe void Render_TickBased()
        {
            // Pixels per beat.
            // eval "spd":
            // spd = 1000000.0 * ppq / [tempo] / fps

            double ppb = 520.0 / ppq * noteSpeed;
            double fileTick = renderFile.MidiTime;
            double tick = 0, tickup, spd = ppq * 2.0 / fps;
            double currBPM = 120.0;

            long spdidx = 0;
            long spdCount = tempos.Count;
            double deltaTicks = (height - keyHeight) / ppb;

            Note** noteBegins = stackalloc Note*[128];
            Note** end = stackalloc Note*[128];
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
                canvas.Clear();
                tickup = tick + deltaTicks;
                while (spdidx != spdCount && tempos[spdidx].Tick < tick)
                {
                    currBPM = 60000000.0 / tempos[spdidx].Value;
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
                        if (noteptr->End >= tick)
                        {
                            l = Global.Color[noteptr->Track % 96];
                            if (!flag && (flag = true))
                            {
                                noteBegins[i] = noteptr;
                            }

                            if (noteptr->Start < tick)
                            {
                                k = keyHeight;
                                j = (uint)((float)(noteptr->End - tick) * ppb);
                                //canvas.keycolor[n.Key] = l;
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
                        if (noteptr == end[i])
                        {
                            break;
                        }
                        ++noteptr;
                    }
                });
                canvas.DrawKeys();
                canvas.WriteFrame();
            }
            canvas.Clear();
            canvas.DrawKeys();
            for (int i = 0; i != 300; i++)
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

        ~Renderer()
        {
            Dispose();
        }
    }
}
