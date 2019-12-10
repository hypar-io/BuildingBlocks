using System;
using System.Collections.Generic;
using System.Linq;
using Elements;
using Elements.Geometry;
using Elements.Geometry.Profiles;
using Elements.Geometry.Solids;

namespace Structure
{
    internal class LevelComparer : IComparer<Level>
    {
        public int Compare(Level x, Level y)
        {
            if(x.Elevation > y.Elevation)
            {
                return 1;
            }
            else if(x.Elevation < y.Elevation)
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

            if(a < b)
            {
                return -1;
            }
            else if(a > b)
            {
                return 1;
            }
            return 0;
        }
    }

    public static class Structure
	{
        private const string ENVELOPE_MODEL_NAME = "Envelope";
        private const string LEVELS_MODEL_NAME = "Levels";
		
        private static List<Material> _lengthGradient = new List<Material>(){
            new Material(Colors.Green, 0.0, 0.0, Guid.NewGuid(), "Gradient 1"),
            new Material(Colors.Cyan, 0.0, 0.0, Guid.NewGuid(), "Gradient 2"),
            new Material(Colors.Lime, 0.0, 0.0, Guid.NewGuid(), "Gradient 3"),
            new Material(Colors.Yellow, 0.0, 0.0, Guid.NewGuid(), "Gradient 4"),
            new Material(Colors.Orange, 0.0, 0.0, Guid.NewGuid(), "Gradient 5"),
            new Material(Colors.Red, 0.0, 0.0, Guid.NewGuid(), "Gradient 6"),
        };

        private static List<WideFlangeProfile> _beamProfiles = new List<WideFlangeProfile>(){
            (WideFlangeProfile)WideFlangeProfileServer.Instance.GetProfileByName("W10x12"),
            (WideFlangeProfile)WideFlangeProfileServer.Instance.GetProfileByName("W12x14"),
            (WideFlangeProfile)WideFlangeProfileServer.Instance.GetProfileByName("W14x22"),
            (WideFlangeProfile)WideFlangeProfileServer.Instance.GetProfileByName("W16x26"),
            (WideFlangeProfile)WideFlangeProfileServer.Instance.GetProfileByName("W18x35"),
            (WideFlangeProfile)WideFlangeProfileServer.Instance.GetProfileByName("W21x44"),
            (WideFlangeProfile)WideFlangeProfileServer.Instance.GetProfileByName("W24x55"),
            (WideFlangeProfile)WideFlangeProfileServer.Instance.GetProfileByName("W27x84")
        };

        private static double _longestGridSpan = 0.0;

        /// <summary>
		/// The Structure function.
		/// </summary>
		/// <param name="model">The model. 
		/// Add elements to the model to have them persisted.</param>
		/// <param name="input">The arguments to the execution.</param>
		/// <returns>A StructureOutputs instance containing computed results.</returns>
		public static StructureOutputs Execute(Dictionary<string, Model> models, StructureInputs input)
		{
            List<Level> levels = null;
            List<Envelope> envelopes = null;
            var model = new Model();
            if(!models.ContainsKey(ENVELOPE_MODEL_NAME))
            {
                // Make a default envelope for testing.
                var a = new Vector3(0,0,0);
                var b = new Vector3(30,0,0);
                var c = new Vector3(30,50,0);
                var d = new Vector3(15,20,0);
                var e = new Vector3(0,50,0);
                var p1 = new Polygon(new[]{a,b,c,d,e});
                var p2 = p1.Offset(-1)[0];
                var env1 = new Envelope(p1,
                                        0,
                                        20,
                                        Vector3.ZAxis,
                                        0,
                                        new Transform(),
                                        BuiltInMaterials.Void,
                                        new Representation(new List<SolidOperation>() { new Extrude(p1, 10, Vector3.ZAxis, 0, false) }),
                                        Guid.NewGuid(),
                                        "Envelope 1");
                var env2 = new Envelope(p2,
                                        20,
                                        50,
                                        Vector3.ZAxis,
                                        0,
                                        new Transform(0,0,10),
                                        BuiltInMaterials.Void,
                                        new Representation(new List<SolidOperation>() { new Extrude(p2, 20, Vector3.ZAxis, 0, false) }),
                                        Guid.NewGuid(),
                                        "Envelope 1");
                envelopes = new List<Envelope>(){env1,env2};
                model.AddElements(envelopes);
                levels = new List<Level>();
                for(var i=0; i<50; i+=3)
                {
                    levels.Add(new Level(new Vector3(0,0,i), Vector3.ZAxis, i, null, Guid.NewGuid(), $"Level {i}"));
                }
            }
            else
            {
                var envelopeModel = models[ENVELOPE_MODEL_NAME];
                envelopes = envelopeModel.AllElementsOfType<Envelope>().Where(e=>e.Direction.IsAlmostEqualTo(Vector3.ZAxis)).ToList();
                if(envelopes.Count() == 0)
                {
                    throw new Exception("No element of type 'Envelope' could be found in the supplied model.");
                }
                var levelsModel = models[LEVELS_MODEL_NAME];
                levels = levelsModel.AllElementsOfType<Level>().ToList();
            }

            List<Line> xGrids;
            List<Line> yGrids;

            CreateGridsFromBoundary(envelopes.First().Profile.Perimeter, input.GridXAxisInterval, input.GridYAxisInterval, input.GridRotation, out xGrids, out yGrids);
            
            levels.Sort(new LevelComparer());
            
            Level last = null;
            var gridXMaterial = new Material("GridX", Colors.Red);
            double lumpingTolerance = 2.0;
            
            foreach(var envelope in envelopes)
            {
                // Inset the footprint just a bit to keep the
                // beams out of the plane of the envelope. Use the biggest 
                // beam that we have.
                var footprint = envelope.Profile.Perimeter.Offset(-((WideFlangeProfile)_beamProfiles.Last()).bf/2)[0];

                // Trim all the grid lines by the boundary
                var boundarySegments = footprint.Segments();
                var trimmedXGrids = TrimGridsToBoundary(xGrids, boundarySegments, model);
                var trimmedYGrids = TrimGridsToBoundary(yGrids, boundarySegments, model);

                // Trim all the grids against the other grids
                var xGridSegments = TrimGridsWithOtherGrids(trimmedXGrids, trimmedYGrids);
                var yGridSegments = TrimGridsWithOtherGrids(trimmedYGrids, trimmedXGrids);
                
                var e = envelope.Elevation;
                if(last != null)
                {
                    e = last.Elevation;
                }
                
                List<Vector3> columnLocations = new List<Vector3>();
                columnLocations.AddRange(CalculateColumnLocations(xGridSegments, e, lumpingTolerance));
                columnLocations.AddRange(CalculateColumnLocations(yGridSegments, e, lumpingTolerance));

                var envLevels = levels.Where(l=>l.Elevation >= envelope.Elevation
                                                && l.Elevation <= envelope.Elevation + envelope.Height).Skip(1).ToList();
                
                if(envLevels.Count == 0)
                {
                    continue;
                }

                last = envLevels.Last();

                foreach(var l in envLevels)
                {
                    var framing = CreateGirders(l.Elevation, xGridSegments, yGridSegments, boundarySegments, input.ColorBeamsByLength);
                    model.AddElements(framing);
                }

                var colProfile = (WideFlangeProfile)WideFlangeProfileServer.Instance.GetProfileByName("W18x76");
                foreach(var lc in columnLocations)
                {
                    var mat = BuiltInMaterials.Steel;
                    var column = new Column(lc, envLevels.Last().Elevation - lc.Z, colProfile, mat, null, 0,0, input.GridRotation);
                    model.AddElement(column); 
                }
            }

			var output = new StructureOutputs(_longestGridSpan);
            output.model = model;
			return output;
		}

        private static List<Vector3> CalculateColumnLocations(List<Line> segments, double elevation, double lumpingTolerance)
        {
            var columnIntersections = new List<Vector3>();
            foreach(var l in segments)
            {
                var start = new Vector3(l.Start.X, l.Start.Y, elevation);
                var end = new Vector3(l.End.X, l.End.Y, elevation);
                if(!columnIntersections.Contains(start)
                    && !IsWithinDistanceTo(start, columnIntersections, lumpingTolerance))
                {
                    columnIntersections.Add(start);
                }
                if(!columnIntersections.Contains(end)
                    && !IsWithinDistanceTo(end, columnIntersections, lumpingTolerance))
                {
                    columnIntersections.Add(end);
                }
            }
            return columnIntersections;
        }

        private static bool RequiresTransfer(Vector3 planLocation, double baseElevation, List<Vector3> columnLocations)
        {
            if(baseElevation == 0)
            {
                return false;
            }

            foreach(var l in columnLocations)
            {
                if(planLocation.X == l.X && planLocation.Y == l.Y)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsWithinDistanceTo(Vector3 @new, List<Vector3> existing, double tolerance)
        {
            foreach(var v in existing)
            {
                if(@new.DistanceTo(v) < tolerance)
                {
                    return true;
                }
            }
            return false;
        }

        private static void CreateGridsFromBoundary(Polygon boundary,
                                                    double xInterval,
                                                    double yInterval,
                                                    double rotation,
                                                    out List<Line> xGrids,
                                                    out List<Line> yGrids)
        {
            var transform = new Transform();
            transform.Rotate(Vector3.ZAxis, rotation);

            // Create a grid across the boundary
            // var bounds = new BBox3(Vector3.Origin, Vector3.Origin);>
            double minx = 10000; double miny = 10000;
            double maxx = -10000; double maxy = -10000;

            foreach(var v in boundary.Vertices)
            {
                if(v.X < minx) minx = v.X;
                if(v.Y < miny) miny = v.Y;
                if(v.X > maxx) maxx = v.X;
                if(v.Y > maxy) maxy = v.Y;
            }
            var overshoot = 100;
            var min = new Vector3(minx - overshoot, miny - overshoot);
            var max = new Vector3(maxx + overshoot, maxy + overshoot);

            var centroid = boundary.Centroid();

            xGrids = new List<Line>();
            yGrids = new List<Line>();

            for(var x=min.X; x<=max.X; x += xInterval)
            {
                yGrids.Add(transform.OfLine(new Line(new Vector3(x,min.Y), new Vector3(x, max.Y))));
            }

            for(var y=min.Y; y<=max.Y; y += yInterval)
            {
                xGrids.Add(transform.OfLine(new Line(new Vector3(min.X,y), new Vector3(max.X, y))));
            }
        }

        private static List<Element> CreateGirders(double elevation,
                                                   List<Line> xGridSegments,
                                                   List<Line> yGridSegments,
                                                   IList<Line> boundarySegments,
                                                   bool colorByLength)
        {
            var beams = new List<Element>();
            var mat = BuiltInMaterials.Steel;
            foreach(var x in xGridSegments)
            {
                try
                {
                    var lengthFactor = (x.Length()/_longestGridSpan);
                    var profile = _beamProfiles[(int)(lengthFactor * (_beamProfiles.Count - 1))];
                    var beam = new Beam(x,
                                        profile,
                                        colorByLength ? _lengthGradient[(int)(lengthFactor*(_lengthGradient.Count-1))] : mat,
                                        startSetback: 0.25,
                                        endSetback: 0.25,
                                        transform: new Transform(new Vector3(0,0, elevation-profile.d/2)));
                    beams.Add(beam);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("There was an error creating a beam.");
                    Console.WriteLine(ex.Message);
                    continue;
                }
            }
            foreach(var y in yGridSegments)
            {
                try
                {
                    var lengthFactor = (y.Length()/_longestGridSpan);
                    var profile = _beamProfiles[(int)(lengthFactor * (_beamProfiles.Count - 1))];
                    var beam = new Beam(y,
                                        profile,
                                        colorByLength ? _lengthGradient[(int)(lengthFactor * (_lengthGradient.Count-1))] : mat,
                                        startSetback: 0.25,
                                        endSetback: 0.25,
                                        transform: new Transform(new Vector3(0,0, elevation-profile.d/2)));
                    beams.Add(beam);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("There was an error creating a beam.");
                    Console.WriteLine(ex.Message);
                    continue;
                }
            }
            foreach(var s in boundarySegments)
            {
                var profile = _beamProfiles[5];
                var beam = new Beam(s,
                                    profile,
                                    BuiltInMaterials.Steel,
                                    transform: new Transform(new Vector3(0,0,elevation - profile.d/2)));
                beams.Add(beam);
            }

            return beams;
        }

        private static List<Line> TrimGridsWithOtherGrids(List<Line> grids, List<Line> trims)
        {
            var result = new List<Line>();
            foreach(var g in grids)
            {
                var xsects = new List<Vector3>();
                xsects.Add(g.Start);
                foreach(var trim in trims)
                {
                    var x = Intersects(g, trim);
                    if(x != null)
                    {
                        xsects.Add(x);
                    }
                }
                xsects.Add(g.End);
                
                for(var i=0; i<xsects.Count-1; i++)
                {
                    if(xsects[i].IsAlmostEqualTo(xsects[i+1]))
                    {
                        continue;
                    }
                    var l = new Line(xsects[i],xsects[i+1]);
                    var d = l.Length();
                    if(d > _longestGridSpan)
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
            foreach(var grid in grids)
            {
                var xsects = new List<Vector3>();
                foreach(var s in boundarySegements)
                {
                    Vector3 xsect = Intersects(s, grid);
                    if(xsect == null)
                    {
                        continue;
                    }
                    xsects.Add(xsect);

                    if(drawTestGeometry)
                    {
                        var pt = Polygon.Circle(0.5);
                        var t = new Transform(xsect);
                        var mc = new ModelCurve(t.OfPolygon(pt), BuiltInMaterials.XAxis);
                        model.AddElement(mc);
                    }
                }

                if(xsects.Count < 2)
                {
                    continue;
                }

                xsects.Sort(new IntersectionComparer(grid.Start));

                for(var i=0; i<xsects.Count; i+=2)
                {
                    if(xsects[i].IsAlmostEqualTo(xsects[i+1]))
                    {
                        continue;
                    }
                    trims.Add(new Line(xsects[i], xsects[i+1]));
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
        public static Vector3 Intersects(Line AB, Line CD) {
            double deltaACy = AB.Start.Y - CD.Start.Y;
            double deltaDCx = CD.End.X - CD.Start.X;
            double deltaACx = AB.Start.X - CD.Start.X;
            double deltaDCy = CD.End.Y - CD.Start.Y;
            double deltaBAx = AB.End.X - AB.Start.X;
            double deltaBAy = AB.End.Y - AB.Start.Y;

            double denominator = deltaBAx * deltaDCy - deltaBAy * deltaDCx;
            double numerator = deltaACy * deltaDCx - deltaACx * deltaDCy;

            if (denominator == 0) 
            {
                if (numerator == 0) {
                    // collinear. Potentially infinite intersection points.
                    // Check and return one of them.
                    if (AB.Start.X >= CD.Start.X && AB.Start.X <= CD.End.X) {
                    return AB.Start;
                    } else if (CD.Start.X >= AB.Start.X && CD.Start.X <= AB.End.X) {
                    return CD.Start;
                    } else {
                    return null;
                    }
                } 
                else 
                { // parallel
                    return null;
                }
            }

            double r = numerator / denominator;
            if (r < 0 || r > 1) 
            {
                return null;
            }

            double s = (deltaACy * deltaBAx - deltaACx * deltaBAy) / denominator;
            if (s < 0 || s > 1) 
            {
                return null;
            }

            return new Vector3 ((AB.Start.X + r * deltaBAx), (AB.Start.Y + r * deltaBAy));
        }
  	}
}