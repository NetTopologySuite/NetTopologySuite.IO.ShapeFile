# NetTopologySuite.IO.Esri

This library provides forward-only readers and writers for [Esri shapefiles](https://support.esri.com/en/white-paper/279). 

## Getting started
  
**Writing features to a shapefile**

```c#
var features = new List<Feature>();
for (int i = 1; i < 5; i++)
{
    var attributes = new AttributesTable();
    attributes.Add("date", new DateTime(2000, 1, i + 1));
    attributes.Add("float", i * 0.1);
    attributes.Add("int", i);
    attributes.Add("logical", i % 2 == 0);
    attributes.Add("text", i.ToString("0.00"));

    var lineCoords = new List<CoordinateZ>();
    lineCoords.Add(new CoordinateZ(i, i + 1, i));
    lineCoords.Add(new CoordinateZ(i, i, i));
    lineCoords.Add(new CoordinateZ(i + 1, i, i));
    var line = new LineString(lineCoords.ToArray());

    var feature = new Feature(line, attributes);
    features.Add(feature);
}

features.SaveToShapefile(shpPath);
```

**Reading features from a shapefile**

```c#
foreach (var feature in ShapefileReader.ReadAll(shpPath))
{
    Console.WriteLine("Record ID: " + feature.Attributes["Id"]);
    foreach (var attrName in feature.Attributes.GetNames())
    {
        Console.WriteLine($"  {attrName}: {feature.Attributes[attrName]}");
    }
    Console.WriteLine($"  SHAPE: {feature.Geometry}");
}
```  


**Reading a SHP file geometries**

```c#
foreach (var geom in ShapefileReader.ReadAllGeometries(shpPath))
{
    Console.WriteLine(geom);
}
```  

## Encoding

The .NET Framework supports a large number of character encodings and code pages. 
On the other hand, .NET Core only supports 
[limited list](https://docs.microsoft.com/en-us/dotnet/api/system.text.codepagesencodingprovider.instance#remarks) of encodings. 
To retrieve an encoding that is present in the .NET Framework on the Windows 
desktop but not in .NET Core, you need to do the following:  
1. Add to your project reference to to the [System.Text.Encoding.CodePages.dll](https://www.nuget.org/packages/System.Text.Encoding.CodePages/).
2. Put the following  line somewhere in your code:  
   `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);`