using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Dbf;
using NUnit.Framework;
using System.Collections.Generic;
using System.Text;
using NetTopologySuite.IO.Shapefile;

namespace NetTopologySuite.IO.ShapeFile.Test
{
    [TestFixture]
    public class ShapeFileEncodingTest
    {
        [SetUp]
        public void Setup()
        {
            var idColumn = new DbfNumericField("id", 8, 0);
            var testColumn = new DbfCharacterField("Test", 15);
            var alderColumn = new DbfNumericField("Ålder", 8, 0);
            var odestextColumn = new DbfCharacterField("Ödestext", 254);

            var at = new AttributesTable();
            at.Add("id", "0");
            at.Add("Test", "Testar");
            at.Add("Ålder", 10);
            at.Add("Ödestext", "Lång text med åäö etc");

            var feature = new Feature(new Point(0, 0), at);
            var columns = new DbfField[] {idColumn, testColumn, alderColumn, odestextColumn};

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var encoding = Encoding.GetEncoding(1252);
            using (var shp = new ShapefilePointWriter("encoding_sample.shp", Shapefile.ShapeType.Point, columns, encoding))
            {
                shp.Write(feature);
            }
        }

        [Test]
        public void TestLoadShapeFileWithEncoding()
        {
            using (var shp = Shapefile.Core.ShapefileReader.Open("encoding_sample.shp"))
            {
                Assert.AreEqual(shp.Encoding, CodePagesEncodingProvider.Instance.GetEncoding(1252), "Invalid encoding!");

                Assert.AreEqual(shp.Fields[1].Name, "Test");
                Assert.AreEqual(shp.Fields[2].Name, "Ålder");
                Assert.AreEqual(shp.Fields[3].Name, "Ödestext");

                Assert.IsTrue(shp.Read(out var deleted), "Error reading file");
                Assert.AreEqual(shp.Fields["Test"].Value, "Testar");
                Assert.AreEqual(shp.Fields["Ödestext"].Value, "Lång text med åäö etc");
            }
        }
    }
}
