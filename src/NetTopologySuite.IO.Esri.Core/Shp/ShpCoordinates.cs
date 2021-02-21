using System;
using System.Collections.Generic;
using System.Text;

namespace NetTopologySuite.IO.Shapefile.Core
{



    /// <summary>
    /// Point represented as X, Y, Z, M coordinates.
    /// </summary>
    public struct ShpCoordinates
    {
        /// <summary>
        /// Represents the smallest positive coordinate value that is greater than zero.
        /// </summary>
        /// <remarks>
        /// This field is equal to  0.000000002777...7 in linear units and 0°00'00.00001" in  decimal degrees (equivalent of ~0.3mm in metric units).
        /// </remarks>
        public static double Epsilon = 0.00001 / 60.0 / 60.0;

        /// <summary>
        /// Point representation of <see cref="ShapeType.NullShape"/>.
        /// </summary>
        public static ShpCoordinates NullPoint = new ShpCoordinates(double.NaN, double.NaN);

        /// <summary>
        /// X coordinate
        /// </summary>
        public double X;


        /// <summary>
        /// Y coordinate
        /// </summary>
        public double Y;


        /// <summary>
        ///  Z coordinate
        /// </summary>
        public double Z;


        /// <summary>
        ///  Measure
        /// </summary>
        public double M;


        /// <summary>
        ///  Initializes a new instance of the <see cref="ShpCoordinates"/> class.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="z">Z coordinate</param>
        /// <param name="m">Measure</param>
        public ShpCoordinates(double x, double y, double z, double m)
        {
            X = x;
            Y = y;
            Z = z;

            if (m < Shapefile.MeasureMinValue)
                M = double.NaN;
            else
                M = m;
        }


        /// <summary>
        ///  Initializes a new instance of the <see cref="ShpCoordinates"/> class.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="m">Measure</param>
        public ShpCoordinates(double x, double y, double m)
        {
            X = x;
            Y = y;
            Z = double.NaN;
            M = m;
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="ShpCoordinates"/> class.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        public ShpCoordinates(double x, double y)
        {
            X = x;
            Y = y;
            Z = double.NaN;
            M = double.NaN;
        }   



        /// <summary>
        /// Indicates whether this <see cref="ShpCoordinates"/> and a specified object are equal.
        /// </summary>
        /// <param name="other">The object to compare with the current <see cref="ShpCoordinates"/>.</param>
        /// <returns>true if obj is <see cref="ShpCoordinates"/> and have the same coordinates; otherwise, false.</returns>
        public override bool Equals(object other)
        {
            return other is ShpCoordinates point && Equals(point);
        }

        /// <summary>
        /// Indicates whether this <see cref="ShpCoordinates"/> and a specified point are equal.
        /// </summary>
        /// <param name="other">The point to compare with the current <see cref="ShpCoordinates"/>.</param>
        /// <returns>true if both points have the same coordinates; otherwise, false.</returns>
        public bool Equals(ShpCoordinates other)
        {
            return Equals(other.X, other.Y, other.Z, other.M);
        }

        /// <summary>
        /// Indicates whether this <see cref="ShpCoordinates"/> and a specified coordinates are equal.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="z">Z coordinate.</param>
        /// <param name="m">Measure value.</param>
        /// <returns>true if specified coordinates are equal to this point coordinates; otherwise, false.</returns>
        public bool Equals(double x, double y, double z, double m)
        {
            return Equals(X, x) && Equals(Y, y) && Equals(Z, z) && Equals(M, m);
        }

        private bool Equals(double v1, double v2)
        {
            if (v1 == v2)
            {
                return true;
            }

            // ESRI Shapefile Technical Description, page 9:
            // The rings are closed (the first and last vertex of a ring MUST be the same).

            //if (Math.Abs(v1- v2) < Epsilon)
            //{
            //    return true;
            //}

            return double.IsNaN(v1) && double.IsNaN(v2);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return X.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (IsNull)
                return ShapeType.NullShape.ToString();

            var fmt = (X < 1000 & Y < 1000) ? "" : "0.000"; // for latitudes and longitudes use full precision
            var sb = new StringBuilder();
            sb.Append("X:" + X.ToString(fmt));
            sb.Append(", Y:");
            sb.Append(Y.ToString(fmt));
            if (HasZ)
            {
                sb.Append(", Z:");
                sb.Append(Z.ToString(fmt));
            }
            if (HasM)
            {
                sb.Append(", M:");
                sb.Append(M.ToString(fmt));
            }

            return sb.ToString();
        }


        /// <summary>
        /// Indicates the point with no geometric data. 
        /// </summary>
        /// <remarks>
        /// According to ESRI Shapefile Technical Description point feature
        /// supports null - it is valid to have points and null points in the same shapefile.
        /// </remarks>
        public bool IsNull => double.IsNaN(X) || double.IsNaN(Y) || X == double.MinValue || Y == double.MinValue; // ArcMap writes empty geometry as double.MinValue coordinates.


        /// <summary>
        /// Indicates wheter this inttance of <see cref="ShpCoordinates"/> contains Z coordinate.
        /// </summary>
        public bool HasZ => !double.IsNaN(Z);


        /// <summary>
        /// Indicates wheter this inttance of <see cref="ShpCoordinates"/> contains M coordinate.
        /// </summary>
        public bool HasM => !double.IsNaN(M);
    }






}
