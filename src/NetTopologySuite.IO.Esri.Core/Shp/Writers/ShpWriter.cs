using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetTopologySuite.IO.Shapefile.Core
{

    /// <summary>
    /// Base class class for writing a fixed-length file header and variable-length records to a *.SHP file.
    /// </summary>
    public abstract class ShpWriter : ManagedDisposable
    {
        private readonly Stream ShpStream;
        private readonly Stream ShxStream;
        private readonly BinaryBufferWriter Header = new BinaryBufferWriter(Shapefile.FileHeaderSize);
        private readonly BinaryBufferWriter RecordContent = new BinaryBufferWriter(Shapefile.FileHeaderSize);
        private int RecordNumber = 1; // Shapefile specs: Record numbers begin at 1.
        internal readonly bool HasM;
        internal readonly bool HasZ;


        /// <summary>
        /// Initializes a new instance of the writer class.
        /// </summary>
        /// <param name="shpStream">SHP file stream.</param>
        /// <param name="shxStream">SHX file stream.</param>
        /// <param name="type">Shape type.</param>
        public ShpWriter(Stream shpStream, Stream shxStream, ShapeType type)
        {
            ShpStream = shpStream ?? throw new ArgumentNullException("Uninitialized SHP stream.", nameof(shpStream));
            ShxStream = shxStream ?? throw new ArgumentNullException("Uninitialized SHX stream.", nameof(shxStream));

            ShapeType = type;
            HasM = type.HasM();
            HasZ = type.HasZ();

            // This advances streams position past header to to records start position
            WriteFileHeader(ShpStream);
            WriteFileHeader(ShxStream);
        }

        internal abstract bool IsNull(ShpShapeBuilder shape);

        internal ShpBoundingBox Extent { get; private set; } = new ShpBoundingBox();


        /// <summary>
        /// Shape type.
        /// </summary>
        public ShapeType ShapeType { get; } = ShapeType.NullShape;

        /// <summary>
        /// Current shape to be written to the underlying stream.
        /// </summary>
        public ShpShapeBuilder Shape { get; } = new ShpShapeBuilder();

        /// <summary>
        /// Writes the <see cref="Shape"/> to the underlying stream and then clears Shape's content.
        /// </summary>
        public void Write()
        {
            RecordContent.Clear();
            if (IsNull(Shape))
            {
                RecordContent.WriteGeometryType(ShapeType.NullShape);
            }
            else
            {
                RecordContent.WriteGeometryType(ShapeType);
                WriteShapeToBinary(RecordContent);
                Extent.Expand(Shape.Extent);
            }

            WriteRecordContent();
            Shape.Clear();
        }

        internal abstract void WriteShapeToBinary(BinaryBufferWriter shpRecordBinary);

        internal void WriteRecordContent()
        {
            Header.Clear();
            Header.WriteShxRecord((int)ShpStream.Position, RecordContent.Size);
            Header.CopyTo(ShxStream); // SHX Record

            Header.Clear();
            Header.WriteShpRecordHeader(RecordNumber, RecordContent.Size);
            Header.CopyTo(ShpStream);           // SHP Record header
            RecordContent.CopyTo(ShpStream);    // SHP Record content

            RecordNumber++;
        }

        private void WriteFileHeader(Stream stream)
        {
            if (stream == null)
                return;

            Header.Clear();
            Header.WriteShpFileHeader(ShapeType, (int)stream.Length, Extent, HasZ, HasM);

            stream.Seek(0, SeekOrigin.Begin);
            Header.CopyTo(stream);
        }

        /// <inheritdoc/>
        protected override void DisposeManagedResources()
        {
            if (ShpStream != null && ShpStream.Position > Shapefile.FileHeaderSize)
            {
                WriteFileHeader(ShpStream);
                WriteFileHeader(ShxStream);
            }

            RecordContent?.Reset();
            Header?.Reset();

            base.DisposeManagedResources(); // This will dispose owned ShpStream and ShxStream.
        }

        internal Exception GetUnsupportedShapeTypeException()
        {
            throw new FileLoadException(GetType().Name + $" does not support {ShapeType} shapes.");
        }
    }


}
