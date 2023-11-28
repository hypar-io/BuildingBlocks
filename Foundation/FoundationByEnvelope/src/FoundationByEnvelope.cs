using Elements;
using Elements.Geometry;
using System.Collections.Generic;
using System;
using System.Linq;
using GeometryEx;

namespace FoundationByEnvelope
{
    public static class FoundationByEnvelope
    {
        /// <summary>
        /// The FoundationByEnvelope function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A FoundationByEnvelopeOutputs instance containing computed results and the model with any new elements.</returns>
        public static FoundationByEnvelopeOutputs Execute(Dictionary<string, Model> inputModels, FoundationByEnvelopeInputs input)
        {

            var envelopes = new List<Envelope>();
            inputModels.TryGetValue("Envelope", out var model);
            var output = new FoundationByEnvelopeOutputs();
            if (model == null)
            {
                output.Errors.Add($"No Envelope found in the model 'Envelope'. Check the output from the function upstream that has a model output 'Envelope'.");
                return output;
            }
            envelopes.AddRange(model.AllElementsOfType<Envelope>());

            var minDepth = input.MinDepth;
            var outputDepth = 0.0;

            var outputEnvelopes = new List<Envelope>();
            var fndMatl = BuiltInMaterials.Concrete;

            var envelopesBelowGrade = envelopes.Where(e => e.Elevation < -0.1);

            if (envelopesBelowGrade.Count() > 0)
            {
                //assume we've got a stacked envelope scenario that already includes basements. In this case, reproduce it as foundation.
                foreach (var envelope in envelopesBelowGrade)
                {
                    var envelopeDepth = envelope.Height;
                    //if the depth is less than the minDepth, add additional depth
                    var finalDepth = Math.Max(minDepth, envelopeDepth);


                    var perimeter = envelope.Profile.Perimeter;
                    var offset = perimeter;
                    var offsetResults = perimeter.Offset(-0.1);
                    //get largest offset
                    if (offsetResults.Length > 0)
                    {
                        offset = offsetResults.OrderBy(p => p.Area()).Last();
                    }

                    offset = offset.Project(new Plane(Vector3.Origin, Vector3.ZAxis));
                    if (finalDepth > outputDepth)
                    {
                        outputDepth = finalDepth;
                    }
                    outputEnvelopes.Add(CreateFoundation(offset, finalDepth));
                }
            }
            else
            {
                //assume there are no existing basements and generate them for all envelopes at grade.
                var envelopesAtGrade = envelopes.Where(EnvelopeIsAtGrade);

                foreach (var envelope in envelopesAtGrade)
                {
                    var perimeter = envelope.Profile.Perimeter;
                    if (minDepth > outputDepth) outputDepth = minDepth;
                    outputEnvelopes.Add(CreateFoundation(perimeter, minDepth));

                }
            }
            output.Depth = outputDepth;
            output.Model.AddElements(outputEnvelopes);
            return output;
        }

        private static bool EnvelopeIsAtGrade(Envelope e)
        {
            return Math.Abs(e.Elevation) < 0.1;
        }

        private static Envelope CreateFoundation(Polygon perimeter, double depth)
        {
            var extrude = new Elements.Geometry.Solids.Extrude(perimeter, depth, Vector3.ZAxis, false);
            var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });

            var material = BuiltInMaterials.Concrete;
            return new Envelope(perimeter, depth * -1, depth, Vector3.ZAxis,
                                0.0, null, new Transform(0.0, 0.0, depth * -1), material,
                                geomRep, false, Guid.NewGuid(), "");
        }

    }

}
