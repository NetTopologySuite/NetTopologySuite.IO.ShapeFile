using System;
using System.IO;
using System.Text;
using NetTopologySuite.IO.Streams;
using NUnit.Framework;

namespace NetTopologySuite.IO.ShapeFile.Test.Streams
{
    [TestFixture]
    public class ManagedStreamProviderFixture
    {
        [TestCase("This is sample text", 1252)]
        [TestCase("Dies sind deutsche Umlaute: Ää. Öö, Üü, ß", 1252)]
        [TestCase("Dies sind deutsche Umlaute: Ää. Öö, Üü, ß", 850)]
        [TestCase("Dies sind deutsche Umlaute: Ää. Öö, Üü, ß", 437)]
        public void TestConstructorText(string constructorText, int codepage)
        {
            var encoding = CodePagesEncodingProvider.Instance.GetEncoding(codepage);

            using (var memoryStream = new MemoryStream())
            {
                memoryStream.Write(encoding.GetBytes(constructorText));
                memoryStream.Position = 0;
                var bsp = new ManagedStreamProvider("Test", memoryStream);

                Assert.That(bsp.UnderlyingStreamIsReadonly, Is.False);

                using (var streamreader = new StreamReader(bsp.OpenRead(), encoding))
                {
                    string streamText = streamreader.ReadToEnd();
                    Assert.That(streamText, Is.EqualTo(constructorText));
                }
            }
        }

        [TestCase(50, 50, true)]
        [TestCase(50, 100, true)]
        [TestCase(50, 50, false)]
        [TestCase(50, 100, false)]
        public void TestConstructor(int length, int maxLength, bool @readonly)
        {
            using (var memoryStream = new MemoryStream(CreateData(length), 0, length, !@readonly))
            {
                memoryStream.Position = 0;
                var bsp = new ManagedStreamProvider("Test", memoryStream);
                Assert.That(bsp.UnderlyingStreamIsReadonly, Is.EqualTo(@readonly));

                using (var ms = (MemoryStream)bsp.OpenRead())
                {
                    byte[] data = ms.ToArray();
                    byte[] originalData = memoryStream.ToArray();
                    Assert.That(data, Is.Not.Null);
                    Assert.That(data.Length, Is.EqualTo(length));
                    Assert.That(ms, Is.EqualTo(memoryStream));
                    for (int i = 0; i < length; i++)
                        Assert.That(data[i], Is.EqualTo(originalData[i]));
                }

                try
                {
                    using (var ms = (MemoryStream)bsp.OpenWrite(false))
                    {
                        var sw = new BinaryWriter(ms);
                        sw.BaseStream.Position = 50;
                        for (int i = 0; i < 10; i++)
                            sw.Write((byte)i);
                        sw.Flush();
                        Assert.That(ms.Length, Is.EqualTo(length + 10));
                        Assert.That(memoryStream.Length, Is.EqualTo(length + 10));
                        Assert.That(memoryStream.ToArray()[59], Is.EqualTo(9));
                    }
                }
                catch (Exception ex)
                {
                    if (ex is AssertionException)
                        throw;

                    if (!@readonly)
                    {
                        Assert.That(ex, Is.TypeOf(typeof(InvalidOperationException)));
                        //Assert.That(length, Is.EqualTo(maxLength));
                    }
                }
            }
        }

        private static byte[] CreateData(int length)
        {
            var rnd = new Random();

            byte[] res = new byte[length];
            for (int i = 0; i < length; i++)
                res[i] = (byte) rnd.Next(0, 255);
            return res;
        }
    }
}
