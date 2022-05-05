using NetTopologySuite.Geometries;
using NetTopologySuite.Features;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NetTopologySuite.IO.Shapefile;
using NetTopologySuite.IO.Dbf;
using System.Linq;

namespace NetTopologySuite.IO.ShapeFile.Test.Attributes
{
    public class AttributesTest
    {
        protected GeometryFactory Factory { get; private set; }

        protected WKTReader Reader { get; private set; }

        /// <summary>
        ///
        /// </summary>
        public void Start()
        {
            // Set current dir to shapefiles dir
            Environment.CurrentDirectory = CommonHelpers.TestShapefilesDirectory;

            this.Factory = new GeometryFactory();
            this.Reader = new WKTReader();

            // ReadFromShapeFile();
            // TestSharcDbf();
            TestShapeCreation();
        }

        private void TestShapeCreation()
        {
            var points = new Coordinate[3];
            points[0] = new Coordinate(0, 0);
            points[1] = new Coordinate(1, 0);
            points[2] = new Coordinate(1, 1);

            var line_string = new LineString(points);

            var attributes = new AttributesTable();
            attributes.Add("FOO", "FOO");

            var feature = new Feature(Factory.CreateMultiLineString(new LineString[] { line_string }), attributes);
            var features = new Feature[1];
            features[0] = feature;

            features.SaveToShapefile("line_string.shp");
        }

        [Test]
        public void TestConstructor2()
        {
            IAttributesTable at = null;
            /*
            Assert.DoesNotThrow(
                () => at = new AttributesTable(new[] {new[] {"key1", new object()}, new[] {"key2", new object()}}));
            Assert.That(at, Is.Not.Null);
            Assert.That(at.Count, Is.EqualTo(2));
            Assert.That(at.Exists("key1"), Is.True);
            Assert.That(at.Exists("key2"), Is.True);
            Assert.Throws<ArgumentException>(
                () => at = new AttributesTable(new[] {new[] {"key1", new object()}, new[] {(object) "key2",}}));
            Assert.Throws<ArgumentException>(
                () => at = new AttributesTable(new[] {new[] {"key1", new object()}, new[] {new object(), "item2",}}));
             */
            Assert.DoesNotThrow(() => at = new AttributesTable { { "key1", new object() }, { "key2", new object() } });
            Assert.That(at, Is.Not.Null);
            Assert.That(at.Count, Is.EqualTo(2));
            Assert.That(at.Exists("key1"), Is.True);
            Assert.That(at.Exists("key2"), Is.True);
        }

        private void TestSharcDbf()
        {
            const string filename = @"Strade.dbf";
            if (!File.Exists(filename))
                throw new FileNotFoundException(filename + " not found at " + Environment.CurrentDirectory);

            using (var reader = new DbfReader(filename))
            {
                Console.WriteLine("RecordSize: " + reader.RecordSize);
                Console.WriteLine("Fields.Count: " + reader.Fields.Count);
                Console.WriteLine("RecordCount: " + reader.RecordCount);
                Console.WriteLine("LastUpdateDate: " + reader.LastUpdateDate);

                foreach (var field in reader.Fields)
                {
                    Console.WriteLine("FieldName: " + field.Name);
                    Console.WriteLine("DBF Type: " + field.FieldType);
                    //Console.WriteLine("CLR Type: " + field.Type);
                    Console.WriteLine("Length: " + field.Length);
                    Console.WriteLine("Length: " + field.Length);
                    Console.WriteLine("Precision: " + field.Precision);
                    //Console.WriteLine("DataAddress: " + field.DataAddress);
                }

                foreach (var record in reader)
                {
                    foreach (var attr in record)
                    {
                        Console.WriteLine("  " + attr.Key + ": " + attr.Value);
                    }
                }
            }
            Console.WriteLine();
        }

        private void ReadFromShapeFile()
        {
            const string filename = @"country";
            if (!File.Exists(filename + ".dbf"))
                throw new FileNotFoundException(filename + " not found at " + Environment.CurrentDirectory);

            List<Feature> featureCollection;
            ShapeType shapeType;
            DbfFieldCollection fields;

            using (var shpReader = ShapefileReader.Open(filename))
            {
                shapeType = shpReader.ShapeType;
                fields = shpReader.Fields;
                featureCollection = shpReader.ToList();
            }

            int index = 0;
            Console.WriteLine("Elements = " + featureCollection.Count);
            foreach (var feature in featureCollection)
            {
                Console.WriteLine("Feature " + index++);
                foreach (string name in feature.Attributes.GetNames())
                    Console.WriteLine(name + ": " + feature.Attributes[name]);
            }

            //Directory
            string dir = CommonHelpers.TestShapefilesDirectory + Path.DirectorySeparatorChar;

            // Test write with stub header
            string file = dir + "testWriteStubHeader";
            featureCollection.SaveToShapefile(file);


            // Test write with header from a existing shapefile
            file = dir + "testWriteShapefileHeader";
            using (var shpWriter = ShapefileWriter.Open(file, shapeType, fields))
            {
                shpWriter.Write(featureCollection);
            }
        }
    }
}
