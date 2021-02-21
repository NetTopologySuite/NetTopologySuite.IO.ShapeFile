using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Dbf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NetTopologySuite.IO.Shapefile
{
    /// <summary>
    /// Base class for reading a shapefile.
    /// </summary>
    public abstract class ShapefileReader : Shapefile, IEnumerable<Feature>
    {
        private protected readonly bool HasZ;
        private protected readonly bool HasM;
        private protected Core.ShapefileReader Reader { get; }

        /// <summary>
        /// Initializes a new instance of the reader class.
        /// </summary>
        /// <param name="shpPath">Path to SHP file.</param>
        /// <param name="encoding">DBF file encoding. If null encoding will be guess from related .CPG file or from reserved DBF bytes.</param>
        internal ShapefileReader(string shpPath, Encoding encoding)
        {
            try
            {
                Reader = CreateReader(shpPath, encoding);
                HasZ = Reader.ShapeType.HasZ();
                HasM = Reader.ShapeType.HasM();

                var bb = Reader.BoundingBox;
                BoundingBox = new Envelope(bb.X.Min, bb.X.Max, bb.Y.Min, bb.Y.Max);
            }
            catch
            {
                DisposeManagedResources();
                throw;
            }
        }

        internal abstract Core.ShapefileReader CreateReader(string shpPath, Encoding encoding);


        /// <inheritdoc/>
        public override ShapeType ShapeType => Reader.ShapeType;

        /// <inheritdoc/>
        public override DbfFieldCollection Fields => Reader.Fields;


        /// <summary>
        /// Encoding used by the shapefile.
        /// </summary>
        public Encoding Encoding => Reader.Encoding;


        /// <summary>
        /// Well-known text representation of coordinate reference system metadata from .prj file.
        /// </summary>
        /// <remarks>
        /// <a href="https://support.esri.com/en/technical-article/000001897">https://support.esri.com/en/technical-article/000001897</a>
        /// </remarks>/>
        public string Projection => Reader.Projection;

        /// <summary>
        /// The minimum bounding rectangle orthogonal to the X and Y.
        /// </summary>
        public Envelope BoundingBox { get; } = new Envelope();


        /// <summary>
        /// Reads geometry and feature attributes from underlying SHP and DBF files. 
        /// </summary>
        /// <param name="geometry">Feature geometry.</param>
        /// <param name="attributes">Feature atrributes.</param>
        /// <param name="deleted">Indicates if the record was marked as deleted.</param>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next record;
        /// false if the enumerator has passed the end of the table.
        /// </returns>
        public abstract bool Read(out Geometry geometry, out AttributesTable attributes, out bool deleted);


        /// <inheritdoc/>
        protected override void DisposeManagedResources()
        {
            Reader?.Dispose();
            base.DisposeManagedResources(); // This will dispose streams used by ShpReader and DbfReader. Do it at the end.
        }

        internal void Restart()
        {
            Reader.Restart();
        }

        #region *** Enumerator ***

        IEnumerator<Feature> IEnumerable<Feature>.GetEnumerator()
        {
            return new FeatureEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new FeatureEnumerator(this);
        }

        private class FeatureEnumerator : IEnumerator<Feature>
        {
            private readonly ShapefileReader Owner;
            public Feature Current { get; private set; }
            object IEnumerator.Current => Current;

            public FeatureEnumerator(ShapefileReader owner)
            {
                Owner = owner;
            }

            public void Reset()
            {
                Owner.Restart();
            }

            public bool MoveNext()
            {
                if (!Owner.Read(out var geometry, out var attributes, out var deleted))
                {
                    return false;
                }

                if (deleted)
                {
                    return MoveNext();
                }

                Current = geometry.ToFeature(attributes); 
                return true;
            }

            public void Dispose()
            {
                // Nothing to dispose
            }
        }

        #endregion


        /// <summary>
        /// Opens shapefile reader.
        /// </summary>
        /// <param name="shpPath">Path to shapefile.</param>
        /// <param name="encoding">DBF file encoding. If null encoding will be guess from related .CPG file or from reserved DBF bytes.</param>
        /// <returns>Shapefile reader.</returns>
        public static ShapefileReader Open(string shpPath, Encoding encoding = null)
        {
            shpPath = Path.ChangeExtension(shpPath, ".shp");
            var shapeType = Core.ShpReader.GetShapeType(shpPath);

            if (shapeType.IsPoint())
            {
                return new ShapefilePointReader(shpPath, encoding);
            }
            else if (shapeType.IsMultiPoint())
            {
                return new ShapefileMultiPointReader(shpPath, encoding);
            }
            else if (shapeType.IsPolyLine())
            {
                return new ShapefilePolyLineReader(shpPath, encoding);
            }
            else if (shapeType.IsPolygon())
            {
                return new ShapefilePolygonReader(shpPath, encoding);
            }
            else
            {
                throw new FileLoadException("Unsupported shapefile type: " + shapeType, shpPath);
            }
        }

        /// <summary>
        /// Reads all shapefile features.
        /// </summary>
        /// <param name="shpPath">Path to shapefile.</param>
        /// <param name="encoding">DBF file encoding. If null encoding will be guess from related .CPG file or from reserved DBF bytes.</param>
        /// <returns>Shapefile features collection.</returns>
        public static Feature[] ReadAll(string shpPath, Encoding encoding = null)
        {
            using (var shpReader = Open(shpPath, encoding))
            {
                return shpReader.ToArray();
            }
        }

        /// <summary>
        /// Reads all shapefile geometries.
        /// </summary>
        /// <param name="shpPath">Path to SHP file.</param>
        /// <returns>Shapefile geometry collection.</returns>
        public static Geometry[] ReadAllGeometries(string shpPath)
        {
            var shapeType = Core.ShpReader.GetShapeType(shpPath);
            var hasZ = shapeType.HasZ();
            var hasM = shapeType.HasM();

            using (var shpStream = new FileStream(shpPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (shapeType.IsPoint())
                {
                    return ReadAllPoints(shpStream, hasZ, hasM).ToArray();
                }
                else if (shapeType.IsMultiPoint())
                {
                    return ReadAllMultiPoints(shpStream, hasZ, hasM).ToArray();
                }
                else if (shapeType.IsPolyLine())
                {
                    return ReadAllPolyLines(shpStream, hasZ, hasM).ToArray();
                }
                else if (shapeType.IsPolygon())
                {
                    return ReadAllPolygons(shpStream, hasZ, hasM).ToArray();
                }
                else
                {
                    throw new FileLoadException("Unsupported shapefile type: " + shapeType, shpPath);
                }
            }
        }

        private static IEnumerable<Point> ReadAllPoints(Stream shpStream, bool hasZ, bool hasM)
        {
            using (var shpReader = new Core.ShpPointReader(shpStream))
            {
                while (shpReader.Read())
                {
                    yield return ShapefilePointReader.GetPoint(shpReader.Shape, hasZ, hasM);
                }
            }
        }

        private static IEnumerable<MultiPoint> ReadAllMultiPoints(Stream shpStream, bool hasZ, bool hasM)
        {
            using (var shpReader = new Core.ShpMultiPartReader(shpStream))
            {
                while (shpReader.Read())
                {
                    yield return ShapefileMultiPointReader.GetMultiPoint(shpReader.Shape, hasZ, hasM);
                }
            }
        }

        private static IEnumerable<MultiLineString> ReadAllPolyLines(Stream shpStream, bool hasZ, bool hasM)
        {
            using (var shpReader = new Core.ShpMultiPartReader(shpStream))
            {
                while (shpReader.Read())
                {
                    yield return ShapefilePolyLineReader.GetMultiLineString(shpReader.Shape, hasZ, hasM);
                }
            }
        }

        private static IEnumerable<MultiPolygon> ReadAllPolygons(Stream shpStream, bool hasZ, bool hasM)
        {
            using (var shpReader = new Core.ShpMultiPartReader(shpStream))
            {
                while (shpReader.Read())
                {
                    yield return ShapefilePolygonReader.GetMultiPolygon(shpReader.Shape, hasZ, hasM);
                }
            }
        }
    }
}
