using NetTopologySuite.IO.Dbf;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetTopologySuite.IO.Shapefile.Core
{

    /// <summary>
    /// Polygon or PolyLine shapefile reader.
    /// </summary>
    public class ShapefileMultiPartReader : ShapefileReader
    {
        /// <inheritdoc/>
        public ShapefileMultiPartReader(Stream shpStream, Stream dbfStream, Encoding encoding = null)
            : base(shpStream, dbfStream, encoding)
        { }

        /// <inheritdoc/>
        public ShapefileMultiPartReader(string shpPath, Encoding encoding = null)
            : base(shpPath, encoding)
        { }


        internal override ShpReader CreateShpReader(Stream shpStream)
        {
            return new ShpMultiPartReader(shpStream);
        }
    }

}
