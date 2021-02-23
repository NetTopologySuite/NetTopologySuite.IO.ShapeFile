using NetTopologySuite.IO.Dbf;
using NetTopologySuite.IO.Shapefile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NetTopologySuite.IO.Esri.TestConsole.Tests
{
    public abstract class Test
    {
        public string Title { get; protected set; }

        public Test()
        {
            Title = GetType().Name;
        }

        public abstract void Run();

        public static readonly string FieldSpace = " ".PadRight(12);

        public static string TestDataDir = GetTestDataDir(Assembly.GetExecutingAssembly().Location);

        private static string GetTestDataDir(string dir)
        {
            dir = Path.GetDirectoryName(dir);
            var testDataDir = Path.Combine(dir, "TestData");

            if (Directory.Exists(testDataDir))
                return testDataDir;

            if (dir.Length < 4) // "C:\"
            {
                return "";
            }

            return GetTestDataDir(dir);
        }

        public static string GetTestFilePath(string filePath)
        {
            if (File.Exists(filePath))
                return filePath;

            var path = Path.Combine(TestDataDir, filePath);
            path = Path.GetFullPath(path);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Test file not found: " + filePath);
            }
            return path;
        }

        public static string GetTempFilePath(string fileName)
        {
            var tempFilePath = Path.Combine(TestDataDir, "temp", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(tempFilePath));
            return tempFilePath;
        }

        public static string CreateFileCopyDir(string sourceFilePath)
        {            
            var filePath = GetTestFilePath(sourceFilePath);                     // arcmap/shp/point.shp
            var copyDirPath = Path.GetDirectoryName(filePath) + "-copy";        // arcmap/shp           => arcmap/shp-copy
            Directory.CreateDirectory(copyDirPath);

            return Path.Combine(copyDirPath, Path.GetFileName(sourceFilePath)); // arcmap/shp-copy/point.shp
        }

        public override string ToString()
        {
            return Title;
        }

        protected void WriteFieldNames(IReadOnlyList<DbfField> fields)
        {
            Console.WriteLine("FIELD LIST");
            Console.WriteLine("----------");
            foreach (var field in fields)
            {
                Console.WriteLine("  " + field.Name.PadRight(10) + " [" + field.FieldType.ToString().PadRight(9) +  field.Length.ToString().PadLeft(4) + field.Precision.ToString().PadLeft(3) + "]");
            }
            Console.WriteLine();
        }

        protected void WriteFieldValues(IReadOnlyList<DbfField> fields, IReadOnlyDictionary<string, object> values)
        {
            Console.WriteLine();
            foreach (var field in fields)
            {
                WriteFieldValue(field.Name, values[field.Name]);
            }
        }

        protected void WriteFieldValue(string name, object value)
        {
            name = name + ": ";
            Console.WriteLine(name.PadRight(12) + ToText(value));
        }

        public void WriteFields(DbfReader dbf)
        {
            WriteFieldNames(dbf.Fields);
            foreach (var values in dbf)
            {
                WriteFieldValues(dbf.Fields, values);

            }
        }

        protected void WriteShape(ShapeType type, IReadOnlyList<IReadOnlyList<Shapefile.Core.ShpCoordinates>> shape)
        {
            if (shape.Count < 1 || shape[0].Count < 1)
            {
                WriteFieldValue("SHAPE", "NullShape");
                return;
            }

            WriteFieldValue("SHAPE", type);
            for (int i = 0; i < shape.Count; i++)
            {
                if (shape.Count > 1)
                    Console.WriteLine(FieldSpace + "Part " + (i + 1) + ":");

                var part = shape[i];
                foreach (var pt in part)
                {
                    Console.WriteLine(FieldSpace + "  " + pt);
                }
            }
            if (shape.Count > 1)
                Console.WriteLine(FieldSpace + "Parts end.");

        }

        public void WriteFeature(ShapeType type, IReadOnlyList<DbfField> fields, Shapefile.Core.ShapefileFeature feature)
        {
            WriteFieldValues(fields, feature.Attributes);
            WriteShape(type, feature.Shape);
        }

        public void WriteFeatures(Shapefile.Core.ShapefileReader shp)
        {
            WriteFieldNames(shp.Fields);

            WriteRecordListHeader();
            foreach (var feature in shp)
            {
                WriteFeature(shp.ShapeType, shp.Fields, feature);
                Console.WriteLine();
            }
        }


        protected string ToText(object value)
        {
            if (value == null)
                return "<null>";

            if (value is string s)
                return "'" + s + "'";

            return value.ToString();
        }
        protected void WriteRecordListHeader()
        {
            Console.WriteLine("RECORD LIST");
            Console.WriteLine("-----------");
            Console.WriteLine();
        }

        public void WriteSectionTitle(string title)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            Console.WriteLine(new string('_', 80));
            Console.WriteLine(title);
            Console.WriteLine(new string('-', 80));
            Console.WriteLine();
            Console.ResetColor();
        }

        public void WriteValidationResult(bool isValid, string message)
        {
            Console.ForegroundColor = isValid ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }



    public abstract class FileTest : Test
    {
        protected readonly string Path;

        public FileTest(string dbfPath)
        {
            Path = dbfPath;
        }

        public override string ToString()
        {
            return $"{Title}: {Path}";
        }
    }
}
