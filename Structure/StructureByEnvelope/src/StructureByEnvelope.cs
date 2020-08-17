using System;
using System.Collections.Generic;
using System.Linq;
using Elements;
using Elements.Geometry;
using Elements.Geometry.Solids;

namespace StructureByEnvelope
{
    internal class LevelComparer : IComparer<Level>
    {
        public int Compare(Level x, Level y)
        {
            if (x.Elevation > y.Elevation)
            {
                return 1;
            }
            else if (x.Elevation < y.Elevation)
            {
                return -1;
            }

            return 0;
        }
    }

    internal class IntersectionComparer : IComparer<Vector3>
    {
        private Vector3 _origin;
        public IntersectionComparer(Vector3 origin)
        {
            this._origin = origin;
        }

        public int Compare(Vector3 x, Vector3 y)
        {
            var a = x.DistanceTo(_origin);
            var b = y.DistanceTo(_origin);

            if (a < b)
            {
                return -1;
            }
            else if (a > b)
            {
                return 1;
            }
            return 0;
        }
    }

    public static class StructureByEnvelope
    {
        private const string ENVELOPE_MODEL_NAME = "Envelope";
        private const string LEVELS_MODEL_NAME = "Levels";

        private static List<Material> _lengthGradient = new List<Material>(){
            new Material(Colors.Green, 0.0f, 0.0f, false, null, false, Guid.NewGuid(), "Gradient 1"),
            new Material(Colors.Cyan, 0.0f, 0.0f, false, null, false, Guid.NewGuid(), "Gradient 2"),
            new Material(Colors.Lime, 0.0f, 0.0f, false, null, false, Guid.NewGuid(), "Gradient 3"),
            new Material(Colors.Yellow, 0.0f, 0.0f, false, null, false, Guid.NewGuid(), "Gradient 4"),
            new Material(Colors.Orange, 0.0f, 0.0f, false, null, false, Guid.NewGuid(), "Gradient 5"),
            new Material(Colors.Red, 0.0f, 0.0f, false, null, false, Guid.NewGuid(), "Gradient 6"),
        };

        private const double mToIn = .0254;

        private static List<Profile> _beamProfiles = new List<Profile>(){
            new Profile(Polygon.Rectangle(4 * mToIn, 10 * mToIn)),
            new Profile(Polygon.Rectangle(5 * mToIn, 14 * mToIn)),
            new Profile(Polygon.Rectangle(5.5 * mToIn, 16 * mToIn)),
            new Profile(Polygon.Rectangle(12 * mToIn, 10 * mToIn)),
            new Profile(Polygon.Rectangle(6 * mToIn, 18 * mToIn)),
            new Profile(Polygon.Rectangle(6.5 * mToIn, 21 * mToIn)),
            new Profile(Polygon.Rectangle(7 * mToIn, 24 * mToIn)),
            new Profile(Polygon.Rectangle(10 * mToIn, 27 * mToIn))
        };

        private static List<double> _halfDepths = new List<double>(){
            (10 * mToIn)/2,
            (14 * mToIn)/2,
            (16 * mToIn)/2,
            (10 * mToIn)/2,
            (18 * mToIn)/2,
            (21 * mToIn)/2,
            (24 * mToIn)/2,
            (27 * mToIn)/2,
        };

        private static double _longestGridSpan = 0.0;

        /// <summary>
		/// The StructureByEnvelope function.
		/// </summary>
		/// <param name="model">The model. 
		/// Add elements to the model to have them persisted.</param>
		/// <param name="input">The arguments to the execution.</param>
		/// <returns>A StructureByEnvelopeOutputs instance containing computed results.</returns>
		public static StructureByEnvelopeOutputs Execute(Dictionary<string, Model> models, StructureByEnvelopeInputs input)
        {
            List<Level> levels = null;
            List<Envelope> envelopes = null;
            var model = new Model();

            var envelopeModel = models[ENVELOPE_MODEL_NAME];
            envelopes = envelopeModel.AllElementsOfType<Envelope>().Where(e => e.Direction.IsAlmostEqualTo(Vector3.ZAxis)).ToList();
            if (envelopes.Count() == 0)
            {
                throw new Exception("No element of type 'Envelope' could be found in the supplied model.");
            }
            var levelsModel = models[LEVELS_MODEL_NAME];
            levels = levelsModel.AllElementsOfType<Level>().ToList();

            List<Line> xGrids;
            List<Line> yGrids;

            var gridRotation = CreateGridsFromBoundary(envelopes.First().Profile.Perimeter,
                                                       input.GridXAxisInterval > 0 ? input.GridXAxisInterval : 4,
                                                       input.GridYAxisInterval > 0 ? input.GridYAxisInterval : 4,
                                                       out xGrids,
                                                       out yGrids,
                                                       model);

            levels.Sort(new LevelComparer());

            Level last = null;
            var gridXMaterial = new Material("GridX", Colors.Red);
            double lumpingTolerance = 2.0;

            foreach (var envelope in envelopes)
            {
                // Inset the footprint just a bit to keep the
                // beams out of the plane of the envelope. Use the biggest 
                // beam that we have.
                var footprint = envelope.Profile.Perimeter.Offset(-0.25)[0];

                // Trim all the grid lines by the boundary
                var boundarySegments = footprint.Segments();
                var trimmedXGrids = TrimGridsToBoundary(xGrids, boundarySegments, model);
                var trimmedYGrids = TrimGridsToBoundary(yGrids, boundarySegments, model);

                // Trim all the grids against the other grids
                var xGridSegments = TrimGridsWithOtherGrids(trimmedXGrids, trimmedYGrids);
                var yGridSegments = TrimGridsWithOtherGrids(trimmedYGrids, trimmedXGrids);

                var e = envelope.Elevation;
                if (last != null)
                {
                    e = last.Elevation;
                }

                List<Vector3> columnLocations = new List<Vector3>();
                columnLocations.AddRange(CalculateColumnLocations(xGridSegments, e, lumpingTolerance));
                columnLocations.AddRange(CalculateColumnLocations(yGridSegments, e, lumpingTolerance));

                var envLevels = levels.Where(l => l.Elevation >= envelope.Elevation
                                                && l.Elevation <= envelope.Elevation + envelope.Height).Skip(1).ToList();

                if (envLevels.Count == 0)
                {
                    continue;
                }

                last = envLevels.Last();

                List<Beam> masterFraming = null;
                foreach (var l in envLevels)
                {
                    if (masterFraming == null)
                    {
                        masterFraming = CreateFramingPlan(l.Elevation, xGridSegments, yGridSegments, boundarySegments);
                        model.AddElements(masterFraming);
                    }
                    else
                    {
                        var instances = CreateFramingPlanInstance(masterFraming, l.Elevation);
                        model.AddElements(instances, false);
                    }
                }

                var colProfile = new Profile(Polygon.Rectangle(11 * mToIn, 18 * mToIn));

                GeometricElement masterColumn = null;
                foreach (var lc in columnLocations)
                {
                    if (masterColumn == null)
                    {
                        masterColumn = new Column(Vector3.Origin,
                                        envLevels.Last().Elevation - lc.Z,
                                        colProfile,
                                        BuiltInMaterials.Steel,
                                        new Transform(lc),
                                        0,
                                        0,
                                        gridRotation);
                        model.AddElement(masterColumn);
                    }
                    else
                    {
                        // var displace = Vector3.Origin - masterColumn.Transform.Origin - lc;
                        masterColumn.IsElementDefinition = true;
                        var instance = masterColumn.CreateInstance(new Transform(lc), "");//new ElementInstance(masterColumn, new Transform(lc));
                        model.AddElement(instance, false);
                    }
                }
            }

            var output = new StructureByEnvelopeOutputs(_longestGridSpan);
            output.Model = model;
            return output;
        }

        private static List<Vector3> CalculateColumnLocations(List<Line> segments, double elevation, double lumpingTolerance)
        {
            var columnIntersections = new List<Vector3>();
            foreach (var l in segments)
            {
                var start = new Vector3(l.Start.X, l.Start.Y, elevation);
                var end = new Vector3(l.End.X, l.End.Y, elevation);
                if (!columnIntersections.Contains(start)
                    && !IsWithinDistanceTo(start, columnIntersections, lumpingTolerance))
                {
                    columnIntersections.Add(start);
                }
                if (!columnIntersections.Contains(end)
                    && !IsWithinDistanceTo(end, columnIntersections, lumpingTolerance))
                {
                    columnIntersections.Add(end);
                }
            }
            return columnIntersections;
        }

        private static bool RequiresTransfer(Vector3 planLocation, double baseElevation, List<Vector3> columnLocations)
        {
            if (baseElevation == 0)
            {
                return false;
            }

            foreach (var l in columnLocations)
            {
                if (planLocation.X == l.X && planLocation.Y == l.Y)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsWithinDistanceTo(Vector3 @new, List<Vector3> existing, double tolerance)
        {
            foreach (var v in existing)
            {
                if (@new.DistanceTo(v) < tolerance)
                {
                    return true;
                }
            }
            return false;
        }

        private static double CreateGridsFromBoundary(Polygon boundary,
                                                    double xInterval,
                                                    double yInterval,
                                                    out List<Line> xGrids,
                                                    out List<Line> yGrids,
                                                    Model model)
        {
            Line longestSide = null;
            foreach (var s in boundary.Segments())
            {
                if (longestSide == null || s.Length() > longestSide.Length())
                {
                    longestSide = s;
                }
            }
            var xAxis = longestSide.Direction();
            var yAxis = xAxis.Cross(Vector3.ZAxis).Negate();

            // Construct a transform with the x axis along
            // the longest side, and the y axis pointing to the "left".
            var transform = new Transform(Vector3.Origin, xAxis, Vector3.ZAxis);
            var rotation = xAxis.AngleTo(Vector3.XAxis);

            // Use the transform to to construct a bounding
            // box oriented along the longest edge
            // containing all the vertices of the boundary.
            double minx = 10000; double miny = 10000;
            double maxx = -10000; double maxy = -10000;

            var ti = new Transform(transform);
            ti.Invert();
            var tBoundary = boundary.TransformedPolygon(ti);

            foreach (var v in tBoundary.Vertices)
            {
                if (v.X < minx) minx = v.X;
                if (v.Y < miny) miny = v.Y;
                if (v.X > maxx) maxx = v.X;
                if (v.Y > maxy) maxy = v.Y;
            }

            var max = new Vector3(maxx, maxy);
            var min = new Vector3(minx, miny);
            var w = max.X - min.X;
            var h = max.Y - min.Y;

            xGrids = new List<Line>();
            yGrids = new List<Line>();

            var start = transform.OfVector(min);
            for (var y = 0.0; y <= h; y += xInterval)
            {
                var p1 = start + yAxis * y;
                var p2 = start + yAxis * y + xAxis * w;
                var l = new Line(p1, p2);
                xGrids.Add(l);
                // model.AddElement(new ModelCurve(l, BuiltInMaterials.XAxis));
            }

            for (var x = 0.0; x <= w; x += yInterval)
            {
                var p1 = start + xAxis * x;
                var p2 = start + xAxis * x + yAxis * h;
                var l = new Line(p1, p2);
                yGrids.Add(l);
                // model.AddElement(new ModelCurve(l, BuiltInMaterials.YAxis));
            }

            // model.AddElement(new ModelCurve(Polygon.Circle(1), transform:new Transform(xGrids[0].Start)));

            return rotation;
        }

        private static List<Element> CreateFramingPlanInstance(List<Beam> framing, double elevation)
        {
            var instanceBeams = new List<Element>();
            foreach (var beam in framing)
            {
                var halfDepth = _halfDepths[_beamProfiles.IndexOf(beam.Profile)];
                beam.IsElementDefinition = true;
                var beamInstance = beam.CreateInstance(new Transform(new Vector3(0, 0, elevation - halfDepth)), "");
                instanceBeams.Add(beamInstance);
            }
            return instanceBeams;
        }

        private static List<Beam> CreateFramingPlan(double elevation,
                                                   List<Line> xGridSegments,
                                                   List<Line> yGridSegments,
                                                   IList<Line> boundarySegments)
        {
            var beams = new List<Beam>();
            var mat = BuiltInMaterials.Steel;
            foreach (var x in xGridSegments)
            {
                try
                {
                    var lengthFactor = (x.Length() / _longestGridSpan);
                    var beamIndex = (int)(lengthFactor * (_beamProfiles.Count - 1));
                    var profile = _beamProfiles[beamIndex];
                    var beam = new Beam(x,
                                        profile,
                                        mat,
                                        startSetback: 0.25,
                                        endSetback: 0.25,
                                        transform: new Transform(new Vector3(0, 0, elevation - _halfDepths[beamIndex])));
                    beams.Add(beam);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("There was an error creating a beam.");
                    Console.WriteLine(ex.Message);
                    continue;
                }
            }
            foreach (var y in yGridSegments)
            {
                try
                {
                    var lengthFactor = (y.Length() / _longestGridSpan);
                    var beamIndex = (int)(lengthFactor * (_beamProfiles.Count - 1));
                    var profile = _beamProfiles[beamIndex];
                    var beam = new Beam(y,
                                        profile,
                                        mat,
                                        startSetback: 0.25,
                                        endSetback: 0.25,
                                        transform: new Transform(new Vector3(0, 0, elevation - _halfDepths[beamIndex])));
                    beams.Add(beam);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("There was an error creating a beam.");
                    Console.WriteLine(ex.Message);
                    continue;
                }
            }
            foreach (var s in boundarySegments)
            {
                var profile = _beamProfiles[5];
                var beam = new Beam(s,
                                    profile,
                                    BuiltInMaterials.Steel,
                                    transform: new Transform(new Vector3(0, 0, elevation - _halfDepths[5])));
                beams.Add(beam);
            }

            return beams;
        }

        private static List<Line> TrimGridsWithOtherGrids(List<Line> grids, List<Line> trims)
        {
            var result = new List<Line>();
            foreach (var g in grids)
            {
                var xsects = new List<Vector3>();
                xsects.Add(g.Start);
                foreach (var trim in trims)
                {
                    if (Intersects(g, trim, out Vector3 x))
                    {
                        xsects.Add(x);
                    }
                }
                xsects.Add(g.End);

                for (var i = 0; i < xsects.Count - 1; i++)
                {
                    if (xsects[i].IsAlmostEqualTo(xsects[i + 1]))
                    {
                        continue;
                    }
                    var l = new Line(xsects[i], xsects[i + 1]);
                    var d = l.Length();
                    if (d > _longestGridSpan)
                    {
                        _longestGridSpan = d;
                    }
                    result.Add(l);
                }
            }
            return result;
        }

        private static List<Line> TrimGridsToBoundary(List<Line> grids, IList<Line> boundarySegements, Model model, bool drawTestGeometry = false)
        {
            var trims = new List<Line>();
            foreach (var grid in grids)
            {
                var xsects = new List<Vector3>();
                foreach (var s in boundarySegements)
                {
                    if (!Intersects(s, grid, out Vector3 xsect))
                    {
                        continue;
                    }
                    xsects.Add(xsect);

                    if (drawTestGeometry)
                    {
                        var pt = new Circle(0.5).ToPolygon(10);
                        var t = new Transform(xsect);
                        var mc = new ModelCurve(pt.TransformedPolyline(t), BuiltInMaterials.XAxis);
                        model.AddElement(mc);
                    }
                }

                if (xsects.Count < 2)
                {
                    continue;
                }

                xsects.Sort(new IntersectionComparer(grid.Start));

                for (var i = 0; i < xsects.Count - 1; i += 2)
                {
                    if (xsects[i].IsAlmostEqualTo(xsects[i + 1]))
                    {
                        continue;
                    }
                    trims.Add(new Line(xsects[i], xsects[i + 1]));
                }
            }
            return trims;
        }

        /// <summary>
        /// https://social.msdn.microsoft.com/Forums/vstudio/en-US/e5993847-c7a9-46ec-8edc-bfb86bd689e3/help-on-line-segment-intersection-algorithm?forum=csharpgeneral
        /// </summary>
        /// <param name="AB"></param>
        /// <param name="CD"></param>
        /// <returns></returns>
        public static bool Intersects(Line AB, Line CD, out Vector3 result)
        {
            double deltaACy = AB.Start.Y - CD.Start.Y;
            double deltaDCx = CD.End.X - CD.Start.X;
            double deltaACx = AB.Start.X - CD.Start.X;
            double deltaDCy = CD.End.Y - CD.Start.Y;
            double deltaBAx = AB.End.X - AB.Start.X;
            double deltaBAy = AB.End.Y - AB.Start.Y;

            double denominator = deltaBAx * deltaDCy - deltaBAy * deltaDCx;
            double numerator = deltaACy * deltaDCx - deltaACx * deltaDCy;

            result = new Vector3();

            if (denominator == 0)
            {
                if (numerator == 0)
                {
                    // collinear. Potentially infinite intersection points.
                    // Check and return one of them.
                    if (AB.Start.X >= CD.Start.X && AB.Start.X <= CD.End.X)
                    {
                        result = AB.Start;
                        return true;
                    }
                    else if (CD.Start.X >= AB.Start.X && CD.Start.X <= AB.End.X)
                    {
                        result = CD.Start;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                { // parallel
                    return false;
                }
            }

            double r = numerator / denominator;
            if (r < 0 || r > 1)
            {
                return false;
            }

            double s = (deltaACy * deltaBAx - deltaACx * deltaBAy) / denominator;
            if (s < 0 || s > 1)
            {
                return false;
            }

            result = new Vector3((AB.Start.X + r * deltaBAx), (AB.Start.Y + r * deltaBAy));
            return true;
        }
    }
}