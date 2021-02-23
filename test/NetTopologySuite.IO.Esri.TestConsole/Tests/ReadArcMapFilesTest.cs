using NetTopologySuite.IO.Shapefile.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTopologySuite.IO.Esri.TestConsole.Tests
{
    public class ReadArcMapFilesTest : ArcMapShapefilesTest
    {
        protected override void RunShapefile(string fileName, ShapefileReader reader)
        {
            WriteSectionTitle(fileName);
            WriteFeatures(reader);
        }
    }
}
