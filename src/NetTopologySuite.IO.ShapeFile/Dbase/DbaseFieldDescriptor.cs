using System;

namespace NetTopologySuite.IO
{
    /// <summary>
    /// Class for holding the information assicated with a dbase field.
    /// </summary>
    public class DbaseFieldDescriptor
    {
        // Field Name
        private string _name;

        // Field Type (C N L D or M)
        private char _type;

        // Field Data Address offset from the start of the record.
        private int _dataAddress;

        // Length of the data in bytes
        private int _length;

        // Field decimal count in Binary, indicating where the decimal is
        private int _decimalCount;

        /// <summary>
        ///
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static char GetDbaseType(Type type)
        {
            if (type == typeof(char))
                return 'C';
            if (type == typeof(string))
                return 'C';
            if (type == typeof(double))
                return 'N';
            if (type == typeof(float))
                return 'N';
            if (type == typeof(short))
                return 'N';
            if (type == typeof(int))
                return 'N';
            if (type == typeof(long))
                return 'N';
            if (type == typeof(ushort))
                return 'N';
            if (type == typeof(uint))
                return 'N';
            if (type == typeof(ulong))
                return 'N';
            if (type == typeof(decimal))
                return 'N';
            if (type == typeof(bool))
                return 'L';
            if (type == typeof(DateTime))
                return 'D';

            throw new NotSupportedException(string.Format("{0} does not have a corresponding dbase type.", type.Name));
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public static DbaseFieldDescriptor ShapeField()
        {
            var shpfield = new DbaseFieldDescriptor();
            shpfield.Name="Geometry";
            shpfield._type='B';
            return shpfield;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public static DbaseFieldDescriptor IdField()
        {
            var shpfield = new DbaseFieldDescriptor();
            shpfield.Name="Row";
            shpfield._type='I';
            return shpfield;
        }

        /// <summary>
        /// Field Name.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        /// <summary>
        /// Field Type (C N L D or M).
        /// </summary>
        public char DbaseType
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
            }
        }

        /// <summary>
        /// Field Data Address offset from the start of the record.
        /// </summary>
        public int DataAddress
        {
            get
            {
                return _dataAddress;
            }
            set
            {
                _dataAddress = value;
            }
        }

        /// <summary>
        /// Length of the data in bytes.
        /// </summary>
        public int Length
        {
            get
            {
                return _length;
            }
            set
            {
                _length = value;
            }
        }

        /// <summary>
        /// Field decimal count in Binary, indicating where the decimal is.
        /// </summary>
        public int DecimalCount
        {
            get
            {
                return _decimalCount;
            }
            set
            {
                _decimalCount = value;
            }
        }

        /// <summary>
        /// Returns the equivalent CLR type for this field.
        /// </summary>
        public Type Type
        {
            get
            {
                switch (_type)
                {
                    case 'L': // logical data type, one character (T,t,F,f,Y,y,N,n)
                        return typeof(bool);
                    case 'C': // char or string
                        return typeof(string);
                    case 'D': // date
                        return typeof(DateTime);
                    case 'N': // numeric
                        if (DecimalCount == 0)
                        {
                            // Align with the logic when creating a header which specifies a length 10 for an Int32.
                            if (Length <= 10)
                                return typeof (int);
                            return typeof (long);
                        }

                        return typeof(double);
                    case 'F': // double
                        return typeof(float);
                    case 'I':
                        return typeof (int);
                    case 'B': // BLOB - not a dbase but this will hold the WKB for a geometry object.
                        return typeof(byte[]);
                    default:
                        throw new NotSupportedException("Do not know how to parse Field type "+_type);
                }
            }
        }

        public override string ToString()
        {
            return string.Format("[ Field '{0}' ('{1}', {2}) ]", Name.TrimEnd(), DbaseType, Length);
        }
    }
}
