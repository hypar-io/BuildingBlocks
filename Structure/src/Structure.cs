using System;
using System.Collections.Generic;
using System.Linq;
using Elements;
using Elements.Geometry;

namespace Structure
{
  	public static class Structure
	{
        private const string ENVELOPE_MODEL_NAME = "Envelope";

		/// <summary>
		/// The Structure function.
		/// </summary>
		/// <param name="model">The model. 
		/// Add elements to the model to have them persisted.</param>
		/// <param name="input">The arguments to the execution.</param>
		/// <returns>A StructureOutputs instance containing computed results.</returns>
		public static StructureOutputs Execute(Dictionary<string, Model> models, StructureInputs input)
		{
            Polygon footprint = null;
            double height;
            if(!models.ContainsKey(ENVELOPE_MODEL_NAME))
            {
                // Make a default envelope for testing.
                var a = new Vector3(-5,0,0);
                var b = new Vector3(30,3,0);
                var c = new Vector3(7,20,0);
                var d = new Vector3(-2,18,0);
                footprint = new Polygon(new[]{a,b,c,d});
                // footprint = Polygon.L(20, 30, 10);
                height = 20;
            }
            else
            {
                var envelopeModel = models["Envelope"];
                var envelope = envelopeModel.AllElementsOfType<Envelope>().FirstOrDefault();
                if(envelope == null)
                {
                    throw new Exception("No element of type 'Envelope' could be found in the supplied model.");
                }
                var footPrint = envelope.Profile;
                if(footPrint == null)
                {
                    throw new Exception("The provided Envelope does not have a profile.");
                }
                height = envelope.Height;
            }

            var transform = new Transform();
            transform.Rotate(Vector3.ZAxis, input.GridRotation);

            // Create a grid across the boundary
            // var bounds = new BBox3(Vector3.Origin, Vector3.Origin);
            double minx = 10000; double miny = 10000;
            double maxx = -10000; double maxy = -10000;

            foreach(var v in footprint.Vertices)
            {
                if(v.X < minx) minx = v.X;
                if(v.Y < miny) miny = v.Y;
                if(v.X > maxx) maxx = v.X;
                if(v.Y > maxy) maxy = v.Y;
            }
            var overshoot = 100;
            var min = new Vector3(minx - overshoot, miny - overshoot);
            var max = new Vector3(maxx + overshoot, maxy + overshoot);

            var centroid = footprint.Centroid();

            var xGrids = new List<Line>();
            var yGrids = new List<Line>();

            for(var x=min.X; x<=max.X; x += input.GridXAxisInterval)
            {
                yGrids.Add(transform.OfLine(new Line(new Vector3(x,min.Y), new Vector3(x, max.Y))));
            }

            for(var y=min.Y; y<=max.Y; y += input.GridYAxisInterval)
            {
                xGrids.Add(transform.OfLine(new Line(new Vector3(min.X,y), new Vector3(max.X, y))));
            }
            
            // Trim all the grid lines by the boundary
            var boundarySegements = footprint.Segments();
            var trimmedXGrids = TrimGridsToBoundary(xGrids, boundarySegements);
            var trimmmedYGrids = TrimGridsToBoundary(yGrids, boundarySegements);
            
            // Trim all the grids against the other grids
            var xGridSegments = TrimGridsWithOtherGrids(trimmedXGrids, trimmmedYGrids);
            var yGridSegments = TrimGridsWithOtherGrids(trimmmedYGrids, trimmedXGrids);
            
            var columnIntersections = new List<Vector3>();
            foreach(var l in xGridSegments)
            {
                columnIntersections.Add(l.Start);
            }
            foreach(var l in yGridSegments)
            {
                if(!columnIntersections.Contains(l.Start))
                {
                    columnIntersections.Add(l.Start);
                }
                if(!columnIntersections.Contains(l.End))
                {
                    columnIntersections.Add(l.End);
                }
            }

            var model = new Model();

            var gridXMaterial = new Material("GridX", Colors.Red);
            foreach(var gridSeg in xGridSegments)
            {
                var mc = new ModelCurve(gridSeg, gridXMaterial);
                model.AddElement(mc);
            }

            var gridYMaterial = new Material("GridY", Colors.Green);
            foreach(var gridSeg in yGridSegments)
            {
                var mc = new ModelCurve(gridSeg, gridYMaterial);
                model.AddElement(mc);
            }
            double floorToFloor = 3;
            double finalHeight = height;
            for(var i=floorToFloor; i<height; i+=floorToFloor)
            {
                var t = new Transform(0,0,i);
                var framing = CreateFramingPlan(t, xGridSegments, yGridSegments, boundarySegements);
                model.AddElements(framing);
                finalHeight = i;
            }

            var colProfile = new Profile(Polygon.Rectangle(0.2,0.2));
            foreach(var lc in columnIntersections)
            {
                var column = new Column(lc, finalHeight, colProfile, BuiltInMaterials.Steel);
                model.AddElement(column); 
            }
            
            var footprintMaterial = new Material("Footprint", Colors.Blue);
            var footprintMc = new ModelCurve(footprint, footprintMaterial);
            model.AddElement(footprintMc);

			var output = new StructureOutputs(trimmedXGrids.Count);
            output.model = model;
			return output;
		}

        private static List<Element> CreateFramingPlan(Transform t, List<Line> xGridSegments, List<Line> yGridSegments, IList<Line> boundarySegements)
        {
            var beams = new List<Element>();

            var beamProfile = new Profile(Polygon.Rectangle(0.1,0.25));
            var beamOffset = new Transform(0,0,-0.25/2);
            var levelTrans = new Transform(t);
            levelTrans.Concatenate(beamOffset);
            foreach(var x in xGridSegments)
            {
                var beam = new Beam(x,
                                    beamProfile,
                                    BuiltInMaterials.Steel,
                                    transform: levelTrans);
                beams.Add(beam);
            }
            foreach(var y in yGridSegments)
            {
                try
                {
                    var beam = new Beam(y,
                                        beamProfile,
                                        BuiltInMaterials.Steel,
                                        transform: levelTrans);
                    beams.Add(beam);
                }
                catch
                {
                    continue;
                }
            }
            foreach(var s in boundarySegements)
            {
                var beam = new Beam(s,
                                    beamProfile,
                                    BuiltInMaterials.Steel,
                                    transform: levelTrans);
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
                foreach(var g1 in trims)
                {
                    var x = Intersects(g, g1);
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
                    result.Add(l);
                }
            }
            return result;
        }

        private static List<Line> TrimGridsToBoundary(List<Line> grids, IList<Line> boundarySegements)
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
                }
                if(xsects.Count == 0)
                {
                    continue;
                }
                for(var i=0; i<xsects.Count-1; i+=2)
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