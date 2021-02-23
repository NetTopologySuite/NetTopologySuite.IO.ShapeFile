using NetTopologySuite.IO.Dbf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTopologySuite.IO.Esri.TestConsole.Tests
{
    public class CoreReadDbf : Test
    {
        public override void Run()
        {
            var dbfPath = GetTestFilePath("arcmap/shp/pt_utf8.dbf");

            using (var dbf = new DbfReader(dbfPath))
            {
                foreach (var field in dbf.Fields)
                {
                    Console.WriteLine(field);
                }

                foreach (var record in dbf)
                {
                    Console.WriteLine("Record ID: " + record["Id"]);
                    foreach (var attr in record)
                    {
                        Console.WriteLine($"  {attr.Key}: {attr.Value}");
                    }
                }
            }
        }
    }
}
