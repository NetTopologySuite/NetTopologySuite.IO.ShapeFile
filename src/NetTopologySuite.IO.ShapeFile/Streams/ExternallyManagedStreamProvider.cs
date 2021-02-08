using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetTopologySuite.IO.Streams
{
    /// <summary>
    /// A stream provider that provides <see cref="Stream"/>s who are managed by the application.
    /// </summary>
    public class ExternallyManagedStreamProvider : IStreamProvider
    {
        /// <summary>
        /// Creates an instance of this class
        /// </summary>
        /// <param name="kind">The kind of stream</param>
        /// <param name="stream">A <see cref="Stream"/> managed by the application</param>
        public ExternallyManagedStreamProvider(string kind, Stream stream)
        {
            Kind = kind;
            Stream = stream;
        }

        /// <summary>
        /// Gets a value indicating that the underlying stream is read-only
        /// </summary>
        public bool UnderlyingStreamIsReadonly => !Stream.CanWrite;

        protected Stream Stream { get; private set; }

        /// <summary>
        /// Function to return a Stream of the bytes
        /// </summary>
        /// <returns>An opened stream</returns>
        public Stream OpenRead()
        {
            return new NonDisposingStream(Stream);
        }

        /// <summary>
        /// Function to open the underlying stream for writing purposes
        /// </summary>
        /// <remarks>If <see cref="UnderlyingStreamIsReadonly"/> is not <value>true</value>
        /// this method shall fail</remarks>
        /// <returns>An opened stream</returns>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="UnderlyingStreamIsReadonly"/> is <value>true</value> or the underlying stream doesn't support seeking</exception>
        public Stream OpenWrite(bool truncate)
        {
            if (UnderlyingStreamIsReadonly)
                throw new InvalidOperationException();

            if (truncate) {
                if (!Stream.CanSeek)
                {
                    throw new InvalidOperationException("The underlying stream doesn't support seeking! You are unable to truncate the data.");
                }
                Stream.SetLength(0);
            }

            return new NonDisposingStream(Stream);
        }

        /// <summary>
        /// Gets a value indicating the kind of stream
        /// </summary>
        public string Kind { get; private set; }


        private class NonDisposingStream : Stream
        {
            public NonDisposingStream(Stream stream)
            {
                Stream = stream;
            }

            protected override void Dispose(bool disposing)
            {
                Stream = null;
            }

            protected Stream Stream { get; private set; }

            public override bool CanRead => Stream.CanRead;

            public override bool CanSeek => Stream.CanSeek;

            public override bool CanWrite => Stream.CanWrite;

            public override void Flush()
            {
                Stream.Flush();
            }

            public override long Length => Stream.Length;

            public override long Position { get => Stream.Position; set => Stream.Position = value; }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return Stream.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return Stream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                Stream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                Stream.Write(buffer, offset, count);
            }
        }
    }
}
