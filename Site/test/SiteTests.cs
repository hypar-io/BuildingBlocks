using Xunit;
using Elements;
using Elements.Geometry;
using Elements.Serialization.glTF;
using System;
using System.Collections.Generic;

namespace Site.tests
{
    public class SiteTests
    {
        [Fact]
        public void Footprint()
        {
            var inputs = new SiteInputs();
            var outputs = Site.Execute(new Dictionary<string, Model>(), inputs);
            outputs.model.ToGlTF("../../../../Site.glb");
            System.IO.File.WriteAllText("../../../../Site.json", outputs.model.ToJson());
        }

		[Fact]
		public void TriangleIntersection()
		{
			var a = new Vertex(new Vector3(-0.5,-0.5, 1.0));
			var b = new Vertex(new Vector3(0.5, -0.5, 1.0));
			var c = new Vertex(new Vector3(0, 0.5, 1.0));
			var t = new Triangle(a,b,c);
			var r = new Ray(Vector3.Origin, Vector3.ZAxis);
			IntersectionResult xsect;
			var intersects = r.Intersects(t, out xsect);
			Assert.True(xsect.Type == IntersectionResultType.Intersect);

			r = new Ray(Vector3.Origin, Vector3.ZAxis.Negate());
			intersects = r.Intersects(t, out xsect);
			Assert.True(xsect.Type == IntersectionResultType.Behind);
		}

		[Fact]
		public void IntersectsAtVertex()
		{
			var a = new Vertex(new Vector3(-0.5,-0.5, 1.0));
			var b = new Vertex(new Vector3(0.5, -0.5, 1.0));
			var c = new Vertex(new Vector3(0, 0.5, 1.0));
			var t = new Triangle(a,b,c);
			var r = new Ray(new Vector3(-0.5, -0.5, 0.0), Vector3.ZAxis);
			IntersectionResult xsect;
			var intersects = r.Intersects(t, out xsect);
			Assert.True(xsect.Type == IntersectionResultType.IntersectsAtVertex);
		}

		[Fact]
		public void IsParallelTo()
		{
			var a = new Vertex(new Vector3(-0.5,-0.5, 1.0));
			var b = new Vertex(new Vector3(0.5, -0.5, 1.0));
			var c = new Vertex(new Vector3(0, 0.5, 1.0));
			var t = new Triangle(a,b,c);
			var r = new Ray(Vector3.Origin, Vector3.XAxis);
			IntersectionResult xsect;
			var intersects = r.Intersects(t, out xsect);
			Assert.True(xsect.Type == IntersectionResultType.Parallel);
		}

		[Fact]
		public void OriginRayIntersectsTopography()
		{
			var elevations = new double[100];

			for(var x=0; x<10; x++)
			{
				for(var y=0; y<10; y++)
				{
					elevations[x*y + y] = Math.Sin((x/10)*Math.PI);
				}
			}
			var topo = new Topography(Vector3.Origin, 1.0, 1.0, elevations, 9, (tri)=>{return Colors.White;});
			var ray = new Ray(Vector3.Origin, Vector3.ZAxis);
			IntersectionResult xsect;
			ray.Intersects(topo, out xsect);
			Assert.True(xsect.Type == IntersectionResultType.IntersectsAtVertex);
		}
    }
}
