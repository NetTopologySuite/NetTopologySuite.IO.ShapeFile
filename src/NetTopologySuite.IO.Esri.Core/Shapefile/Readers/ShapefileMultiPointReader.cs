using NetTopologySuite.IO.Dbf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetTopologySuite.IO.Shapefile.Core
{

    /// <summary>
    /// MultiPoint shapefile reader.
    /// </summary>
    public class ShapefileMultiPointReader : ShapefileReader
    {

        /// <inheritdoc/>
        public ShapefileMultiPointReader(Stream shpStream, Stream dbfStream, Encoding encoding = null)
            : base(shpStream, dbfStream, encoding)
        { }


        /// <inheritdoc/>
        public ShapefileMultiPointReader(string shpPath, Encoding encoding = null)
            : base(shpPath, encoding)
        { }


        internal override ShpReader CreateShpReader(Stream shpStream)
        {
            return new ShpMultiPointReader(shpStream);
        }
    }


}
