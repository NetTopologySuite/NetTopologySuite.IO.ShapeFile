using NetTopologySuite.IO.Dbf;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetTopologySuite.IO.Shapefile.Core
{

    /// <summary>
    /// Polygon or PolyLine shapefile writer.
    /// </summary>
    public class ShapefileMultiPartWriter : ShapefileWriter
    {
        /// <inheritdoc/>
        public ShapefileMultiPartWriter(Stream shpStream, Stream shxStream, Stream dbfStream, ShapeType type, IReadOnlyList<DbfField> fields, Encoding encoding = null)
            : base(shpStream, shxStream, dbfStream, type, fields, encoding)
        { }

        /// <inheritdoc/>
        public ShapefileMultiPartWriter(string shpPath, ShapeType type, IReadOnlyList<DbfField> fields, Encoding encoding = null, string projection = null)
            : base(shpPath, type, fields, encoding, projection)
        { }

        /// <inheritdoc/>
        public ShapefileMultiPartWriter(string shpPath, ShapeType type, params DbfField[] fields)
            : base(shpPath, type, fields)
        {
        }

        internal override ShpWriter CreateShpWriter(Stream shpStream, Stream shxStream)
        {
            return new ShpMultiPartWriter(shpStream, shxStream, ShapeType);
        }
    }
}
