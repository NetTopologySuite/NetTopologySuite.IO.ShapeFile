using NetTopologySuite.IO.Dbf;
using NUnit.Framework;
using System.Collections;
using System.IO;
using System.Linq;

namespace NetTopologySuite.IO.ShapeFile.Test
{
    [TestFixture]
    [Ignore("Sample file(s) not published")]
    public class ShapeFileInvalidHeaderTest
    {
        private readonly string _invalidPath = Path.Combine(CommonHelpers.TestShapefilesDirectory, "invalidheader.shp");

        [Test]
        public void TestInvalidShapeFile()
        {
            /*
            var s = new NetTopologySuite.IO.ShapefileReader(_invalidPath);
            var sh = s.Header;
            var g = s.ReadAll();
            */
            string dbfFile = Path.ChangeExtension(_invalidPath, ".dbf");
            using (var dbf = new DbfReader(dbfFile))
            {
                
                var de = (dbf as IEnumerable).GetEnumerator();
                Assert.IsNull(de.Current);
                de.MoveNext();
                Assert.IsNotNull(de.Current);
            }
        }
    }
}
