using Elements;
using Elements.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using GeometryEx;

namespace CoreByEnvelope
{
    public static class CoreMaker
    {
        public struct CoreDef
        {
            public Polygon perimeter;
            public double elevation;
            public double height;
            public double length;
            public double width;
            public double rotation;
        }

        public const double ROOF_ACCESS_HEIGHT = 3.0;

        public static CoreDef MakeCore(CoreByEnvelopeInputs inputs, List<Envelope> envelopes)
        {
            var height = 0.0;
            envelopes = envelopes.OrderBy(e => e.Elevation).ToList();
            envelopes.ForEach(e => height += e.Height);
            height += ROOF_ACCESS_HEIGHT;
            var footprint = envelopes.First().Profile.Perimeter;
            var crown = envelopes.Last().Profile.Perimeter.Offset(inputs.MinimumPerimeterOffset * -1.0).First();
            var area = Math.Abs(footprint.Area()) * inputs.PercentageArea;
            var ratio = 1.0;
            var perimeter = Shaper.RectangleByArea(area, ratio);
            var pCompass = new CompassBox(perimeter);
            var coreDef = new CoreDef
            {
                perimeter = perimeter,
                elevation = envelopes.First().Elevation,
                height = height,
                length = pCompass.SizeX,
                width = pCompass.SizeY,
                rotation = 0.0
            };
            var positions = new List<Vector3>();
            var centroid = crown.Centroid();
            var coordGrid = new CoordinateGrid(crown);
            positions.AddRange(coordGrid.Available);
            if (crown.Covers(centroid))
            {
                positions = positions.OrderBy(p => p.DistanceTo(centroid)).ToList();
                positions.Insert(0, centroid);
            }
            foreach (var position in positions)
            {
                perimeter = perimeter.MoveFromTo(perimeter.Centroid(), position);
                while (ratio >= 0.2)
                {
                    var rotation = 0.0;
                    while (rotation <= 90.0)
                    {
                        perimeter = perimeter.Rotate(position, rotation);
                        if (crown.Covers(perimeter))
                        {
                            centroid = perimeter.Centroid();
                            coreDef.perimeter = perimeter.MoveFromTo(centroid, new Vector3(centroid.X, centroid.Y, coreDef.elevation));
                            coreDef.rotation = rotation;
                            return coreDef;
                        }
                        rotation += 5.0;
                    }
                    ratio -= 0.1;
                    perimeter = Shaper.RectangleByArea(area, ratio);
                }
                ratio = 1.0;
                perimeter = coreDef.perimeter;
            }
            var offset = -0.1;
            var footArea = Math.Abs(footprint.Area());
            while (Math.Abs(crown.Area()) / footArea > inputs.PercentageArea || crown.Vertices.Count > 4)
            {
                offset -= 0.1;
                crown = crown.Offset(offset).OrderByDescending(p => Math.Abs(p.Area())).ToList().First();
            }
            centroid = crown.Centroid();
            coreDef.perimeter = crown.MoveFromTo(centroid, new Vector3(centroid.X, centroid.Y, coreDef.elevation));         
            return coreDef;
        }
    }
}