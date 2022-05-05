using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace NetTopologySuite.IO.Handlers
{
    /// <summary>
    /// Serves to probe linear rings
    /// </summary>
    /// <author>Bruno.Labrecque@mddep.gouv.qc.ca</author>
    internal class ProbeLinearRing : IComparer<Polygon>,  IComparer<LinearRing>
    {

        internal enum Order
        {
            Ascending,
            Descending
        }

        internal ProbeLinearRing()
            : this(Order.Descending)
        {
        }

        internal ProbeLinearRing(Order order)
        {
            switch (order)
            {
                case Order.Ascending:
                    _r1 = 1;
                    _r2 = -1;
                    break;
                case Order.Descending:
                    _r1 = -1;
                    _r2 = 1;
                    break;
            }
        }

        private readonly int _r1;

        private readonly int _r2;

        [System.Obsolete()]
        public int Compare(LinearRing x, LinearRing y)
        {
            var pm = PrecisionModel.MostPrecise(x.PrecisionModel, y.PrecisionModel);
            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(pm, x.SRID);

            // If we keep creating new polygons for each comparison
            // we can't cache values like Area or Length
            var p1 = geometryFactory.CreatePolygon(x, null);
            var p2 = geometryFactory.CreatePolygon(y, null); ;
            Compare(p1, p2);

            if (p1.Area < p2.Area)
                return _r1;
            return p1.Area > p2.Area ? _r2 : 0;
        }

        public int Compare(Polygon x, Polygon y)
        {
            if (x.Area < y.Area)
                return _r1;
            return x.Area > y.Area ? _r2 : 0;
        }
    }
}
