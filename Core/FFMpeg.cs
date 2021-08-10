using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using SharpExtension.IO;

namespace QQS_UI.Core
{
    /// <summary>
    /// 简单地封装操作ffmpeg的逻辑.
    /// </summary>
    public unsafe class FFMpeg : IDisposable
    {
        private readonly CStream stream;
        private readonly ulong frameSize;
        /// <summary>
        /// 初始化一个新的 <see cref="FFMpeg"/> 实例.
        /// </summary>
        /// <param name="ffargs">初始化ffmpeg的参数.</param>
        /// <param name="width">输入视频的宽.</param>
        /// <param name="height">输入视频的高.</param>
        public FFMpeg(string ffargs, int width, int height)
        {
            string ffcommand;
            stream = CStream.OpenPipe(ffcommand = "ffmpeg " + ffargs, "wb");
            frameSize = (uint)width * (uint)height * 4;
            Console.WriteLine("FFMpeg 启动命令: {0}", ffcommand);
        }
        /// <summary>
        /// 向 FFMpeg 写入一帧.
        /// </summary>
        /// <param name="buffer">存有视频画面的缓冲区.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFrame(in void* buffer)
        {
            _ = stream.WriteWithoutLock(buffer, frameSize, 1);
        }
        public void Dispose()
        {
            if (!stream.Closed)
            {
                _ = stream.Close();
            }
            GC.SuppressFinalize(this);
        }
        ~FFMpeg()
        {
            stream.Dispose();
        }
    }
}
