using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using SharpExtension.IO;
using SharpExtension;
using SharpExtension.Collections;

namespace QQS_UI.Core
{
    public unsafe class MidiStream : IDisposable
    {
        private readonly CStream stream;

        public MidiStream(string path)
        {
            stream = CStream.OpenFile(path, "rb");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadInt16()
        {
            int hi = stream.GetChar();
            return (ushort)((hi << 8) | stream.GetChar());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadInt32()
        {
            int h1, h2, h3;
            h1 = stream.GetChar();
            h2 = stream.GetChar();
            h3 = stream.GetChar();
            return (uint)((h1 << 24) | (h2 << 16) | (h3 << 8) | stream.GetChar());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadInt8()
        {
            return (byte)stream.GetChar();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Read(void* contentBuffer, ulong size, ulong count)
        {
            return stream.ReadWithoutLock(contentBuffer, size, count);
        }

        public bool Associated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => !stream.Closed;
        }

        public void Dispose()
        {
            stream.Dispose();
            GC.SuppressFinalize(this);
        }

        ~MidiStream()
        {
            stream.Dispose();
        }
    }
    internal unsafe struct RenderTrackInfo
    {
        public byte* Data;
        public ulong Size;
        public uint TrackTime;
        public UnmanagedList<Note> Notes;
    }
    public unsafe class RenderFile
    {
        private readonly string midiPath;

        public ushort TrackCount;
        public ushort Division;
        public uint MidiTime = 0;
        public long NoteCount = 0;
        public UnmanagedList<Note>[] Notes = new UnmanagedList<Note>[128];
        public UnmanagedList<Tempo> Tempos = new UnmanagedList<Tempo>();
        private void Parse()
        {
            MidiStream stream = new MidiStream(midiPath);
            sbyte* hdr = stackalloc sbyte[4];

            _ = stream.Read(hdr, 4, 1);
            // 判断是否与MThd相等
            if (!(hdr[0] == 'M' && hdr[1] == 'T' && hdr[2] == 'h' && hdr[3] == 'd'))
            {
                throw new Exception();
            }
            uint hdrSize = stream.ReadInt32();
            if (hdrSize != 6)
            {
                throw new Exception();
            }
            if (stream.ReadInt16() == 2)
            {
                throw new Exception();
            }
            TrackCount = stream.ReadInt16();
            Division = stream.ReadInt16();
            if (Division > 32767)
            {
                throw new Exception();
            }

            RenderTrackInfo[] trkInfo = new RenderTrackInfo[TrackCount];
            for (int i = 0; i != TrackCount; ++i)
            {
                _ = stream.Read(hdr, 4, 1);
                if (!(hdr[0] == 'M' && hdr[1] == 'T' && hdr[2] == 'r' && hdr[3] == 'k'))
                {
                    throw new Exception();
                }
                trkInfo[i] = new RenderTrackInfo
                {
                    Size = stream.ReadInt32(),
                    Notes = new UnmanagedList<Note>(),
                    TrackTime = 0,
                };
                trkInfo[i].Data = (byte*)UnsafeMemory.Allocate(trkInfo[i].Size);
                _ = stream.Read(trkInfo[i].Data, trkInfo[i].Size, 1);
                Console.WriteLine("拷贝音轨 #{0} 的信息. 音轨大小: {1} 字节.", i, trkInfo[i].Size);
            }
            for (int i = 0; i != 128; ++i)
            {
                Notes[i] = new UnmanagedList<Note>();
            }
            _ = Parallel.For(0, TrackCount, (i) =>
            {
                UnmanagedList<Note> nl = trkInfo[i].Notes; // pass by ref
                ForwardLinkedList<long>[] fll = new ForwardLinkedList<long>[128];
                for (int j = 0; j != 128; ++j)
                {
                    fll[j] = new ForwardLinkedList<long>();
                }

                uint trkTime = 0;
                bool loop = true;
                byte* p = trkInfo[i].Data;
                byte prev = 0;
                byte comm;
                while (loop)
                {
                    trkTime += Global.ParseVLInt(ref p);
                    comm = *p++;
                    if (comm < 0x80)
                    {
                        comm = prev;
                        --p;
                    }
                    prev = comm;
                    switch (comm & 0b11110000)
                    {
                        case 0x80:
                            {
                                byte k = *p++;
                                ++p;
                                if (fll[k].Any())
                                {
                                    long idx = fll[k].Pop();
                                    nl[idx].End = trkTime;
                                }
                            }
                            continue;
                        case 0x90:
                            {
                                byte k = *p++;
                                byte v = *p++;
                                if (v != 0)
                                {
                                    long idx = nl.Count;
                                    fll[k].Add(idx);
                                    nl.Add(new Note
                                    {
                                        Start = trkTime,
                                        Key = k,
                                        Track = (ushort)i
                                    });
                                }
                                else
                                {
                                    if (fll[k].Any())
                                    {
                                        long idx = fll[k].Pop();
                                        nl[idx].End = trkTime;
                                    }
                                }
                            }
                            continue;
                        case 0xA0:
                        case 0xB0:
                        case 0xE0:
                            p += 2;
                            continue;
                        case 0xC0:
                        case 0xD0:
                            ++p;
                            continue;
                        default:
                            break;
                    }
                    switch (comm)
                    {
                        case 0xF0:
                            while (*p++ != 0xF7)
                            {

                            }
                            continue;
                        case 0xF1:
                            continue;
                        case 0xF2:
                        case 0xF3:
                            p += 0xF4 - comm;
                            continue;
                        default:
                            break;
                    }
                    if (comm < 0xFF)
                    {
                        continue;
                    }
                    comm = *p++;
                    if (comm >= 0 && comm <= 0x0A)
                    {
                        uint l = Global.ParseVLInt(ref p); // 这个中间变量不可以去掉
                        p += l;
                        continue;
                    }
                    switch (comm)
                    {
                        case 0x2F:
                            ++p;
                            for (int k = 0; k != 128; ++k)
                            {
                                foreach (long l in fll[k])
                                {
                                    nl[l].End = trkTime;
                                }
                            }
                            loop = false;
                            break;
                        case 0x51:
                            _ = Global.ParseVLInt(ref p);
                            byte b1 = *p++;
                            byte b2 = *p++;
                            uint t = (uint)((b1 << 16) | (b2 << 8) | (*p++));
                            lock (Tempos)
                            {
                                Tempos.Add(new Tempo
                                {
                                    Tick = trkTime,
                                    Value = t
                                });
                            }
                            break;
                        default:
                            uint dl = Global.ParseVLInt(ref p); // 这个中间变量不可以去掉
                            p += dl;
                            break;
                    }
                }
                Console.WriteLine("音轨 #{0} 解析完成. 音符数: {1}.", i, nl.Count);
                UnsafeMemory.Free(trkInfo[i].Data);
                trkInfo[i].Data = null;
                trkInfo[i].TrackTime = trkTime;
            });
            for (int i = 0; i != TrackCount; ++i)
            {
                if (trkInfo[i].TrackTime > MidiTime)
                {
                    MidiTime = trkInfo[i].TrackTime;
                }
                NoteCount += trkInfo[i].Notes.Count;
            }
            Tempos.TrimExcess();
            Console.WriteLine("正在处理 Midi 事件...");
            for (int i = 0; i != TrackCount; ++i)
            {
                if (trkInfo[i].Notes.Count == 0)
                {
                    continue;
                }
                for (long j = 0, len = trkInfo[i].Notes.Count; j != len; ++j)
                {
                    ref Note n = ref trkInfo[i].Notes[j];
                    Notes[n.Key].Add(n);
                }
                trkInfo[i].Notes.Clear();
            }
            _ = Parallel.ForEach(Notes, (nl) =>
            {
                nl.TrimExcess();
                NoteSorter.Sort(nl);
            });

            // sort tempos
            Tempo[] temp = Tempos.ToManaged();
            Array.Sort(temp, (left, right) =>
            {
                return left.Tick < right.Tick ? -1 : left.Tick == right.Tick ? 0 : 1;
            });
            UnmanagedArray<Tempo> arr = Interoperability.MakeUnmanagedArray(temp);
            Tempos.Clear();
            Tempos.AddRange(arr);
            arr.Dispose();

            Console.WriteLine("Midi 事件处理完成. 音符总数: {0}.", NoteCount);
            stream.Dispose();
        }
        public RenderFile(string path)
        {
            midiPath = path;
            if (!File.Exists(path))
            {
                throw new FileNotFoundException();
            }
            Stopwatch sw = Stopwatch.StartNew();
            Parse();
            sw.Stop();
            Console.WriteLine("加载 Midi 用时: {0:F2} s.", sw.ElapsedMilliseconds / 1000.0);
        }

        public string MidiPath => midiPath;
    }
}
