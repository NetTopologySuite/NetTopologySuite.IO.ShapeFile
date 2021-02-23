using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NetTopologySuite.IO.Shapefile.Core
{

    /// <summary>
    /// Base class class for reading a fixed-length file header and variable-length records from a *.SHP file. 
    /// </summary>
    public abstract class ShpReader : ManagedDisposable
    {
        private readonly Stream ShpStream;
        private readonly int ShpEndPosition;
        private readonly BinaryBufferReader Header = new BinaryBufferReader(Shapefile.FileHeaderSize);
        private readonly BinaryBufferReader RecordContent = new BinaryBufferReader();
        internal readonly bool HasM;
        internal readonly bool HasZ;

        /// <summary>
        /// Shapefile Spec: <br/>
        /// The one-to-one relationship between geometry and attributes is based on record number.
        /// Attribute records in the dBASE file must be in the same order as records in the main file.
        /// </summary>
        /// <remarks>
        /// DBF does not have recor number attribute.
        /// </remarks>
        private int RecordNumber = 1;


        /// <summary>
        /// Initializes a new instance of the reader class.
        /// </summary>
        /// <param name="shpStream">SHP file stream.</param>
        public ShpReader(Stream shpStream) 
        {
            ShpStream = shpStream ?? throw new ArgumentNullException("Uninitialized SHP stream.", nameof(shpStream));

            if (ShpStream.Position != 0)
                ShpStream.Seek(0, SeekOrigin.Begin);

            Header.LoadFrom(ShpStream, Shapefile.FileHeaderSize);
            Header.ReadShpFileHeader(out var type, out var fileLength, BoundingBox);
            ShpEndPosition = fileLength - 1;

            ShapeType = type;
            HasM = type.HasM();
            HasZ = type.HasZ();

            Debug.Assert(RecordContent.End, "Shapefile header", "Unexpected SHP binary reader position.");
        }


        internal void Restart()
        {
            ShpStream.Seek(Shapefile.FileHeaderSize, SeekOrigin.Begin);
        }

        /// <summary>
        /// Shape type.
        /// </summary>
        public ShapeType ShapeType { get; } = ShapeType.NullShape;

        /// <summary>
        /// Current shape read from the underlying stream. 
        /// </summary> 
        public ShpShapeBuilder Shape { get; } = new ShpShapeBuilder();

        /// <summary>
        /// The minimum bounding rectangle orthogonal to the X and Y (and potentially the M and Z).
        /// </summary>
        public ShpBoundingBox BoundingBox { get; } = new ShpBoundingBox();

        /// <summary>
        /// Reads content of the <see cref="Shape"/> from the underlying stream.
        /// </summary>
        /// <returns>Value indicating if reading next record was successful.</returns>
        public bool Read()
        {
            Shape.Clear();

            if (!ReadRecordContent())
                return false;

            var type = RecordContent.ReadGeometryType();
            if (type == ShapeType.NullShape)
            {
                return true; // Empty points collection
            }
            else if (type != ShapeType)
            {
                throw GetInvalidRecordTypeException(type);
            }

            ReadShape(RecordContent);
            return true;
        }

        internal abstract void ReadShape(BinaryBufferReader shapeBinary);


        internal bool ReadRecordContent()
        {
            if (ShpStream.Position >= ShpEndPosition)
                return false;

            Header.LoadFrom(ShpStream, Shapefile.RecordHeaderSize);
            Header.ReadShpRecordHeader(out var recordNumber, out var contentLength);

            RecordContent.LoadFrom(ShpStream, contentLength);

            Debug.Assert(recordNumber == RecordNumber++, "Shapefile record", $"Unexpected SHP record number: {recordNumber} (expected {RecordNumber}).");
            return true;
        }

        /// <inheritdoc/>
        protected override void DisposeManagedResources()
        {
            Header?.Reset();
            RecordContent?.Reset();

            //Debug.Assert(ShpStream.Position == ShpEndPosition + 1, "Shapefile reader", "Unexpected SHP file length. This may happen when reading SHP file was not finished.");
            base.DisposeManagedResources();
        }


        internal Exception GetInvalidRecordTypeException(ShapeType type)
        {
            return new FileLoadException($"Ivalid shapefile record type. {GetType().Name} does not support { type } shapes.");
        }

        internal Exception GetUnsupportedShapeTypeException()
        {
            throw new FileLoadException(GetType().Name + $" does not support {ShapeType} shapes.");
        }

        internal static ShapeType GetShapeType(Stream shpStream)
        {
            var binary = new BinaryBufferReader(Shapefile.FileHeaderSize);
            binary.LoadFrom(shpStream, Shapefile.FileHeaderSize);

            var fileCode = binary.ReadInt32BigEndian();
            if (fileCode != Shapefile.FileCode)
                throw new FileLoadException("Invalid shapefile format.");

            binary.Advance(28);
            return binary.ReadGeometryType();
        }

        /// <summary>
        /// Reads shape type information from SHP file.
        /// </summary>
        /// <param name="shpPath">Path to SHP file.</param>
        /// <returns>Shape type.</returns>
        public static ShapeType GetShapeType(string shpPath)
        {
            if (Path.GetExtension(shpPath).ToLowerInvariant() != ".shp")
                throw new FileLoadException("Specified file must have .shp extension.");

            using (var shpStream = new FileStream(shpPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return GetShapeType(shpStream);
            }
        }


        [Conditional("DEBUG_BINARY")]
        internal void GetBinaryDiff(ShpReader other, List<string> differences)
        {
            Header.GetBinaryDiff("SHP Header", other.Header, differences);
            RecordContent.GetBinaryDiff("SHP RecordContent", other.RecordContent, differences);
        }


    }


}
