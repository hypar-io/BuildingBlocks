using System;
using System.Collections.Generic;
using System.Linq;
using Elements;
using Elements.Geometry;
using Elements.Geometry.Solids;

namespace Facade
{
    public static class Facade
	{
        private static string ENVELOPE_MODEL_NAME = "Envelope";
        private static string LEVELS_MODEL_NAME = "Levels";

        /// <summary>
        /// Adds facade Panels to one or more Masses named 'envelope'.
        /// </summary>
        /// <param name="model">The model. 
        /// Add elements to the model to have them persisted.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A FacadeOutputs instance containing computed results.</returns>
        public static FacadeOutputs Execute(Dictionary<string, Model> models, FacadeInputs input)
		{
            Envelope envelope;
            List<Level> levels = null;
            if(models.ContainsKey(ENVELOPE_MODEL_NAME))
            {
                var envelopeModel = models[ENVELOPE_MODEL_NAME];
                envelope = envelopeModel.AllElementsOfType<Envelope>().First(e=>e.Direction.IsAlmostEqualTo(Vector3.ZAxis));
                var levelsModel = models[LEVELS_MODEL_NAME];
                levels = levelsModel.AllElementsOfType<Level>().ToList();
            }
            else
            {
                var envMaterial = new Material("Envelope", Colors.Coral);
                envelope = new Envelope(Polygon.L(20, 20, 5), 0, 20, Vector3.ZAxis, 0.0, new Transform(), envMaterial, null, Guid.NewGuid(), "envelope");
                levels = new List<Level>();
                for(var i=0; i<10; i+=3)
                {
                    levels.Add(new Level(new Vector3(0,0,i), Vector3.ZAxis, i, null, null, null, Guid.NewGuid(), $"Level {i}"));
                }
            }

            var panelCount = 0;

            var model = new Model();

            var panelMat = new Material("envelope", new Color(1.0, 1.0, 1.0, 1), 0.0f, 0.5f);
            var boundarySegments = envelope.Profile.Perimeter.Segments();
            foreach(var s in boundarySegments)
            {
                var d = s.Direction();
                for(var i=0; i<levels.Count-1; i++)
                {
                    var level1 = levels[i];
                    var level2 = levels[i+1];
                    var bottom = new Line(new Vector3(s.Start.X, s.Start.Y, level1.Elevation), new Vector3(s.End.X, s.End.Y, level1.Elevation));
                    var top = new Line(new Vector3(s.Start.X, s.Start.Y, level2.Elevation), new Vector3(s.End.X, s.End.Y, level2.Elevation));
                    var topSegments = top.DivideByLength(input.PanelWidth);
                    var bottomSegments = bottom.DivideByLength(input.PanelWidth);
                    for(var j=0; j<bottomSegments.Count(); j++)
                    {
                        var bs = bottomSegments[j];
                        var ts = topSegments[j];
                        var pts = new[]{bs.Start, bs.End, ts.End, ts.Start}.Shrink(input.MullionWidth/2);
                        var t = new Transform(bs.Start, d, d.Cross(Vector3.ZAxis));
                        var panel = CreateFacadePanel($"FP_{i}_{j}",
                                                      bs.Length(),
                                                      level2.Elevation - level1.Elevation,
                                                      input.GlassInset,
                                                      0.1,
                                                      panelMat,
                                                      t,
                                                      model);
                        panelCount++;
                    }
                }
                var e = levels.Last().Elevation;
                var parapet= new StandardWall(new Line(new Vector3(s.Start.X, s.Start.Y, e), new Vector3(s.End.X, s.End.Y, e)), 0.1, 0.9, panelMat);
                model.AddElement(parapet);
            }
            
            var output = new FacadeOutputs(panelCount);
            output.model = model;
			return output;
		}

        private static FacadePanel CreateFacadePanel(string name,
                                                     double width,
                                                     double height,
                                                     double inset,
                                                     double thickness,
                                                     Material material,
                                                     Transform lowerLeft,
                                                     Model model)
        {
            var a = new Vector3(0,0,0);
            var b = new Vector3(width,0,0);
            var c = new Vector3(width, height, 0);
            var d = new Vector3(0, height, 0);

            Profile profile;
            if(width - 2*inset > 0)
            {
                var a1 = new Vector3(inset, inset, 0);
                var b1 = new Vector3(width-inset, inset,0);
                var c1 = new Vector3(width-inset, height-inset, 0);
                var d1 = new Vector3(inset, height-inset, 0);
                var inner = new Polygon(new[]{d1,c1,b1,a1});
                profile = new Profile(new Polygon(new[]{a,b,c,d}), inner);
                var glazing = new Panel(inner, BuiltInMaterials.Glass, lowerLeft);
                model.AddElement(glazing);
            }
            else
            {
                profile = new Profile(new Polygon(new[]{a,b,c,d}));
            }
            
            var solidOps = new List<SolidOperation>(){new Extrude(profile, thickness, Vector3.ZAxis, 0.0, false)};
            var representation = new Representation(solidOps);
            var panel = new FacadePanel(thickness, lowerLeft, material, representation, Guid.NewGuid(), name);
            model.AddElement(panel);

            return panel;
        }
  	}
}