using NetTopologySuite.IO.Esri.TestConsole.Tests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NetTopologySuite.IO.Esri.TestConsole
{
    class Program
    {

        private static Test[] TestList = {
            new DbaseRreaderTest("arcmap/shp/fields_utf8.dbf"),
            new ReadArcMapFilesTest(),
            new CopyArcMapFilesTest(),
            new CreateShapefileTest(),
            new CoreReadDbf(),
            new CoreReadShp(),
            new CoreReadShapefile1(),
            new CoreReadShapefile2(),
            new CoreWriteShapefile1(),
            new CoreWriteShapefile2(),
            new NtsReadShapefile(),
            new NtsWriteShapefile1(),
        };


        static void Main(string[] args)
        {
            Console.WriteLine("NetTopologySuite.IO.Esri.TestConsole");

            if (string.IsNullOrEmpty(Test.TestDataDir))
            {
                Console.WriteLine("ERROR: TestData folder not found.");
                Console.WriteLine("Pres any key to exit.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("TestData directory: " + Test.TestDataDir);
            Console.WriteLine();

            WriteTestList();

            var testNumber = Console.ReadLine();
            while (!string.IsNullOrEmpty(testNumber))
            {
                RunTest(testNumber);
                WriteTestList();
                testNumber = Console.ReadLine();
            }
        }

        private static void WriteTestList()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("TEST LIST");
            Console.WriteLine("---------");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            for (int i = 0; i < TestList.Length; i++)
            {
                var test = TestList[i];
                Console.WriteLine((i + 1).ToString() + ": " + test.ToString());
            }
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Write test number or pres ENTER to exit: ");
        }

        private static void WriteError(Exception ex, bool exitApp = false)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR: " + ex.Message);
            ex = ex.InnerException;
            while (ex != null)
            {
                Console.WriteLine("- " + ex.Message);
                ex = ex.InnerException;
            }
            if (exitApp)
            {
                Console.WriteLine("Pres any key to exit.");
                Console.ReadKey();
                Environment.Exit(1);
            }
        }

        private static void RunTest(string testNumber)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Gray;
            if (int.TryParse(testNumber, out int number) && number > 0 && number <= TestList.Length)
            {
                var test = TestList[number - 1];
                var testName = test.ToString(); ;
                Console.WriteLine(testName);
                Console.WriteLine(new string('=', testName.Length));
                Console.WriteLine();

                test.Run();
                try
                {
                    //test.Run();
                }
                catch (Exception ex)
                {
                    WriteError(ex);
                    throw;
                }
                finally
                {
                    testName = test.Title + " finished.";
                    Console.WriteLine(testName);
                    Console.WriteLine(new string('=', testName.Length));
                    Console.WriteLine();
                }

            }
            else
            {
                Console.WriteLine("Invalid test number.");
            }
        }
    }
}
