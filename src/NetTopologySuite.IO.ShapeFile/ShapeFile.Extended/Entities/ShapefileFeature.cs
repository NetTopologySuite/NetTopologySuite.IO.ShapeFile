using System;
using System.Threading;
using System.Runtime.Serialization;

using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Handlers;

namespace NetTopologySuite.IO.ShapeFile.Extended.Entities
{
    [Serializable]
    internal class ShapefileFeature : IShapefileFeature, ISerializable
    {

        public ShapefileFeature(ShapeReader shapeReader, DbaseReader dbfReader, ShapeLocationInFileInfo shapeLocation, GeometryFactory geoFactory)
        {
            FeatureId = shapeLocation.ShapeIndex;

            // removed "lazy" evaluation to avoid dispose issues (see #27)
            var geom = shapeReader.ReadShapeAtOffset(shapeLocation.OffsetFromStartOfFile, geoFactory);
            var attributes = dbfReader.ReadEntry(shapeLocation.ShapeIndex);

            Geometry = geom;
            Attributes = attributes;
        }

        private ShapefileFeature(SerializationInfo info, StreamingContext context)
        {
            var geom = (Geometry)info.GetValue("Geometry", typeof(Geometry));
            var attributes = (IAttributesTable)info.GetValue("Attributes", typeof(IAttributesTable));

            FeatureId = info.GetInt64("FeatureId");
            Geometry = geom;
            Attributes = attributes;
        }

        public Geometry Geometry { get; }

        public Envelope BoundingBox => Geometry.EnvelopeInternal;

        public IAttributesTable Attributes { get; }

        public long FeatureId { get; }

        Geometry IFeature.Geometry
        {
            get => Geometry;
            set => throw new NotSupportedException("Setting geometry on a shapefile reader is not supported!");
        }

        Envelope IFeature.BoundingBox
        {
            get => BoundingBox;
            set => throw new InvalidOperationException("Setting BoundingBox not allowed for Shapefile feature");
        }

        IAttributesTable IFeature.Attributes
        {
            get => Attributes;
            set => throw new NotSupportedException("Setting attributes on a shapefile reader is not supported!");
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info is null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue("Geometry", Geometry);
            info.AddValue("Attributes", Attributes);
            info.AddValue("FeatureId", FeatureId);
        }
    }
}
