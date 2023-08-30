using Elements;
using Elements.Geometry;
using Elements.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FacadeGridByColumnGrid
{
    public static class FacadeGridByColumnGrid
    {
        /// <summary>
        /// The FacadeGridByColumnGrid function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A FacadeGridByColumnGridOutputs instance containing computed results and the model with any new elements.</returns>
        public static FacadeGridByColumnGridOutputs Execute(Dictionary<string, Model> inputModels, FacadeGridByColumnGridInputs input)
        {
            Random = new Random(5);
            var outputModel = new Model();
            var basePanels = new Dictionary<string, Panel>();
            var levelEnvelopes = inputModels["Levels"].AllElementsOfType<LevelVolume>();
            var columnGridLines = inputModels["Column Grid"].AllElementsOfType<GridLine>();
            var defaultSolidMaterial = new Material(new Color(1, 1, 1, 0.7), 0.1, 0.1, false, null, true, Guid.NewGuid(), "White Transparent");
            var invisibleMaterial = new Material(new Color(1, 1, 1, 0.0), 0.1, 0.1, true, null, true, Guid.NewGuid(), "Invisible");
            if (levelEnvelopes.Count() == 0)
            {
                throw new Exception("There were no Level Volumes found in the model. Try using Simple Levels By Envelope to generate levels instead.");
            }
            var facadeGridCells = new List<Panel>();
            foreach (var levelVolume in levelEnvelopes)
            {
                var profile = levelVolume.Profile;
                if (input.OffsetFromFacade > 0)
                {
                    var perimeter = profile.Perimeter.Offset(input.OffsetFromFacade).First();
                    var voids = profile.Voids.SelectMany(v => v.Offset(input.OffsetFromFacade)).ToList();
                    profile = new Profile(perimeter, voids, Guid.NewGuid(), "OffsetProfile");
                }
                var segments = profile.Perimeter.Segments().Union(profile.Voids.SelectMany(v => v.Segments()));
                foreach (var segment in segments)
                {
                    var edge = new LevelEdge(new List<string>(), levelVolume.Id.ToString(), segment, Guid.NewGuid(), null);
                    var uGrid = new Grid1d(segment);
                    // subdivide U grid
                    foreach (var gl in columnGridLines)
                    {
                        foreach (var glSegment in gl.Geometry.Segments())
                        {
                            var dir = glSegment.Direction();
                            //only consider grid lines that are somewhat perpendicular to the segment
                            if (Math.Abs(dir.Dot(segment.Direction())) < 0.8 && segment.Intersects(glSegment, out var intersection))
                            {
                                if (input.MinPanelWidth == 0 ||
                                    (intersection.DistanceTo(segment.Start) > input.MinPanelWidth &&
                                    intersection.DistanceTo(segment.End) > input.MinPanelWidth))
                                {
                                    uGrid.SplitAtPoint(intersection);
                                }
                            }

                        }
                    }
                    if (input.MaxPanelWidth > 0)
                    {
                        foreach (var cell in uGrid.GetCells())
                        {
                            if (cell.Domain.Length > input.MaxPanelWidth + 0.001)
                            {
                                cell.DivideByApproximateLength(input.MaxPanelWidth, EvenDivisionMode.RoundUp);
                            }
                        }
                    }

                    foreach (var cell in uGrid.GetCells())
                    {
                        cell.Type = $"{cell.Domain.Length:0.00}";
                    }
                    var vGrid = new Grid1d(new Line(segment.Start, segment.Start + new Vector3(0, 0, levelVolume.Height)));
                    vGrid.Type = $"{levelVolume.Height:0.00}";
                    var grid2d = new Grid2d(uGrid, vGrid);
                    var cells = grid2d.GetCells();
                    foreach (var cell in cells)
                    {
                        if (cell.Type == null)
                        {
                            cell.Type = "Unknown Type: " + Guid.NewGuid();
                        }
                        if (!basePanels.ContainsKey(cell.Type))
                        {
                            var cellBoundary = new Polygon(new Vector3[] {
                                new Vector3(0,0),
                                new Vector3(cell.U.Domain.Length, 0, 0),
                                new Vector3(cell.U.Domain.Length, cell.V.Domain.Length, 0),
                                new Vector3(0, cell.V.Domain.Length, 0),
                            });
                            var cellColorMaterial = default(Material);
                            switch (input.DisplayMode)
                            {
                                case FacadeGridByColumnGridInputsDisplayMode.Color_By_Type:
                                    cellColorMaterial = new Material(cell.Type, NextRandomColor(), 0.0, 0.0, null, true, false);
                                    break;
                                case FacadeGridByColumnGridInputsDisplayMode.Edges_Only:
                                    cellColorMaterial = invisibleMaterial;
                                    break;
                                case FacadeGridByColumnGridInputsDisplayMode.Solid_Color:
                                    cellColorMaterial = defaultSolidMaterial;
                                    break;
                            }
                            var panel = new Panel(cellBoundary as Polygon, cellColorMaterial, levelVolume.Transform, null, true, Guid.NewGuid(), cell.Type);
                            basePanels.Add(cell.Type, panel);
                        }
                        var panelBoundaryInSpace = cell.GetCellGeometry() as Polygon;
                        var panelBottomEdge = panelBoundaryInSpace.Segments()[0];
                        var panelTransform = new Transform(panelBottomEdge.Start, panelBottomEdge.Direction(), panelBottomEdge.Direction().Cross(Vector3.ZAxis));
                        panelTransform.Concatenate(levelVolume.Transform);
                        var panelInstance = basePanels[cell.Type].CreateInstance(panelTransform, cell.Type);
                        var boundary = new ModelCurve(cell.GetCellGeometry() as Polygon, null, levelVolume.Transform, null, false, Guid.NewGuid(), cell.Type);
                        edge.PanelIds.Add(panelInstance.Id.ToString());
                        outputModel.AddElement(panelInstance);
                        outputModel.AddElement(boundary);
                    }
                    outputModel.AddElement(edge);
                }
            }

            var output = new FacadeGridByColumnGridOutputs(basePanels.Count, outputModel.AllElementsOfType<ElementInstance>().Count());
            output.Model = outputModel;
            return output;
        }

        private static Random Random = new Random(5);
        private static Color NextRandomColor()
        {
            return new Color(Random.NextDouble(), Random.NextDouble(), Random.NextDouble(), 1);
        }
    }
}