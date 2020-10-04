using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;
using Elements;
using Elements.Geometry;
using GeometryEx;

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
            var highPoint = input.RoofElevation + input.RoofThickness;
            polygons = polygons.OrderByDescending(p => Math.Abs(p.Area())).ToList();
            var polygon = polygons.First().IsClockWise() ? polygons.First().Reversed() : polygons.First();
            polygon = polygon.TransformedPolygon(new Transform(0.0, 0.0, highPoint));
            var underBoundary = polygon.TransformedPolygon(new Transform(0.0, 0.0, input.RoofThickness * -1.0));
            var ePoints = polygon.Vertices.ToList();
            var uPoints = underBoundary.Vertices.ToList();

            var topSide = polygon.ToMesh(true);
            var underSide = underBoundary.ToMesh(false);

            var sideTriangles = new List<Elements.Geometry.Triangle>();
            for (var i = 0; i < ePoints.Count; i++)
            {
                sideTriangles.Add(new Elements.Geometry.Triangle(new Vertex(ePoints[i]),
                                               new Vertex(uPoints[i]),
                                               new Vertex(uPoints[(i + 1) % uPoints.Count])));
                sideTriangles.Add(new Elements.Geometry.Triangle(new Vertex(ePoints[i]),
                                               new Vertex(uPoints[(i + 1) % uPoints.Count]),
                                               new Vertex(ePoints[(i + 1) % ePoints.Count])));
            }


            // Create an aggregated list of Triangles representing the Roof envelope.
            var envTriangles = new List<Elements.Geometry.Triangle>();
            topSide.Triangles.ForEach(t => envTriangles.Add(t));
            underSide.Triangles.ForEach(t => envTriangles.Add(t));
            sideTriangles.ForEach(t => envTriangles.Add(t));

            // Create an aggregated list of Vertices representing the Roof envelope.
            var enVertices = new List<Vertex>();
            envTriangles.ForEach(t => enVertices.AddRange(t.Vertices));

            // Construct the roof envelope in Elements.Geometry.mesh form.
            var Envelope = new Elements.Geometry.Mesh();
            envTriangles.ForEach(t => Envelope.AddTriangle(t));
            enVertices.ForEach(v => Envelope.AddVertex(v));
            Envelope.ComputeNormals();

            // Construct serializable topside mesh
            var triangles = new List<triangles>();
            var indices = new List<vertices>();
            var tsIV = topSide.ToIndexedVertices();
            tsIV.triangles.ForEach(t => triangles.Add(new triangles(t)));
            tsIV.vertices.ForEach(v => indices.Add(new vertices(v.index, v.isBoundary, v.position)));
            var topside = new Elements.Mesh(triangles, indices);
            

            // Construct serializable underside mesh           
            triangles.Clear();
            indices.Clear();
            var usIV = underSide.ToIndexedVertices();
            usIV.triangles.ForEach(t => triangles.Add(new triangles(t)));
            usIV.vertices.ForEach(v => indices.Add(new vertices(v.index, v.isBoundary, v.position)));
            var underside = new Elements.Mesh(triangles, indices);

            // Construct serializable envelope mesh           
            triangles.Clear();
            indices.Clear();
            var enIV = Envelope.ToIndexedVertices();
            enIV.triangles.ForEach(t => triangles.Add(new triangles(t)));
            enIV.vertices.ForEach(v => indices.Add(new vertices(v.index, v.isBoundary, v.position)));
            var envelope = new Elements.Mesh(triangles, indices);

            var roof =
                new Roof(
                    envelope,
                    topside,
                    underside,
                    underBoundary,
                    input.RoofElevation,
                    highPoint,
                    input.RoofThickness,
                    polygon.Area(),
                    new Transform(),
                    BuiltInMaterials.Concrete,
                    null, false, Guid.NewGuid(), "Roof");
            var output = new RoofByDXFOutputs(polygon.Area());
            output.Model.AddElement(new MeshElement(Envelope, BuiltInMaterials.Concrete));
            output.Model.AddElement(roof);
            return output;
        }
    }
}

//polygons = polygons.Skip(1).ToList();
//polygons.ForEach(p => p = p.IsClockWise() ? p : p.Reversed());
//var polys = new List<Polygon>();
//foreach (var poly in polygons)
//{
//    if (!polygon.Contains(poly))
//    {
//        continue;
//    }
//    if (!poly.IsClockWise())
//    {
//        polys.Add(poly.Reversed());
//        continue;
//    }
//    polys.Add(poly);
//}
//polys.Insert(0, polygon);
//var shape = new Profile(polys);
//var extrude = new Elements.Geometry.Solids.Extrude(shape, input.RoofThickness, Vector3.ZAxis, false);
//var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });