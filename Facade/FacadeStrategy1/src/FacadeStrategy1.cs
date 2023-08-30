using Elements;
using Elements.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace FacadeStrategy1
{
    public static class FacadeStrategy1
    {
        /// <summary>
        /// The FacadeStrategy1 function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A FacadeStrategy1Outputs instance containing computed results and the model with any new elements.</returns>
        public static FacadeStrategy1Outputs Execute(Dictionary<string, Model> inputModels, FacadeStrategy1Inputs input)
        {
            var output = new FacadeStrategy1Outputs();
            var facadeTypes = inputModels["Facade Grid"].AllElementsOfType<FacadeType>().ToList();
            var matchingFacadeType = facadeTypes.Find(x => x.Name == input.FacadeType);
            var matchingPanels = inputModels["Facade Grid"].AllElementsOfType<Panel>().Where(x => x.Name.StartsWith($"{input.FacadeType} - ")).ToList();
            var instances = inputModels["Facade Grid"].AllElementsOfType<ElementInstance>();
            foreach (var panel in matchingPanels)
            {
                var geo = panel.Representation;
                // Create new geometry for the panel you want to create, based on the geometry of the 
                // matching panel definition. Note that panel geometry is located at the origin, with the
                // facade's normal along the Z axis. 
                var panelElem = new GeometricElement()
                {
                    Representation = geo,
                    Material = new Material(panel.Name)
                    {
                        Color = input.Color
                    },
                    IsElementDefinition = true
                };
                // create instances of that panel
                var instancesOfPanel = instances.Where(i => i.BaseDefinition.Id == panel.Id);
                foreach (var inst in instancesOfPanel)
                {
                    var panelInstance = panelElem.CreateInstance(inst.Transform, inst.Name);
                    panelInstance.AdditionalProperties = inst.AdditionalProperties;
                    output.Model.AddElement(panelInstance);
                }

            }
            return output;
        }
    }
}