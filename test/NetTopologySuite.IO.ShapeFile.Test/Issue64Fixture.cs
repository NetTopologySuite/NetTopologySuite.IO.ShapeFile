using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace NetTopologySuite.IO.ShapeFile.Test
{
    [TestFixture]
    [ShapeFileIssueNumber(64)]
    public class Issue64Fixture
    {
        /// <summary>
        /// <see href="https://github.com/NetTopologySuite/NetTopologySuite.IO.ShapeFile/issues/64"/>
        /// </summary>
        [Test]
        public void Dbase_Read_Null_from_logical()
        {
            string test64 = Path.Combine(CommonHelpers.TestShapefilesDirectory, "test64.dbf");
            var header = new DbaseFileHeader();
            header.AddColumn("bool", 'L', 1, 0);
            header.NumRecords = 1;

            object[] values = new[] { (object)null };

            using (var writer = new DbaseFileWriter(test64))
            {
                writer.Write(header);
                writer.Write(values);
            }

            var reader = new DbaseFileReader(test64);

            reader.GetHeader();

            foreach (ArrayList readValues in reader)
            {
                Assert.AreEqual(values[0], readValues[0]);
            }
        }

        /// <summary>
        /// <see href="https://github.com/NetTopologySuite/NetTopologySuite.IO.ShapeFile/issues/64"/>
        /// </summary>
        [Test]
        public void Dbase_Read_Null_from_date()
        {
            string test64 = Path.Combine(CommonHelpers.TestShapefilesDirectory, "test64.dbf");
            var header = new DbaseFileHeader();
            header.AddColumn("date", 'D', 8, 0);
            header.NumRecords = 1;

            object[] values = new[] { (object)null };

            using (var writer = new DbaseFileWriter(test64))
            {
                writer.Write(header);
                writer.Write(values);
            }

            var reader = new DbaseFileReader(test64);

            reader.GetHeader();

            foreach (ArrayList readValues in reader)
            {
                Assert.AreEqual(values[0], readValues[0]);
            }
        }

        /// <summary>
        /// <see href="https://github.com/NetTopologySuite/NetTopologySuite.IO.ShapeFile/issues/64"/>
        /// </summary>
        [Test]
        public void Dbase_Read_Null_from_float()
        {
            string test64 = Path.Combine(CommonHelpers.TestShapefilesDirectory, "test64.dbf");
            var header = new DbaseFileHeader();
            header.AddColumn("date", 'F', 8, 0);
            header.NumRecords = 1;

            object[] values = new[] { (object)null };

            using (var writer = new DbaseFileWriter(test64))
            {
                writer.Write(header);
                writer.Write(values);
            }

            var reader = new DbaseFileReader(test64);

            reader.GetHeader();

            foreach (ArrayList readValues in reader)
            {
                Assert.AreEqual(values[0], readValues[0]);
            }
        }

        /// <summary>
        /// <see href="https://github.com/NetTopologySuite/NetTopologySuite.IO.ShapeFile/issues/64"/>
        /// </summary>
        [Test]
        public void Dbase_Read_Null_from_Number()
        {
            string test64 = Path.Combine(CommonHelpers.TestShapefilesDirectory, "test64.dbf");
            var header = new DbaseFileHeader();
            header.AddColumn("date", 'N', 8, 0);
            header.NumRecords = 1;

            object[] values = new[] { (object)null };

            using (var writer = new DbaseFileWriter(test64))
            {
                writer.Write(header);
                writer.Write(values);
            }

            var reader = new DbaseFileReader(test64);

            reader.GetHeader();

            foreach (ArrayList readValues in reader)
            {
                Assert.AreEqual(values[0], readValues[0]);
            }
        }

        /// <summary>
        /// <see href="https://github.com/NetTopologySuite/NetTopologySuite.IO.ShapeFile/issues/64"/>
        /// </summary>
        [Test]
        public void Dbase_Read_Null_from_Char()
        {
            string test64 = Path.Combine(CommonHelpers.TestShapefilesDirectory, "test64.dbf");
            var header = new DbaseFileHeader();
            header.AddColumn("date", 'C', 8, 0);
            header.NumRecords = 1;

            object[] values = new[] { (object)null };

            using (var writer = new DbaseFileWriter(test64))
            {
                writer.Write(header);
                writer.Write(values);
            }

            var reader = new DbaseFileReader(test64);

            reader.GetHeader();

            foreach (ArrayList readValues in reader)
            {
                Assert.AreEqual(values[0], readValues[0]);
            }
        }
    }
}
