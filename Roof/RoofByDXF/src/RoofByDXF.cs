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
        /// Generates a Roof from a DXF Polyline and supplied elevation and thickness values.
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
                if (entity.EntityType != DxfEntityType.LwPolyline)
                {
                    continue;
                }
                var pline = (DxfLwPolyline)entity;
                if (pline.IsClosed == false)
                {
                    continue;
                }
                var vertices = pline.Vertices.ToList();
                var verts = new List<Vector3>();
                vertices.ForEach(v => verts.Add(new Vector3(v.X, v.Y)));
                polygons.Add(new Polygon(verts));
            }
            if (polygons.Count == 0)
            {
                throw new ArgumentException("No LWPolylines found in DXF.");
            }
            polygons = polygons.OrderByDescending(p => Math.Abs(p.Area())).ToList();
            var polygon = polygons.First().IsClockWise() ? polygons.First().Reversed() : polygons.First();
            polygons = polygons.Skip(1).ToList();
            polygons.ForEach(p => p = p.IsClockWise() ? p : p.Reversed());
            var polys = new List<Polygon>();
            foreach (var poly in polygons)
            {
                if (!polygon.Contains(poly))
                {
                    continue;
                }
                if (!poly.IsClockWise())
                {
                    polys.Add(poly.Reversed());
                    continue;
                }
                polys.Add(poly);
            }
            polys.Insert(0, polygon);
            var shape = new Profile(polys);
            var extrude = new Elements.Geometry.Solids.Extrude(shape, input.RoofThickness, Vector3.ZAxis, false);
            var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
            var roofMatl = BuiltInMaterials.Concrete;
            var roof = new Roof(shape,
                                input.RoofElevation,
                                input.RoofThickness,
                                shape.Area(),
                                new Transform(0.0, 0.0, input.RoofElevation - input.RoofThickness),
                                roofMatl,
                                geomRep,
                                false,
                                Guid.NewGuid(), "");
            var output = new RoofByDXFOutputs(shape.Area());
            output.Model.AddElement(roof);
            return output;
        }
    }
}