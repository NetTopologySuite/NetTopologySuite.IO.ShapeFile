using System;

namespace NetTopologySuite.IO.Shapefile.Core
{

    /// <summary>
    /// The Bounding Box in the main file header storing the actual extent of the shapes in the file.
    /// </summary>
    public class ShpBoundingBox
    {
        /// <summary>
        /// Minimum and maximum X coordinate values.
        /// </summary>
        public ShpRange X { get; }

        /// <summary>
        /// Minimum and maximum Y coordinate values.
        /// </summary>
        public ShpRange Y { get; }

        /// <summary>
        /// Minimum and maximum Z coordinate values.
        /// </summary>
        public ShpRange Z { get; }

        /// <summary>
        /// Minimum and maximum Measure values.
        /// </summary>
        public ShpRange M { get; }

        /// <summary>
        /// Initializes new <see cref="ShpBoundingBox"/> class instance.
        /// </summary>
        public ShpBoundingBox()
        {
            X = new ShpRange();
            Y = new ShpRange();
            Z = new ShpRange();
            M = new ShpRange();
        }

        /// <summary>
        /// Clears the bounding box.
        /// </summary>
        public void Clear()
        {
            X.Clear();
            Y.Clear();
            Z.Clear();
            M.Clear();
        }


        /// <summary>
        /// Expands the boundign box by other bounding box.
        /// </summary>
        /// <param name="other">Bounding box used to expand this instance.</param>
        public void Expand(ShpBoundingBox other)
        {
            X.Expand(other.X);
            Y.Expand(other.Y);
            Z.Expand(other.Z);
            M.Expand(other.M);
        }

        /// <summary>
        /// Expands the boundign box by point coordinates.
        /// </summary>
        /// <param name="point">Point coordinates.</param>
        public void Expand(ShpCoordinates point)
        {
            X.Expand(point.X);
            Y.Expand(point.Y);
            Z.Expand(point.Z);
            M.Expand(point.M);
        }
    }


    /// <summary>
    /// Minimum and maximum values of coordinate.
    /// </summary>
    public class ShpRange 
    {
        /// <summary>
        /// Initializes empty <see cref="ShpRange"/> class instance.
        /// </summary>
        public ShpRange()
        {
            Clear();
        }

        /// <summary>
        /// Mimimum value.
        /// </summary>
        public double Min { get; private set; }

        /// <summary>
        /// Maximum value.
        /// </summary>
        public double Max { get; private set; }

        /// <summary>
        /// Specifies if this <see cref="ShpRange"/> is emty.
        /// </summary>
        public bool IsEmpty => double.IsNaN(Min) || double.IsNaN(Max);

        /// <summary>
        /// Expands the range by specified value.
        /// </summary>
        /// <param name="value"></param>
        public void Expand(double value)
        {
            if (double.IsNaN(value) || value == double.MinValue) // ArcMap 10.6 saves empty point coordinates as doubule.MinValue
                return;

            if (double.IsNaN(Min)) // NaN > value => false;
            {
                Min = value;
            }
            else if (Min > value) // Min > NaN => false;   
            {
                Min = value; 
            }

            if (double.IsNaN(Max))  //   NaN < value => false;
            {
                Max = value;
            }
            else if (Max < value) // Max < NaN => false; 
            {
                Max = value;
            }
        }

        /// <summary>
        /// Expands the range by other range.
        /// </summary>
        /// <param name="other">Range used to expand this instance.</param>
        public void Expand(ShpRange other)
        {
            Expand(other.Min);
            Expand(other.Max);
        }

        /// <summary>
        /// Clears the range.
        /// </summary>
        public void Clear()
        {
            this.Min = double.NaN;
            this.Max = double.NaN;
        }
    }

}
