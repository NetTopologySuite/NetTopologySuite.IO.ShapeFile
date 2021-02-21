using NetTopologySuite.IO.Dbf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetTopologySuite.IO.Shapefile.Core
{

    /// <summary>
    /// Base class for writing a shapefile.
    /// </summary>
    public abstract class ShapefileWriter : Shapefile
    {
        private readonly ShpWriter ShpWriter;
        private readonly DbfWriter DbfWriter;

        /// <summary>
        /// Initializes a new instance of the writer class.
        /// </summary>
        /// <param name="shpStream">SHP file stream.</param>
        /// <param name="shxStream">SHX file stream.</param>
        /// <param name="dbfStream">DBF file stream.</param>
        /// <param name="type">Shape type.</param>
        /// <param name="fields">Shapefile fields definitions.</param>
        /// <param name="encoding">DBF file encoding. If null encoding will be guess from related .CPG file or from reserved DBF bytes.</param>
        internal ShapefileWriter(Stream shpStream, Stream shxStream, Stream dbfStream, ShapeType type, IReadOnlyList<DbfField> fields, Encoding encoding)
        {
            DbfWriter = new DbfWriter(dbfStream, fields, encoding);


            ShpWriter = CreateShpWriter(shpStream, shxStream);
            ShapeType = type;
        }


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
                DbfWriter = new DbfWriter(Path.ChangeExtension(shpPath, ".dbf"), fields, encoding);

                ShapeType = type;
                var shpStream = OpenManagedFileStream(shpPath, ".shp", FileMode.Create);
                var shxStream = OpenManagedFileStream(shpPath, ".shx", FileMode.Create);
                ShpWriter = CreateShpWriter(shpStream, shxStream);

                if (!string.IsNullOrWhiteSpace(projection))
                    File.WriteAllText(Path.ChangeExtension(shpPath, ".prj"), projection);
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

        internal abstract ShpWriter CreateShpWriter(Stream shpStream, Stream shxStream);

        /// <inheritdoc/>
        public override ShapeType ShapeType { get; } = ShapeType.NullShape;

        /// <inheritdoc/>
        public override DbfFieldCollection Fields => DbfWriter.Fields;

        /// <summary>
        /// Current shape to be written to underlying SHP file. 
        /// </summary> 
        public ShpShapeBuilder Shape => ShpWriter.Shape;


        /// <summary>
        /// Wrties <see cref="Shape"/> and <see cref="Fields"/> values to underlying SHP and DBF files. 
        /// </summary>
        public void Write()
        {
            ShpWriter.Write();
            DbfWriter.Write();
        }

        /// <summary>
        /// Writes next feature record containing shape geometry and its attributes.
        /// </summary>
        /// <param name="shapeParts">Shape parts.</param>
        /// <param name="attributes">Attributes  associated with the feature.</param>
        public void Write(IEnumerable<IEnumerable<ShpCoordinates>> shapeParts, IReadOnlyDictionary<string, object> attributes)
        {
            Shape.Clear();
            foreach (var part in shapeParts)
            {
                Shape.StartNewPart();
                foreach (var pt in part)
                {
                    Shape.AddPoint(pt);
                }
            }

            Fields.SetValues(attributes);

            Write();
        }

        /// <summary>
        /// Writes feature to the shapefile.
        /// </summary>
        public void Write(ShapefileFeature feature)
        {
                Write(feature.Shape, feature.Attributes);
        }

        /// <summary>
        /// Writes features to the shapefile.
        /// </summary>
        public void Write(IEnumerable<ShapefileFeature> features)
        {
            foreach (var feature in features)
            {
                Write(feature);
            }
        }

        /// <inheritdoc/>
        protected override void DisposeManagedResources()
        {
            ShpWriter?.Dispose();
            DbfWriter?.Dispose();

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
            else if (type.IsPolyLine() || type.IsPolygon())
            {
                return new ShapefileMultiPartWriter(shpPath, type, fields, encoding, projection);
            }
            else
            {
                throw new FileLoadException("Unsupported shapefile type: " + type, shpPath);
            }
        }
    }


}
