using Elements;
using Elements.Geometry;
using Elements.Spatial;
using System.Collections.Generic;
using System;
using System.Linq;
using Newtonsoft.Json;

namespace CustomGrids
{
    class Debug
    {
        public static Material LightweightBlueMaterial = new Material("Blue", new Color(0, 0.5, 1, 0.5));
        public static Material MagentaMaterial = new Material("Magenta", new Color(1, 0, 0.75, 0.5));

        private static Random random = new Random();

        public static void DrawGrid(Model model, Grid2d grid, List<Vector3> uPoints, List<Vector3> vPoints)
        {
            foreach (var cell in grid.GetCells())
            {
                var polygon = (Polygon)cell.GetCellGeometry();
                foreach (var vertex in polygon.Vertices)
                {
                    Debug.DrawPoint(model, vertex, MagentaMaterial);
                }
                var color = RandomExtensions.NextColor(random);
                foreach (var cellPiece in cell.GetTrimmedCellGeometry())
                {
                    var material = new Material(color.ToString(), new Color(color.Red, color.Green, color.Blue, 0.5), unlit: true);
                    var poly = (Polygon)cellPiece;
                    if (poly.Vertices.Count >= 3)
                    {
                        model.AddElement(new Panel(poly, material: material));
                    }
                }
            }

            foreach (var pt in vPoints)
            {
                Debug.DrawPoint(model, pt, MagentaMaterial);
            }
            foreach (var pt in uPoints)
            {
                Debug.DrawPoint(model, pt, MagentaMaterial);
            }
        }

        public static void DrawPoint(Model model, Vector3 location, Material material)
        {
            var transform = new Transform(location);
            model.AddElement(new Panel(new Circle(Vector3.EPSILON * 2).ToPolygon(), material, transform));
            model.AddElement(new Panel(new Circle(0.003).ToPolygon(), material, transform));
        }

    }
}