﻿using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetTopologySuite.IO.ShapeFile.Test
{
    /// <summary>
    /// 
    /// </summary>
    public class ShapeRead
    {
        protected IGeometryFactory Factory { get; private set; }

        protected WKTReader Reader { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public ShapeRead()
        {
            // Set current dir to shapefiles dir
            Environment.CurrentDirectory = CommonHelpers.TestShapefilesDirectory;

            this.Factory = new GeometryFactory();
            this.Reader = new WKTReader();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Start()
        {
            //TestBugMultipolygonHShuntao();
            //TestBugCimino();

            //// Bug with a.shp and b.shp and intersection
            //IGeometryCollection aColl = ReadShape("a.shp");
            //IGeometryCollection bColl = ReadShape("b.shp");
            //IGeometry result = aColl.Intersection(bColl);

            //// Point shapefile            
            //TestShapeReadWrite("tnp_pts.shp", "Test_tnp_pts.shp");

            //// Arc shapefile
            TestShapeReadWrite("tnp_arc.shp", "arc.shp");
            TestShapeReadWrite("Stato_Fatto.shp", "Test_Stato_Fatto.shp");
            TestShapeReadWrite("Stato_Progetto.shp", "Test_Stato_Progetto.shp");
            TestShapeReadWrite("Zone_ISTAT.shp", "Test_Zone_ISTAT.shp");
            TestShapeReadWrite("Strade.shp", "Test_Strade.shp");

            //// Polygon shapefile
            //TestShapeReadWrite("tnp_pol.shp", "Test_tnp_pol.shp");

            //// MultiPoint shapefile
            //TestShapeReadWrite("tnp_multipoint.shp", "Test_tnp_multipoint.shp");

            // TestShapeReadWrite("a.shp", "Test_a.shp");
            // TestShapeReadWrite("b.shp", "Test_b.shp");            
        }

        //private void TestBugMultipolygonHShuntao()
        //{
        //    IGeometryCollection gc1 = null;
        //    IGeometryCollection gc2 = null;
        //    string file = "BJmultipolygon.shp";
        //    if (!File.Exists(file))
        //        throw new FileNotFoundException();

        //    // Test with Default ShapefileReader
        //    try
        //    {
        //        var sfr = new ShapefileReader(file);
        //        gc1 = sfr.ReadAll();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }

        //    //// Test with MyShapefileReader (only for debug purpose!)
        //    //try
        //    //{
        //    //    MyShapeFileReader reader = new MyShapeFileReader();
        //    //    gc2 = reader.Read(file);                                
        //    //}
        //    //catch (Exception ex)
        //    //{
        //    //    throw ex;
        //    //}

        //    //// Check for equality
        //    //if (!gc1.EqualsExact(gc2))
        //    //    throw new TopologyException("Both geometries must be equals!");
        //}

        //private void TestBugCimino()
        //{
        //    try
        //    {
        //        string file = "countryCopy.shp";
        //        if (!File.Exists(file))
        //            throw new FileNotFoundException();

        //        var sfr = new ShapefileReader(file);

        //        var gc = sfr.ReadAll();
        //        for (int i = 0; i < gc.NumGeometries; i++)
        //            Console.WriteLine(i + " " + gc.Geometries[i].Envelope);

        //        // IsValidOp.CheckShellsNotNested molto lento nell'analisi di J == 7 (Poligono con 11600 punti)
        //        string write = Path.Combine(Path.GetTempPath(), "copy_countryCopy");
        //        var sfw = new ShapefileWriter(gc.Factory);
        //        sfw.Write(write, gc);
        //        Console.WriteLine("Write Complete!");
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        private static IGeometryCollection ReadShape(string shapepath)
        {
            if (!File.Exists(shapepath))
                throw new ArgumentException("File " + shapepath + " not found!");

            var reader = new ShapefileReader(shapepath);
            var geometries = reader.ReadAll();
            return geometries;
        }

        private static void WriteShape(IGeometryCollection geometries, string shapepath)
        {
            if (File.Exists(shapepath))
                File.Delete(shapepath);
            var sfw = new ShapefileWriter(geometries.Factory);
            sfw.Write(Path.GetFileNameWithoutExtension(shapepath), geometries);
        }

        private static void TestShapeReadWrite(string shapepath, string outputpath)
        {
            var collection = ReadShape(shapepath);
            WriteShape(collection, outputpath);
            var testcollection = ReadShape(outputpath);

            if (!collection.EqualsExact(testcollection))
                throw new ArgumentException("Geometries are not equals");
            Console.WriteLine("TEST OK!");
        }
    }
}
