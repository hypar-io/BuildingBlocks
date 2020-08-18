using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;
using Elements;
using Elements.Geometry;

namespace RoofByDXF
{
      public static class RoofByDXF
    {
        /// <summary>
        /// The RoofByDXF function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A RoofByDXFOutputs instance containing computed results and the model with any new elements.</returns>
        public static RoofByDXFOutputs Execute(Dictionary<string, Model> inputModels, RoofByDXFInputs input)
        {
            DxfFile dxfFile;
            using (FileStream fs = new FileStream(input.DXF.LocalFilePath, FileMode.Open))
            {
                dxfFile = DxfFile.Load(fs);
            }
            var polygons = new List<Polygon>();
            foreach (DxfEntity entity in dxfFile.Entities)
            {
                if (entity.EntityType == DxfEntityType.LwPolyline)
                {
                    var pline = (DxfLwPolyline)entity;
                    if (pline.IsClosed == false)
                    {
                        continue;
                    }
                    var vertices = pline.Vertices;
                    var verts = new List<Vector3>();
                    foreach (var vtx in vertices)
                    {
                        verts.Add(new Vector3(vtx.X, vtx.Y));
                    }
                    if ((verts[1].X - verts[0].X) * (verts[2].Y - verts[0].Y) -
                        (verts[2].X - verts[0].X) * (verts[1].Y - verts[0].Y) < 0)
                    {
                        verts.Reverse();
                    }
                    polygons.Add(new Polygon(verts.ToArray()));
                }
            }
            Polygon perimeter = null;
            if (polygons.Count > 0)
            {
                perimeter = polygons.First();
            }
            else
            {
                perimeter = Polygon.Rectangle(50.0, 50.0);
            }
            var extrude = new Elements.Geometry.Solids.Extrude(perimeter, input.RoofThickness, Vector3.ZAxis, false);
            var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
            var roofMatl = BuiltInMaterials.Concrete;
            var roof = new Roof(perimeter,
                                input.RoofElevation,
                                input.RoofThickness,
                                perimeter.Area(),
                                new Transform(0.0, 0.0, input.RoofElevation - input.RoofThickness),
                                roofMatl,
                                geomRep,
                                false,
                                Guid.NewGuid(), "");
            //if (polygons.Count > 1)
            //{
            //    polygons = polygons.OrderByDescending(p => p.Area()).ToList();
            //    foreach (var polygon in polygons.Skip(1))
            //    {
            //        if (perimeter.Covers(polygon))
            //        {
            //            floor.Openings.Add(new Opening(polygon));
            //        }
            //    }
            //}
            var output = new RoofByDXFOutputs(perimeter.Area());
            output.Model.AddElement(roof);
            return output;
        }
    }
}