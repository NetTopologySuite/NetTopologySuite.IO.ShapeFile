using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NetTopologySuite.IO.Shapefile.Core
{

    /// <summary>
    /// Shape coordiantes collection.
    /// </summary>
    public class ShpShapeBuilder : IReadOnlyList<IReadOnlyList<ShpCoordinates>>
    {
        internal ShpCoordinates[] Points;
        private int[] Offsets;
        internal readonly ShpBoundingBox Extent = new ShpBoundingBox();


        /// <summary>
        /// Creates ne instance of ShpPoints.
        /// </summary>
        public ShpShapeBuilder()
        {
            Reset();
        }


        /// <summary>
        /// Point count.
        /// </summary>
        public int PointCount { get; private set; }


        /// <summary>
        /// Part count.
        /// </summary>
        public int PartCount { get; private set; }


        /// <summary>
        /// Gets the point at specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the point to get.</param>
        /// <returns>Editable reference to the poinat at the specified index.</returns>
        public ShpCoordinates this[int index]
        {
            get
            {
                if (index < 0 || index >= PointCount)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return Points[index];
            }
            // set { }  // This would involve recalculating Extent at every single point setting.
        }

        internal bool FirstPointIsNull => PointCount < 1; // || Points[0].IsNull;


        /// <summary>
        /// Clear all parts and points. 
        /// </summary>
        /// <remarks>This method does not resize the internal buffer capacity.</remarks>
        public void Clear()
        {
            PointCount = 0;
            PartCount = 0;
            //Offsets[0] = 0; // First offset must always be present and must always be zero.
            Extent.Clear();

            // Assure that for ShpPointReader (NullShape case)
            Points[0].X = double.NaN;
            Points[0].Y = double.NaN;
            Points[0].Z = double.NaN;
            Points[0].M = double.NaN;
        }

        /// <summary>
        /// Clear all data from underlying buffer and resize internal buffer capacity to its initial state.
        /// </summary>
        public void Reset()
        {
            Points = new ShpCoordinates[1];
            Offsets = new int[1];

            Clear(); // Reset new instances created above
        }


        /// <summary>
        /// Start new PolyLine or Polygon part.
        /// </summary>
        public void StartNewPart()
        {
            if (PointCount < 1)
            {
                AddPartOffset(0);
                return;
            }

            if (PartCount < 1) // && PointCount > 0
            {
                AddPartOffset(0);  // First offset must always be zero.

            }

            // At this moment we always have a least one part.
            if (Offsets[PartCount - 1] == PointCount)
            {
                return; // Do not duplicate the same part offset
            }


            AddPartOffset(PointCount);
        }


        internal void AddPartOffset(int offset)
        {
            if (Offsets.Length <= PartCount)
            {
                ArrayBuffer.Expand(ref Offsets);
            }

            Offsets[PartCount] = offset;
            Debug.Assert(PartCount < 1 || Offsets[PartCount - 1] < Offsets[PartCount],
                GetType().Name + "." + nameof(AddPartOffset) + "()",
                "Corrupted SHP file - iInvalid part offset sequence. Preceding part offset must be less than succeeding part offset.");

            PartCount++;
        }

        /// <summary>
        /// Gets index to first point in part.
        /// </summary>
        /// <param name="partIndex">Part index.</param>
        /// <returns>Index to first point in part.</returns>
        public int GetPartOffset(int partIndex)
        {
            if (partIndex < 0 || partIndex >= PartCount)
                throw new IndexOutOfRangeException(nameof(partIndex));

            return Offsets[partIndex];
        }

        /// <summary>
        /// Adds a point.
        /// </summary>
        /// <param name="point">The point to be added.</param>
        public void AddPoint(ShpCoordinates point)
        {
            AddPoint(point.X, point.Y, point.Z, point.M);
        }


        /// <summary>
        /// Adds a point.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="z">Z coordinate.</param>
        /// <param name="m">Measure value.</param>
        public void AddPoint(double x, double y, double z, double m)
        {
            if (Points.Length <= PointCount)
            {
                ArrayBuffer.Expand(ref Points);
            }

            Points[PointCount].X = x;
            Points[PointCount].Y = y;
            Points[PointCount].Z = z;
            Points[PointCount].M = m;
            PointCount++;

            Extent.X.Expand(x);
            Extent.Y.Expand(y);
            Extent.Z.Expand(z);
            if (m >= Shapefile.MeasureMinValue) // !
            {
                Extent.M.Expand(m);
            }
        }


        /// <summary>
        /// Adds a point.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="m">Measure value.</param>
        public void AddPoint(double x, double y, double m)
        {
            AddPoint(x, y, double.NaN, m);
        }


        /// <summary>
        /// Adds a point.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        public void AddPoint(double x, double y)
        {
            AddPoint(x, y, double.NaN, double.NaN);
        }


        /// <summary>
        /// Gets point count held by specific part.
        /// </summary>
        /// <param name="partIndex">Part index.</param>
        /// <returns>Point count held by specific part.</returns>
        public int GetPointCount(int partIndex)
        {
            if (partIndex < 0)
                throw new IndexOutOfRangeException(nameof(partIndex));

            if (partIndex < PartCount - 1)
                return Offsets[partIndex + 1] - Offsets[partIndex];

            if (partIndex == 0 && PartCount < 1)
                return PointCount;

            if (partIndex == PartCount - 1)
                return PointCount - Offsets[partIndex];

            throw new IndexOutOfRangeException(nameof(partIndex));
        }


        /// <summary>
        /// Get all points divided into one or more PolyLine or Polygon parts.  
        /// </summary>
        /// <returns>All points divided into parts.</returns>
        /// <remarks>A part is a connected sequence of two or more points.</remarks>
        public IReadOnlyList<IReadOnlyList<ShpCoordinates>> GetParts()
        {
            var parts = new ShpCoordinates[Math.Max(PartCount, 1)][];
            for (int partIndex = 0; partIndex < parts.Length; partIndex++)
            {
                parts[partIndex] = GetPart(partIndex);
            }
            return parts;
        }

        private ShpCoordinates[] GetPart(int partIndex)
        {
            var pointCount = GetPointCount(partIndex);
            var partPoints = new ShpCoordinates[pointCount];
            Array.Copy(Points, Offsets[partIndex], partPoints, 0, partPoints.Length);

            return partPoints;
        }

        /// <summary>
        /// Determines whether the specified ShpPointCollection is equal to the current collection.
        /// </summary>
        /// <param name="other">The collection to compare with the current object.</param>
        /// <returns> true if the specified collection is equal to the current collection; otherwise, false.</returns>
        public bool Equals(ShpShapeBuilder other)
        {
            if (PointCount != other.PointCount || PartCount != other.PartCount)
                return false;

            // Start from Parts. There will be usually less to check.
            for (int partIndex = 0; partIndex < PartCount; partIndex++)
            {
                if (Offsets[partIndex] != other.Offsets[partIndex])
                    return false;
            }

            for (int i = 0; i < PointCount; i++)
            {
                if (!Points[i].Equals(other.Points[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Creates copy of this point collection.
        /// </summary>
        /// <returns>ShpPoint collection.</returns>
        public ShpShapeBuilder Copy()
        {
            var copy = new ShpShapeBuilder();

            copy.Points = new ShpCoordinates[Math.Max(1, PointCount)];
            copy.PointCount = PointCount;
            Array.Copy(Points, copy.Points, PointCount);

            copy.Offsets = new int[Math.Max(1, PartCount)];
            copy.PartCount = PartCount;
            Array.Copy(Offsets, copy.Offsets, PartCount);

            copy.Extent.Expand(Extent);
            return copy;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (PartCount > 0)
                return $"MultiPart [{PartCount}:{PointCount}]";

            if (PointCount < 1)
                return ShapeType.NullShape.ToString();

            if (PointCount == 1)
                return $"Point [{Points[0]}]";

            return $"MultiPoint [{PointCount}]";

        }


        #region *** IReadOnlyCollection ***

        int IReadOnlyCollection<IReadOnlyList<ShpCoordinates>>.Count => PartCount;
        IReadOnlyList<ShpCoordinates> IReadOnlyList<IReadOnlyList<ShpCoordinates>>.this[int partIndex] => GetPart(partIndex);

        IEnumerator<IReadOnlyList<ShpCoordinates>> IEnumerable<IReadOnlyList<ShpCoordinates>>.GetEnumerator()
        {
            return new ShpPartEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ShpPartEnumerator(this);
        }

        private class ShpPartEnumerator : IEnumerator<IReadOnlyList<ShpCoordinates>>
        {
            private readonly ShpShapeBuilder Owner;
            private int CurrentIndex;

            public IReadOnlyList<ShpCoordinates> Current { get; private set; }
            object IEnumerator.Current => Current;

            public ShpPartEnumerator(ShpShapeBuilder owner)
            {
                Owner = owner ?? throw new ArgumentNullException(nameof(owner));
                Reset();
            }

            public void Reset()
            {
                CurrentIndex = 0;
            }

            public bool MoveNext()
            {
                if (Owner.PartCount < 1 || CurrentIndex >= Owner.PartCount)
                    return false;

                Current = Owner.GetPart(CurrentIndex++);
                return true;
            }

            public void Dispose()
            {
                // Nothing to dispose
            }
        }

        #endregion
    }

}
