using NetTopologySuite.IO.Dbf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTopologySuite.IO.Esri.TestConsole.Tests
{
    public class DbaseRreaderTest : FileTest
    {
        public DbaseRreaderTest(string dbfPath) : base(dbfPath)
        {
            Title = "Read DBF file";
        }

        public override void Run()
        {
            var fullPath = GetTestFilePath(Path);

            using (var dbfReader = new DbfReader(fullPath))
            {
                WriteFields(dbfReader);
            }
        }





    }
}
