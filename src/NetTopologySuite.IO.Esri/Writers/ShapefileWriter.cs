using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Dbf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NetTopologySuite.IO.Shapefile
{
    /// <summary>
    /// Base class for writing a shapefile.
    /// </summary>
    public abstract class ShapefileWriter : Shapefile
    {
        private readonly Core.ShapefileWriter Writer;


        /// <summary>
        /// Initializes a new instance of the writer class.
        /// </summary>
        /// <param name="shpPath">Path to SHP file.</param>
        /// <param name="type">Shape type.</param>
        /// <param name="fields">Shapefile attribute definitions.</param>
        /// <param name="encoding">DBF file encoding. If null encoding will be guess from related .CPG file or from reserved DBF bytes.</param>
        /// <param name="projection">Projection metadata for the shapefile (.prj file).</param>
        internal ShapefileWriter(string shpPath, ShapeType type, IReadOnlyList<DbfField> fields, Encoding encoding, string projection)
        {
            try
            {
                Writer = CreateWriter(shpPath, type, fields, encoding, projection);
                ShapeType = type;
            }
            catch
            {
                DisposeManagedResources();
                throw;
            }
        }


        /// <summary>
        /// Initializes a new instance of the writer class.
        /// </summary>
        /// <param name="shpPath">Path to SHP file.</param>
        /// <param name="type">Shape type.</param>
        /// <param name="fields">Shapefile attribute definitions.</param>
        internal ShapefileWriter(string shpPath, ShapeType type, params DbfField[] fields) : this(shpPath, type, fields, null, null)
        {
        }

        internal abstract Core.ShapefileWriter CreateWriter(string shpPath, ShapeType type, IReadOnlyList<DbfField> fields, Encoding encoding, string projection);

        internal abstract void GetShape(Geometry geometry, Core.ShpShapeBuilder shape);

        /// <inheritdoc/>
        public override ShapeType ShapeType { get; } = ShapeType.NullShape;

        /// <inheritdoc/>
        public override DbfFieldCollection Fields => Writer.Fields;


        /// <summary>
        /// Writes geometry and feature attributes to underlying SHP and DBF files.
        /// </summary>
        public void Write(Geometry geometry, IAttributesTable attributes)
        {
            if (geometry != null && !geometry.IsEmpty)
            {
                GetShape(geometry, Writer.Shape);
            }
            else
            {
                Writer.Shape.Clear();
            }

            foreach (var field in Writer.Fields)
            {
                field.Value = attributes[field.Name];
            }
            Writer.Write();
        }


        /// <summary>
        /// Writes geometry and feature attributes to underlying SHP and DBF files.
        /// </summary>
        public void Write(IFeature feature)
        {
            Write(feature.Geometry, feature.Attributes);
        }


        /// <summary>
        /// Writes geometry and feature attributes to underlying SHP and DBF files.
        /// </summary>
        public void Write(IEnumerable<IFeature> features)
        {
            foreach (var feature in features)
            {
                Write(feature);
            }
        }


        internal Exception GetUnsupportedGeometryTypeException(Geometry geometry)
        {
            return new ArgumentException(GetType().Name + " does not support " + geometry.GeometryType + " geometry.");
        }


        /// <inheritdoc/>
        protected override void DisposeManagedResources()
        {
            Writer?.Dispose();
            base.DisposeManagedResources();  // This will dispose streams used by ShpWriter and DbfWriter. Do it at the end.
        }


        /// <summary>
        /// Opens shapefile writer.
        /// </summary>
        /// <param name="shpPath">Path to shapefile.</param>
        /// <param name="type">Shape type.</param>
        /// <param name="fields">Shapefile fields definitions.</param>
        /// <param name="encoding">DBF file encoding. If null encoding will be guess from related .CPG file or from reserved DBF bytes.</param>
        /// <param name="projection">Projection metadata for the shapefile (.prj file).</param>
        /// <returns>Shapefile writer.</returns>
        public static ShapefileWriter Open(string shpPath, ShapeType type, IReadOnlyList<DbfField> fields, Encoding encoding = null, string projection = null)
        {
            if (type.IsPoint())
            {
                return new ShapefilePointWriter(shpPath, type, fields, encoding, projection);
            }
            else if (type.IsMultiPoint())
            {
                return new ShapefileMultiPointWriter(shpPath, type, fields, encoding, projection);
            }
            else if (type.IsPolyLine())
            {
                return new ShapefilePolyLineWriter(shpPath, type, fields, encoding, projection);
            }
            else if (type.IsPolygon())
            {
                return new ShapefilePolygonWriter(shpPath, type, fields, encoding, projection);
            }
            else
            {
                throw new FileLoadException("Unsupported shapefile type: " + type, shpPath);
            }
        }

        /// <summary>
        /// Opens shapefile writer.
        /// </summary>
        /// <param name="shpPath">Path to shapefile.</param>
        /// <param name="type">Shape type.</param>
        /// <param name="fields">Shapefile fields definitions.</param>
        /// <returns>Shapefile writer.</returns>
        public static ShapefileWriter Open(string shpPath, ShapeType type, params DbfField[] fields)
        {
            return Open(shpPath, type, fields, null, null);
        }
    }
}
