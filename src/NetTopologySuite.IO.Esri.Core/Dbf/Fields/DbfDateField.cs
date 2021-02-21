using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace NetTopologySuite.IO.Dbf
{

    /// <summary>
    /// Date field definition.
    /// </summary>
    public class DbfDateField : DbfField
    {
        private static readonly int FieldLength = 8;           // This width is fixed and cannot be changed
        private static readonly string DateFormat = "yyyyMMdd";
        private static readonly byte[] DefaultValue = GetBytes(' ', FieldLength);

        /// <summary>
        ///  Initializes a new instance of the field class.
        /// </summary>
        /// <param name="name">Field name.</param>
        public DbfDateField(string name) 
            : base(name, DbfType.Date, FieldLength, 0)
        {
        }

        /// <summary>
        /// Date representation of current field value.
        /// </summary>
        public DateTime? DateValue { get; set; }

        /// <inheritdoc/>
        public override object Value
        {
            get { return DateValue; }
            set { DateValue = (DateTime?)value; }
        }


        internal override void ReadValue(BinaryBufferReader recordData)
        {
            var valueText = recordData.ReadString(Length, Encoding.ASCII)?.Trim();
            if (string.IsNullOrEmpty(valueText))
            {
                DateValue = null;
            }
            else
            {
                DateValue = DateTime.ParseExact(valueText, DateFormat, CultureInfo.InvariantCulture);
            }
        }

        internal override void WriteValue(BinaryBufferWriter recordData)
        {
            if (DateValue.HasValue)
            {
                recordData.WriteString(DateValue.Value.ToString(DateFormat, CultureInfo.InvariantCulture), Length, Encoding.ASCII);
            }
            else
            {
                // ArcMap 10.6 can create different null date representation in one .shp file!
                // My test file pt_utf8.shp have field named 'date' with such binary data:
                // === record 0     Stream.Position: 673
                // date    BinaryBuffer.Position: 183
                // ReadString(191): '▬▬▬▬▬▬▬▬'                  // '▬' == char.MinValue == (char)0 
                // === record 1     Stream.Position: 1145
                // date    BinaryBuffer.Position: 183
                // ReadString(191): '        '

                // According to https://desktop.arcgis.com/en/arcmap/latest/manage-data/shapefiles/geoprocessing-considerations-for-shapefile-output.htm
                // Null value substitution for Date field is 'Stored as zero'. Storing zero (null) values is also consistent with Numeric and Float field. 

                //recordData.WriteBytes(DefaultValue);
                recordData.WriteNullBytes(Length);
            }
        }
    }


}
