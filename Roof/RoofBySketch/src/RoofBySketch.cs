using System;
using Elements;
using Elements.Geometry;
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
            var extrude = new Elements.Geometry.Solids.Extrude(input.Perimeter, input.RoofThickness, Vector3.ZAxis, false);
            var geomRep = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude });
            var roofMatl = BuiltInMaterials.Concrete;
            var output = new RoofBySketchOutputs(input.Perimeter.Area());
            output.Model.AddElement(new Roof(input.Perimeter, 
                                             input.RoofElevation, 
                                             input.RoofThickness, 
                                             input.Perimeter.Area(),
                                             new Transform(0.0, 0.0, input.RoofElevation - input.RoofThickness), 
                                             roofMatl,
                                             geomRep,
                                             false,
                                             Guid.NewGuid(), ""));
            return output;
        }
      }
}