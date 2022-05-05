using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Dbf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetTopologySuite.IO.Shapefile
{
    /// <summary>
    /// Feature helper methods.
    /// </summary>
    public static class FeatureExtensions
    {
        /// <summary>
        /// Writes features to the shapefile.
        /// </summary>
        /// <param name="features">Features to be written.</param>
        /// <param name="shpPath">Path to shapefile.</param>
        /// <param name="encoding">DBF file encoding. If null encoding will be guess from related .CPG file or from reserved DBF bytes.</param>
        /// <param name="projection">Projection metadata for the shapefile (.prj file).</param>
        public static void SaveToShapefile(this IEnumerable<IFeature> features, string shpPath, Encoding encoding = null, string projection = null)
        {
            if (features == null)
                throw new ArgumentNullException(nameof(features));

            var firstFeature = features.FirstOrDefault();
            if (firstFeature == null)
                throw new ArgumentException(nameof(ShapefileWriter) + " requires at least one feature to be written.");

            var fields = firstFeature.Attributes.GetDbfFields();
            var shapeType = features.FindNonEmptyGeometry().GetShapeType();

            using (var shpWriter = ShapefileWriter.Open(shpPath, shapeType, fields, encoding, projection))
            {
                foreach (var feature in features)
                {
                    shpWriter.Write(feature);
                }
            }
        }


        internal static Feature ToFeature(this Geometry geometry, AttributesTable attributes)
        {
            var feature = new Feature(geometry, attributes);
            feature.BoundingBox = geometry.EnvelopeInternal;
            return feature;
        }


        internal static AttributesTable GetAttributesTable(this DbfFieldCollection fields)
        {
            return new AttributesTable(fields.GetValues());
        }


        internal static DbfField[] GetDbfFields(this IAttributesTable attributes)
        {
            var names = attributes.GetNames();
            var fields = new DbfField[names.Length];

            for (int i = 0; i < names.Length; i++)
            {
                var name = names[i];
                var type = attributes.GetType(name);
                fields[i] = DbfField.Create(name, type);
            }
            return fields;
        }

        /// <summary>
        /// Gets default <see cref="ShapeType"/> for specified geometry.
        /// </summary>
        /// <param name="geometry">A Geometry object.</param>
        /// <returns>Shape type.</returns>
        public static ShapeType GetShapeType(this Geometry geometry)
        {
            geometry = FindNonEmptyGeometry(geometry);

            if (geometry == null || geometry.IsEmpty)
                return ShapeType.NullShape;

            var ordinates = geometry.GetOrdinates();

            if (geometry is Point)
                return GetPointType(ordinates);

            if (geometry is MultiPoint)
                return GetMultiPointType(ordinates);

            if (geometry is LineString || geometry is MultiLineString)
                return GetPolyLineType(ordinates);

            if (geometry is Polygon || geometry is MultiPolygon)
                return GetPolygonType(ordinates);

            throw new ArgumentException("Unsupported shapefile geometry: " + geometry.GetType().Name);
        }


        private static Geometry FindNonEmptyGeometry(Geometry geometry)
        {
            if (geometry == null || geometry.IsEmpty)
                return null;

            var geomColl = geometry as GeometryCollection;

            // Shapefile specification distinguish between Point and MultiPoint.
            // That not the case for PolyLine and Polygon.
            if (geometry is MultiPoint || geomColl == null)
            {
                return geometry;
            }

            for (int i = 0; i < geomColl.Count; i++)
            {
                var geom = geomColl[i];

                // GeometryCollection -> MultiPolygon -> Polygon
                if (geom is GeometryCollection)
                    geom = FindNonEmptyGeometry(geom);

                if (geom != null && !geom.IsEmpty)
                    return geom;
            }
            return null;
        }

        internal static Geometry FindNonEmptyGeometry(this IEnumerable<IFeature> features)
        {
            if (features == null)
                return null;

            foreach (var feature in features)
            {
                var geometry = FindNonEmptyGeometry(feature.Geometry);
                if (geometry != null)
                    return geometry;
            }

            return null;
        }


        private static Ordinates GetOrdinates(this Geometry geometry)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));

            if (geometry is Point point)
                return point.CoordinateSequence.Ordinates;

            if (geometry is LineString line)
                return line.CoordinateSequence.Ordinates;

            if (geometry is Polygon polygon)
                return polygon.Shell.CoordinateSequence.Ordinates;

            if (geometry.NumGeometries > 0)
                return GetOrdinates(geometry.GetGeometryN(0));

            throw new ArgumentException("Unsupported shapefile geometry: " + geometry.GetType().Name);
        }


        private static ShapeType GetPointType(Ordinates ordinates)
        {
            if (ordinates == Ordinates.XYM)
                return ShapeType.PointM;

            if (ordinates == Ordinates.XYZ || ordinates == Ordinates.XYZM)
                return ShapeType.PointZM;

            return ShapeType.Point;
        }


        private static ShapeType GetMultiPointType(Ordinates ordinates)
        {
            if (ordinates == Ordinates.XYM)
                return ShapeType.MultiPointM;

            if (ordinates == Ordinates.XYZ || ordinates == Ordinates.XYZM)
                return ShapeType.MultiPointZM;

            return ShapeType.MultiPoint;
        }


        private static ShapeType GetPolyLineType(Ordinates ordinates)
        {
            if (ordinates == Ordinates.XYM)
                return ShapeType.PolyLineM;

            if (ordinates == Ordinates.XYZ || ordinates == Ordinates.XYZM)
                return ShapeType.PolyLineZM;

            return ShapeType.PolyLine;
        }


        private static ShapeType GetPolygonType(Ordinates ordinates)
        {
            if (ordinates == Ordinates.XYM)
                return ShapeType.PolygonM;

            if (ordinates == Ordinates.XYZ || ordinates == Ordinates.XYZM)
                return ShapeType.PolygonZM;

            return ShapeType.Polygon;
        }
    }
}
