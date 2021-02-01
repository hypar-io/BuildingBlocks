using Elements;
using Elements.Geometry;
using Elements.Spatial;
using System.Collections.Generic;
using System;
using System.Linq;

namespace CustomGrids
{
    public static class CustomGrids
    {
        private static double MinCircleRadius = 0.5;
        private static double CircleRadius = 1;

        private static Material GridlineMaterial = new Material("Gridline", Colors.Red);

        /// <summary>
        ///
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A CustomGridsOutputs instance containing computed results and the model with any new elements.</returns>
        public static CustomGridsOutputs Execute(Dictionary<string, Model> inputModels, CustomGridsInputs input)
        {
            var output = new CustomGridsOutputs();
            foreach (var gridArea in input.GridAreas)
            {
                // output.Model.AddElement(new ModelCurve(gridArea.Polygon, name: "Outline"));
                // var bounds = gridArea.Polygon.Bounds();

                if (gridArea.Orientation.Vertices.Count < 3)
                {
                    throw new Exception("Your grid orientation must have 3 vertices");
                }

                var origin = gridArea.Orientation.Vertices[0];
                var uDirection = (gridArea.Orientation.Vertices[1] - gridArea.Orientation.Vertices[0]).Unitized();
                var vDirection = (gridArea.Orientation.Vertices[2] - gridArea.Orientation.Vertices[0]).Unitized();

                var u = gridArea.U;
                var v = new U(gridArea.V.Name, gridArea.V.Spacing);

                var uMin = u.Spacing.Count > 0 ? u.Spacing.Min() : 10;
                var vMin = v.Spacing.Count > 0 ? v.Spacing.Min() : 10;

                CircleRadius = Math.Max(MinCircleRadius, Math.Max(uMin, vMin) * 0.3);

                Drawlines(output.Model, origin, uDirection, vDirection, u, Math.Max(v.Spacing.Sum(), 1));
                Drawlines(output.Model, origin, vDirection, uDirection, v, Math.Max(u.Spacing.Sum(), 1));
            }
            return output;
        }

        private static void DrawLine(Model model, Line line, Material material, string namingPattern, int idx)
        {
            var name = $"{namingPattern}-{idx}";

            if (namingPattern == "{A}")
            {
                name = ((char)(idx + 65)).ToString(); // nth letter of alphabet
            }
            if (namingPattern == "{1}")
            {
                name = (idx + 1).ToString();
            }
            var circleCenter = line.Start - (line.End - line.Start).Unitized() * CircleRadius;

            model.AddElement(new GridLine(new Polyline(new List<Vector3>() { line.Start, line.End }), Guid.NewGuid(), name));
            model.AddElement(new ModelCurve(line, material, name: name));
            model.AddElement(new ModelCurve(new Circle(circleCenter, CircleRadius), GridlineMaterial));
            model.AddElement(new LabelDot(circleCenter, name));
        }

        private static void Drawlines(Model model, Vector3 origin, Vector3 gridDir, Vector3 lineDir, U u, double length)
        {
            var curPoint = origin;
            var sectionIdx = 0;

            var overflow = CircleRadius * 4;

            void drawNext()
            {
                var line = new Line(curPoint - (lineDir * overflow), curPoint + (lineDir * (length + overflow)));
                DrawLine(model, line, GridlineMaterial, u.Name, sectionIdx);
            };

            foreach (var spacing in u.Spacing)
            {
                var endPoint = curPoint + (gridDir * spacing);
                drawNext();
                curPoint = curPoint + gridDir * spacing;
                sectionIdx += 1;
            }
            drawNext();
        }

        // private static void Drawlines(Model model, Vector3 origin, Vector3 gridDir, Vector3 lineDir, U u, BBox3 bounds)
        // {
        // var curPoint = origin;
        // var material = new Material("U", Colors.Red);
        // var sectionIdx = 0;
        // foreach (var section in u.Sections)
        // {

        //     if (section.Spacing == 0)
        //     {
        //         throw new Exception("Section spacing must be a non-zero value");
        //     }

        //     var sectionLength = section.DistanceFromOrigin != 0 ? section.DistanceFromOrigin : section.Spacing * section.Cells;
        //     var diagonalDist = bounds.Max.DistanceTo(bounds.Min);


        //     if (sectionLength < 0)
        //     {
        //         throw new Exception("We currently only support postiive distance from origin for sections");
        //     }

        //     if (sectionLength == 0)
        //     {
        //         return;
        //     }

        //     var endPoint = curPoint + (gridDir * sectionLength);
        //     // var grid = new Grid1d(sectionLength);
        //     var curDist = 0.0;

        //     while (curDist < sectionLength)
        //     {

        //         var line = new Line(curPoint - (lineDir * diagonalDist / 10), curPoint + (lineDir * diagonalDist / 2));
        //         DrawLine(model, line, material, u.Name, (int)(curDist / section.Spacing));

        //         curDist += section.Spacing;
        //         curPoint = curPoint + gridDir * section.Spacing;
        //     }

        //     if (sectionIdx == u.Sections.Count - 1)
        //     {
        //         var line = new Line(curPoint - (lineDir * diagonalDist / 10), curPoint + (lineDir * diagonalDist / 2));
        //         DrawLine(model, line, material, u.Name, (int)(curDist / section.Spacing));
        //     }

        //     sectionIdx += 1;
        // }
        // }



    }




}