using System.Collections.Generic;
using System.Linq;
using System;
using Elements;
using Elements.Geometry;

namespace EnvelopeByCenterline
{
    public static class EnvelopeByCenterline
    {
        /// <summary>
        /// The EnvelopeByCenterline function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A EnvelopeByCenterlineOutputs instance containing computed results and the model with any new elements.</returns>
        public static EnvelopeByCenterlineOutputs Execute(Dictionary<string, Model> inputModels, EnvelopeByCenterlineInputs input)
        {

            var Centerline = input.Centerline;
            var perimeter = Centerline.Offset(input.BarWidth / 2, EndType.Butt).First();

            // Create the foundation Envelope.
            var extrude = new Elements.Geometry.Solids.Extrude(perimeter, input.FoundationDepth, Vector3.ZAxis, false);
            var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
            var fndMatl = new Material("foundation", new Color(0.6, 0.60000002384185791, 0.6, 1), 0.0f, 0.0f);
            var envMatl = new Material("envelope", new Color(0.3, 0.7, 0.7, 0.6), 0.0f, 0.0f);
            var envelopes = new List<Envelope>()
            {
                new Envelope(perimeter, input.FoundationDepth * -1, input.FoundationDepth, Vector3.ZAxis,
                             0.0, new Transform(0.0, 0.0, input.FoundationDepth * -1), fndMatl, geomRep, false, Guid.NewGuid(), "")
            };

            // Create the Envelope at the location's zero plane.
            var output = new EnvelopeByCenterlineOutputs(input.BuildingHeight, input.FoundationDepth);

            extrude = new Elements.Geometry.Solids.Extrude(perimeter, input.BuildingHeight, Vector3.ZAxis, false);
            geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
            envelopes.Add(new Envelope(perimeter, 0.0, input.BuildingHeight, Vector3.ZAxis, 0.0,
                          new Transform(), envMatl, geomRep, false, Guid.NewGuid(), ""));
            output.Model.AddElements(envelopes);
            var sketch = new Sketch(input.Centerline, Guid.NewGuid(), "Centerline Sketch");
            output.Model.AddElement(sketch);
            return output;
        }

    }
}