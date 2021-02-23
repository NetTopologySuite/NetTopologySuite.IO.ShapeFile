using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetTopologySuite.IO
{
    internal class BinaryBufferWriter : BinaryBuffer
    {

        public BinaryBufferWriter(int initialCapacity = 256) : base(initialCapacity)
        {
        }


        /// <summary>
        /// Gets or sets the value at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The value at the specified index.</returns>
        public byte this[int index]
        {
            get
            {
                if (index < 0 || index >= UsedBufferSize)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return Buffer[index];
            }
            private set
            {
                if (index < 0 || index >= UsedBufferSize)
                    throw new ArgumentOutOfRangeException(nameof(index));
                Buffer[index] = value;
            }
        }


        /// <summary>
        /// Gets the count of bytes written to the underlying buffer.
        /// </summary>
        public int Size => UsedBufferSize;


        /// <summary>
        /// Copies written bytes to the stream.
        /// </summary>
        /// <param name="dest"></param>
        public void CopyTo(Stream dest)
        {
            if (dest == null)
                throw new ArgumentNullException(this.GetType().Name + " cannot write to an uninitialized stream.", nameof(dest));

            dest.Write(Buffer, 0, UsedBufferSize);
        }

        /// <summary>
        /// Sets the reserved bytes count to zero and clears the unused bytes in the underlying buffer (sets thier values to zeros). It does not resize the underlying buffer.
        /// </summary>
        public void Clear()
        {
            SetUsedBufferSize(0);
        }

        /// <summary>
        /// Writes a 8-bit unsigned integer.
        /// </summary>
        /// <param name="value"> A 8-bit unsigned integer value.</param>
        public void WriteByte(byte value)
        {
            var index = UsedBufferSize;
            SetUsedBufferSize(UsedBufferSize + 1);
            this[index] = value;
        }

        public void WriteByteChar(char c)
        {
            if (c > byte.MaxValue)
                throw new ArgumentException("Character exceeds ASCII code table: " + c, nameof(c));

            WriteByte((byte)c);
        }


        public void WriteBytes(byte[] source)
        {
            if (source == null || source.Length < 1)
                throw new ArgumentNullException(nameof(source));

            var startIndex = UsedBufferSize;
            SetUsedBufferSize(UsedBufferSize + source.Length);

            for (int i = 0; i < source.Length; i++)
            {
                this[startIndex + i] = source[i];
            }
        }
        public void WriteBytes(byte value, int count)
        {
            var startIndex = UsedBufferSize;
            SetUsedBufferSize(UsedBufferSize + count);

            for (int i = 0; i < count; i++)
            {
                this[startIndex + i] = value;
            }
        }
        public void WriteNullBytes(int count)
        {
            WriteBytes(byte.MinValue, count);
        }


        public void WriteUInt16LittleEndian(UInt16 value)
        {
            var startIndex = UsedBufferSize;
            SetUsedBufferSize(UsedBufferSize + sizeof(UInt16));

            this[startIndex + 0] = (byte)value;
            this[startIndex + 1] = (byte)(value >> 8);
        }


        public void WriteUInt32LittleEndian(UInt32 value)
        {
            var startIndex = UsedBufferSize;
            SetUsedBufferSize(UsedBufferSize + sizeof(UInt32));

            this[startIndex + 0] = (byte)value;
            this[startIndex + 1] = (byte)(value >> 8);
            this[startIndex + 2] = (byte)(value >> 16);
            this[startIndex + 3] = (byte)(value >> 24);

        }


        public void WriteInt32LittleEndian(Int32 value)
        {
            var startIndex = UsedBufferSize;
            SetUsedBufferSize(UsedBufferSize + sizeof(Int32));

            this[startIndex + 0] = (byte)value;
            this[startIndex + 1] = (byte)(value >> 8);
            this[startIndex + 2] = (byte)(value >> 16);
            this[startIndex + 3] = (byte)(value >> 24);

        }


        public void WriteInt32BigEndian(Int32 value)
        {
            var startIndex = UsedBufferSize;
            SetUsedBufferSize(UsedBufferSize + sizeof(Int32));

            this[startIndex + 3] = (byte)value;
            this[startIndex + 2] = (byte)(value >> 8);
            this[startIndex + 1] = (byte)(value >> 16);
            this[startIndex + 0] = (byte)(value >> 24);

        }



        public unsafe void WriteDoubleLittleEndian(double value)
        {
            var startIndex = UsedBufferSize;
            SetUsedBufferSize(UsedBufferSize + sizeof(double));

            ulong valueRef = *(ulong*)&value;

            this[startIndex + 0] = (byte)valueRef;
            this[startIndex + 1] = (byte)(valueRef >> 8);
            this[startIndex + 2] = (byte)(valueRef >> 16);
            this[startIndex + 3] = (byte)(valueRef >> 24);
            this[startIndex + 4] = (byte)(valueRef >> 32);
            this[startIndex + 5] = (byte)(valueRef >> 40);
            this[startIndex + 6] = (byte)(valueRef >> 48);
            this[startIndex + 7] = (byte)(valueRef >> 56);
        }


        /// <summary>
        /// Writes string to BinaryDataWriter.
        /// </summary>
        /// <param name="s">String value to write.</param>
        /// <param name="bytesCount">Bytes count to be written in BinaryDataWriter.</param>
        /// <param name="encoding">Encoding used to translate string to bytes.</param>
        public void WriteString(string s, int bytesCount, Encoding encoding)
        {
            s = s ?? string.Empty;

            if (s.Length > bytesCount)
            {
                s = s.Substring(0, bytesCount);
            }

            // Specific encoding can add some extra bytes for national characters. Check it.
            var bytes = encoding.GetBytes(s);
            while (bytes.Length > bytesCount)
            {
                s = s.Substring(0, s.Length - 1);
                bytes = encoding.GetBytes(s);
            }

            if (bytes.Length < bytesCount)
            {
                var fixedBytes = new byte[bytesCount]; // Filled with '\0' by default
                Array.Copy(bytes, fixedBytes, bytes.Length);
                bytes = fixedBytes; // NULL terminated fixed length string
            }

            WriteBytes(bytes);  // This will SetUsedBufferSize()
        }
    }
}
