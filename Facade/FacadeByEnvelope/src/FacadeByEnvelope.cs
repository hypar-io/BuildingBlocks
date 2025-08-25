using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Elements;
using Elements.Geometry;
using Elements.Geometry.Solids;
using Elements.Spatial;

namespace FacadeByEnvelope
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

    public static class FacadeByEnvelope
    {
        private const int Elevation = 10;
        private static readonly string ENVELOPE_MODEL_NAME = "Envelope";
        private static readonly string LEVELS_MODEL_NAME = "Levels";

        private static readonly string CONCEPTUAL_MASS_NAME = "Conceptual Mass";

        private static readonly Material GLAZING = BuiltInMaterials.Glass; // new("Glazing", new Color(1.0, 1.0, 1.0, 0.7), 0.8f, 1.0f);

        /// <summary>
        /// Adds facade Panels to one or more Masses named 'envelope'.
        /// </summary>
        /// <param name="model">The model.
        /// Add elements to the model to have them persisted.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A FacadeOutputs instance containing computed results.</returns>
        public static FacadeByEnvelopeOutputs Execute(Dictionary<string, Model> models, FacadeByEnvelopeInputs input)
        {
            // Log all the properties of the input.
            Debug.WriteLine($"Panel Width: {input.PanelWidth}");
            Debug.WriteLine($"Glass Left/Right Inset: {input.GlassLeftRightInset}");
            Debug.WriteLine($"Glass Top/Bottom Inset: {input.GlassTopBottomInset}");
            Debug.WriteLine($"Panel Color: {input.PanelColor}");
            Debug.WriteLine($"Ground Floor Setback: {input.GroundFloorSetback}");

            List<Envelope> envelopes = null;
            List<Level> levels = null;
            List<ConceptualMass> masses = null;

            var model = new Model();
            var output = new FacadeByEnvelopeOutputs();

            if (models.TryGetValue(ENVELOPE_MODEL_NAME, out var envelopesModel))
            {
                envelopes = [.. envelopesModel.AllElementsOfType<Envelope>().Where(e => e.Elevation >= 0.0)];
                if (envelopes == null || envelopes.Count == 0)
                {
                    Console.WriteLine($"No Envelopes found in the model 'Envelope'. Check the output from the function upstream that has a model output 'Envelope'.");
                    // return output;
                }
            }
            else if (models.TryGetValue(CONCEPTUAL_MASS_NAME, out var massesModel))
            {
                masses = [.. massesModel.AllElementsOfType<ConceptualMass>()];
                if (masses != null && masses.Count == 0 && envelopes != null && envelopes.Count == 0)
                {
                    output.Errors.Add($"There were no conceptual masses or envelopes found in the model. There's nothing to do.");
                    return output;
                }
            }

            if (models.TryGetValue(LEVELS_MODEL_NAME, out var levelsModel))
            {
                levels = [.. levelsModel.AllElementsOfType<Level>()];
            }

            if (levels.Count == 0)
            {
                output.Errors.Add($"No Levels found in the model 'Levels'. Check the output from the function upstream that has a model output 'Levels'.");
            }
            levels.Sort(new LevelComparer());

            List<Level> envLevels = null;

            var panelCount = 0;

            // TODO: Refactor this to use the same code for sorting levels.
            if (masses != null)
            {
                foreach (var mass in masses)
                {
                    var massT = new Transform(new Vector3(mass.Transform.Origin.X, mass.Transform.Origin.Y, 0), mass.Transform.XAxis, mass.Transform.ZAxis);

                    var boundarySegments = mass.Boundary.Perimeter.TransformedPolygon(massT).Segments();

                    var bottomLevel = levels.First(l => mass.Level == l.Id);
                    var topLevel = levels.First(l => mass.TopLevel == l.Id);
                    var bottomElevation = bottomLevel.Elevation;
                    var topElevation = topLevel.Elevation;
                    var height = mass.Height;

                    envLevels = [.. levels.Where(l => l.Elevation >= bottomElevation && l.Elevation <= topElevation)];

                    // If we're finding levels from multiple envelopes, we'll have
                    // duplicates. Make the collection distinct.
                    envLevels = [.. envLevels.DistinctBy((l) => l.Elevation)];

                    panelCount = PanelLevels(envLevels,
                                             boundarySegments,
                                             input.PanelWidth,
                                             input.GlassLeftRightInset,
                                             input.GlassTopBottomInset,
                                             model,
                                             input.PanelColor,
                                             input.PanelThickness);
                }
            }
            else if (envelopes != null)
            {
                foreach (var envelope in envelopes)
                {
                    var boundarySegments = envelope.Profile.Perimeter.Segments();
                    Level last = null;
                    if (envLevels != null)
                    {
                        // If levels don't correspond exactly with the change
                        // in envelopes, then we need the last level of the previous
                        // set to become the first level of the next set.
                        last = envLevels.Last();
                    }
                    envLevels = levels.Where(l => l.Elevation >= envelope.Elevation && l.Elevation <= envelope.Elevation + envelope.Height).ToList();
                    if (last != null)
                    {
                        envLevels.Insert(0, last);
                    }

                    panelCount = PanelLevels(envLevels,
                                             boundarySegments,
                                             input.PanelWidth,
                                             input.GlassLeftRightInset,
                                             input.GlassTopBottomInset,
                                             model,
                                             input.PanelColor,
                                             input.PanelThickness);
                }

                var groundFloorEnvelope = envelopes.OrderBy(e => e.Elevation).FirstOrDefault(e => e.Elevation >= -Vector3.EPSILON);
                if (groundFloorEnvelope != null)
                {
                    var boundarySegments = groundFloorEnvelope.Profile.Perimeter.Offset(-input.GroundFloorSetback)[0].Segments();
                    var groundLevels = levels.Where(l => l.Elevation >= groundFloorEnvelope.Elevation && l.Elevation <= groundFloorEnvelope.Elevation + groundFloorEnvelope.Height).ToList();
                    var bottom = groundLevels.First().Elevation;
                    var top = groundLevels.Count > 1 ? groundLevels[1].Elevation : groundFloorEnvelope.Elevation + groundFloorEnvelope.Height;
                    PanelGroundFloor(bottom, top, boundarySegments, input.PanelWidth, model);
                }
            }

            output.PanelQuantity = panelCount;
            output.Model = model;
            return output;
        }

        private static int PanelLevels(List<Level> envLevels,
                                       Line[] boundarySegments,
                                       double panelWidth,
                                       double glassLeftRight,
                                       double glassTopBottom,
                                       Model model,
                                       Color panelColor,
                                       double panelThickness = 0.1)
        {
            var panelMat = new Material("panel", panelColor, 0.8f, 0.5f);
            var panelCount = 0;

            for (var i = 0; i < envLevels.Count - 1; i++)
            {
                var level1 = envLevels[i];
                var level2 = envLevels[i + 1];

                // Create the master representations.
                CreateFacadePanelRepresentationInstances(panelWidth,
                                                    level2.Elevation - level1.Elevation,
                                                    glassLeftRight,
                                                    glassTopBottom,
                                                    panelThickness,
                                                    panelWidth,
                                                    panelMat,
                                                    out RepresentationInstance masterPanel,
                                                    out RepresentationInstance masterGlazing,
                                                    out Polygon outer,
                                                    out Polygon inner);
                for(var k=0; k<boundarySegments.Length; k++)
                {
                    var s0 = boundarySegments[k == 0 ? boundarySegments.Length - 1 : k-1 ];
                    var d0 = s0.Direction().Negate();

                    var s1 = boundarySegments[k == boundarySegments.Length - 1 ? 0 : k + 1];
                    var d1 = s1.Direction();

                    var s = boundarySegments[k];
                    var d = s.Direction();

                    // Console.WriteLine($"d0: {d0}, d: {d}");
                    // Calculate the plane which is the half angle miter between the two segments.
                    var miterAngleStart = d.PlaneAngleTo(d0)/2;
                    var miterAngleEnd = d.PlaneAngleTo(d1)/2;

                    var bottomSegments = DivideByLengthFromCenter(s, panelWidth);

                    try
                    {
                        for (var j = 0; j < bottomSegments.Count; j++)
                        {
                            var bs = bottomSegments[j];
                            var t = new Transform(bs.Start + new Vector3(0, 0, level1.Elevation), d, d.Cross(Vector3.ZAxis));
                            var l = bs.Length();

                            // If the segment width is within Epsilon of
                            // the input panel width, then create a
                            // panel with glazing.
                            if (Math.Abs(l - panelWidth) < Vector3.EPSILON)
                            {
                                // Create a panel instance.
                                var panelInstance = new FacadePanel(0.1, t, panelMat, name: $"FP_{i}_{j}")
                                {
                                    RepresentationInstances = [masterPanel]
                                };
                                panelInstance.AdditionalProperties.Add("Level", level1.Id);
                                model.AddElement(panelInstance);

                                var glazingInstance = new Panel(inner, GLAZING, t, null, name: $"FG_{i}_{j}")
                                {
                                    RepresentationInstances = [masterGlazing]
                                };
                                glazingInstance.AdditionalProperties.Add("Level", level1.Id);
                                model.AddElement(glazingInstance);
                            }
                            // Otherwise, create a panel with no glazing.
                            else
                            {
                                var tStandard = new Transform(bs.Start + new Vector3(0, 0, level1.Elevation), d, Vector3.ZAxis);

                                CreateStandardPanel($"FP_{i}_{j}",
                                                    l,
                                                    level2.Elevation - level1.Elevation,
                                                    panelThickness,
                                                    tStandard,
                                                    panelMat,
                                                    miterAngleStart,
                                                    miterAngleEnd,
                                                    j == 0,
                                                    out FacadePanel panel);
                                panel.AdditionalProperties.Add("Level", level1.Id);
                                model.AddElement(panel);
                            }
                            panelCount++;
                        }

                        // if (i == envLevels.Count - 2)
                        // {
                        //     var parapet = new StandardWall(new Line(new Vector3(s.Start.X, s.Start.Y, 0), new Vector3(s.End.X, s.End.Y, 0)), 0.1, 0.9, panelMat, new Transform(0,0,level2.Elevation));
                        //     model.AddElement(parapet);
                        // }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        continue;
                    }
                }
            }

            return panelCount;
        }

        private static void PanelGroundFloor(double bottomElevation,
                                             double topElevation,
                                             Line[] boundarySegments,
                                             double panelWidth,
                                             Model model)
        {
            var hight = topElevation - bottomElevation;
            foreach (var segment in boundarySegments)
            {
                var u = new Grid1d(segment);
                var v = new Grid1d(new Line(segment.Start, new Vector3(segment.Start.X, segment.Start.Y, segment.Start.Z + hight)));
                Transform t = new Transform(0, 0, bottomElevation);
                var grid2d = new Grid2d(u, v);
                grid2d.U.DivideByFixedLength(1.5);
                grid2d.V.DivideByCount(2);
                foreach (var sep in grid2d.GetCellSeparators(GridDirection.U))
                {
                    var mullion = new Beam((Line)sep, Polygon.Rectangle(0.05, 0.05), t, BuiltInMaterials.Black);
                    model.AddElement(mullion);
                }
                foreach (var sep in grid2d.GetCellSeparators(GridDirection.V))
                {
                    var mullion = new Beam((Line)sep, Polygon.Rectangle(0.05, 0.05), t, BuiltInMaterials.Black);
                    model.AddElement(mullion);
                }
                var panel = new Panel(new Polygon([
                    segment.Start, segment.End , new Vector3(segment.End.X, segment.End.Y, segment.End.Z + hight), new Vector3(segment.Start.X, segment.Start.Y, segment.Start.Z + hight)
                ]), GLAZING, t);
                model.AddElement(panel);
            }
        }

        private static List<Line> DivideByLengthFromCenter(Line line, double d)
        {
            var l = line.Length();
            var lines = new List<Line>();

            if (l <= d)
            {
                lines.Add(line);
                return lines;
            }

            var divs = (int)(l / d);
            var span = divs * d;
            var halfSpan = span / 2;
            var mid = line.Mid();
            var dir = line.Direction();
            var start = mid - dir * halfSpan;
            var end = mid + dir * halfSpan;
            if (!line.Start.IsAlmostEqualTo(start))
            {
                lines.Add(new Line(line.Start, start));
            }
            for (var i = 0; i < divs; i++)
            {
                var p1 = start + (i * d) * dir;
                var p2 = p1 + dir * d;
                lines.Add(new Line(p1, p2));
            }
            if (!line.End.IsAlmostEqualTo(end))
            {
                lines.Add(new Line(end, line.End));
            }

            return lines;
        }

        private static void CreateStandardPanel(string name,
                                                        double width,
                                                        double height,
                                                        double thickness,
                                                        Transform lowerLeft,
                                                        Material material,
                                                        double miterAngleStart,
                                                        double miterAngleEnd,
                                                        bool start,
                                                        out FacadePanel facadePanel)
        {
            var o = Vector3.YAxis.Negate();
            var a = new Vector3(0, 0, 0);
            var b = new Vector3(width, 0, 0);
            var extend = Math.Tan(Units.DegreesToRadians(start? 90- miterAngleStart : miterAngleEnd)) * thickness;
            Vector3 c, d = new();
            if (start)
            {
                c = b + o * thickness;
                d = a + (o * thickness) + Vector3.XAxis * -extend;
            }
            else
            {
                c = b + o * thickness + Vector3.XAxis * extend;
                d = a + (o * thickness);
            }

            var profile = new Profile(new Polygon([a, b, c, d]));
            var solidOps = new List<SolidOperation>() { new Extrude(profile, height, Vector3.ZAxis, false) };
            var representation = new Representation(solidOps);
            facadePanel = new FacadePanel(thickness, lowerLeft, material, representation, false, Guid.NewGuid(), name);
        }

        private static void CreateFacadePanelRepresentationInstances(double width,
                                                                     double height,
                                                                     double leftRightInset,
                                                                     double topBottomInset,
                                                                     double thickness,
                                                                     double defaultWidth,
                                                                     Material material,
                                                                     out RepresentationInstance facadePanel,
                                                                     out RepresentationInstance glazing,
                                                                     out Polygon outer,
                                                                     out Polygon inner)
        {
            var a = new Vector3(0, 0, 0);
            var b = new Vector3(width, 0, 0);
            var c = new Vector3(width, height, 0);
            var d = new Vector3(0, height, 0);

            var a1 = new Vector3(leftRightInset, topBottomInset, 0);
            var b1 = new Vector3(width - leftRightInset, topBottomInset, 0);
            var c1 = new Vector3(width - leftRightInset, height - topBottomInset, 0);
            var d1 = new Vector3(leftRightInset, height - topBottomInset, 0);
            inner = new Polygon([d1, c1, b1, a1]);
            outer = new Polygon(new[] { a, b, c, d }.Shrink(0.01));
            var profile = new Profile(outer, inner);
            var glazingOp = new Lamina(inner, false);
            glazing = new RepresentationInstance(new SolidRepresentation(glazingOp), GLAZING);
            var solidOps = new List<SolidOperation>() { new Extrude(profile, thickness, Vector3.ZAxis, false) };
            facadePanel = new RepresentationInstance(new SolidRepresentation(solidOps), material);
        }
    }
}