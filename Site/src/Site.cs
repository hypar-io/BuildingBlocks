using System;
using System.Collections.Generic;
using System.Linq;
using Elements;
using Elements.Geometry;
using GeometryEx;

namespace Site
{
  	public static class Site
	{
		/// <summary>
		/// The SiteSelector function.
		/// </summary>
		/// <param name="model">The model. 
		/// Add elements to the model to preserve them.</param>
		/// <param name="input">The arguments to the execution.</param>
		/// <returns>A SiteSelectorOutputs instance containing computed results.</returns>
		public static SiteOutputs Execute(Dictionary<string, Model> models, SiteInputs input)
		{
            var boundary = ExtractSiteOutlineFromFeatures(input.Location);
            var perimeters = boundary.Offset(input.SiteSetback * -1);
            if (perimeters.Count() == 0)
            {
                return new SiteOutputs(0.0, input.BuildingLength, input.BuildingWidth, boundary.Area(), 0.0);
            }
            var setback = perimeters.OrderByDescending(s => s.Area()).First();
            var footprint = Polygon.Rectangle(input.BuildingLength, input.BuildingWidth);
            if (setback.Area() < footprint.Area())
            {
                return new SiteOutputs(0.0, input.BuildingLength, input.BuildingWidth, boundary.Area(), footprint.Area());
            }
            var grid = new CoordinateGrid(setback, input.SearchGridResolution, input.SearchGridResolution);
            var rnd = new Random((int)input.SearchSeed);
            var places = grid.Available.OrderBy(v => rnd.Next()).ToArray();
            var lotCover = 0.0;
            Polygon tstFoot = null;
            var outputModel = new Model();
            var site = new Mass(new Profile(boundary), 0.1, new Material(Guid.NewGuid(), "field", new Color(0.457f, 0.707f, 0.468f, 1.0f), 0.0f, 0.0f));
            site.Transform.Move(new Vector3(0.0, 0.0, -0.1));
            outputModel.AddElement(site);
            for (int i = 0; i < places.Count(); i++)
            {
                for (int j = 0; j < 360; j += 5)
                {
                    tstFoot = footprint.Rotate(Vector3.Origin, j).MoveFromTo(Vector3.Origin, places[i]);
                    if (setback.Covers(tstFoot))
                    {
                        lotCover = tstFoot.Area() / boundary.Area();
                        var foot = new Mass(new Profile(footprint), 0.1, BuiltInMaterials.Concrete)
                        {
                            Name = "footprint"
                        };
                        foot.Transform.Rotate(Vector3.ZAxis, j);
                        foot.Transform.Move(places[i]);
                        outputModel.AddElement(foot);
                        i = places.Count();
                        break;
                    }
                }
            }
            var outputs = new SiteOutputs(lotCover,
                                          input.BuildingLength,
                                          input.BuildingWidth,
                                          boundary.Area(),
                                          footprint.Area())
            {
                model = outputModel
            };
            return outputs;


            //Model outputModel = new Model();
            //var boundary = new SiteMaker(input.Lot).Boundary;
            //var offset = boundary.Offset(input.SiteSetback * -1);
            //if (offset.Count() == 0)
            //{
            //    return new SiteOutputs(0.0, input.BuildingLength, input.BuildingWidth, boundary.Area(), 0.0);
            //}
            //var setback = offset.OrderByDescending(s => s.Area()).First();
            //var building = Polygon.Rectangle(input.BuildingLength, input.BuildingWidth);
            //if (setback.Area() < building.Area())
            //{
            //    return new SiteOutputs(0.0, input.BuildingLength, input.BuildingWidth, boundary.Area(), building.Area());
            //}
            //var grid = new CoordinateGrid(setback, input.SearchGridResolution, input.SearchGridResolution);
            //var rnd = new Random((int)input.SearchSeed);
            //var places = grid.Available.OrderBy(v => rnd.Next()).ToArray();
            //Polygon tstBldg = null;
            //var locate = input.Lot[0].Geometry as Elements.GeoJSON.Polygon;
            //outputModel.Origin = locate.Coordinates[0][0];
            //var lotCover = 0.0;
            //for (int i = 0; i < places.Count(); i++)
            //{
            //    for (int j = 0; j < 360; j += 5)
            //    {
            //        tstBldg = building.Rotate(Vector3.Origin, j).MoveFromTo(Vector3.Origin, places[i]);
            //        if (setback.Covers(tstBldg))
            //        {
            //            lotCover = tstBldg.Area() / boundary.Area();
            //            var bldg = new Mass(new Profile(building), 0.1, BuiltInMaterials.Concrete)
            //            {
            //                Name = "footprint"
            //            };
            //            bldg.Transform.Rotate(Vector3.ZAxis, j);
            //            bldg.Transform.Move(places[i]);
            //            outputModel.AddElement(bldg);
            //            i = places.Count();
            //            break;
            //        }
            //    }
            //}
            //var outputs = new SiteOutputs(lotCover,
            //                              input.BuildingLength,
            //                              input.BuildingWidth,
            //                              boundary.Area(),
            //                              building.Area())
            //{
            //    model = outputModel
            //};
            //return outputs;
		}

        private static Polygon ExtractSiteOutlineFromFeatures(Elements.GeoJSON.Feature[] features)
        {
            // Extract location data.
            // The GeoJSON may contain a number of features. Here we just
            // take the first one assuming it's a Polygon, and we use
            // its first point as the origin. 
            var outline = (Elements.GeoJSON.Polygon)features[0].Geometry;
            var origin = outline.Coordinates[0][0];
            var offset = origin.ToVectorMeters();
            var plines = outline.ToPolygons();
            var pline = plines[0];
            var tverts = new Vector3[pline.Vertices.Length];
            for (var i = tverts.Length - 1; i >= 0; i--)
            {
                var v = pline.Vertices[i];
                tverts[i] = new Vector3(v.X - offset.X, v.Y - offset.Y, v.Z);
            }
            return new Elements.Geometry.Polygon(tverts);
        }

    }
}