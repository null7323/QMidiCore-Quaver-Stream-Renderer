using SharpExtension.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QQS_UI.Core
{
    public abstract class RendererBase
    {
        protected readonly RenderFile renderFile;
        protected readonly UnmanagedList<Note>[] noteMap;
        protected readonly UnmanagedList<Tempo> tempos;

        protected readonly ushort ppq;
        protected readonly double noteSpeed;
        protected readonly int fps;
        protected readonly uint height;
        protected readonly uint keyHeight;
        protected readonly bool isTickBased;
        protected readonly bool isPreview;

        public bool Interrupt = false;
        public RendererBase(RenderFile file, in RenderOptions options)
        {
            renderFile = file;
            noteMap = file.Notes;
            tempos = file.Tempos;

            ppq = file.Division;
            noteSpeed = options.NoteSpeed;
            fps = options.FPS;
            height = (uint)options.Height;
            keyHeight = (uint)options.KeyHeight;
            isTickBased = options.TickBased;
            isPreview = options.PreviewMode;
        }

        public abstract void Render();
    }
}
