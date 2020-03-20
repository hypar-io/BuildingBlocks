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
        public const double CORE_ROTATE_INCREMENT = 5.0;

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
            height += inputs.ServiceCorePenthouseHeight;
            var footprint = envelopes.First().Profile.Perimeter;
            var ftArea = Math.Abs(footprint.Area());
            var area = ftArea * inputs.PercentageArea;
            var crown = envelopes.Last().Profile.Perimeter;
            var crownOff = crown.Offset(inputs.MinimumPerimeterOffset * -1.0);
            if (crownOff.Count() > 0)
            {
                crown = crownOff.First();
            }
            var angLine = crown.Segments().OrderByDescending(s => s.Length()).ToList().First();
            var angle = Math.Atan2(angLine.End.Y - angLine.Start.Y, angLine.End.X - angLine.Start.X) * (180 / Math.PI);
            var perimeter = Shaper.RectangleByArea(area, inputs.LengthToWidthRatio);
            perimeter = perimeter.Rotate(perimeter.Centroid(), angle);
            var compass = new CompassBox(perimeter);
            var coreDef = new CoreDef
            {
                perimeter = perimeter,
                elevation = envelopes.First().Elevation,
                height = height,
                length = compass.SizeX,
                width = compass.SizeY,
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
                var rotation = coreDef.rotation;
                while (rotation <= angle + 90.0)
                {
                    perimeter = perimeter.Rotate(position, rotation);
                    if (crown.Covers(perimeter)) 
                    {
                        coreDef.perimeter = perimeter;
                        coreDef.rotation = rotation;
                        return coreDef; // Return the first successful interior placement.
                    }
                    rotation += CORE_ROTATE_INCREMENT;
                }
            }

            // If no internal position found, place the service core to penetrate all envelopes along their longest side.

            angLine = footprint.Segments().OrderByDescending(s => s.Length()).ToList().First();
            angle = Math.Atan2(angLine.End.Y - angLine.Start.Y, angLine.End.X - angLine.Start.X) * (180 / Math.PI);
            perimeter = Shaper.RectangleByArea(area, inputs.LengthToWidthRatio);
            perimeter = perimeter.MoveFromTo(perimeter.Centroid(), angLine.Midpoint()).Rotate(angLine.Midpoint(), angle);
            crown = envelopes.Last().Profile.Perimeter;
            if (!perimeter.Intersects(crown))
            {
                angLine = crown.Segments().OrderByDescending(s => s.Length()).ToList().First();
                angle = Math.Atan2(angLine.End.Y - angLine.Start.Y, angLine.End.X - angLine.Start.X) * (180 / Math.PI);
                perimeter = Shaper.RectangleByArea(area, inputs.LengthToWidthRatio);
                perimeter = perimeter.MoveFromTo(perimeter.Centroid(), angLine.Midpoint()).Rotate(angLine.Midpoint(), angle);
            }
            coreDef.perimeter = perimeter;
            return coreDef;
        }
    }
}