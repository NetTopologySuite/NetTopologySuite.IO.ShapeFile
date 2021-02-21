using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace NetTopologySuite.IO
{

    /// <summary>
    /// Represents a buffer of binary data.
    /// </summary>
    internal abstract class BinaryBuffer
    {
        protected BinaryBuffer(int initialCapacity = 256)
        {
            if (initialCapacity < 0)
                throw new ArgumentException(nameof(initialCapacity));

            Buffer = new byte[initialCapacity];
            UsedBufferSize = 0;
        }


        /// <summary>
        /// Underlying buffer data.
        /// </summary>
        protected byte[] Buffer;

        protected int UsedBufferSize { get; private set; }

        /// <summary>
        /// Used for debugging purposes.
        /// </summary>
        internal byte[] UsedBuffer
        {
            get
            {
                var bytes = new byte[UsedBufferSize];
                Array.Copy(Buffer, bytes, bytes.Length);
                return bytes;
            }
        }


        protected void SetUsedBufferSize(int newSize)
        {
            if (newSize < 0)
                throw new ArgumentException(nameof(newSize));

            if (newSize == UsedBufferSize)
                return;

            if (newSize < UsedBufferSize)
            {
                Array.Clear(Buffer, newSize, UsedBufferSize - newSize);
            }
            else if (newSize > Buffer.Length)
            {
                ArrayBuffer.Expand(ref Buffer, newSize);
            }

            UsedBufferSize = newSize;
        }

        internal virtual void Reset()
        {
            Buffer = new byte[0];
            UsedBufferSize = 0;
        }

        public override string ToString()
        {
            var count = Math.Min(64, UsedBufferSize);
            var sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                sb.Append(i + ":" + Buffer[i]);
                sb.Append(" | ");
            }
            if (count < UsedBufferSize)
            {
                sb.Append("... ");
                sb.Append(UsedBufferSize - 1);
                sb.Append(":");
                sb.Append(Buffer[UsedBufferSize - 1]);
            }
            return sb.ToString();
        }


        [Conditional("DEBUG_BINARY")]
        internal void GetBinaryDiff(string name, BinaryBufferReader other, List<string> differences)
        {
            if (UsedBufferSize != other.UsedBufferSize)
            {
                differences.Add(name + "." + nameof(UsedBufferSize) + ": " + UsedBufferSize + " | " + other.UsedBufferSize);
            }

            var end = Math.Min(UsedBufferSize, other.UsedBufferSize);

            for (int i = 0; i < end; i++)
            {
                if (this.Buffer[i] != other.Buffer[i])
                    differences.Add(name + "[" + i + "]: '" + Buffer[i].ToChar() + "' | '" + other.Buffer[i].ToChar() + "'  " + Buffer[i].ToString().PadLeft(3) + " | " + other.Buffer[i].ToString().PadLeft(3));
            }
        }

        [Conditional("DEBUG_BINARY")]
        internal void TraceToConsole(string header, int startIndex, int count)
        {
            if (!string.IsNullOrEmpty(header))
                Console.WriteLine(header);

            var end = startIndex + count;
            for (int i = startIndex; i < end; i++)
            {
                Console.WriteLine(Buffer[i].ToText());
            }
            Console.WriteLine();
        }

        [Conditional("DEBUG_BINARY")]
        internal static void TraceToConsole(string msg)
        {
            Console.WriteLine(msg.Replace(char.MinValue, '▬')); //  ⌂  ↔ ¤
        }


    }
}
