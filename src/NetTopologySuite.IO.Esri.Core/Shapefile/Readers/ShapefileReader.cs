using NetTopologySuite.IO.Dbf;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace NetTopologySuite.IO.Shapefile.Core
{

    /// <summary>
    /// Base class for reading a shapefile.
    /// </summary>
    public abstract class ShapefileReader : Shapefile, IEnumerable<ShapefileFeature>
    {
        private readonly ShpReader ShpReader;
        private readonly DbfReader DbfReader;


        /// <summary>
        /// Initializes a new instance of the reader class.
        /// </summary>
        /// <param name="shpStream">SHP file stream.</param>
        /// <param name="dbfStream">DBF file stream.</param>
        /// <param name="encoding">DBF file encoding. If null encoding will be guess from related .CPG file or from reserved DBF bytes.</param>
        public ShapefileReader(Stream shpStream, Stream dbfStream, Encoding encoding)
        {
            DbfReader = new DbfReader(dbfStream, encoding);

            ShpReader = CreateShpReader(shpStream);
            ShapeType = ShpReader.ShapeType;

        }

        /// <summary>
        /// Initializes a new instance of the reader class.
        /// </summary>
        /// <param name="shpPath">Path to SHP file.</param>
        /// <param name="encoding">DBF file encoding. If null encoding will be guess from related .CPG file or from reserved DBF bytes.</param>
        public ShapefileReader(string shpPath, Encoding encoding = null)
        {
            try
            {
                DbfReader = new DbfReader(Path.ChangeExtension(shpPath, ".dbf"), encoding);

                var shpStream = OpenManagedFileStream(shpPath, ".shp", FileMode.Open);
                ShpReader = CreateShpReader(shpStream);
                ShapeType = ShpReader.ShapeType;

                var prjFile = Path.ChangeExtension(shpPath, ".prj");
                if (File.Exists(prjFile))
                    Projection = File.ReadAllText(prjFile);
            }
            catch
            {
                DisposeManagedResources();
                throw;
            }
        }

        internal abstract ShpReader CreateShpReader(Stream shpStream);

        /// <inheritdoc/>
        public override DbfFieldCollection Fields => DbfReader.Fields;

        /// <inheritdoc/>
        public override ShapeType ShapeType { get; } = ShapeType.NullShape;

        /// <summary>
        /// Current shape read from the underlying SHP file. 
        /// </summary> 
        public ShpShapeBuilder Shape => ShpReader.Shape;


        /// <summary>
        /// Encoding used by the shapefile.
        /// </summary>
        public Encoding Encoding => DbfReader.Encoding;


        /// <summary>
        /// The minimum bounding rectangle orthogonal to the X and Y (and potentially the M and Z).
        /// </summary>
        public ShpBoundingBox BoundingBox => ShpReader.BoundingBox;


        /// <summary>
        /// Well-known text representation of coordinate reference system metadata from .prj file.
        /// </summary>
        /// <remarks>
        /// <a href="https://support.esri.com/en/technical-article/000001897">https://support.esri.com/en/technical-article/000001897</a>
        /// </remarks>/>
        public string Projection { get; } = null;

        /// <summary>
        /// Reads feature shape and attributes from underlying SHP and DBF files and writes them to <see cref="Shape"/> and <see cref="Fields"/> properties. 
        /// </summary>
        /// <param name="deleted">Indicates if the record was marked as deleted.</param>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next record;
        /// false if the enumerator has passed the end of the table.
        /// </returns>
        public bool Read(out bool deleted)
        {
            var readShpSucceed = ShpReader.Read();
            var readDbfSucceed = DbfReader.Read(out deleted);

            if (readDbfSucceed != readShpSucceed)
                throw new FileLoadException("Corrupted shapefile data. "
                    + "The dBASE table must contain feature attributes with one record per feature. "
                    + "There must be one-to-one relationship between geometry and attributes.");

            return readDbfSucceed;
        }


        /// <inheritdoc/>
        protected override void DisposeManagedResources()
        {
            ShpReader?.Dispose();
            DbfReader?.Dispose();

            base.DisposeManagedResources(); // This will dispose streams used by ShpReader and DbfReader. Do it at the end.
        }

        /// <summary>
        /// Moves reader back to its initial position. 
        /// </summary>
        public void Restart()
        {
            DbfReader.Restart();
            ShpReader.Restart();
        }

        #region *** Enumerator ***

        IEnumerator<ShapefileFeature> IEnumerable<ShapefileFeature>.GetEnumerator()
        {
            return new ShapefileEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ShapefileEnumerator(this);
        }

        private class ShapefileEnumerator : IEnumerator<ShapefileFeature>
        {
            private readonly ShapefileReader Owner;
            public ShapefileFeature Current { get; private set; }
            object IEnumerator.Current => Current;

            public ShapefileEnumerator(ShapefileReader owner)
            {
                Owner = owner;
            }

            public void Reset()
            {
                Owner.Restart();
            }

            public bool MoveNext()
            {
                if (!Owner.Read(out var deleted))
                {
                    return false;
                }

                if (deleted)
                {
                    return MoveNext();
                }

                Current = new ShapefileFeature(Owner.Shape.GetParts(), Owner.Fields.GetValues());
                return true;
            }

            public void Dispose()
            {
                // Nothing to dispose
            }
        }

        #endregion


        private readonly static BinaryBufferReader ShpHeader = new BinaryBufferReader(Shapefile.FileHeaderSize);

        /// <summary>
        /// Opens shapefile reader.
        /// </summary>
        /// <param name="shpPath">Path to shapefile.</param>
        /// <param name="encoding">DBF file encoding. If null encoding will be guess from related .CPG file or from reserved DBF bytes.</param>
        /// <returns>Shapefile reader.</returns>
        public static ShapefileReader Open(string shpPath, Encoding encoding = null)
        {
            var shapeType = Core.ShpReader.GetShapeType(shpPath);

            if (shapeType.IsPoint())
            {
                return new ShapefilePointReader(shpPath, encoding);
            }
            else if (shapeType.IsMultiPoint())
            {
                return new ShapefileMultiPointReader(shpPath, encoding);
            }
            else if (shapeType.IsPolyLine() || shapeType.IsPolygon())
            {
                return new ShapefileMultiPartReader(shpPath, encoding);
            }
            else
            {
                throw new FileLoadException("Unsupported shapefile type: " + shapeType, shpPath);
            }
        }


        /// <summary>
        /// Reads all features from shapefile.
        /// </summary>
        /// <param name="shpPath">Path to shapefile.</param>
        /// <param name="encoding">DBF file encoding. If null encoding will be guess from related .CPG file or from reserved DBF bytes.</param>
        /// <returns>Shapefile features.</returns>
        public static ShapefileFeature[] ReadAll(string shpPath, Encoding encoding = null)
        {
            using (var shp = Open(shpPath, encoding))
            {
                return shp.ToArray();
            }
        }

        [Conditional("DEBUG_BINARY")]
        internal void GetBinaryDiff(ShapefileReader other, List<string> differences)
        {
            ShpReader.GetBinaryDiff(other.ShpReader, differences);
            DbfReader.GetBinaryDiff(other.DbfReader, differences);

            differences.Add("SHP Records");
            int recNo = 1;
            while (Read(out var del) && other.Read(out var delOther))
            {
                differences.Add("Record number: " + recNo++);
                if (del != delOther)
                {
                    differences.Add("Deleted: " + del + " | " + delOther);
                }
                ShpReader.GetBinaryDiff(other.ShpReader, differences);
            }
            
        }
    }



}
