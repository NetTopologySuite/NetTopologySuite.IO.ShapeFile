using NetTopologySuite.IO.Dbf;
using NUnit.Framework;
using System;
using System.Collections;
using System.IO;
using System.Linq;

namespace NetTopologySuite.IO.ShapeFile.Test
{
    /// <summary>
    ///
    /// </summary>
    [TestFixture]
    public class DbfDateTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NormalizeTest"/> class.
        /// </summary>
        public DbfDateTest()
        {

        }

        /// <summary>
        ///
        /// </summary>
        [Test]
        public void ReadDbfDate()
        {
            string file = Path.Combine(CommonHelpers.TestShapefilesDirectory, "date.dbf");

            if (!File.Exists(file))
                throw new FileNotFoundException("file not found at " + Path.GetDirectoryName(file));

            using (var reader = new DbfReader(file))
            {
                var dateField = reader.Fields.FirstOrDefault(f => f is DbfDateField) as DbfDateField;
                Assert.IsNotNull(dateField);

                var firstRecord = reader.FirstOrDefault();

                Assert.IsNotNull(firstRecord);
                Assert.AreEqual(2, firstRecord.Count);

                foreach (object item in firstRecord.Values)
                    Assert.IsNotNull(item);

                var date = dateField.DateValue.Value;

                Assert.AreEqual(10, date.Day);
                Assert.AreEqual(3, date.Month);
                Assert.AreEqual(2006, date.Year);
            }

        }
    }
}
