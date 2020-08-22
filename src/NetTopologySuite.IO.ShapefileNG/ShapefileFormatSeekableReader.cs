using System.IO;

namespace NetTopologySuite.IO
{
    internal sealed class ShapefileFormatSeekableReader
    {
        private readonly ShapefileFormatForwardOnlyReader _mainFileReader;

        private readonly Stream _seekableMainFileStream;

        private readonly ShapefileFormatForwardOnlyReader _indexFileReader;

        private readonly Stream _seekableIndexFileStream;

        private int _lastRecordIndex = -1;

        private ShapefileMainFileRecordHeaderNG _lastMainFileRecordHeader;

        public ShapefileFormatSeekableReader(Stream seekableMainFileStream, Stream seekableIndexFileStream)
        {
            _mainFileReader = new ShapefileFormatForwardOnlyReader(seekableMainFileStream);
            _indexFileReader = new ShapefileFormatForwardOnlyReader(seekableIndexFileStream);

            _seekableMainFileStream = seekableMainFileStream;
            _seekableIndexFileStream = seekableIndexFileStream;

            seekableMainFileStream.Position = 0;
            MainFileHeader = _mainFileReader.ReadHeader();
            if (seekableMainFileStream.Length != MainFileHeader.FileLengthInBytes)
            {
                throw new InvalidDataException($"Main file stream is {seekableMainFileStream.Length} bytes long, but the header indicates that it should be {MainFileHeader.FileLengthInBytes} bytes long.");
            }

            seekableIndexFileStream.Position = 0;
            IndexFileHeader = _indexFileReader.ReadHeader();
            if (seekableIndexFileStream.Length != IndexFileHeader.FileLengthInBytes)
            {
                throw new InvalidDataException($"Index file stream is {seekableIndexFileStream.Length} bytes long, but the header indicates that it should be {IndexFileHeader.FileLengthInBytes} bytes long.");
            }

            RecordCount = (int)((IndexFileHeader.FileLengthInBytes - 100) / 8);
        }

        public ShapefileHeaderNG MainFileHeader { get; }

        public ShapefileHeaderNG IndexFileHeader { get; }

        public int RecordCount { get; }

        public ShapefileMainFileRecordHeaderNG GetMainFileRecordHeaderNG(int index)
        {
            if (index == _lastRecordIndex)
            {
                return _lastMainFileRecordHeader;
            }

            _seekableIndexFileStream.Position = 100 + ((uint)index * 8);
            var indexFileRecordHeader = _indexFileReader.ReadIndexFileRecordHeader();

            _seekableMainFileStream.Position = indexFileRecordHeader.RecordHeaderOffsetInBytes;
            var result = _mainFileReader.ReadMainFileRecordHeader();
            if (result.RecordNumber != index + 1)
            {
                throw new InvalidDataException($"Index file is inconsistent with the main file: index record #{index + 1} points to main file record #{result.RecordNumber}.");
            }

            _lastRecordIndex = index;
            return _lastMainFileRecordHeader = result;
        }
    }
}
