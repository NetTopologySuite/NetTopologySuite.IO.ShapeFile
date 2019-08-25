using System;
using System.ComponentModel;
using System.Text;
using System.Data;
using NetTopologySuite.Geometries;

namespace NetTopologySuite.IO
{
    public partial class Shapefile
    {
        /// <summary>
        /// Creates a DataTable representing the information in a shape file.
        /// </summary>
        /// <param name="filename">The filename (minus the . and extension) to read.</param>
        /// <param name="tableName">The name to give to the table.</param>
        /// <param name="geometryFactory">The geometry factory to use when creating the objects.</param>
        /// <param name="encoding">The encoding to use when writing data</param>
        /// <returns>DataTable representing the data </returns>
        public static DataTable CreateDataTable(string filename, string tableName, GeometryFactory geometryFactory, Encoding encoding = null)
        {
            if (filename == null)
                throw new ArgumentNullException("filename");
            if (tableName == null)
                throw new ArgumentNullException("tableName");
            if (geometryFactory == null)
                throw new ArgumentNullException("geometryFactory");

            var shpfileDataReader = new ShapefileDataReader(filename, geometryFactory, encoding);
            var table = new DataTable(tableName);

            // use ICustomTypeDescriptor to get the properies/ fields. This way we can get the
            // length of the dbase char fields. Because the dbase char field is translated into a string
            // property, we lost the length of the field. We need to know the length of the
            // field when creating the table in the database.

            var enumerator = shpfileDataReader.GetEnumerator();
            bool moreRecords = enumerator.MoveNext();
            var typeDescriptor = (ICustomTypeDescriptor)enumerator.Current;
            foreach (PropertyDescriptor property in typeDescriptor.GetProperties())
            {
                var column = (ColumnStructure)property;
                var fieldType = column.PropertyType;
                var datacolumn = new DataColumn(column.Name, fieldType);
                if (fieldType == typeof(string))
                    // use MaxLength to pass the length of the field in the dbase file
                    datacolumn.MaxLength = column.Length;
                table.Columns.Add(datacolumn);
            }

            // add the rows - need a do-while loop because we read one row in order to determine the fields
            int iRecordCount = 0;
            table.BeginLoadData();
            object[] values = new object[shpfileDataReader.FieldCount];
            do
            {
                iRecordCount++;
                shpfileDataReader.GetValues(values);
                table.LoadDataRow(values, true);
                moreRecords = enumerator.MoveNext();
            }
            while (moreRecords);
            table.EndLoadData();

            return table;
        }

    }
}
