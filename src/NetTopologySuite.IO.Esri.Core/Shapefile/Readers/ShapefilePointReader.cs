using NetTopologySuite.IO.Dbf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace NetTopologySuite.IO.Shapefile.Core
{


    /// <summary>
    /// Point shapefile reader.
    /// </summary>
    public class ShapefilePointReader : ShapefileReader, IEnumerable<ShapefilePointFeature>
    {
        /// <inheritdoc/>
        public ShapefilePointReader(Stream shpStream, Stream dbfStream, Encoding encoding = null)
            : base(shpStream, dbfStream, encoding)
        { }

        /// <inheritdoc/>
        public ShapefilePointReader(string shpPath, Encoding encoding = null)
            : base(shpPath, encoding)
        { }


        internal override ShpReader CreateShpReader(Stream shpStream)
        {
            return new ShpPointReader(shpStream);
        }

        /// <summary>
        /// Reads next feature record containing shape geometry and its attributes.
        /// </summary>
        /// <param name="feature">Shapefile point feature.</param>
        /// <param name="deleted">Indicates if this reacord was marked as deleted.</param>
        /// <returns>Value indicating if reading next record was successful.</returns>
        public bool Read(out ShapefilePointFeature feature, out bool deleted)
        {
            if (!Read(out deleted))
            {
                feature = new ShapefilePointFeature(ShpCoordinates.NullPoint, DbfField.EmptyFieldValues);
                return false;
            }

            feature = new ShapefilePointFeature(Shape[0], Fields.GetValues());
            return true;
        }

        /// <inheritdoc/>
        public IEnumerator<ShapefilePointFeature> GetEnumerator()
        {
            return new ShapefilePointEnumerator(this);
        }

        private class ShapefilePointEnumerator : IEnumerator<ShapefilePointFeature>
        {
            private readonly ShapefilePointReader Owner;
            public ShapefilePointFeature Current { get; private set; }
            object IEnumerator.Current => Current;

            public ShapefilePointEnumerator(ShapefilePointReader owner)
            {
                Owner = owner;
            }

            public void Reset()
            {
                Owner.Restart();
            }

            public bool MoveNext()
            {
                ShapefilePointFeature feature;
                var succeed = Owner.Read(out feature, out var deleted);

                if (deleted)
                {
                    return MoveNext();
                }
                Current = feature;
                return succeed;
            }

            public void Dispose()
            {
                // Nothing to dispose
            }
        }
    }


}
