using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetTopologySuite.IO
{
    internal static class Utils
    {
        public static Feature[] ToFeatures(this GeometryCollection geometries)
        {
            var attributes = new AttributesTable();
            attributes.Add("Id", 1);

            var features = new Feature[geometries.Count];
            for (int i = 0; i < geometries.Count; i++)
            {
                features[i] = new Feature(geometries[i], attributes);
            }
            return features;
        }

        public static GeometryCollection ToGeometryCollection(this IEnumerable<Geometry> geometries)
        {
            return new GeometryCollection(geometries.ToArray());
        }
    }
}
