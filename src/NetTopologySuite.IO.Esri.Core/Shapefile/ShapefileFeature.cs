using System;
using System.Collections.Generic;
using System.Text;

namespace NetTopologySuite.IO.Shapefile.Core
{

    /// <summary>
    /// Geometry and attribute information for the spatial feature in shapefile.
    /// </summary>
    public class ShapefileFeature
    {
        /// <summary>
        /// Feature shape points.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<ShpCoordinates>> Shape { get; }

        /// <summary>
        /// Feature attributes.
        /// </summary>
        public IReadOnlyDictionary<string, object> Attributes { get; }


        /// <summary>
        /// Initializes new ShapefileFeature struct.
        /// </summary>
        /// <param name="shape">Feature shape points.</param>
        /// <param name="attributes">Feature attributes.</param>
        public ShapefileFeature(IReadOnlyList<IReadOnlyList<ShpCoordinates>> shape, IReadOnlyDictionary<string, object> attributes)
        {
            Shape = shape;
            Attributes = attributes;
        }
    }




    /// <summary>
    /// Geometry and attribute information for the point feature in shapefile.
    /// </summary>
    public class ShapefilePointFeature
    {
        /// <summary>
        /// Feature point.
        /// </summary>
        public ShpCoordinates Shape { get; }

        /// <summary>
        /// Feature attributes.
        /// </summary>
        public IReadOnlyDictionary<string, object> Attributes { get; }


        /// <summary>
        /// Initializes new ShapefileFeature struct.
        /// </summary>
        /// <param name="shape">Feature shape points.</param>
        /// <param name="attributes">Feature attributes.</param>
        public ShapefilePointFeature(ShpCoordinates shape, IReadOnlyDictionary<string, object> attributes)
        {
            Shape = shape;
            Attributes = attributes;
        }
    }
}
