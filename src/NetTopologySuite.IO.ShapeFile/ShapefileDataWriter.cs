using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Streams;

namespace NetTopologySuite.IO
{
    /// <summary>
    /// A simple test class for write a complete (shp, shx and dbf) shapefile structure.
    /// </summary>
    public class ShapefileDataWriter
    {
        #region Static

        /// <summary>
        /// Gets the stub header.
        /// </summary>
        /// <param name="feature">The feature.</param>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        public static DbaseFileHeader GetHeader(IFeature feature, int count)
        {
            return GetHeader(feature, count, null);
        }

        /// <summary>
        /// Gets the stub header.
        /// </summary>
        /// <param name="feature">The feature.</param>
        /// <param name="count">The count.</param>
        /// <param name="encoding">The encoding.</param>            
        /// <returns></returns>
        public static DbaseFileHeader GetHeader(IFeature feature, int count, Encoding encoding)
        {
            var attribs = feature.Attributes;
            string[] names = attribs.GetNames();
            var header = new DbaseFileHeader(encoding);
            header.NumRecords = count;
            foreach (string name in names)
            {
                var type = attribs.GetType(name);
                if (type == typeof(double) || type == typeof(float))
                    header.AddColumn(name, 'N', DoubleLength, DoubleDecimals);
                else if (type == typeof(short) || type == typeof(ushort) ||
                         type == typeof(int) || type == typeof(uint))
                    header.AddColumn(name, 'N', IntLength, IntDecimals);
                else if (type == typeof(long) || type == typeof(ulong))
                    header.AddColumn(name, 'N', LongLength, IntDecimals);
                else if (type == typeof(string))
                    header.AddColumn(name, 'C', StringLength, StringDecimals);
                else if (type == typeof(bool))
                    header.AddColumn(name, 'L', BoolLength, BoolDecimals);
                else if (type == typeof(DateTime))
                    header.AddColumn(name, 'D', DateLength, DateDecimals);
                else throw new ArgumentException("Type " + type.Name + " not supported");
            }
            return header;
        }

        /// <summary>
        /// Gets the header from a dbf file.
        /// </summary>
        /// <param name="dbfFile">The DBF file.</param>
        /// <returns></returns>
        public static DbaseFileHeader GetHeader(string dbfFile)
        {
            return GetHeader(new ShapefileStreamProviderRegistry(dbfFile, false, true, false));
        }

        public static DbaseFileHeader GetHeader(IStreamProviderRegistry streamProviderRegistry)
        {

            var header = new DbaseFileHeader();

            using (var stream = streamProviderRegistry[StreamTypes.Data].OpenRead())
            using (var reader = new BinaryReader(stream))
                header.ReadHeader(reader, streamProviderRegistry[StreamTypes.Data] is FileStreamProvider ? ((FileStreamProvider)streamProviderRegistry[StreamTypes.Data]).Path : null);
            return header;
        }

        public static DbaseFileHeader GetHeader(DbaseFieldDescriptor[] dbFields, int count)
        {
            var header = new DbaseFileHeader();
            header.NumRecords = count;

            foreach (var dbField in dbFields)
                header.AddColumn(dbField.Name, dbField.DbaseType, dbField.Length, dbField.DecimalCount);

            return header;
        }

        #endregion

        private const int DoubleLength = 18;
        private const int DoubleDecimals = 8;
        private const int IntLength = 10;
        private const int LongLength = 18;
        private const int IntDecimals = 0;
        private const int StringLength = 254;
        private const int StringDecimals = 0;
        private const int BoolLength = 1;
        private const int BoolDecimals = 0;
        private const int DateLength = 8;
        private const int DateDecimals = 0;

        //private readonly string _shpFile = String.Empty;
        //private readonly string _dbfFile = String.Empty;

        private IStreamProviderRegistry _streamProviderRegistry;

        private readonly DbaseFileWriter _dbaseWriter;

        private DbaseFileHeader _header;

        /// <summary>
        /// Gets or sets the header of the shapefile.
        /// </summary>
        /// <value>The header.</value>
        public DbaseFileHeader Header
        {
            get { return _header; }
            set { _header = value; }
        }

        private GeometryFactory _geometryFactory;

        /// <summary>
        /// Gets or sets the geometry factory.
        /// </summary>
        /// <value>The geometry factory.</value>
        protected GeometryFactory GeometryFactory
        {
            get { return _geometryFactory; }
            set { _geometryFactory = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShapefileDataWriter"/> class.
        /// </summary>
        /// <param name="fileName">Name of the file with or without any extension.</param>
        public ShapefileDataWriter(string fileName) : this(fileName, Geometries.GeometryFactory.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShapefileDataWriter"/> class.
        /// </summary>
        /// <param name="fileName">File path without any extension</param>
        /// <param name="geometryFactory"></param>
        public ShapefileDataWriter(string fileName, GeometryFactory geometryFactory)
            : this(fileName, geometryFactory, DbaseEncodingUtility.GetEncodingForCodePageIdentifier(1252))
        {

        }

        public ShapefileDataWriter(string fileName, GeometryFactory geometryFactory, Encoding encoding)
            : this(new ShapefileStreamProviderRegistry(fileName, false, false, false), geometryFactory, encoding)
        {

        }

        public ShapefileDataWriter(IStreamProviderRegistry streamProviderRegistry, GeometryFactory geometryFactory, Encoding encoding)
        {
            _geometryFactory = geometryFactory;

            _streamProviderRegistry = streamProviderRegistry;

            // Writers
            _dbaseWriter = new DbaseFileWriter(streamProviderRegistry, encoding);
        }

        /// <summary>
        /// Writes the specified feature collection.
        /// </summary>
        /// <param name="featureCollection">The feature collection.</param>
        public void Write(IEnumerable<IFeature> featureCollection)
        {
            // Test if the Header is initialized
            if (Header is null)
                throw new ApplicationException("Header must be set first!");

            using (var featuresEnumerator = featureCollection.GetEnumerator())
            {
                // scan the original sequence looking for a geometry that we can
                // use to figure out the shape type.  keep the original features
                // around so we don't have to loop through the input twice; we
                // shouldn't have to look *too* far for a non-empty geometry.
                Geometry representativeGeometry = null;
                var headFeatures = new List<IFeature>();
                while (representativeGeometry?.IsEmpty != false && featuresEnumerator.MoveNext())
                {
                    var feature = featuresEnumerator.Current;
                    headFeatures.Add(feature);
                    representativeGeometry = feature.Geometry;
                }

                var shapeFileType = Shapefile.GetShapeType(representativeGeometry);
                using (_dbaseWriter)
                using (var shapefileWriter = new ShapefileWriter(_geometryFactory, _streamProviderRegistry, shapeFileType))
                {
                    _dbaseWriter.Write(Header);
                    string[] fieldNames = Array.ConvertAll(Header.Fields, field => field.Name);
                    object[] values = new object[fieldNames.Length];

                    // first, write the one(s) that we scanned already.
                    foreach (var feature in headFeatures)
                    {
                        Write(feature);
                    }

                    // now continue through the features we haven't scanned yet.
                    while (featuresEnumerator.MoveNext())
                    {
                        Write(featuresEnumerator.Current);
                    }

                    void Write(IFeature feature)
                    {
                        shapefileWriter.Write(feature.Geometry);

                        var attribs = feature.Attributes;
                        for (int i = 0; i < fieldNames.Length; i++)
                        {
                            values[i] = attribs[fieldNames[i]];
                        }

                        _dbaseWriter.Write(values);
                    }
                }
            }
        }
    }
}
