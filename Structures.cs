using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QQS_UI
{
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
}
