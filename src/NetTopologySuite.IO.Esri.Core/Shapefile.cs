using NetTopologySuite.IO.Dbf;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace NetTopologySuite.IO.Shapefile
{


    /// <summary>
    /// Base shapefile class.
    /// </summary>
    public abstract class Shapefile : ManagedDisposable
    {
        internal const int FileCode = 9994; // 0x0000270A; 
        internal const int Version = 1000;

        internal static readonly int FileHeaderSize = 100;
        internal static readonly int RecordHeaderSize = 2 * sizeof(int);


        /// <summary>
        /// Minimal Measure value considered as not "no-data".
        /// </summary>
        /// <remarks>
        /// Any floating point number smaller than –10E38 is considered by a shapefile reader
        /// to represent a "no data" value. This rule is used only for measures (M values).
        /// <br />
        /// http://www.esri.com/library/whitepapers/pdfs/shapefile.pdf (page 2, bottom)
        /// </remarks>
        internal static readonly double MeasureMinValue = -10e38;

        /// <summary>
        /// Shape type.
        /// </summary>
        public abstract ShapeType ShapeType { get; }

        /// <summary>
        /// Shapefile attribute definitions with current attribute values.
        /// </summary>
        public abstract DbfFieldCollection Fields { get; }

    }

}
