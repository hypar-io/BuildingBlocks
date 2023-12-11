using Elements;
using Elements.Geometry;
using Elements.Search;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SketchGrids
{
    public static class SketchGrids
    {
        /// <summary>
        /// The SketchGrids function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A SketchGridsOutputs instance containing computed results and the model with any new elements.</returns>
        public static SketchGridsOutputs Execute(Dictionary<string, Model> inputModels, SketchGridsInputs input)
        {
            var output = new SketchGridsOutputs();
            var edgeDisplaySettings = new EdgeDisplaySettings()
            {
                WidthMode = EdgeDisplayWidthMode.ScreenUnits,
                LineWidth = 1,
                DashSize = 40,
                // DashMode = EdgeDisplayDashMode.ScreenUnits
            };

            var gridMaterial = new Material("Grid Line", Colors.Darkgray)
            {
                EdgeDisplaySettings = edgeDisplaySettings
            };

            var gridLines = new List<GridLine>();

            if (inputModels.ContainsKey("Conceptual Mass"))
            {
                CreateGridLinesForConceptualMass(inputModels, input, gridMaterial, gridLines);
            }

            gridLines = input.Overrides.GridLines.CreateElements(input.Overrides.Additions.GridLines, input.Overrides.Removals.GridLines,
            (add) =>
            {
                var gridLine = new GridLine()
                {
                    Curve = add.Value.Curve,
                    Material = gridMaterial,
                    Name = add.Value.Name ?? "XXX"
                };
                gridLine.AdditionalProperties["Creation Id"] = add.Id;
                return gridLine;
            }, (gl, identity) =>
            {
                return identity.Curve != null && ((Line)gl.Curve).IsAlmostEqualTo(identity.Curve, false);
            }, (gl, @override) =>
            {
                gl.Curve = @override.Value.Curve;
                gl.Name = @override.Value.Name ?? "XXX";
                return gl;
            }, gridLines);

            output.Model.AddElements(gridLines);

            var texts = new List<(Vector3 location, Vector3 facingDirection, Vector3 lineDirection, string text, Color? color)>();
            foreach (var gridLine in gridLines)
            {
                var t = gridLine.GetCircleTransform();
                texts.Add(((t.Origin.X, t.Origin.Y), Vector3.ZAxis, Vector3.XAxis, $"{gridLine.Name ?? "XXX"}", Colors.Black));
            }
            output.Model.AddElement(new ModelText(texts, FontSize.PT60, scale: 50));

            var network = Network<GridLine>.FromSegmentableItems(gridLines, (gl) => { return (Line)gl.Curve; }, out var allNodeLocations, out _);

            var r = new Random(); // testY

            var closedRegions = network.FindAllClosedRegions(allNodeLocations).SkipLast(1);
            foreach (var cr in closedRegions)
            {
                try
                {
                    var p = new Panel(new Polygon(cr.Select(r => allNodeLocations[r]).ToList()), r.NextMaterial());
                    output.Model.AddElement(p);
                }
                catch
                {
                    continue;
                }
            }

            return output;
        }

        private static void CreateGridLinesForConceptualMass(Dictionary<string, Model> inputModels,
                                                             SketchGridsInputs input,
                                                             Material gridMaterial,
                                                             List<GridLine> gridLines)
        {
            var conceptualMasses = inputModels["Conceptual Mass"].AllElementsOfType<ConceptualMass>();

            var gridIndex = 0;
            var allVoids = new List<Polygon>();

            foreach (var conceptualMass in conceptualMasses)
            {
                var offsetHull = conceptualMass.Profile.Perimeter.Offset(1)[0];

                conceptualMass.UpdateRepresentations();
                conceptualMass.UpdateBoundsAndComputeSolid();

                if (conceptualMass.Skeleton != null)
                {
                    // Create grids for the skeleton
                    if (input.AddSkeletonGrids)
                    {
                        foreach (var edge in conceptualMass.Skeleton)
                        {
                            var newGridLine = edge.ExtendTo(offsetHull);
                            CheckAndCreateGridline(newGridLine, gridLines, ref gridIndex, gridMaterial, input);
                        }
                    }

                    var allPerimeters = new List<Polygon>();

                    if (conceptualMass.Profile != null && conceptualMass.Profile.Voids != null)
                    {
                        allPerimeters.Add(conceptualMass.Profile.Perimeter);
                        foreach (var profileVoid in conceptualMass.Profile.Voids)
                        {
                            allVoids.Add(profileVoid);
                        }
                    }

                    // Offset perimeters to the inside.
                    foreach (var cutPerimeter in allPerimeters)
                    {
                        foreach (var segment in cutPerimeter.Offset(-input.OffsetDistanceFromConceptualMass)[0].Segments())
                        {
                            var newGridLine = segment.ExtendTo(offsetHull);
                            CheckAndCreateGridline(newGridLine, gridLines, ref gridIndex, gridMaterial, input);
                        }
                    }

                    // TODO: Are there ever holes in a skeleton mass?
                    // Offset holes to the outside.
                    // foreach (var cutVoid in allVoids)
                    // {
                    //     foreach (var segment in cutVoid.Offset(-structuralOffset)[0].Segments())
                    //     {
                    //         var newGridLine = segment.ExtendTo(offsetHull);
                    //         CheckAndCreateGridline(newGridLine, gridLines, ref gridIndex, gridMaterial);
                    //     }
                    // }
                }
                else
                {
                    foreach (var segment in conceptualMass.Profile.Perimeter.Offset(-input.OffsetDistanceFromConceptualMass)[0].Segments())
                    {
                        var newGridLine = segment.ExtendTo(offsetHull);
                        CheckAndCreateGridline(newGridLine, gridLines, ref gridIndex, gridMaterial, input);
                    }

                    if (conceptualMass.Profile.Voids != null)
                    {
                        allVoids.AddRange(conceptualMass.Profile.Voids);
                    }
                }
            }

            // We process the voids last so that we have all grids
            // created from the faces of the conceptual masses to respond
            // to. This will create the shortest spanning edge grids for holes.
            foreach (var profileVoid in allVoids)
            {
                var holeGridLines = new List<Line>();
                var offsetHull = profileVoid.Offset(input.OffsetDistanceFromConceptualMass)[0];

                foreach (var segment in offsetHull.Segments())
                {
                    holeGridLines.Add(segment);
                }

                foreach (var holeGridLine in holeGridLines)
                {
                    CheckAndCreateGridline(holeGridLine, gridLines, ref gridIndex, gridMaterial, input);
                }
            }
        }

        private static void CheckAndCreateGridline(Line newGridLine,
                                                   List<GridLine> gridLines,
                                                   ref int gridIndex,
                                                   Material gridMaterial,
                                                   SketchGridsInputs input)
        {
            if (gridLines.Any(gl => (gl.Curve as Line).IsAlmostEqualTo(newGridLine, false)))
            {
                return;
            }

            GridLinesOverride matchingEditOverride = null;
            if (input.Overrides?.GridLines != null)
            {
                matchingEditOverride = input.Overrides.GridLines.FirstOrDefault((glo) => glo.Identity.Curve != null && glo.Identity.Curve.IsAlmostEqualTo(newGridLine, false));
                if (matchingEditOverride != null)
                {
                    Console.WriteLine($"Found matching edit override for grid line {newGridLine}.");
                    newGridLine = matchingEditOverride.Value.Curve;
                }
            }

            var gl = new GridLine()
            {
                Curve = newGridLine,
                Name = gridIndex.ToString(),
                Material = gridMaterial
            };
            gridLines.Add(gl);
            gridIndex++;

            if (matchingEditOverride != null)
            {
                Identity.AddOverrideIdentity(gl, matchingEditOverride);
            }
        }
    }
}
