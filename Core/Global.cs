using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpExtension.Collections;

namespace QQS_UI.Core
{
    /// <summary>
    /// 存放全局数据.
    /// </summary>
    public static class Global
    {
        public static short[] GenKeyX = {
            0, 12, 18, 33, 36, 54, 66, 72, 85, 90, 105, 108
        };
        public static short[] DrawMap = {
            0, 2, 4, 5, 7, 9, 11, 12, 14, 16, 17, 19, 21, 23, 24, 26, 28, 29,
            31, 33, 35, 36, 38, 40, 41, 43, 45, 47, 48, 50, 52, 53, 55, 57,
            59, 60, 62, 64, 65, 67, 69, 71, 72, 74, 76, 77, 79, 81, 83, 84,
            86, 88, 89, 91, 93, 95, 96, 98, 100, 101, 103, 105, 107, 108,
            110, 112, 113, 115, 117, 119, 120, 122, 124, 125, 127, 1, 3,
            6, 8, 10, 13, 15, 18, 20, 22, 25, 27, 30, 32, 34, 37, 39, 42, 44,
            46, 49, 51, 54, 56, 58, 61, 63, 66, 68, 70, 73, 75, 78, 80, 82,
            85, 87, 90, 92, 94, 97, 99, 102, 104, 106, 109, 111, 114, 116,
            118, 121, 123, 126
        };
        public static readonly RGBAColor[] DefaultColors = {
            0xFF3366FF, 0xFFFF7E33, 0xFF33FF66, 0xFFFF3381, 0xFF33E1E1, 0xFFE433E1,
            0xFF99E133, 0xFF4B33E1, 0xFFFFCC33, 0xFF33B4FF, 0xFFFF3333, 0xFF33FFB1,
            0xFFFF33CC, 0xFF4EFF33, 0xFF9933FF, 0xFFE7FF33, 0xFF3366FF, 0xFFFF7E33,
            0xFF33FF66, 0xFFFF3381, 0xFF33E1E1, 0xFFE433E1, 0xFF99E133, 0xFF4B33E1,
            0xFFFFCC33, 0xFF33B4FF, 0xFFFF3333, 0xFF33FFB1, 0xFFFF33CC, 0xFF4EFF33,
            0xFF9933FF, 0xFFE7FF33, 0xFF3366FF, 0xFFFF7E33, 0xFF33FF66, 0xFFFF3381,
            0xFF33E1E1, 0xFFE433E1, 0xFF99E133, 0xFF4B33E1, 0xFFFFCC33, 0xFF33B4FF,
            0xFFFF3333, 0xFF33FFB1, 0xFFFF33CC, 0xFF4EFF33, 0xFF9933FF, 0xFFE7FF33,
            0xFF3366FF, 0xFFFF7E33, 0xFF33FF66, 0xFFFF3381, 0xFF33E1E1, 0xFFE433E1,
            0xFF99E133, 0xFF4B33E1, 0xFFFFCC33, 0xFF33B4FF, 0xFFFF3333, 0xFF33FFB1,
            0xFFFF33CC, 0xFF4EFF33, 0xFF9933FF, 0xFFE7FF33, 0xFF3366FF, 0xFFFF7E33,
            0xFF33FF66, 0xFFFF3381, 0xFF33E1E1, 0xFFE433E1, 0xFF99E133, 0xFF4B33E1,
            0xFFFFCC33, 0xFF33B4FF, 0xFFFF3333, 0xFF33FFB1, 0xFFFF33CC, 0xFF4EFF33,
            0xFF9933FF, 0xFFE7FF33, 0xFF3366FF, 0xFFFF7E33, 0xFF33FF66, 0xFFFF3381,
            0xFF33E1E1, 0xFFE433E1, 0xFF99E133, 0xFF4B33E1, 0xFFFFCC33, 0xFF33B4FF,
            0xFFFF3333, 0xFF33FFB1, 0xFFFF33CC, 0xFF4EFF33, 0xFF9933FF, 0xFFE7FF33
        };
        public static RGBAColor[] Colors;
        public static uint[] ColorHSV = new uint[96];
        static Global()
        {
            Colors = new RGBAColor[96];
            Array.Copy(DefaultColors, Colors, 96);
        }
        public static TimeSpan GetTimeOf(uint midiTime, ushort ppq, UnmanagedList<Tempo> tempos)
        {
            if (tempos == null)
            {
                return new TimeSpan((long)(5000000.0 * midiTime / ppq));
            }
            if (tempos.Count == 0)
            {
                return new TimeSpan((long)(5000000.0 * midiTime / ppq));
            }
            double ticks = 0;
            double tempo = 500000.0;
            uint lastEventTime = 0;
            IIterator<Tempo> iterator = tempos.GetIterator();
            while (iterator.MoveNext())
            {
                if (iterator.Current.Tick > midiTime)
                {
                    break;
                }
                uint dtTime = iterator.Current.Tick - lastEventTime;
                ticks += tempo * 10.0 * dtTime / ppq;
                lastEventTime = iterator.Current.Tick;
                tempo = iterator.Current.Value;
            }
            ticks += tempo * 10.0 * (midiTime - lastEventTime) / ppq;
            return new TimeSpan((long)ticks);
        }

        public static bool LimitPreviewFPS = true;
    }

    public struct Note
    {
        public byte Key;
        public ushort Track;
        public uint Start, End;
    }
    public struct Tempo
    {
        public uint Tick;
        public uint Value;
    }

    public struct PreviewEvent
    {
        public uint Time;
        public uint Value;
    }
}
