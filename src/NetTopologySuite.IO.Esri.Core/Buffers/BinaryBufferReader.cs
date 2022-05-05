using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NetTopologySuite.IO
{
    internal class BinaryBufferReader : BinaryBuffer
    {

        public BinaryBufferReader(int initialCapacity = 256) : base(initialCapacity)
        {
        }

        /// <summary>
        /// Gets the value at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The value at the specified index.</returns>
        public byte this[int index]
        {
            get
            {
                if (index < 0 || index >= UsedBufferSize)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return Buffer [index];
            }
        }

        /// <summary>
        /// Gets the current position of processed data.
        /// </summary>
        public int Position { get; private set; }


        /// <summary>
        /// Gets the count of bytes reserved by the reader.
        /// </summary>
        public int Size => UsedBufferSize;


        /// <summary>
        /// Gets the current position in the buffer.
        /// </summary>
        public bool End => Position >= UsedBufferSize;

        internal override void Reset()
        {
            base.Reset();
            SetPosition(0, 0);
        }


        /// <summary>
        /// Loads binary data from the stream and resets reader position.
        /// </summary>
        /// <param name="source">Binary data source stream.</param>
        /// <param name="bytesCount">The maximum number of bytes to be read from the stream.</param>
        public void LoadFrom(Stream source, int bytesCount)
        {
            if (source == null)
                throw new ArgumentNullException(this.GetType().Name + " cannot read from an uninitialized source stream.", nameof(source));

            SetUsedBufferSize(bytesCount);
            source.Read(Buffer, 0, bytesCount);
            SetPosition(0, 0);
        }

        /// <summary>
        /// Sets current position of processed data.
        /// </summary>
        /// <param name="startIndex">Index of the first element in BinarySegment which was processed.</param>
        /// <param name="bytesCount">The number of processed elements.</param>
        protected void SetPosition(int startIndex, int bytesCount)
        {
            if (startIndex + bytesCount > UsedBufferSize)
            {
                throw new ArgumentException("Current " + GetType().Name + ".Position could not be greater than reserved buffer size.", nameof(Position));
            }
            Position = startIndex + bytesCount;
        }

        public void MoveTo(int position)
        {
            SetPosition(position, 0);
        }

        /// <summary>
        /// Moves data processing ahead a specified number of items.
        /// </summary>
        /// <param name="count"></param>
        public void Advance(int count)
        {
            SetPosition(Position, count);
        }

        /// <summary>
        /// Advances past consecutive instances of the given value.
        /// </summary>
        /// <param name="value">The value past which the reader is to advance.</param>
        /// <returns>The number of positions the reader has advanced or -1 if value was not found.</returns>
        public bool TryAdvanceTo(byte value)
        {
            var newPosition = Array.IndexOf(Buffer, value, Position, UsedBufferSize);
            if (newPosition < 1)
            {
                Position = UsedBufferSize;
                return false;
            }
            else
            {
                Position = newPosition;
                return true;
            }
        }

        /// <summary>
        /// Advances past consecutive instances of the given value.
        /// </summary>
        /// <param name="value">The value past which the reader is to advance.</param>
        /// <returns>The number of positions the reader has advanced.</returns>
        public int AdvancePast(byte value)
        {
            int startPosition = Position;
            while (!End && Buffer[Position] == value)
            {
                Position++;
            }
            return Position - startPosition;
        }



        /// <summary>
        /// Reads a 8-bit unsigned integer and advances the current Position by one.
        /// </summary>
        /// <param name="index">The position within underlying binary data.</param>
        /// <returns>Value read from underlying data.</returns>
        public byte ReadByte(int index)
        {
            SetPosition(index, 1);
            return this[index];
        }
        public byte ReadByte()
        {
            return ReadByte(Position);
        }


        public char ReadByteChar(int index)
        {
            return (char)ReadByte(index);
        }
        public char ReadByteChar()
        {
            return ReadByteChar(Position);
        }

        protected byte[] ReadBytes(int startIndex, int count)
        {
            SetPosition(startIndex, count);
            var res = new byte[count];
            for (int i = 0; i < count; i++)
            {
                res[i] = this[startIndex + i];
            }
            return res;
        }

        protected byte[] ReadReversedBytes(int startIndex, int count)
        {
            SetPosition(startIndex, count);
            var res = new byte[count];
            for (int i = 0; i < count; i++)
            {
                res[count - i - 1] = this[startIndex + i];
            }
            return res;
        }


        /// <summary>
        /// Reads a 16-bit unsigned integer converted from two bytes at a specified position in the underlying data
        /// and advances the current Position by the number of bytes read.
        /// </summary>
        /// <param name="startIndex">The starting position within the underlying data.</param>
        /// <returns>Value read from underlying data.</returns>
        public ushort ReadUInt16LittleEndian(int startIndex)
        {
            SetPosition(startIndex, sizeof(ushort));
            return (ushort)(
                  this[startIndex + 0]
                | this[startIndex + 1] << 8
            );
        }
        public ushort ReadUInt16LittleEndian()
        {
            return ReadUInt16LittleEndian(Position);
        }


        /// <summary>
        /// Returns a 32-bit unsigned integer converted from two bytes at a specified position in the underlying data
        /// and advances the current Position by the number of bytes read.
        /// </summary>
        /// <param name="startIndex">The starting position within the underlying data.</param>
        /// <returns>Value read from underlying data.</returns>
        public uint ReadUInt32LittleEndian(int startIndex)
        {
            SetPosition(startIndex, sizeof(uint));
            return (uint)(
                  this[startIndex + 0]
                | this[startIndex + 1] << 8
                | this[startIndex + 2] << 16
                | this[startIndex + 3] << 24
            );
        }
        public uint ReadUInt32LittleEndian()
        {
            return ReadUInt32LittleEndian(Position);
        }


        public int ReadInt32LittleEndian(int startIndex)
        {
            SetPosition(startIndex, sizeof(int));
            return (int)(
                  this[startIndex + 0]
                | this[startIndex + 1] << 8
                | this[startIndex + 2] << 16
                | this[startIndex + 3] << 24
            );
        }
        public int ReadInt32LittleEndian()
        {
            return ReadInt32LittleEndian(Position);
        }


        public int ReadInt32BigEndian(int startIndex)
        {
            SetPosition(startIndex, sizeof(int));
            return (int)(
                  this[startIndex + 3]
                | this[startIndex + 2] << 8
                | this[startIndex + 1] << 16
                | this[startIndex + 0] << 24
            );
        }
        public int ReadInt32BigEndian()
        {
            return ReadInt32BigEndian(Position);
        }

        public unsafe double ReadDoubleLittleEndian(int startIndex)
        {
            /*
            var loVal = (ulong)(
                  this[startIndex + 0]
                | this[startIndex + 1] << 8
                | this[startIndex + 2] << 16
                | this[startIndex + 3] << 24
            );

            var hiVal = (ulong)(
                  this[startIndex + 4]
                | this[startIndex + 5] << 8
                | this[startIndex + 6] << 16
                | this[startIndex + 7] << 24
            );
            */

            var loVal = ReadUInt32LittleEndian(startIndex);
            var hiVal = ReadUInt32LittleEndian();

            ulong resRef = ((ulong)hiVal) << 32 | loVal;
            return *((double*)&resRef);
        }

        public double ReadDoubleLittleEndian()
        {
            return ReadDoubleLittleEndian(Position);
        }


        public string ReadString(int startIndex, int bytesCount, Encoding encoding)
        {
            // var bytes = ReadBytes(length); //Do not allocate new array if it is not needed.
            SetPosition(startIndex, bytesCount);
            var s = encoding.GetString(this.Buffer, startIndex, bytesCount);

            TraceToConsole("ReadString(" + Position.ToString() + "): '" + s + "'");

            if (IsNullString(s))
            {
                return null;
            }
            return s.Trim(char.MinValue);
        }

        private bool IsNullString(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] != char.MinValue)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Rreds string from the underlying data and advances the current Position by the number of bytes read.
        /// </summary>
        /// <param name="bytesCount"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public string ReadString(int bytesCount, Encoding encoding)
        {
            return ReadString(Position, bytesCount, encoding);
        }
    }
}
