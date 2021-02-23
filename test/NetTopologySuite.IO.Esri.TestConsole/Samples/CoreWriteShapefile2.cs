using NetTopologySuite.IO.Dbf;
using NetTopologySuite.IO.Shapefile;
using NetTopologySuite.IO.Shapefile.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTopologySuite.IO.Esri.TestConsole.Tests
{
    public class CoreWriteShapefile2 : Test
    {
        public override void Run()
        {
            var shpPath = GetTempFilePath("abcd2.shp");

            var dateField = new DbfDateField("date");
            var floatField = new DbfFloatField("float");
            var intField = new DbfNumericField("int");
            var LogicalField = new DbfLogicalField("logical");
            var textField = new DbfCharacterField("text");

            using (var shp = new ShapefileMultiPartWriter(shpPath, ShapeType.PolyLine, dateField, floatField, intField, LogicalField, textField))
            {
                for (int i = 1; i < 5; i++)
                {
                    // Avoid expensive boxing and unboxing value types
                    dateField.DateValue = new DateTime(2000, 1, i + 1);
                    floatField.NumericValue = i * 0.1;
                    intField.NumericValue = i;
                    LogicalField.LogicalValue = i % 2 == 0;
                    textField.StringValue = i.ToString("0.00");

                    // Avoid realocating new ShpCoordinates[] array over and over.
                    shp.Shape.Clear();
                    shp.Shape.StartNewPart();
                    shp.Shape.AddPoint(i, i + 1, i);
                    shp.Shape.AddPoint(i, i, i);
                    shp.Shape.AddPoint(i + 1, i, i);

                    shp.Write();
                    Console.WriteLine("Feature number " + i + " was written.");
                }
            }
        }
    }
}
