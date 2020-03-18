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
            var area = Math.Abs(footprint.Area()) * inputs.PercentageArea;
            var ratio = 1.0;
            var angLine = footprint.Segments().OrderByDescending(s => s.Length()).ToList().First();
            var angle = Math.Atan2(angLine.End.Y - angLine.Start.Y, angLine.End.X - angLine.Start.X) * (180 / Math.PI);
            var crown = envelopes.Last().Profile.Perimeter.Offset(inputs.MinimumPerimeterOffset * -1.0).First();
            var perimeter = Shaper.RectangleByArea(area, ratio);
            perimeter = perimeter.Rotate(perimeter.Centroid(), angle);
            var pCompass = new CompassBox(perimeter);
            var coreDef = new CoreDef
            {
                perimeter = perimeter,
                elevation = envelopes.First().Elevation,
                height = height,
                length = pCompass.SizeX,
                width = pCompass.SizeY,
                rotation = angle
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
                    var rotation = coreDef.rotation;
                    while (rotation <= angle + 90.0)
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
                    perimeter = perimeter.Rotate(perimeter.Centroid(), angle);
                }
                ratio = 1.0;
                perimeter = coreDef.perimeter;
            }
            perimeter = Shaper.RectangleByArea(area, ratio);
            var compass = perimeter.Compass();
            perimeter = perimeter.MoveFromTo(compass.W, angLine.Midpoint()).Rotate(angLine.Midpoint(), angle);
            centroid = perimeter.Centroid();
            coreDef.perimeter = perimeter.MoveFromTo(centroid, new Vector3(centroid.X, centroid.Y, coreDef.elevation));    
            return coreDef;
        }
    }
}