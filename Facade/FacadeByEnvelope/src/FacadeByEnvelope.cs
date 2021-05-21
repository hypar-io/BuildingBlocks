using System;
using System.Collections.Generic;
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
        private static string ENVELOPE_MODEL_NAME = "Envelope";
        private static string LEVELS_MODEL_NAME = "Levels";

        private static Material _glazing = new Material("Glazing", new Color(1.0, 1.0, 1.0, 0.7), 0.8f, 1.0f);
        private static Material _nonStandardPanel = new Material(Colors.Orange, 0.0f, 0.0f, false, null, false, Guid.NewGuid(), "Non-standard Panel");

        /// <summary>
        /// Adds facade Panels to one or more Masses named 'envelope'.
        /// </summary>
        /// <param name="model">The model. 
        /// Add elements to the model to have them persisted.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A FacadeOutputs instance containing computed results.</returns>
        public static FacadeByEnvelopeOutputs Execute(Dictionary<string, Model> models, FacadeByEnvelopeInputs input)
        {
            List<Envelope> envelopes;
            List<Level> levels = null;
            var model = new Model();

            var envelopeModel = models[ENVELOPE_MODEL_NAME];
            envelopes = envelopeModel.AllElementsOfType<Envelope>().Where(e => e.Elevation >= 0.0).ToList();
            if (envelopes.Count() == 0)
            {
                throw new Exception("No element of type 'Envelope' could be found in the supplied model.");
            }

            var levelsModel = models[LEVELS_MODEL_NAME];
            levels = levelsModel.AllElementsOfType<Level>().ToList();
            if (levels.Count() == 0)
            {
                throw new Exception("No element of type 'Level' could be found in the supplied model.");
            }
            levels.Sort(new LevelComparer());

            List<Level> envLevels = null;

            var wireframeMaterial = new Material("wireframe", new Color(0.5, 0.5, 0.5, 1.0));

            var panelCount = 0;
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
                                         input.PanelColor);
            }

            var groundFloorEnvelope = envelopes.First(e => e.Elevation == 0.0);
            if (groundFloorEnvelope != null)
            {
                var boundarySegments = groundFloorEnvelope.Profile.Perimeter.Offset(-input.GroundFloorSetback)[0].Segments();
                var groundLevels = levels.Where(l => l.Elevation >= groundFloorEnvelope.Elevation && l.Elevation <= groundFloorEnvelope.Elevation + groundFloorEnvelope.Height).ToList();
                var bottom = groundLevels.First().Elevation;
                var top = groundLevels.Count > 1 ? groundLevels[1].Elevation : groundFloorEnvelope.Elevation + groundFloorEnvelope.Height;
                PanelGroundFloor(bottom, top, boundarySegments, input.PanelWidth, model);
            }

            var output = new FacadeByEnvelopeOutputs(panelCount);
            output.Model = model;
            return output;
        }

        private static int PanelLevels(List<Level> envLevels,
                                       Line[] boundarySegments,
                                       double panelWidth,
                                       double glassLeftRight,
                                       double glassTopBottom,
                                       Model model,
                                       Color panelColor)
        {
            var panelMat = new Material("panel", panelColor, 0.8f, 0.5f);
            var panelCount = 0;

            for (var i = 1; i < envLevels.Count - 1; i++)
            {
                var level1 = envLevels[i];
                var level2 = envLevels[i + 1];

                foreach (var s in boundarySegments)
                {
                    FacadePanel masterPanel = null;
                    Panel masterGlazing = null;

                    var d = s.Direction();
                    var bottomSegments = DivideByLengthFromCenter(s, panelWidth);

                    try
                    {
                        for (var j = 0; j < bottomSegments.Count(); j++)
                        {
                            var bs = bottomSegments[j];
                            var t = new Transform(bs.Start + new Vector3(0, 0, level1.Elevation), d, d.Cross(Vector3.ZAxis));
                            var l = bs.Length();

                            // If the segment width is within Epsilon of 
                            // the input panel width, then create a
                            // panel with glazing.
                            if (Math.Abs(l - panelWidth) < Vector3.EPSILON)
                            {
                                if (masterPanel == null)
                                {
                                    // Create a master panel for each level.
                                    // This panel will be instanced at every location.
                                    CreateFacadePanel($"FP_{i}",
                                                            panelWidth,
                                                            level2.Elevation - level1.Elevation,
                                                            glassLeftRight,
                                                            glassTopBottom,
                                                            0.1,
                                                            panelWidth,
                                                            panelMat,
                                                            t,
                                                            out masterPanel,
                                                            out masterGlazing);
                                    model.AddElement(masterPanel);
                                    model.AddElement(masterGlazing);
                                }

                                // Create a panel instance.
                                var panelInstance = masterPanel.CreateInstance(t, $"FP_{i}_{j}");
                                model.AddElement(panelInstance, false);
                                var glazingInstance = masterGlazing.CreateInstance(t, $"FG_{i}_{j}");
                                model.AddElement(glazingInstance, false);

                            }
                            // Otherwise, create a panel with not glazing.
                            else
                            {
                                CreateStandardPanel($"FP_{i}_{j}",
                                                    l,
                                                    level2.Elevation - level1.Elevation,
                                                    0.1,
                                                    t,
                                                    panelMat,
                                                    out FacadePanel panel);
                                model.AddElement(panel);
                            }
                            panelCount++;
                        }

                        if (i == envLevels.Count - 2)
                        {
                            var parapet = new StandardWall(new Line(new Vector3(s.Start.X, s.Start.Y, level2.Elevation), new Vector3(s.End.X, s.End.Y, level2.Elevation)), 0.1, 0.9, panelMat);
                            model.AddElement(parapet);
                        }
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
            foreach (var segment in boundarySegments)
            {
                var u = new Grid1d(segment);
                var v = new Grid1d(new Line(segment.Start, new Vector3(segment.Start.X, segment.Start.Y, segment.Start.Z + topElevation)));
                var grid2d = new Grid2d(u, v);
                grid2d.U.DivideByFixedLength(1.5);
                grid2d.V.DivideByCount(2);
                foreach (var sep in grid2d.GetCellSeparators(GridDirection.U))
                {
                    var mullion = new Beam((Line)sep, Polygon.Rectangle(0.05, 0.05), BuiltInMaterials.Black);
                    model.AddElement(mullion);
                }
                foreach (var sep in grid2d.GetCellSeparators(GridDirection.V))
                {
                    var mullion = new Beam((Line)sep, Polygon.Rectangle(0.05, 0.05), BuiltInMaterials.Black);
                    model.AddElement(mullion);
                }
                var panel = new Panel(new Polygon(new[]{
                    segment.Start, segment.End , new Vector3(segment.End.X, segment.End.Y, segment.End.Z + topElevation), new Vector3(segment.Start.X, segment.Start.Y, segment.Start.Z + topElevation)
                }), BuiltInMaterials.Glass);
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
            // Console.WriteLine($"The line {l} units long will create {divs} panels of {d} width");
            var span = divs * d;
            var halfSpan = span / 2;
            var mid = line.PointAt(0.5);
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

        private static ModelCurve CreateFacadePanelWireframe(string name,
                                                        double width,
                                                        double height,
                                                        Transform lowerLeft,
                                                        Model model,
                                                        Material material)
        {
            var ll = new Vector3(0, 0, 0);
            var lr = new Vector3(width, 0, 0);
            var ur = new Vector3(width, height, 0);
            var ul = new Vector3(0, height, 0);

            var p = new Polygon(new[] { ll, lr, ur, ul });
            var mc = new ModelCurve(p, material, lowerLeft);
            model.AddElement(mc);

            return mc;
        }

        private static void CreateStandardPanel(string name,
                                                        double width,
                                                        double height,
                                                        double thickness,
                                                        Transform lowerLeft,
                                                        Material material,
                                                        out FacadePanel facadePanel)
        {
            var a = new Vector3(0, 0, 0);
            var b = new Vector3(width, 0, 0);
            var c = new Vector3(width, height, 0);
            var d = new Vector3(0, height, 0);

            var profile = new Profile(new Polygon(new[] { a, b, c, d }.Shrink(0.01)));
            var solidOps = new List<SolidOperation>() { new Extrude(profile, thickness, Vector3.ZAxis, false) };
            var representation = new Representation(solidOps);
            facadePanel = new FacadePanel(thickness, lowerLeft, material, representation, false, Guid.NewGuid(), name);
        }

        private static void CreateFacadePanel(string name,
                                                        double width,
                                                        double height,
                                                        double leftRightInset,
                                                        double topBottomInset,
                                                        double thickness,
                                                        double defaultWidth,
                                                        Material material,
                                                        Transform lowerLeft,
                                                        out FacadePanel facadePanel,
                                                        out Panel glazing)
        {
            var a = new Vector3(0, 0, 0);
            var b = new Vector3(width, 0, 0);
            var c = new Vector3(width, height, 0);
            var d = new Vector3(0, height, 0);

            var a1 = new Vector3(leftRightInset, topBottomInset, 0);
            var b1 = new Vector3(width - leftRightInset, topBottomInset, 0);
            var c1 = new Vector3(width - leftRightInset, height - topBottomInset, 0);
            var d1 = new Vector3(leftRightInset, height - topBottomInset, 0);
            var inner = new Polygon(new[] { d1, c1, b1, a1 });
            var profile = new Profile(new Polygon(new[] { a, b, c, d }.Shrink(0.01)), inner);
            glazing = new Panel(inner, _glazing, lowerLeft, isElementDefinition: true);


            var solidOps = new List<SolidOperation>() { new Extrude(profile, thickness, Vector3.ZAxis, false) };
            var representation = new Representation(solidOps);
            facadePanel = new FacadePanel(thickness, lowerLeft, material, representation, true, Guid.NewGuid(), name);
        }
    }
}