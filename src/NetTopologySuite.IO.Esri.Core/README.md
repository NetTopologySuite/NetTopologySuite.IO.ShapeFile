# NetTopologySuite.IO.Esri.Core

This library provides forward-only readers and writers for [Esri shapefiles](https://support.esri.com/en/white-paper/279). 
It is vanilla .NET Standard 2.0 library without any dependencies. 


## Dbf

Shapefile feature attributes are held in a dBASE format file (.dbf extension). Each attribute record 
has a one-to-one relationship with the associated shape record. Classes whose name starts 
with `Dbf` (eg. `DbfReader`) provide direct access to dBASE files. 

```c#
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
```


## Shp

The main file (.shp extension) is a variable-record-length file in which each record describes 
a shape with a list of its vertices. Classes whose name starts with `Shp` (eg. `ShpPointReader`) 
provide direct access to main file.  

```c#
using (var shpStream = File.OpenRead(shpPath))
using (var shp = new ShpPointReader(shpStream))
{
    Console.WriteLine(shp.ShapeType);
    while (shp.Read())
    {
        Console.WriteLine(shp.Shape);
    }
}
```


## Shx

The index file (.shx extension) stores the offset and content length for each record in SHP file. 
As there is no additional value, this file is ignored during reading shapefiles. 
Writing SHX data is handled directly by `ShpWriter` classes.


## Shapefile

All three files described above form a shapefile. Unified access to shapefile triplet 
is provided through classes whose name starts with `Shapefile` (eg. `ShapefilePointReader`). 
Under the hood they are decorators wrapping `Dbf` and `Shp` classes.

### Reading shapefiles using c# code

```c#
// Intuitive but GC heavy method
using (var shp = new ShapefilePointReader(shpPath))
{
    Console.WriteLine(shp.ShapeType);
    foreach (var field in shp.Fields)
    {
        Console.WriteLine(field);
    }

    foreach (var feature in shp)
    {
        Console.WriteLine("Record ID: " + feature.Attributes["Id"]);
        foreach (var attr in feature.Attributes)
        {
            Console.WriteLine($"  {attr.Key}: {attr.Value}");
        }
        Console.WriteLine($"  SHAPE: {feature.Shape}");
    }
}
```
   
```c#
// Fast and GC friendly method
using (var shp = ShapefileReader.Open(shpPath))
{
    Console.WriteLine(shp.ShapeType);
    foreach (var field in shp.Fields)
    {
        Console.WriteLine(field);
    }

    while (shp.Read(out var deleted))
    {
        if (deleted)
            continue;

        Console.WriteLine("Record ID: " + shp.Fields["Id"].Value);
        foreach (var field in shp.Fields)
        {
            Console.WriteLine($"  {field.Name}: {field.Value}");
        }
        Console.WriteLine($"  SHAPE: {shp.Shape}");
    }
}
```


### Writing shapefiles using c# code


```c#
// Intuitive but GC heavy method
var features = new List<ShapefileFeature>();
for (int i = 1; i < 5; i++)
{
    var attributes = new Dictionary<string, object>();
    attributes["date"] = new DateTime(2000, 1, i + 1);
    attributes["float"] = i * 0.1;
    attributes["int"] = i;
    attributes["logical"] = i % 2 == 0;
    attributes["text"] = i.ToString("0.00");

    var line = new List<ShpCoordinates>();
    line.Add(new ShpCoordinates(i, i + 1, i));
    line.Add(new ShpCoordinates(i, i, i));
    line.Add(new ShpCoordinates(i + 1, i, i));

    var shapeParts = new List<List<ShpCoordinates>>();
    shapeParts.Add(line);

    var feature = new ShapefileFeature(shapeParts, attributes);
    features.Add(feature);
}

var dateField = DbfField.Create("date", typeof(DateTime));
var floatField = DbfField.Create("float", typeof(double));
var intField = DbfField.Create("int", typeof(int));
var LogicalField = DbfField.Create("logical", typeof(bool));
var textField = DbfField.Create("text", typeof(string));

using (var shp = new ShapefileMultiPartWriter(shpPath, ShapeType.PolyLine, dateField, floatField, intField, LogicalField, textField))
{
    shp.Write(features);
}

foreach (var feature in ShapefileReader.ReadAll(shpPath))
{
    Console.WriteLine(feature.Attributes);
    Console.WriteLine(feature.Shape);
}
```
  
```c#
// Fast and GC friendly method
var dateField = new DbfDateField("date");
var floatField = new DbfFloatField("float");
var intField = new DbfNumericField("int");
var LogicalField = new DbfLogicalField("logical");
var textField = new DbfCharacterField("text");

using (var shp = new ShapefileMultiPartWriter(shpPath, ShapeType.PolyLine, dateField, floatField, intField, LogicalField, textField))
{
    for (int i = 1; i < 5; i++)
    {
        // Avoid expensive boxing and unboxing value types
        dateField.DateValue = new DateTime(2000, 1, i + 1);
        floatField.NumericValue = i * 0.1;
        intField.NumericValue = i;
        LogicalField.LogicalValue = i % 2 == 0;
        textField.StringValue = i.ToString("0.00");

        // Avoid realocating new ShpCoordinates[] array over and over.
        shp.Shape.Clear();
        shp.Shape.StartNewPart();
        shp.Shape.AddPoint(i, i + 1, i);
        shp.Shape.AddPoint(i, i, i);
        shp.Shape.AddPoint(i + 1, i, i);

        shp.Write();
        Console.WriteLine("Feature number " + i + " was written.");
    }
}
```


## Performance
This library was designed with performance in mind. Thats why preferred implementation 
forces reading/writing whole shapefile using once initialized ShpShapeBuilder 
and once initialized attributes array buffer. That way .NET garbage collector 
doesn't have to dispose every single Point instance to reclaim the memory, 
and after that alocate new memory for next Point instance. 
It's like using [ArrayPool<T>.Shared](https://docs.microsoft.com/en-us/dotnet/api/system.buffers.arraypool-1.shared) 
but without the cost of renting and returning. 

There is a lot of other optimizations, to name a few of them:
- Using [structs over classes](https://adamsitnik.com/Value-Types-vs-Reference-Types/) 
  for storing `ShpCoordinates`.
- Using the `ShpShapeBuilder` which under the hood is a buffer with smart resizing capabilities.
- Using dedicated `BinaryBuffer` class which avoids file I/O operations 
  by reading/writing a whole shape record data at once instead of reading/writing 
  every single coordinate one by one. Again - without resource costly  memory realocating.
- The `BinaryBuffer` have also custom bit-converter functions with support 
  for big-endian and little-endian byte order. It's implementation avoids realocating 
  new byte[8] array again and again, any time you want to write single coordinate. 
  This avoid bottleneck GC and much faster than 
  [BitConverter](https://docs.microsoft.com/en-us/dotnet/api/system.bitconverter.getbytes#System_BitConverter_GetBytes_System_Double_) . 

See also https://github.com/dotnet/runtime/issues/7291

## Encoding

The .NET Framework supports a large number of character encodings and code pages. 
On the other hand, .NET Core only supports 
[limited list](https://docs.microsoft.com/en-us/dotnet/api/system.text.codepagesencodingprovider.instance#remarks) of encodings. 
To retrieve an encoding that is present in the .NET Framework on the Windows 
desktop but not in .NET Core, you need to do the following:  
1. Add to your project reference to to the [System.Text.Encoding.CodePages.dll](https://www.nuget.org/packages/System.Text.Encoding.CodePages/).
2. Put the following  line somewhere in your code:  
   `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);`


## Validation

For performance reasons this library does not provide any kind of validation 
during reading or writing shapefiles. If you write new shapefile it is your 
responsibility to write properly formated data. Eg. if you try to write 
polygon with only two points it will not throw any error. 

- If you read a shapefile from trustworthy source (eg. authored using ArcMap) 
  you will proceed very fast. If not, it is up to you to validate every 
  single shape (`ShpPointCollection`) you read. 
- If you write shapefile based on trustworthy geometries (eg. from PostGIS database) 
  you will proceed very fast. If not, it is up to you to validate 
  every singe shape (`ShpPointCollection`) you write. 

However, there are some quirks. Where it is possible the library is forgiving. 
If you read corrupted shapefile containing polygon with only two points 
it will return `NullShape` (empty `ShpPointCollection`) instead of throwing an error.


## Test

This library was tested with shapefiles created by ArcMap 10.6. 
Library read those files to memory and then wrote it back to files. 
Then output files was checked byte by byte for differences. 
At the moment the only inconsistency spoted is related to Date fields. 
ArcMap 10.6 can create different null date representation in one .shp file!
Test file pt_utf8.shp have field named 'date' with such binary data:
```
=== record 0     Stream.Position: 673
...
date    BinaryBuffer.Position: 183
ReadString(191): '▬▬▬▬▬▬▬▬'                  // '▬' == char.MinValue == (char)0 
=== record 1     Stream.Position: 1145
...
date    BinaryBuffer.Position: 183
ReadString(191): '        '
```
According to [Esri documentation](https://desktop.arcgis.com/en/arcmap/latest/manage-data/shapefiles/geoprocessing-considerations-for-shapefile-output.htm)
Null value substitution for Date field is *'Stored as zero'*. So this library saves null dates as zero (null) bytes which is also consistent with Numeric and Float fields. 
