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
         /// <summary>
         /// Structure carrying specifications to create a ServiceCore Element.
         /// </summary>
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
        public const double MIN_CORE_SIDE_RATIO = 0.2;
        public const double CORE_SIDE_RATIO = 1.0;
        public const double CORE_SIDE_RATIO_INCREMENT = 0.1;
        public const double CORE_ROTATE_INCREMENT = 1.0;

        /// <summary>
        /// Creates the shell of a building service core by attempting to place a footprint of various side ratios and rotations within the smallest envelope of the building. If the specified service core cannot fit within the building, the core is displaced to the exterior at the midpoint of the longest side of the highest envelope.
        /// </summary>
        /// <param name="inputs">Inputs from the UI.</param>
        /// <param name="envelopes">List of Envelopes from an incoming model.</param>
        /// <returns>A CoreDef structure specifying a service core.</returns>
        public static CoreDef MakeCore(CoreByEnvelopeInputs inputs, List<Envelope> envelopes)
        {
            var height = 0.0;
            envelopes = envelopes.OrderBy(e => e.Elevation).ToList();
            envelopes.ForEach(e => height += e.Height);
            height += ROOF_ACCESS_HEIGHT;
            var footprint = envelopes.First().Profile.Perimeter;
            var area = Math.Abs(footprint.Area()) * inputs.PercentageArea;
            var ratio = CORE_SIDE_RATIO;
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
                while (ratio >= MIN_CORE_SIDE_RATIO)
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
                        rotation += CORE_ROTATE_INCREMENT;
                    }
                    ratio -= CORE_SIDE_RATIO_INCREMENT;
                    perimeter = Shaper.RectangleByArea(area, ratio);
                    perimeter = perimeter.Rotate(perimeter.Centroid(), angle);
                }
                ratio = CORE_SIDE_RATIO;
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