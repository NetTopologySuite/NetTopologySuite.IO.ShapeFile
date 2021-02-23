using System;
using System.Collections.Generic;
using System.Text;

namespace NetTopologySuite.IO.Shapefile.Core
{
    internal static class ShxBinaryExtensions
    {
        // ESRI Shapefile Technical Description: The index file header is identical in organization to the SHP file header.

        public static void WriteShxFileHeader(this BinaryBufferWriter binary, ShapeType type, int fileLength, ShpBoundingBox extent, bool hasZ, bool hasM)
        {
            binary.WriteShpFileHeader(type, fileLength, extent, hasZ, hasM);
        }

        public static void ReadShxFileHeader(this BinaryBufferReader binary, out ShapeType type, out int fileLength)
        {
            binary.ReadShpFileHeader(out type, out fileLength, new ShpBoundingBox());
        }


        // SHX Record

        public static void WriteShxRecord(this BinaryBufferWriter shxRecord, int offset, int length)
        {
            shxRecord.Write16BitWords(offset);
            shxRecord.Write16BitWords(length); 
        }

        public static void ReadShxRecord(this BinaryBufferReader shxRecord, out int offset, out int length)
        {
            offset = shxRecord.Read16BitWords();
            length = shxRecord.Read16BitWords();
        }


        // ESRI Shapefile Technical Description:

        // The value for file length is the total length of the file in 16-bit words,
        // including the fifty 16-bit words that make up the header (which is 100 bytes long).

        // The content length for a record is the length of the record contents section measured in 16-bit words.Each record
        // Therefore, contributes (4 + content length) 16-bit words toward the total length of the file, as stored at Byte 24 in the file header.

        // The file length stored in the index file header is the total length of the index file in 16-bit
        // words(the fifty 16-bit words of the header plus 4 times the number of records).

        // The offset of a record in the main file is the number of 16-bit words from the start of the main fileto the first byte of the record header for the record.
        // Thus, the offset for the first record in the main file is 50, given the 100-byte header.

        public static void Write16BitWords(this BinaryBufferWriter binary, int value)
        {
            binary.WriteInt32BigEndian(value / 2);
        }
        public static int Read16BitWords(this BinaryBufferReader binary)
        {
            return binary.ReadInt32BigEndian() * 2;
        }
    }
}
