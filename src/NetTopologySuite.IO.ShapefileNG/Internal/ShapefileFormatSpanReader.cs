using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NetTopologySuite.IO.Internal
{
    internal readonly ref struct ShapefileFormatSpanReader
    {
        private readonly ReadOnlySpan<byte> _mainFile;

        private readonly ReadOnlySpan<byte> _indexFile;

        public ShapefileFormatSpanReader(ReadOnlySpan<byte> mainFile, ReadOnlySpan<byte> indexFile)
        {
            _mainFile = mainFile;
            _indexFile = indexFile;
        }

        public readonly void ValidateFileStructure()
        {
            if (_mainFile.Length < Unsafe.SizeOf<ShapefileHeaderStruct>())
            {
                throw new InvalidDataException("Main file must be at least 100 bytes long.");
            }

            ref readonly var mainFileHeader = ref MainFileHeader;

            if (_indexFile.Length < Unsafe.SizeOf<ShapefileHeaderStruct>())
            {
                throw new InvalidDataException("Index file must be at least 100 bytes long.");
            }

            ref readonly var indexFileHeader = ref IndexFileHeader;

            // it's fine if the header indicates a SMALLER length than what we were created with,
            // since that just means that the caller didn't slice off the end.  not a huge deal.
            if (mainFileHeader.FileLengthInBytes > _mainFile.Length)
            {
                throw new InvalidDataException("Main file's header thinks that the file is bigger than the size we were created with.");
            }

            // avoid repeated 32 --> 64 conversions by doing this once
            long mainFileLength = mainFileHeader.FileLengthInBytes;

            if (indexFileHeader.FileLengthInBytes > _indexFile.Length)
            {
                throw new InvalidDataException("Index file's header thinks that the file is bigger than the size we were created with.");
            }

            if (indexFileHeader.FileLengthInBytes != (RecordCount * 8) + Unsafe.SizeOf<ShapefileHeaderStruct>())
            {
                throw new InvalidDataException("Index file must contain exactly 8 bytes per record after the 100-byte header.");
            }

            int nextRecordNumber = 1;
            var fileShapeType = mainFileHeader.ShapeType;
            foreach (ref readonly var indexRecord in MemoryMarshal.Cast<byte, ShapefileIndexFileRecordNG>(_indexFile.Slice(Unsafe.SizeOf<ShapefileHeaderStruct>())))
            {
                // work in longs here, since invalid data could otherwise cause overflow.
                long recordHeaderOffset = ShapefilePrimitiveHelpers.SwapByteOrderOnLittleEndianMachines(indexRecord.BigEndianRecordHeaderOffsetInWords) * 2L;
                if (recordHeaderOffset < Unsafe.SizeOf<ShapefileHeaderStruct>())
                {
                    throw new InvalidDataException("All records in the main file must begin after the 100-byte header.");
                }

                long recordContentLengthInBytesFromIndex = ShapefilePrimitiveHelpers.SwapByteOrderOnLittleEndianMachines(indexRecord.BigEndianRecordContentLengthInWords) * 2L;
                if (recordContentLengthInBytesFromIndex < Unsafe.SizeOf<ShapeTypeNG>())
                {
                    throw new InvalidDataException("All records in the main file be at least 4 bytes long.");
                }

                long recordContentByteOffset = recordHeaderOffset + Unsafe.SizeOf<ShapefileMainFileRecordHeaderNG>();
                long recordEndByteOffset = recordContentByteOffset + recordContentLengthInBytesFromIndex;
                if (mainFileLength < recordEndByteOffset)
                {
                    throw new InvalidDataException("The index file identifies one or more records that would extend beyond the end of the main file.");
                }

                var recordHeader = MemoryMarshal.Read<ShapefileMainFileRecordHeaderNG>(_mainFile.Slice((int)recordHeaderOffset));
                long recordContentLengthInBytesFromMainFile = ShapefilePrimitiveHelpers.SwapByteOrderOnLittleEndianMachines(recordHeader.BigEndianRecordContentLengthInWords) * 2L;
                if (recordContentLengthInBytesFromIndex != recordContentLengthInBytesFromMainFile)
                {
                    throw new InvalidDataException("The index file disagrees with main file about the size of one or more records.");
                }

                if (recordHeader.RecordNumber != nextRecordNumber++)
                {
                    throw new InvalidDataException("Records in the main file are out of order.");
                }

                var recordShapeType = MemoryMarshal.Read<ShapeTypeNG>(_mainFile.Slice((int)recordContentByteOffset));
                if (recordShapeType != fileShapeType)
                {
                    throw new InvalidDataException("Shape type for all records in the main file must match shape type from its header.");
                }
            }
        }

        public ref readonly ShapefileHeaderStruct MainFileHeader
        {
            get
            {
                return ref Unsafe.As<byte, ShapefileHeaderStruct>(ref Unsafe.AsRef(in MemoryMarshal.GetReference(_mainFile)));
            }
        }

        public ref readonly ShapefileHeaderStruct IndexFileHeader
        {
            get
            {
                return ref Unsafe.As<byte, ShapefileHeaderStruct>(ref Unsafe.AsRef(in MemoryMarshal.GetReference(_indexFile)));
            }
        }

        public int RecordCount
        {
            get
            {
                return (IndexFileHeader.FileLengthInBytes - Unsafe.SizeOf<ShapefileHeaderStruct>()) / Unsafe.SizeOf<ShapefileIndexFileRecordNG>();
            }
        }

        public ReadOnlySpan<byte> GetInnerRecordContents(int recordIndex)
        {
            var indexRecord = MemoryMarshal.Read<ShapefileIndexFileRecordNG>(_indexFile.Slice(100 + (recordIndex * 8)));
            int recordContentsStart = indexRecord.RecordHeaderOffsetInBytes + Unsafe.SizeOf<ShapefileMainFileRecordHeaderNG>();
            int recordContentLength = indexRecord.RecordContentLengthInBytes;
            var recordContents = _mainFile.Slice(recordContentsStart, recordContentLength);

            // "inner" record contents = everything after the ShapeType
            return recordContents.Slice(Unsafe.SizeOf<ShapeTypeNG>());
        }
    }
}
