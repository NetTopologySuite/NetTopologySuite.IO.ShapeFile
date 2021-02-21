using NetTopologySuite.IO.Dbf;
using NetTopologySuite.IO.Shapefile.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTopologySuite.IO.Esri.TestConsole.Tests
{
    public class CreateShapefileTest : Test
    {
        public override void Run()
        {
            var shpPath = GetTempFilePath("point.shp");
            var dateField = new DbfDateField("date");
            var floatField = new DbfFloatField("float");
            var intField = new DbfNumericField("int");
            var LogicalField = new DbfLogicalField("logical");
            var textField = new DbfCharacterField("text");

            using (var shp = new ShapefilePointWriter(shpPath, Shapefile.ShapeType.Point, dateField, floatField, intField, LogicalField, textField))
            {
                for (int i = 1; i < 5; i++)
                {
                    dateField.DateValue = new DateTime(2000, 1, i + 1);
                    floatField.NumericValue = i * 0.1;
                    intField.NumericValue = i;
                    LogicalField.LogicalValue = i % 2 == 0;
                    textField.StringValue = i.ToString("0.00");

                    shp.Point = new ShpCoordinates(i, i + 1, i + 2);

                    shp.Write();
                    Console.WriteLine("Record number " + i + " was written.");
                }
                Console.WriteLine();
            }

            using (var shp = new Shapefile.Core.ShapefilePointReader(shpPath))
            {
                WriteFeatures(shp);
            }
        }
    }
}
