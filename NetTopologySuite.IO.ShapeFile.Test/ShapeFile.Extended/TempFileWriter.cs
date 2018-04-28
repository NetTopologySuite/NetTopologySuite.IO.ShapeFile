using System;
using System.IO;
using System.Threading;

using NUnit.Framework;

namespace NetTopologySuite.IO.Tests.ShapeFile.Extended
{
    internal sealed class TempFileWriter : IDisposable
    {
        public TempFileWriter(string ext, byte[] data)
        {
            this.Path = System.IO.Path.GetFullPath(System.IO.Path.ChangeExtension(TestContext.CurrentContext.Test.ID, ext));
            File.WriteAllBytes(this.Path, data);
        }

        ~TempFileWriter() => this.InternalDispose();

        public string Path { get; }

        public void Dispose()
        {
            this.InternalDispose();
            GC.SuppressFinalize(this);
        }

        private void InternalDispose()
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    File.Delete(this.Path);
                }
                catch
                {
                    // sometimes Git opens these files at just the wrong time.
                    Thread.Sleep(15);
                }
            }
        }
    }
}
