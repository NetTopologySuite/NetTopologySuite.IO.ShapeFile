using System;
using System.Collections.Generic;

using NetTopologySuite.DataStructures;

namespace NetTopologySuite.Geometries
{
    // ReSharper disable ImpureMethodCallOnReadonlyValueField

    /// <summary>
    /// Utility class for storing coordinates
    /// </summary>
    /// <remarks>
    /// This class may be useful for other IO classes as well
    /// </remarks>
    public class CoordinateBuffer : IEquatable<CoordinateBuffer>
    {
        #region NoDataChecker
        /// <summary>
        /// Utility to check <see cref="double"/> values for a defined null/no-data-value
        /// </summary>
        private struct DoubleNoDataChecker
        {
            private enum IsNoDataCheck
            {
                NaN,
                PosInf,
                NegInf,
                Inf,
                Equal,
                LessThan,
            }

            private readonly double _noDataCheckValue;
            private readonly double _noDataValue;
            private readonly IsNoDataCheck _isNoDataCheck;

            /// <summary>
            /// Initializes this structure with a <paramref name="noDataValue"/>
            /// </summary>
            /// <param name="noDataValue">The value that is to be treated as <c>null</c></param>
            /// <param name="lessThan">This optional parameter controls whether a value has to be less than <paramref name="noDataValue"/> to be considered <c>null</c></param>
            public DoubleNoDataChecker(double noDataValue, bool lessThan = false)
            {
                _noDataValue = _noDataCheckValue = noDataValue;
                if (double.IsNaN(noDataValue))
                    _isNoDataCheck = IsNoDataCheck.NaN;
                else if (double.IsPositiveInfinity(noDataValue))
                    _isNoDataCheck = IsNoDataCheck.PosInf;
                else if (double.IsNegativeInfinity(noDataValue))
                    _isNoDataCheck = IsNoDataCheck.NegInf;
                else if (double.IsInfinity(noDataValue))
                    _isNoDataCheck = IsNoDataCheck.Inf;
                else
                {
                    if (lessThan)
                    {
                        _isNoDataCheck = IsNoDataCheck.LessThan;
                        _noDataValue = _noDataCheckValue * 1.01d;
                    }
                    else
                    {
                        _isNoDataCheck = IsNoDataCheck.Equal;
                    }
                }
            }

            /// <summary>
            /// Checks if <paramref name="value"/> doesn't satisfy null-check
            /// </summary>
            /// <param name="value">The value to check</param>
            /// <returns><c>true</c> if <paramref name="value"/> is not equal to <see cref="_noDataCheckValue"/></returns>
            public bool IsNotNoDataValue(double value)
            {
                switch (_isNoDataCheck)
                {
                    case IsNoDataCheck.NaN:
                        return !double.IsNaN(value);
                    case IsNoDataCheck.PosInf:
                        return !double.IsPositiveInfinity(value);
                    case IsNoDataCheck.NegInf:
                        return !double.IsNegativeInfinity(value);
                    case IsNoDataCheck.Inf:
                        return !double.IsInfinity(value);
                    case IsNoDataCheck.LessThan:
                        return value>=_noDataCheckValue;
                    default:
                        return _noDataCheckValue != value;
                }
            }

            /// <summary>
            /// Checks if <paramref name="value"/> does satisfy null-check
            /// </summary>
            /// <param name="value">The value to check</param>
            /// <returns><c>true</c> if <paramref name="value"/> is equal to <see cref="_noDataCheckValue"/></returns>
            public bool IsNoDataValue(double value)
            {
                switch (_isNoDataCheck)
                {
                    case IsNoDataCheck.NaN:
                        return double.IsNaN(value);
                    case IsNoDataCheck.PosInf:
                        return double.IsPositiveInfinity(value);
                    case IsNoDataCheck.NegInf:
                        return double.IsNegativeInfinity(value);
                    case IsNoDataCheck.Inf:
                        return double.IsInfinity(value);
                    case IsNoDataCheck.LessThan:
                        return value < _noDataCheckValue;
                    default:
                        return _noDataCheckValue == value;
                }
            }

            /// <summary>
            /// Gets the defined <c>null</c> value
            /// </summary>
            public double NoDataValue => _noDataValue;

            public override string ToString()
            {
                switch (_isNoDataCheck)
                {
                    case IsNoDataCheck.Equal:
                    case IsNoDataCheck.LessThan:
                        return string.Format("IsNullCheck: {0} {1}", _isNoDataCheck, _noDataCheckValue);
                    default:
                        return string.Format("IsNullCheck: {0}", _isNoDataCheck);
                }
            }

        }
        #endregion

        private static readonly object FactoryLock = new object();
        private static volatile CoordinateSequenceFactory _factory;

        private readonly Envelope _extents = new Envelope();
        private Interval _zInterval = Interval.Create();
        private Interval _mInterval = Interval.Create();

        private Ordinates _definedOrdinates = Ordinates.XY;
        private readonly DoubleNoDataChecker _doubleNoDataChecker;

        private readonly List<XYZM> _coordinates;
        private readonly List<int> _markers = new List<int>();

        /// <summary>
        /// Creates an instance of this class
        /// </summary>
        public CoordinateBuffer()
        {
            _coordinates = new List<XYZM>();
            _doubleNoDataChecker = new DoubleNoDataChecker(Coordinate.NullOrdinate);
        }

        /// <summary>
        /// Creates an instance of this class with <paramref name="nullValue"/> defining the values that should be treated as null.
        /// </summary>
        /// <param name="nullValue">The value that should be treated as null.</param>
        /// <param name="lessThan">This optional parameter controls whether a value has to be less than <paramref name="nullValue"/> to be considered <c>null</c></param>
        public CoordinateBuffer(double nullValue, bool lessThan = false)
        {
            _coordinates = new List<XYZM>();
            _doubleNoDataChecker = new DoubleNoDataChecker(nullValue, lessThan);
        }

        /// <summary>
        /// Creates an instance of this class with an initial <paramref name="capacity"/>
        /// </summary>
        /// <param name="capacity">The initial capacity of the buffer.</param>
        public CoordinateBuffer(int capacity)
        {
            _coordinates = new List<XYZM>(capacity);
            _doubleNoDataChecker = new DoubleNoDataChecker(double.NaN);
        }

        /// <summary>
        /// Creates an instance of this class with an initial <paramref name="capacity"/>
        /// </summary>
        /// <param name="capacity">The initial capacity of the buffer.</param>
        /// <param name="nullValue">The value that should be treated as null.</param>
        /// <param name="lessThan">This optional parameter controls whether a value has to be less than <paramref name="nullValue"/> to be considered <c>null</c></param>
        public CoordinateBuffer(int capacity, double nullValue, bool lessThan = false)
        {
            _coordinates = new List<XYZM>(capacity);
            _doubleNoDataChecker = new DoubleNoDataChecker(nullValue, lessThan);
        }

        /// <summary>
        /// Updates the <see cref="_definedOrdinates"/> flags
        /// </summary>
        /// <param name="z">The z-Ordinate</param>
        /// <param name="m">The m-Ordinate</param>
        private void CheckDefinedOrdinates(ref double z, ref double m)
        {
            if (_doubleNoDataChecker.IsNotNoDataValue(z))
                _definedOrdinates |= Ordinates.Z;

            else
                z = Coordinate.NullOrdinate;

            if (_doubleNoDataChecker.IsNotNoDataValue(m))
                _definedOrdinates |= Ordinates.M;
            else
                m = Coordinate.NullOrdinate;
        }

        /// <summary>
        /// Gets or sets the <see cref="CoordinateSequenceFactory"/> used to create a coordinate sequence from the coordinate data in the buffer.
        /// </summary>
        public CoordinateSequenceFactory Factory
        {
            get
            {
                if (_factory != null)
                    return _factory;

                lock (FactoryLock)
                {
                    if (_factory == null)
                        _factory = NtsGeometryServices.Instance.DefaultCoordinateSequenceFactory;
                }

                return _factory;
            }

            set
            {
                lock (FactoryLock)
                {
                    _factory = value;
                }
            }
        }

        /// <summary>
        /// Gets the number of coordinates added to the buffer
        /// </summary>
        public int Count => _coordinates.Count;

        /// <summary>
        /// Gets the defined ordinates in this buffer
        /// </summary>
        public Ordinates DefinedOrdinates => _definedOrdinates;

        /// <summary>
        /// Gets the number of dimension a coordinate sequence must provide
        /// </summary>
        public int Dimension
        {
            get
            {
                int res = 2;
                if (HasM) res++;
                if (HasZ) res++;
                return res;
            }
        }

        /// <summary>
        /// Gets a value indicating if this buffer contains any z-ordinate values
        /// </summary>
        public bool HasZ => (_definedOrdinates & Ordinates.Z) == Ordinates.Z;

        /// <summary>
        /// Gets a value indicating if this buffer contains any m-ordinate values
        /// </summary>
        public bool HasM => (_definedOrdinates & Ordinates.M) == Ordinates.M;

        /// <summary>
        /// Gets the (current) capacity of the buffer
        /// </summary>
        public int Capacity => _coordinates.Capacity;

        /// <summary>
        /// Adds a coordinate made up of the ordinates (x, y, z, m) to the buffer.
        /// </summary>
        /// <param name="x">The x-Ordinate</param>
        /// <param name="y">The y-Ordinate</param>
        /// <param name="z">The (optional) z-Ordinate</param>
        /// <param name="m">The (optional) m-Ordinate</param>
        /// <param name="allowRepeated">Allows repeated coordinates to be added</param>
        /// <returns><value>true</value> if the coordinate was successfully added.</returns>
        public bool AddCoordinate(double x, double y, double? z = null, double? m = null, bool allowRepeated = true)
        {
            return InsertCoordinate(_coordinates.Count, x, y, z, m, allowRepeated);
        }

        /// <summary>
        /// Method to add a marker
        /// </summary>
        public void AddMarker()
        {
            _markers.Add(_coordinates.Count);
        }

        /// <summary>
        /// Inserts a coordinate made up of the ordinates (<paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>, <paramref name="m"/>) at index <paramref name="index"/> to the buffer.
        ///  </summary>
        /// <param name="index">The index at which to insert the ordinate.</param>
        /// <param name="x">The x-Ordinate</param>
        /// <param name="y">The y-Ordinate</param>
        /// <param name="z">The (optional) z-Ordinate</param>
        /// <param name="m">The (optional) m-Ordinate</param>
        /// <param name="allowRepeated">Allows repeated coordinates to be added</param>
        /// <returns><value>true</value> if the coordinate was successfully inserted.</returns>
        public bool InsertCoordinate(int index, double x, double y, double? z = null, double? m = null, bool allowRepeated = true)
        {
            // Assign NoDataValue if not provided
            // Update defined flag and set Coordinate.NullValue where necessary
            var toAdd = new XYZM(x,
                                 y,
                                 z ?? _doubleNoDataChecker.NoDataValue,
                                 m ?? _doubleNoDataChecker.NoDataValue);
            CheckDefinedOrdinates(ref toAdd.Z, ref toAdd.M);

            if (!allowRepeated)
            {
                if (index > 0)
                {
                    //Check before
                    if (_coordinates[index - 1].EqualInXY(toAdd))
                        return false;
                }
                if (index >= 0 && index < _coordinates.Count)
                {
                    //Check after
                    if (_coordinates[index].EqualInXY(toAdd))
                        return false;
                }
            }
            _coordinates.Insert(index, toAdd);

            // Update envelope
            _extents.ExpandToInclude(x, y);

            // Update extents for z- and m-values
            _zInterval = _zInterval.ExpandedByValue(toAdd.Z);
            _mInterval = _mInterval.ExpandedByValue(toAdd.M);

            // Signal success
            return true;
        }

        /// <summary>
        /// Clears the contents of this buffer
        /// </summary>
        public void Clear()
        {
            _coordinates.Clear();
            _definedOrdinates = Ordinates.XY;
        }

        /// <summary>
        /// Converts the contents of the buffer to an array of <see cref="Coordinate"/>s
        /// </summary>
        /// <returns>An array of <see cref="Coordinate"/>s</returns>
        public Coordinate[] ToCoordinateArray()
        {
            var res = new Coordinate[_coordinates.Count];
            bool hasZ = HasZ;
            bool hasM = HasM;
            int dim = 2 + (hasZ ? 1 : 0) + (hasM ? 1 : 0);
            var template = Coordinates.Create(dim, hasM ? 1 : 0);
            for (int i = 0; i < _coordinates.Count; i++)
            {
                var coord = _coordinates[i];
                var c = res[i] = template.Copy();
                c.X = coord.X;
                c.Y = coord.Y;
                if (hasZ)
                {
                    c.Z = coord.Z;
                }

                if (hasM)
                {
                    c.M = coord.M;
                }
            }
            return res;
        }

        /// <summary>
        /// Converts the contents of this <see cref="CoordinateBuffer"/> to a coordinate sequence using the provided <paramref name="converter"/>.
        /// </summary>
        /// <param name="converter">The converter to use</param>
        /// <returns>A coordinate sequence</returns>
        public CoordinateSequence ToSequence(Func<CoordinateBuffer, CoordinateSequence> converter)
        {
            // If we have a converter, use it
            if (converter != null)
                return converter(this);

            // so we don't. Bummer
            return ToSequence();
        }

        /// <summary>
        /// Converts the contents of this <see cref="CoordinateBuffer"/> to a coordinate sequence.
        /// </summary>
        /// <returns>A coordinate sequence</returns>
        public CoordinateSequence ToSequence(CoordinateSequenceFactory factory = null)
        {
            // Set the coordinate sequence factory, if not assigned
            if (factory == null)
                factory = _factory ?? (_factory = NtsGeometryServices.Instance.DefaultCoordinateSequenceFactory);

            // determine ordinates to apply
            var useOrdinates = _definedOrdinates & factory.Ordinates;
            bool useZ = useOrdinates.HasFlag(Ordinates.Z);
            bool useM = useOrdinates.HasFlag(Ordinates.M);

            // create the sequence
            var sequence = factory.Create(_coordinates.Count, useOrdinates);
            for (int i = 0; i < _coordinates.Count; i++)
            {
                var coord = _coordinates[i];
                sequence.SetX(i, coord.X);
                sequence.SetY(i, coord.Y);
                if (useZ)
                {
                    sequence.SetZ(i, coord.Z);
                }

                if (useM)
                {
                    sequence.SetM(i, coord.M);
                }
            }
            return sequence;
        }

        /// <summary>
        /// Converts the contents of this <see cref="CoordinateBuffer"/> to a coordinate sequence.
        /// </summary>
        /// <returns>A coordinate sequence</returns>
        public CoordinateSequence[] ToSequences(CoordinateSequenceFactory factory = null)
        {
            // Set the coordinate sequence factory, if not assigned
            if (factory == null)
                factory = _factory ?? (_factory = NtsGeometryServices.Instance.DefaultCoordinateSequenceFactory);

            // Copy the markers, append if necessary
            var markers = new List<int>(_markers);
            if (markers.Count == 0 || markers[markers.Count-1] < _coordinates.Count)
                markers.Add(_coordinates.Count);

            // determine ordinates to apply
            var useOrdinates = _definedOrdinates & factory.Ordinates;
            bool useZ = useOrdinates.HasFlag(Ordinates.Z);
            bool useM = useOrdinates.HasFlag(Ordinates.M);

            var res = new CoordinateSequence[markers.Count];
            int offset = 0;

            //Iterate over all sections
            for (int s = 0; s < markers.Count; s++)
            {
                // compute the length of the current sequence
                int length = markers[s] - offset;

                // create a sequence of the appropriate size
                var sequence = res[s] = factory.Create(length, useOrdinates);

                // fill the sequence
                for (int i = 0; i < length; i++)
                {
                    var coord = _coordinates[offset + i];
                    sequence.SetX(i, coord.X);
                    sequence.SetY(i, coord.Y);
                    if (useZ)
                    {
                        sequence.SetZ(i, coord.Z);
                    }

                    if (useM)
                    {
                        sequence.SetM(i, coord.M);
                    }
                }
                //Move the offset
                offset = offset + length;
            }
            return res;
        }

        /// <summary>
        /// Sets a z-value at the provided <paramref name="index"/>
        /// </summary>
        /// <param name="index">The index</param>
        /// <param name="z">The value</param>
        public void SetZ(int index, double z)
        {
            if (_doubleNoDataChecker.IsNoDataValue(z))
            {
                z = Coordinate.NullOrdinate;
            }
            else
            {
                _definedOrdinates |= Ordinates.Z;
                _zInterval = _zInterval.ExpandedByValue(z);
            }

            _coordinates[index].Z = z;
        }

        /// <summary>
        /// Sets a m-value at the provided <paramref name="index"/>
        /// </summary>
        /// <param name="index">The index</param>
        /// <param name="m">The value</param>
        public void SetM(int index, double m)
        {
            if (_doubleNoDataChecker.IsNoDataValue(m))
            {
                m = Coordinate.NullOrdinate;
            }
            else
            {
                _definedOrdinates |= Ordinates.M;
                _mInterval = _mInterval.ExpandedByValue(m);
            }

            _coordinates[index].M = m;
        }

        /// <summary>
        /// Converts the contents of this <see cref="CoordinateBuffer"/> to an array of <see cref="Ordinate.X"/> and <see cref="Ordinate.Y"/> values.
        /// </summary>
        /// <returns>An array of <see cref="double"/>s</returns>
        public double[] ToXY()
        {
            double[] xy = new double[Count * 2];

            int xyIndex = 0;
            foreach (var coord in _coordinates)
            {
                xy[xyIndex++] = coord.X;
                xy[xyIndex++] = coord.Y;
            }

            return xy;
        }

        /// <summary>
        /// Converts the contents of this <see cref="CoordinateBuffer"/> to an array of <see cref="Ordinate.X"/> and <see cref="Ordinate.Y"/> values.
        /// Additionally an array of <see cref="Ordinate.Z"/> values is supplied if this instance <see cref="HasZ"/> property is <c>true</c>
        /// </summary>
        /// <returns>An array of <see cref="double"/>s</returns>
        public double[] ToXYZ(out double[] z)
        {
            if (!HasZ)
            {
                z = null;
                return ToXY();
            }

            double[] xy = new double[Count * 2];
            z = new double[Count];

            int xyIndex = 0, zIndex = 0;
            foreach (var coord in _coordinates)
            {
                xy[xyIndex++] = coord.X;
                xy[xyIndex++] = coord.Y;
                z[zIndex++] = coord.Z;
            }

            return xy;
        }

        /// <summary>
        /// Converts the contents of this <see cref="CoordinateBuffer"/> to an array of <see cref="Ordinate.X"/> and <see cref="Ordinate.Y"/> values.
        /// Additionally an array of <see cref="Ordinate.M"/> values is supplied if this instance <see cref="HasM"/> property is <c>true</c>
        /// </summary>
        /// <returns>An array of <see cref="double"/>s</returns>
        public double[] ToXYM(out double[] m)
        {
            if (!HasM)
            {
                m = null;
                return ToXY();
            }

            double[] xy = new double[Count * 2];
            m = new double[Count];

            int xyIndex = 0, mIndex = 0;
            foreach (var coord in _coordinates)
            {
                xy[xyIndex++] = coord.X;
                xy[xyIndex++] = coord.Y;
                m[mIndex++] = coord.M;
            }

            return xy;
        }

        /// <summary>
        /// Converts the contents of this <see cref="CoordinateBuffer"/> to an array of <see cref="Ordinate.X"/> and <see cref="Ordinate.Y"/> values.
        /// Additionally an array of <see cref="Ordinate.M"/> and one of <see cref="Ordinate.M"/> values is supplied if this instance <see cref="HasZ"/> and or <see cref="HasM"/> property is <c>true</c>
        /// </summary>
        /// <returns>An array of <see cref="double"/>s</returns>
        public double[] ToXYZM(out double[] z, out double[] m)
        {
            if (!HasZ)
            {
                z = null;
                return ToXYM(out m);
            }

            if (!HasM)
            {
                m = null;
                return ToXYZ(out z);
            }

            double[] xy = new double[Count*2];
            z = new double[Count];
            m = new double[Count];

            int xyIndex = 0, zIndex = 0, mIndex = 0;
            foreach (var coord in _coordinates)
            {
                xy[xyIndex++] = coord.X;
                xy[xyIndex++] = coord.Y;
                z[zIndex++] = coord.Z;
                m[mIndex++] = coord.M;
            }

            return xy;
        }

        /// <summary>
        /// Converts the contents of this <see cref="CoordinateBuffer"/> to an array of <see cref="Ordinate"/> values.
        /// </summary>
        /// <returns>The number of dimensions and an array of <see cref="double"/>s</returns>
        public int ToPackedArray(out double[] ordinateValues)
        {
            bool hasZ = HasZ;
            bool hasM = HasM;
            int dimension = 2 + (hasZ ? 1 : 0) + (hasM ? 1 : 0);
            ordinateValues = new double[Count * dimension];

            int i = 0;
            foreach (var coord in _coordinates)
            {
                ordinateValues[i++] = coord.X;
                ordinateValues[i++] = coord.Y;
                if (hasZ)
                {
                    ordinateValues[i++] = coord.Z;
                }

                if (hasM)
                {
                    ordinateValues[i++] = coord.M;
                }
            }

            return dimension;
        }

        /// <summary>
        /// Converts the contents of this <see cref="CoordinateBuffer"/> to an array of <see cref="Ordinate"/> values.
        /// </summary>
        /// <returns>The number of dimensions and an array of <see cref="double"/>s</returns>
        public int ToPackedArray(out float[] ordinateValues)
        {
            bool hasZ = HasZ;
            bool hasM = HasM;
            int dimension = 2 + (hasZ ? 1 : 0) + (hasM ? 1 : 0);
            ordinateValues = new float[Count * dimension];

            int i = 0;
            foreach (var coord in _coordinates)
            {
                ordinateValues[i++] = (float)coord.X;
                ordinateValues[i++] = (float)coord.Y;
                if (hasZ)
                {
                    ordinateValues[i++] = (float)coord.Z;
                }

                if (hasM)
                {
                    ordinateValues[i++] = (float)coord.M;
                }
            }

            return dimension;
        }

        /// <summary>
        /// Checks of <paramref name="other"/> <see cref="CoordinateBuffer"/> is equal to this.
        /// </summary>
        /// <param name="other">The coordinate buffer to test.</param>
        /// <returns><c>true</c> if the coordinates in this buffer match those of other.</returns>
        public bool Equals(CoordinateBuffer other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (other == null)
                return false;
            if (other.DefinedOrdinates != DefinedOrdinates)
                return false;
            if (other.Count != Count)
                return false;

            var lst1 = _coordinates;
            var lst2 = other._coordinates;
            for (int i = 0, cnt = Count; i < cnt; i++)
            {
                if (!Equals(lst1[i], lst2[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks a coordinate sequence for equality with this
        /// </summary>
        /// <param name="other">The coordinate sequence to test</param>
        /// <returns><c>true</c> if the coordinates in the coordinate sequence are equal to those in this buffer.</returns>
        public bool Equals(CoordinateSequence other)
        {
            if (other == null)
                return false;

            /*
            if (other.Ordinates != DefinedOrdinates)
                return false;
            */
            if (other.Count != Count)
                return false;

            bool checkZ = HasZ && other.HasZ;
            bool checkM = HasM && other.HasM;
            for (int i = 0; i < _coordinates.Count; i++)
            {
                var coord = _coordinates[i];
                if (coord.X != other.GetX(i) || coord.Y != other.GetY(i))
                {
                    return false;
                }

                if (checkZ && !coord.Z.Equals(other.GetZ(i)))
                {
                    return false;
                }

                if (checkM && !coord.M.Equals(other.GetM(i)))
                {
                    return false;
                }
            }
            return true;
        }

        /// <inheritdoc cref="object.GetHashCode"/>
        public override int GetHashCode()
        {
            return 685146 ^ _coordinates.Count ^ _extents.GetHashCode();
        }

        /// <inheritdoc cref="object.ToString"/>
        public override string ToString()
        {
            return string.Format("CoordinateBuffer: {0} coordinates, Extent {1}, Z-{2}, M-{3}",
                _coordinates.Count, _extents, _zInterval, _mInterval);
        }

        /// <inheritdoc cref="object.Equals(object)"/>
        public override bool Equals(object obj)
        {
            return obj is CoordinateBuffer other && Equals(other);
        }

        /// <summary>
        /// Creates a coordinate sequence, that has all possibly repeated points removed
        /// </summary>
        /// <param name="checkZM">Controls if z- and m-values are to be considered in the equality check.</param>
        /// <returns>A coordinate buffer without repeated points</returns>
        public CoordinateBuffer RemoveRepeated(bool checkZM = false)
        {
            var res = new CoordinateBuffer(_coordinates.Count, Coordinate.NullOrdinate);
            foreach (var coordinate in _coordinates)
                res.AddCoordinate(coordinate.X, coordinate.Y, coordinate.Z, coordinate.M, checkZM);
            return res;
        }

        private sealed class XYZM : IEquatable<XYZM>
        {
            public double X;

            public double Y;

            public double Z;

            public double M;

            public XYZM(double x, double y, double z, double m) => (X, Y, Z, M) = (x, y, z, m);

            public override bool Equals(object obj) => obj is XYZM other && Equals(other);

            public bool Equals(XYZM other) => (X, Y, Z, M).Equals((other.X, other.Y, other.Z, other.M));

            public bool EqualInXY(XYZM other) => (X, Y).Equals((other.X, other.Y));

            public override int GetHashCode() => (X, Y, Z, M).GetHashCode();

            public override string ToString() => (X, Y, Z, M).ToString();
        }

        // ReSharper restore ImpureMethodCallOnReadonlyValueField
    }
}
