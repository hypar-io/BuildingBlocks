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
            Model outputModel = new Model();
            var site = new SiteMaker(input.Lot);
            var boundary = site.Boundary;
            var land = new Mass(new Profile(boundary),
                                1.0,
                                new Material("site", new Color(0.19531f, 0.65625f, 0.0f, 1.0f), 0.0f, 0.0f),
                                new Transform(0.0, 0.0, -1.0))
            {
                Name = "site"
            };
            outputModel.AddElement(land);
            var offset = boundary.Offset(input.SiteSetback * -1);
            if (offset.Count() == 0)
            {
                throw new InvalidOperationException("Site setback value exceeds available site distance.");
            }
            var setback = offset.OrderByDescending(s => s.Area()).First();
            var building = Polygon.Rectangle(input.BuildingLength, input.BuildingWidth);
            if (setback.Area() < building.Area())
            {
                throw new InvalidOperationException("Building area exceeds available site area.");
            }
            var grid = new CoordGrid(setback, input.SearchGridResolution, input.SearchGridResolution);
            var rnd = new Random((int)input.SearchSeed);
            var places = grid.Available.OrderBy(v => rnd.Next()).ToArray();
            Polygon tstBldg = null;
            var locate = input.Lot[0].Geometry as Elements.GeoJSON.Polygon;
            outputModel.Origin = locate.Coordinates[0][0];
            var lotCover = 0.0;
            for (int i = 0; i < places.Count(); i++)
            {
                for (int j = 0; j < 360; j += 5)
                {
                    tstBldg = building.Rotate(Vector3.Origin, j).MoveFromTo(Vector3.Origin, places[i]);
                    if (setback.Covers(tstBldg))
                    {
                        lotCover = tstBldg.Area() / boundary.Area();
                        var bldg = new Mass(new Profile(building), 0.1, BuiltInMaterials.Concrete)
                        {
                            Name = "footprint"
                        };
                        bldg.Transform.Rotate(Vector3.ZAxis, j);
                        bldg.Transform.Move(places[i]);
                        outputModel.AddElement(bldg);
                        i = places.Count();
                        break;
                    }
                }
            }
            if(outputModel.GetElementByName("footprint") == null)
            {
                throw new InvalidOperationException("Unable to fit the requested building footprint to the selected site.");
            }
            var outputs = new SiteOutputs(lotCover,
                                          input.BuildingLength,
                                          input.BuildingWidth,
                                          boundary.Area(),
                                          building.Area())
            {
                model = outputModel
            };
            return outputs;
		}
  	}
}