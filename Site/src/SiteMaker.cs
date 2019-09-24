using System;
using System.Collections.Generic;
using System.Linq;
using RestSharp;
using ImageMagick;
using Elements.GeoJSON;
using Elements.Geometry;

namespace Site
{
    public class SiteMaker
    {
        public SiteMaker(Feature[] location)
        {
            var outline = (Elements.GeoJSON.Polygon)location[0].Geometry;
            var origin = outline.Coordinates[0][0];
            var offset = origin.ToVectorMeters();
            var plines = outline.ToPolygons();
            var pline = plines[0];
            var tverts = new Vector3[pline.Vertices.Length];
            for(var i = tverts.Length-1; i >= 0; i--)
            {
                var v = pline.Vertices[i];
                tverts[i] = new Vector3(v.X - offset.X, v.Y - offset.Y, v.Z);
            }
            Boundary = new Elements.Geometry.Polygon(tverts);


            // var outline = (Elements.GeoJSON.Polygon)location[0].Geometry;
            // var origin = outline.Coordinates[0][0];
            // var offset = origin.ToVectorMeters();
            // var pline = outline.ToPolygons()[0];
            // var verts = pline.Vertices;
            // verts.Reverse();
            
        }
        public Elements.Geometry.Polygon Boundary { get; }
    }
}
