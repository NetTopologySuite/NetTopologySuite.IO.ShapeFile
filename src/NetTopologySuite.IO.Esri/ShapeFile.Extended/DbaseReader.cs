using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NetTopologySuite.Features;
using NetTopologySuite.IO.Streams;

namespace NetTopologySuite.IO.ShapeFile.Extended
{
    public class DbaseReader : IEnumerable<IAttributesTable>, IDisposable
    {
        private DbaseHeader m_Header = null;
        //private readonly string m_Filename;
        private readonly StreamProvider m_StreamProvider;
        private BinaryReader m_FileReader;
        private bool m_IsDisposed;

        /// <summary>
        /// Initializes a new instance of the DbaseFileReader class.
        /// </summary>
        /// <param name="filename"></param>
        public DbaseReader(string filename)
            : this(new StreamProvider(StreamTypes.Data, filename, true))
        {
        }

        public DbaseReader(StreamProvider streamProvider)
        {
            m_StreamProvider = streamProvider;
            //ReadHeader();
        }

        ~DbaseReader()
        {
            Dispose(false);
        }

        internal int NumOfRecords => m_Header.RecordCount;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IAttributesTable ReadEntry(int index)
        {
            if (m_IsDisposed)
            {
                throw new InvalidOperationException("Reader was disposed, cannot read from a disposed reader");
            }

            if (index < 0)
            {
                throw new ArgumentException("Index must be positive", "index");
            }

            int seekLocation = 1; // m_Header.FieldHeaderSize + (index * m_Header.RecordSize);

            if (seekLocation >= m_FileReader.BaseStream.Length)
            {
                throw new ArgumentOutOfRangeException("index", "No DBF entry with index " + index);
            }

            m_FileReader.BaseStream.Seek(seekLocation, SeekOrigin.Begin);

            var tbl = new AttributesTable();
            var data = ReadCurrentEntry();

            for (int i = 0; i < data.Count; i++)
            {
                tbl.Add(m_Header.Fields[i].Name, data[i]);
            }

            return tbl;
        }

        public IEnumerator<IAttributesTable> GetEnumerator()
        {
            return new DbaseEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<IAttributesTable>)this).GetEnumerator();
        }

        internal DbaseReader Clone()
        {
            return new DbaseReader(m_StreamProvider);
        }

        private ArrayList ReadCurrentEntry()
        {
            ArrayList attrs = null;

            // retrieve the record length
            int tempNumFields = 1; // m_Header.NumFields;

            // storage for the actual values
            attrs = new ArrayList(tempNumFields);

            // read the deleted flag
            char tempDeleted = (char)m_FileReader.ReadChar();

            // read the record length
            int tempRecordLength = 1; // for the deleted character just read.

            // read the Fields
            for (int j = 0; j < tempNumFields; j++)
            {
                // find the length of the field.
                int tempFieldLength = m_Header.Fields[j].Length;
                tempRecordLength = tempRecordLength + tempFieldLength;

                // find the field type
                char tempFieldType = (char)m_Header.Fields[j].FieldType;

                // read the data.
                object tempObject = null;
                switch (tempFieldType)
                {
                    case 'L':   // logical data type, one character (T,t,F,f,Y,y,N,n)
                        char tempChar = (char)m_FileReader.ReadByte();
                        if ((tempChar == 'T') || (tempChar == 't') || (tempChar == 'Y') || (tempChar == 'y'))
                            tempObject = true;
                        else tempObject = false;
                        break;

                    case 'C':   // character record.

                        if (m_Header.Encoding == null)
                        {
                            char[] sbuffer = m_FileReader.ReadChars(tempFieldLength);
                            tempObject = new string(sbuffer).Trim().Replace("\0", string.Empty);   //.ToCharArray();
                        }
                        else
                        {
                            byte[] buf = m_FileReader.ReadBytes(tempFieldLength);
                            tempObject = m_Header.Encoding.GetString(buf, 0, buf.Length).Trim();
                        }
                        break;

                    case 'D':   // date data type.
                        char[] ebuffer = m_FileReader.ReadChars(8);
                        string tempString = new string(ebuffer, 0, 4);

                        int year;
                        if (!int.TryParse(tempString, NumberStyles.Integer, CultureInfo.InvariantCulture, out year))
                            break;
                        tempString = new string(ebuffer, 4, 2);

                        int month;
                        if (!int.TryParse(tempString, NumberStyles.Integer, CultureInfo.InvariantCulture, out month))
                            break;
                        tempString = new string(ebuffer, 6, 2);

                        int day;
                        if (!int.TryParse(tempString, NumberStyles.Integer, CultureInfo.InvariantCulture, out day))
                            break;

                        try
                        {
                            if (day > 0 && year > 0 && month > 0 && month <= 12) // don't try to parse date when day is invalid - it will be useless and slow for large files
                                tempObject = new DateTime(year, month, day);
                        }
                        catch (Exception)
                        {
                        }

                        break;

                    case 'N': // number
                    case 'F': // floating point number
                        char[] fbuffer = m_FileReader.ReadChars(tempFieldLength);
                        tempString = new string(fbuffer);

                        // if we can't format the number, just save it as a string
                        tempObject = tempString;

                        double result;
                        var numberStyle = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowExponent;
                        if (double.TryParse(tempString, numberStyle, CultureInfo.InvariantCulture, out result))
                        {
                            tempObject = result;
                        }
                        break;

                    default:
                        throw new NotSupportedException("Do not know how to parse Field type " + tempFieldType);
                }

                attrs.Add(tempObject);
            }

            // ensure that the full record has been read.
            if (tempRecordLength < m_Header.RecordSize)
            {
                m_FileReader.ReadBytes(m_Header.RecordSize - tempRecordLength);
            }
            return attrs;
        }

        private void Dispose(bool disposing)
        {
            if (!m_IsDisposed)
            {
                if (m_FileReader != null)
                {
                    m_FileReader.Close();
                }

                m_IsDisposed = true;
            }
        }
    }
}
