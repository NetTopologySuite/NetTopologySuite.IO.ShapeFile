using System;
using System.Collections.Generic;
using System.Text;

namespace NetTopologySuite.IO
{
    internal class ArrayBuffer
    {
        private static readonly int ExpandLimit = 4 * 1024 * 1024; // 4MB * sizeof(T)

        private static int GetExpandedLength<T>(T[] array)
        {
            if (array.Length < 4)
                return array.Length + 4;

            if (array.Length > ExpandLimit)
                return array.Length + ExpandLimit;

            return array.Length * 2;
        }

        public static void Expand<T>(ref T[] array, int minLength)
        {
            if (array.Length < minLength)
            {
                var expandedLength = Math.Max(minLength, GetExpandedLength(array));
                Array.Resize(ref array, expandedLength);
            }
        }

        public static void Expand<T>(ref T[] array)
        {
            Array.Resize(ref array, GetExpandedLength(array));
        }
    }
}
