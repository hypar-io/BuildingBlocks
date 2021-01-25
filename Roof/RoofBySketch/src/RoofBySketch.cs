using System;
using System.Linq;
using Elements;
using Elements.Geometry;
using Elements.Geometry.Solids;
using GeometryEx;
using System.Collections.Generic;

namespace RoofBySketch
{
    public static class RoofBySketch
    {
        /// <summary>
        /// Creates a Roof from a supplied Polygon sketch and a supplied elevation.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A RoofBySketchOutputs instance containing computed results and the model with any new elements.</returns>
        public static RoofBySketchOutputs Execute(Dictionary<string, Model> inputModels, RoofBySketchInputs input)
        {
            var envelope = MakeEnvelope();
            var topside = MakeTopside();
            var underside = MakeUnderside();
            var underBoundary = Polygon.Rectangle(20.0, 20.0);
            var elevation = 10.0;
            var highPoint = 15.0;
            var area = 100.0;

            var roof =
                new Roof(
                    envelope,
                    topside,
                    underside,
                    underBoundary,
                    elevation,
                    highPoint,
                    input.Thickness,
                    area,
                    new Transform(),
                    BuiltInMaterials.Concrete,
                    null, false, Guid.NewGuid(), "Roof");
            var output = new RoofBySketchOutputs(area);


            output.Model.AddElement(new MeshElement(envelope, BuiltInMaterials.Concrete));
            output.Model.AddElement(roof);
            return output;
        }

        static Mesh MakeEnvelope()
        {
            var triangles = new List<Elements.Geometry.Triangle>
            {
                new Elements.Geometry.Triangle(new Vertex(new Vector3(2.0, 2.0, 12.0)),
                             new Vertex(new Vector3(9.0, 2.0, 12.0)),
                             new Vertex(new Vector3(5.0, 4.0, 10.0))),
                new Elements.Geometry.Triangle(new Vertex(new Vector3(5.0, 4.0, 10.0)),
                             new Vertex(new Vector3(9.0, 2.0, 12.0)),
                             new Vertex(new Vector3(5.0, 11.0, 10.0))),
                new Elements.Geometry.Triangle(new Vertex(new Vector3(5.0, 11.0, 10.0)),
                             new Vertex(new Vector3(9.0, 2.0, 12.0)),
                             new Vertex(new Vector3(9.0, 13.0, 12.0))),
                new Elements.Geometry.Triangle(new Vertex(new Vector3(9.0, 13.0, 12.0)),
                             new Vertex(new Vector3(2.0, 13.0, 12.0)),
                             new Vertex(new Vector3(5.0, 11.0, 10.0))),
                new Elements.Geometry.Triangle(new Vertex(new Vector3(2.0, 13.0, 12.0)),
                             new Vertex(new Vector3(2.0, 2.0, 12.0)),
                             new Vertex(new Vector3(5.0, 4.0, 10.0))),
                new Elements.Geometry.Triangle(new Vertex(new Vector3(5.0, 4.0, 10.0)),
                             new Vertex(new Vector3(5.0, 11.0, 10.0)),
                             new Vertex(new Vector3(2.0, 13.0, 12.0)))
            };
            var mesh = new Mesh();
            triangles.ForEach(t => mesh.AddTriangle(t));
            return mesh;
        }

        static Mesh MakeTopside()
        {
            var triangles = new List<Elements.Geometry.Triangle>
            {
                new Elements.Geometry.Triangle(new Vertex(new Vector3(5.0, 4.0, 10.0)),
                             new Vertex(new Vector3(9.0, 2.0, 12.0)),
                             new Vertex(new Vector3(5.0, 11.0, 10.0))),
                new Elements.Geometry.Triangle(new Vertex(new Vector3(5.0, 11.0, 10.0)),
                             new Vertex(new Vector3(9.0, 2.0, 12.0)),
                             new Vertex(new Vector3(9.0, 13.0, 12.0))),
                new Elements.Geometry.Triangle(new Vertex(new Vector3(9.0, 13.0, 12.0)),
                             new Vertex(new Vector3(2.0, 13.0, 12.0)),
                             new Vertex(new Vector3(5.0, 11.0, 10.0))),
                new Elements.Geometry.Triangle(new Vertex(new Vector3(2.0, 13.0, 12.0)),
                             new Vertex(new Vector3(2.0, 2.0, 12.0)),
                             new Vertex(new Vector3(5.0, 4.0, 10.0))),
                new Elements.Geometry.Triangle(new Vertex(new Vector3(5.0, 4.0, 10.0)),
                             new Vertex(new Vector3(5.0, 11.0, 10.0)),
                             new Vertex(new Vector3(2.0, 13.0, 12.0)))
            };
            var mesh = new Mesh();
            triangles.ForEach(t => mesh.AddTriangle(t));
            return mesh;
        }

        static Mesh MakeUnderside()
        {
            var triangles = new List<Elements.Geometry.Triangle>
            {
                new Elements.Geometry.Triangle(new Vertex(new Vector3(5.0, 11.0, 10.0)),
                             new Vertex(new Vector3(9.0, 2.0, 12.0)),
                             new Vertex(new Vector3(9.0, 13.0, 12.0))),
                new Elements.Geometry.Triangle(new Vertex(new Vector3(9.0, 13.0, 12.0)),
                             new Vertex(new Vector3(2.0, 13.0, 12.0)),
                             new Vertex(new Vector3(5.0, 11.0, 10.0))),
                new Elements.Geometry.Triangle(new Vertex(new Vector3(2.0, 13.0, 12.0)),
                             new Vertex(new Vector3(2.0, 2.0, 12.0)),
                             new Vertex(new Vector3(5.0, 4.0, 10.0))),
                new Elements.Geometry.Triangle(new Vertex(new Vector3(5.0, 4.0, 10.0)),
                             new Vertex(new Vector3(5.0, 11.0, 10.0)),
                             new Vertex(new Vector3(2.0, 13.0, 12.0)))
            };
            var mesh = new Mesh();
            triangles.ForEach(t => mesh.AddTriangle(t));
            return mesh;
        }
    }
}