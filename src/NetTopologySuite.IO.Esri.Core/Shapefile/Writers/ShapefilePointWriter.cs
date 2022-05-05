using NetTopologySuite.IO.Dbf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace NetTopologySuite.IO.Shapefile.Core
{

    /// <summary>
    /// Point shapefile writer.
    /// </summary>
    public class ShapefilePointWriter : ShapefileWriter
    {
        /// <inheritdoc/>
        public ShapefilePointWriter(Stream shpStream, Stream shxStream, Stream dbfStream, ShapeType type, IReadOnlyList<DbfField> fields, Encoding encoding = null)
            : base(shpStream, shxStream, dbfStream, type, fields, encoding)
        { }

        /// <inheritdoc/>
        public ShapefilePointWriter(string shpPath, ShapeType type, IReadOnlyList<DbfField> fields, Encoding encoding = null, string projection = null)
            : base(shpPath, type, fields, encoding, projection)
        { }

        /// <inheritdoc/>
        public ShapefilePointWriter(string shpPath, ShapeType type, params DbfField[] fields)
           : base(shpPath, type, fields)
        {
        }

        private ShpPointWriter PointWriter;

        internal override ShpWriter CreateShpWriter(Stream shpStream, Stream shxStream)
        {
            PointWriter = new ShpPointWriter(shpStream, shxStream, ShapeType);
            return PointWriter;
        }

        /// <summary>
        /// Writes feature record.
        /// </summary>
        /// <param name="feature">Feature record.</param>
        public void Write(ShapefilePointFeature feature)
        {
            Point = feature.Shape;
            Fields.SetValues(feature.Attributes);

            Write();
        }

        /// <summary>
        /// Writes feature record collection.
        /// </summary>
        /// <param name="features">Feature record collection</param>
        public void Write(IEnumerable<ShapefilePointFeature> features)
        {
            foreach (var feature in features)
            {
                Write(feature);
            }
        }

        /// <summary>
        /// Point shape.
        /// </summary>
        public ShpCoordinates Point
        {
            get => PointWriter.Point;
            set => PointWriter.Point = value;
        }

    }
}
