using System;
using System.Collections.Generic;
using System.Text;

namespace NetTopologySuite.IO
{
    internal static class ByteExtensions
    {
        public static string ToText(this byte b)
        {
            return b.ToChar() + " " + b.ToString().PadLeft(3);
        }

        public static char ToChar(this byte b)
        {
            if (b == 0)
                return '▬';

            if (b < 32)
                return '¤'; // ⌂  ↔ ¤

            return (char)b;
        }

        public static string CompareToText(this byte b1, byte b2)
        {
            var sb = new StringBuilder(20);

            sb.Append(b1.ToString().PadLeft(3));
            sb.Append(" | ");
            sb.Append(b2.ToString().PadLeft(3));

            sb.Append("   '" + b2.ToText());
            sb.Append("' | '");
            sb.Append(b2.ToText() + "'");

            return sb.ToString();
        }
    }
}
