using System;
using System.Collections.Generic;
using System.Linq;
using Elements;
using Elements.Geometry;
using Elements.Geometry.Profiles;
using Elements.Spatial;
using Elements.Spatial.CellComplex;

namespace Structure
{
    public static class Structure
    {
        private const string BAYS_MODEL_NAME = "Bays";
        private const string GRIDS_MODEL_NAME = "Grids";

        private static List<Material> _lengthGradient = new List<Material>(){
            new Material(Colors.Green, 0.0f, 0.0f, false, null, false, Guid.NewGuid(), "Gradient 1"),
            new Material(Colors.Cyan, 0.0f, 0.0f, false, null, false, Guid.NewGuid(), "Gradient 2"),
            new Material(Colors.Lime, 0.0f, 0.0f, false, null, false, Guid.NewGuid(), "Gradient 3"),
            new Material(Colors.Yellow, 0.0f, 0.0f, false, null, false, Guid.NewGuid(), "Gradient 4"),
            new Material(Colors.Orange, 0.0f, 0.0f, false, null, false, Guid.NewGuid(), "Gradient 5"),
            new Material(Colors.Red, 0.0f, 0.0f, false, null, false, Guid.NewGuid(), "Gradient 6"),
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
            var model = new Model();

            var cellsModel = models[BAYS_MODEL_NAME];
            var cellComplex = cellsModel.AllElementsOfType<CellComplex>().First();

            var gridsModel = models[GRIDS_MODEL_NAME];
            var gridLines = gridsModel.AllElementsOfType<GridLine>();
            var primaryDirection = gridLines.ElementAt(0).Geometry.Segments()[0].Direction();

            var structureMaterial = new Material("Steel", Colors.Gray, 0.5, 0.3);
            model.AddElement(structureMaterial);

            var wideFlangeFactory = new WideFlangeProfileFactory();
            var columnProfile = wideFlangeFactory.GetProfileByName(input.ColumnType.ToString());
            var colProfileBounds = columnProfile.Perimeter.Bounds();
            var colProfileDepth = colProfileBounds.Max.Y - colProfileBounds.Min.Y;
            var girderProfile = wideFlangeFactory.GetProfileByName(input.GirderType.ToString());
            var girdProfileBounds = columnProfile.Perimeter.Bounds();
            var girderProfileDepth = girdProfileBounds.Max.Y - girdProfileBounds.Min.Y;
            var beamProfile = wideFlangeFactory.GetProfileByName(input.BeamType.ToString());
            var beamProfileBounds = beamProfile.Perimeter.Bounds();
            var beamProfileDepth = beamProfileBounds.Max.Y - beamProfileBounds.Min.Y;


            var edges = cellComplex.GetEdges();
            var lowestTierSet = false;
            var lowestTierElevation = double.MaxValue;

            // Order edges from lowest to highest.
            foreach (var edge in edges.OrderBy(e =>
                Math.Min(cellComplex.GetVertex(e.StartVertexId).Value.Z, cellComplex.GetVertex(e.EndVertexId).Value.Z)
            ))
            {
                var isExternal = edge.GetFaces().Count < 4;

                var start = cellComplex.GetVertex(edge.StartVertexId).Value;
                var end = cellComplex.GetVertex(edge.EndVertexId).Value;

                var l = new Line(start - new Vector3(0, 0, input.SlabThickness + girderProfileDepth / 2), end - new Vector3(0, 0, input.SlabThickness + girderProfileDepth / 2));
                StructuralFraming framing = null;

                if (l.IsVertical())
                {
                    if (!input.InsertColumnsAtExternalEdges && isExternal)
                    {
                        continue;
                    }
                    var origin = start.IsLowerThan(end) ? start : end;
                    var rotation = Vector3.XAxis.AngleTo(primaryDirection);
                    framing = new Column(origin, l.Length(), columnProfile, structureMaterial, rotation: rotation);
                }
                else
                {
                    if (!lowestTierSet)
                    {
                        lowestTierElevation = l.Start.Z;
                        lowestTierSet = true;
                    }

                    if (input.CreateBeamsOnFirstLevel)
                    {
                        framing = new Beam(l, girderProfile, structureMaterial);
                    }
                    else
                    {
                        if (l.Start.Z > lowestTierElevation)
                        {
                            framing = new Beam(l, girderProfile, structureMaterial);
                        }
                    }
                }
                if (framing != null)
                {
                    model.AddElement(framing, false);
                }
            }

            foreach (var cell in cellComplex.GetCells())
            {
                var topFace = cell.GetTopFace();
                var p = topFace.GetGeometry();
                var longestEdge = p.Segments().OrderBy(s => s.Length()).Last();
                var d = longestEdge.Direction();
                var grid = new Grid1d(longestEdge);
                grid.DivideByFixedLength(input.BeamSpacing, FixedDivisionMode.RemainderAtBothEnds);
                var segments = p.Segments();
                foreach (var pt in grid.GetCellSeparators())
                {
                    // Skip beams that would be too close to the ends 
                    // to be useful.
                    if (pt.DistanceTo(longestEdge.Start) < 1 || pt.DistanceTo(longestEdge.End) < 1)
                    {
                        continue;
                    }
                    var t = new Transform(pt, d, Vector3.ZAxis);
                    var r = new Ray(t.Origin, t.YAxis);
                    foreach (var s in segments)
                    {
                        if (s == longestEdge)
                        {
                            continue;
                        }

                        if (r.Intersects(s, out Vector3 xsect))
                        {
                            if (t.Origin.DistanceTo(xsect) < 1)
                            {
                                continue;
                            }
                            var l = new Line(t.Origin - new Vector3(0, 0, input.SlabThickness + beamProfileDepth / 2), xsect - new Vector3(0, 0, input.SlabThickness + beamProfileDepth / 2));
                            var beam = new Beam(l, beamProfile, structureMaterial);
                            model.AddElement(beam, false);
                        }
                    }
                    // model.AddElements(t.ToModelCurves());
                }
            }
            var output = new StructureOutputs(_longestGridSpan);
            output.Model = model;
            return output;
        }
    }

    internal static class Vector3Extensions
    {
        public static bool IsDirectlyUnder(this Vector3 a, Vector3 b)
        {
            return a.Z > b.Z && a.X.ApproximatelyEquals(b.X) && a.Y.ApproximatelyEquals(b.Y);
        }

        public static bool IsHigherThan(this Vector3 a, Vector3 b)
        {
            return a.Z > b.Z;
        }

        public static bool IsLowerThan(this Vector3 a, Vector3 b)
        {
            return a.Z < b.Z;
        }

        public static bool IsVertical(this Line line)
        {
            return line.Start.IsDirectlyUnder(line.End) || line.End.IsDirectlyUnder(line.Start);
        }
    }
}