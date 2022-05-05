using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.IO;
using NetTopologySuite.IO.Shapefile.Core;

namespace NetTopologySuite.IO.Esri.TestConsole.Tests
{
    public class CopyArcMapFilesTest : ArcMapShapefilesTest
    {
        protected override void RunShapefile(string srcFile, ShapefileReader src)
        {
            Console.WriteLine(srcFile);
            var copyFile = CreateFileCopyDir(srcFile);
            srcFile = GetTestFilePath(srcFile);



            using (var copy = ShapefileWriter.Open(copyFile, src.ShapeType, src.Fields, src.Encoding, src.Projection))
            {
                copy.Write(src.ToArray());
            }

            Console.WriteLine("===");  

            using (var copyReader = ShapefileReader.Open(copyFile))
            {
                copyReader.ToArray();
            }

            CompareFiles(srcFile, copyFile, ".shp");
            CompareFiles(srcFile, copyFile, ".shx");
            CompareFiles(srcFile, copyFile, ".dbf");
            CompareFiles(srcFile, copyFile, ".cpg");
            CompareFiles(srcFile, copyFile, ".prj");

            Console.WriteLine();
        }

        public void CompareFiles(string file1, string file2, string ext)
        {
            ext = ext.ToLowerInvariant();
            file1 = Path.ChangeExtension(file1, ext);
            file2 = Path.ChangeExtension(file2, ext);

            if (!File.Exists(file1))
                return;

            Console.Write(Path.GetFileName(file2).PadRight(20) + "  ");
            Console.ForegroundColor = ConsoleColor.Red;

            if (!File.Exists(file2))
            {
                Console.WriteLine("file does not exists.");
                Console.ResetColor();
                return;
            }

            var bytes1 = File.ReadAllBytes(file1);
            var bytes2 = File.ReadAllBytes(file2);
            if (bytes1.Length != bytes2.Length)
            {
                Console.WriteLine($"files have different size: {bytes1.Length} | {bytes2.Length}" );
                Console.ResetColor();
                return;
            }

            var hasErrors = false;
            for (int i = 0; i < bytes1.Length; i++)
            {
                if (hasErrors && IsFileHeaderEnd(i, ext))
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("--- File header end ---");
                    Console.ForegroundColor = ConsoleColor.Red;
                }

                if (i > 0 && i < 4 && ext == ".dbf")
                    continue; // DBF file date

                if (WriteDifferentBytes(i, bytes1[i], bytes2[i], !hasErrors))
                    hasErrors = true;
            }

            if (!hasErrors)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK");
            }
            Console.ResetColor();
        }

        public bool IsFileHeaderEnd(int pos, string ext)
        {
            if ((ext == ".shp" || ext == ".shx") && pos == 100)
                return true;

            if (ext == ".dbf" && pos == 32)
                return true;

            return false;
        }

        public bool WriteDifferentBytes(int index, byte b1, byte b2, bool newLine)
        {
            if (b1 == b2)
                return false;


            var sb = new StringBuilder();
            sb.Append("- byte[" + index + "]: ");

            sb.Append(b1.ToString().PadLeft(3));
            sb.Append(" | ");
            sb.Append(b2.ToString().PadLeft(3));

            sb.Append("   '" + (char)b1);
            sb.Append("' | '");
            sb.Append( (char)b2 + "'");
            sb.Replace(char.MinValue, '▬');

            if (newLine)
                Console.WriteLine();
            Console.WriteLine(sb.ToString());

            return true;
        }

        public void CompareShp(string shpFile1, string shpFile2)
        {

            var differences = new List<string>();
            using (var shp1 = ShapefileReader.Open(shpFile1))
            using (var shp2 = ShapefileReader.Open(shpFile1))
            {
                //shp1.GetBinaryDiff(shp2, differences);
            }

            foreach (var diff in differences)
            {
                Console.WriteLine( diff);
            }

        }


    }
}
