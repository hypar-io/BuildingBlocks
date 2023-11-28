using System;
using System.Linq;
using System.Collections.Generic;
using Elements;
using Elements.Geometry;
using GeometryEx;

namespace CoreByEnvelope
{
    public static class CoreByEnvelope
    {
        /// <summary>
        /// Creates the volume of a building service core either placed wholly within the building envelope or partially emerged if no interior fit can be found.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A CoreByEnvelopeOutputs instance containing computed results and the model with any new elements.</returns>
        public static CoreByEnvelopeOutputs Execute(Dictionary<string, Model> inputModels,
                                                    CoreByEnvelopeInputs inputs)
        {
            var output = new CoreByEnvelopeOutputs();
            var envelopes = new List<Envelope>();
            inputModels.TryGetValue("Envelope", out var model);
            if (model == null)
            {
                output.Errors.Add("The model output named 'Envelope' could not be found. Check the upstream functions for errors.");
                return output;
            }
            else if (model.AllElementsOfType<Envelope>().Count() == 0)
            {
                output.Errors.Add("The model output named 'Envelope' could not be found. Check the upstream functions for errors.");
                return output;
            }
            envelopes.AddRange(model.AllElementsOfType<Envelope>());
            var coreDef = CoreMaker.MakeCore(inputs, envelopes);
            var extrude = new Elements.Geometry.Solids.Extrude(coreDef.perimeter, coreDef.height, Vector3.ZAxis, false);
            var corRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
            var corMatl = new Material("serviceCore", Palette.White, 0.0f, 0.0f);

            output.ServiceCoreLength = coreDef.length;
            output.ServiceCoreWidth = coreDef.width;
            output.ServiceCoreRotation = coreDef.rotation;
            output.Model.AddElement(new ServiceCore(coreDef.perimeter,
                                                    coreDef.elevation,
                                                    coreDef.height,
                                                    coreDef.centroid,
                                                    new Transform(0.0, 0.0, coreDef.elevation),
                                                    corMatl,
                                                    corRep,
                                                    false,
                                                    Guid.NewGuid(),
                                                    "Service Core"));
            return output;
        }
    }
}