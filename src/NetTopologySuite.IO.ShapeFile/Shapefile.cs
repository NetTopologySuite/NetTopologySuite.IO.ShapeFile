using System;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Handlers;
using NetTopologySuite.IO.Streams;

namespace NetTopologySuite.IO
{
    /// <summary>
    ///     This class is used to read and write ESRI Shapefiles.
    /// </summary>
    public partial class Shapefile
    {
        internal const int ShapefileId = 9994;
        internal const int Version = 1000;

        /// <summary>
        ///     Given a geomtery object, returns the equivalent shape file type.
        /// </summary>
        /// <param name="geom">A Geometry object.</param>
        /// <returns>The equivalent for the geometry object.</returns>
        public static ShapeGeometryType GetShapeType(Geometry geom)
        {
            var refGeom = TryGetNonEmptyGeometry(geom);
            if (refGeom == null)
            {
                // geom null/empty or a collection with ALL null/empty elements
                return ShapeGeometryType.NullShape;
            }

            var geomType = refGeom.OgcGeometryType;
            if (geomType == OgcGeometryType.Point &&
                geom.OgcGeometryType == OgcGeometryType.MultiPoint)
            {
                // NOTE: shapefile specification distinguishes between Point / MultiPoint,
                // but not between LineString / MultiLineString or Polygon / MultiPolygon
                geomType = OgcGeometryType.MultiPoint;
            }

            switch (geomType)
            {
                case OgcGeometryType.Point:
                    switch (((Point)refGeom).CoordinateSequence.Ordinates)
                    {
                        case Ordinates.XYM:
                            return ShapeGeometryType.PointM;
                        case Ordinates.XYZ:
                        case Ordinates.XYZM:
                            return ShapeGeometryType.PointZM;
                        default:
                            return ShapeGeometryType.Point;
                    }
                case OgcGeometryType.MultiPoint:
                    switch (((Point)refGeom.GetGeometryN(0)).CoordinateSequence.Ordinates)
                    {
                        case Ordinates.XYM:
                            return ShapeGeometryType.MultiPointM;
                        case Ordinates.XYZ:
                        case Ordinates.XYZM:
                            return ShapeGeometryType.MultiPointZM;
                        default:
                            return ShapeGeometryType.MultiPoint;
                    }
                case OgcGeometryType.LineString:
                case OgcGeometryType.MultiLineString:
                    switch (((LineString)refGeom.GetGeometryN(0)).CoordinateSequence.Ordinates)
                    {
                        case Ordinates.XYM:
                            return ShapeGeometryType.LineStringM;
                        case Ordinates.XYZ:
                        case Ordinates.XYZM:
                            return ShapeGeometryType.LineStringZM;
                        default:
                            return ShapeGeometryType.LineString;
                    }
                case OgcGeometryType.Polygon:
                case OgcGeometryType.MultiPolygon:
                    switch (((Polygon)refGeom.GetGeometryN(0)).Shell.CoordinateSequence.Ordinates)
                    {
                        case Ordinates.XYM:
                            return ShapeGeometryType.PolygonM;
                        case Ordinates.XYZ:
                        case Ordinates.XYZM:
                            return ShapeGeometryType.PolygonZM;
                        default:
                            return ShapeGeometryType.Polygon;
                    }
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Try to retrieve a not-empty geometry to use asa reference
        /// to use when evaluating the <see cref="ShapeGeometryType"/>.
        /// </summary>
        /// <param name="geom">The original geometry.</param>
        /// <returns>
        /// The <paramref name="geom"/> itself if not-empty AND not a collection,
        /// or the first not-empty child if <paramref name="geom"/> is
        /// a collection,or <c>null</c>.
        /// </returns>        
        private static Geometry TryGetNonEmptyGeometry(Geometry geom)
        {
            if (geom == null || geom.IsEmpty)
                return null;

            for (int i = 0; i < geom.NumGeometries; i++)
            {
                var testGeom = geom.GetGeometryN(i);
                if (testGeom != null && !testGeom.IsEmpty)
                    return testGeom;
            }
            return null;
        }

        /// <summary>
        ///     Returns the appropriate class to convert a shaperecord to an OGIS geometry given the type of shape.
        /// </summary>
        /// <param name="type">The shapefile type.</param>
        /// <returns>An instance of the appropriate handler to convert the shape record to a Geometry object.</returns>
        public static ShapeHandler GetShapeHandler(ShapeGeometryType type)
        {
            switch (type)
            {
                case ShapeGeometryType.Point:
                case ShapeGeometryType.PointM:
                case ShapeGeometryType.PointZM:
                    return new PointHandler(type);

                case ShapeGeometryType.Polygon:
                case ShapeGeometryType.PolygonM:
                case ShapeGeometryType.PolygonZM:
                    return new PolygonHandler(type);

                case ShapeGeometryType.LineString:
                case ShapeGeometryType.LineStringM:
                case ShapeGeometryType.LineStringZM:
                    return new MultiLineHandler(type);

                case ShapeGeometryType.MultiPoint:
                case ShapeGeometryType.MultiPointM:
                case ShapeGeometryType.MultiPointZM:
                    return new MultiPointHandler(type);

                case ShapeGeometryType.NullShape:
                    return new NullShapeHandler();

                default:
                    return null;
            }
        }

        /// <summary>
        ///     Returns an ShapefileDataReader representing the data in a shapefile.
        /// </summary>
        /// <param name="filename">The filename (minus the . and extension) to read.</param>
        /// <param name="geometryFactory">The geometry factory to use when creating the objects.</param>
        /// <returns>An ShapefileDataReader representing the data in the shape file.</returns>
        public static ShapefileDataReader CreateDataReader(string filename, GeometryFactory geometryFactory)
        {
            if (filename == null)
                throw new ArgumentNullException("filename");

            return
                CreateDataReader(
                    new ShapefileStreamProviderRegistry(new FileStreamProvider(StreamTypes.Shape, filename + ".shp", true),
                        new FileStreamProvider(StreamTypes.Data, filename + ".dbf", true), true, true), geometryFactory);
        }

        public static ShapefileDataReader CreateDataReader(IStreamProviderRegistry streamProviderRegistry,
            GeometryFactory geometryFactory)
        {
            if (streamProviderRegistry == null)
                throw new ArgumentNullException("streamProviderRegistry");
            if (geometryFactory == null)
                throw new ArgumentNullException("geometryFactory");
            var shpDataReader = new ShapefileDataReader(streamProviderRegistry, geometryFactory);
            return shpDataReader;
        }
    }
}
