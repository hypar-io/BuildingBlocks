using Elements;
using Elements.Annotations;
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
                // DashMode = EdgeDisplayDashMode.ScreenUnits,
                // DashSize = 10,
                // GapSize = 2
            };

            var gridMaterial = new Material("Grid Line", Colors.Darkgray)
            {
                EdgeDisplaySettings = edgeDisplaySettings
            };

            var gridLines = input.Overrides.GridLines.CreateElements(input.Overrides.Additions.GridLines, input.Overrides.Removals.GridLines, (addition) =>
            {
                var gridLine = new GridLine()
                {
                    Curve = addition.Value.Curve,
                    Material = gridMaterial
                };
                gridLine.AdditionalProperties["Creation Id"] = addition.Id;
                return gridLine;
            }, (gl, gli) =>
            {
                return Guid.Parse((string)gl.AdditionalProperties["Creation Id"]) == gli.CreationId;
            }, (gl, glo) =>
            {
                gl.Curve = glo.Value.Curve;
                return gl;
            });

            output.Model.AddElements(gridLines);

            if (inputModels.ContainsKey("Conceptual Mass"))
            {
                var conceptualMasses = inputModels["Conceptual Mass"].AllElementsOfType<ConceptualMass>();

                // Find the outer bounds of all the conceptual masses.
                // This will be used to extend grids to cover the whole mass.
                var hull = ConvexHull.FromPolylines(conceptualMasses.Select(m => m.Profile.Perimeter));
                var offsetHull = hull.Offset(2)[0];

                // TODO: Create an input for structural offset
                var allGridLines = new List<Line>();

                var gridIndex = 0;

                foreach (var conceptualMass in conceptualMasses)
                {
                    conceptualMass.UpdateRepresentations();
                    conceptualMass.UpdateBoundsAndComputeSolid();

                    if (conceptualMass.Skeleton != null)
                    {
                        // Create grids for the skeleton
                        // foreach (var edge in conceptualMass.Skeleton)
                        // {
                        //     CheckAndCreateGridline(edge, gridLines, ref gridIndex, gridMaterial);
                        // }

                        var allPerimeters = new List<Polygon>();
                        var allVoids = new List<Polygon>();
                        var intersectionPlane = new Plane(new Vector3(0, 0, conceptualMass.Transform.Origin.Z), Vector3.ZAxis);
                        foreach (var so in conceptualMass.Representation.SolidOperations)
                        {
                            if (so.Solid.Intersects(intersectionPlane, out var result))
                            {
                                if (so.IsVoid)
                                {
                                    allVoids.AddRange(result);
                                }
                                else
                                {
                                    allPerimeters.AddRange(result);
                                }
                            }
                        }

                        // Offset perimeters to the inside.
                        foreach (var cutPerimeter in allPerimeters)
                        {
                            foreach (var segment in cutPerimeter.Offset(-input.OffsetDistanceFromConceptualMass)[0].Segments())
                            {
                                var newGridLine = segment.ExtendTo(offsetHull);
                                CheckAndCreateGridline(newGridLine, gridLines, ref gridIndex, gridMaterial);
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
                            CheckAndCreateGridline(newGridLine, gridLines, ref gridIndex, gridMaterial);
                        }

                        if (conceptualMass.Profile.Voids != null)
                        {
                            foreach (var profileVoid in conceptualMass.Profile.Voids)
                            {
                                foreach (var segment in profileVoid.Offset(input.OffsetDistanceFromConceptualMass)[0].Segments())
                                {
                                    var newGridLine = segment.ExtendTo(offsetHull);
                                    CheckAndCreateGridline(newGridLine, gridLines, ref gridIndex, gridMaterial);
                                }
                            }
                        }
                    }
                }
            }

            output.Model.AddElements(gridLines);

            var network = Network<GridLine>.FromSegmentableItems(gridLines, (gl) => { return (Line)gl.Curve; }, out var allNodeLocations, out _);

            var r = new Random();

            var closedRegions = network.FindAllClosedRegions(allNodeLocations);
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

            var bbox = new BBox3(allNodeLocations);

            var vectorEqualityComparer = new VectorEqualityComparer();
            var gridLineGroups = gridLines.GroupBy(gl => (gl.Curve as Line).Direction().Unitized(), vectorEqualityComparer);
            var directionComparer = new DirectionComparer(bbox.Min);

            foreach (var gridLineGroup in gridLineGroups)
            {
                var grids = gridLineGroup.ToList();
                var orderedGrids = grids.OrderByDescending(gl => gl.Curve.PointAt(0), directionComparer).ToList();
                for (var i = 0; i < orderedGrids.Count - 1; i++)
                {
                    var dim = new AlignedDimension(orderedGrids[i].Curve.PointAt(0), orderedGrids[i + 1].Curve.PointAt(0), 0);
                    output.Model.AddElement(dim);
                    // output.Model.AddElements(dim.ToModelArrowsAndText(Colors.Black));
                }
            }

            return output;
        }

        private static void CheckAndCreateGridline(Line newGridLine, List<GridLine> gridLines, ref int gridIndex, Material gridMaterial)
        {
            var directionReference = new Vector3(1, -1);

            if (gridLines.Any(gl => (gl.Curve as Line).IsAlmostEqualTo(newGridLine, false)))
            {
                return;
            }
            var d = newGridLine.Direction();
            var dot = directionReference.Dot(d);

            var gl = new GridLine()
            {
                Curve = dot >= 0 ? newGridLine : new Line(newGridLine.End, newGridLine.Start),
                Name = gridIndex.ToString(),
                Material = gridMaterial
            };
            gridLines.Add(gl);
            gridIndex++;
        }
    }
}
