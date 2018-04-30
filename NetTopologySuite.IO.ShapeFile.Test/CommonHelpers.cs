using System;
using System.IO;

using GeoAPI;

using NetTopologySuite;

using NUnit.Framework;

[SetUpFixture]
internal sealed class CommonHelpers
{
    public static readonly string TestShapefilesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestShapefiles");

    [OneTimeSetUp]
    public void RunBeforeAnyTests() => GeometryServiceProvider.Instance = NtsGeometryServices.Instance;
}
