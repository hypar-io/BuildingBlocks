using Elements;
using Elements.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FacadeStrategy2
{
    public static class FacadeStrategy2
    {
        /// <summary>
        /// The FacadeStrategy2 function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A FacadeStrategy2Outputs instance containing computed results and the model with any new elements.</returns>
        public static FacadeStrategy2Outputs Execute(Dictionary<string, Model> inputModels, FacadeStrategy2Inputs input)
        {
            var output = new FacadeStrategy2Outputs();
            var facadeTypes = inputModels["Facade Grid"].AllElementsOfType<FacadeType>();
            var panelDefinitions = inputModels["Facade Grid"].AllElementsOfType<Panel>();
            var panelInstances = inputModels["Facade Grid"].AllElementsOfType<ElementInstance>();
            bool setAnyFacades = false;
            foreach (var ft in facadeTypes)
            {
                // establish proxy element to contain settings for strategy 2
                var proxy = ft.Proxy("Facade Grid");
                // create default settings
                var settings = CreateDefaultPanelSettings();
                if (input.Overrides?.FacadeStrategy2Settings != null)
                {
                    var matchingOverride = input.Overrides.FacadeStrategy2Settings.FirstOrDefault(o => o.Identity.Name == ft.Name);
                    if (matchingOverride != null)
                    {
                        Identity.AddOverrideValue(proxy, matchingOverride.GetName(), matchingOverride.Value);
                        settings = matchingOverride.Value;
                    }
                }
                // if not enabled for this facade type, do nothing
                if (settings.ApplyFacadeStrategy2 == false)
                {
                    continue;
                }
                setAnyFacades = true;
                // create new panels and instances for this facade type
                var matchingPanels = panelDefinitions.Where(pd => pd.Name.StartsWith(ft.Name + " - "));
                var mat = new Material("Strategy 2 material", settings.Color);
                foreach (var panel in matchingPanels)
                {
                    var geo = panel.Representation;
                    var newPanel = new GeometricElement()
                    {
                        Representation = geo,
                        Material = mat,
                        IsElementDefinition = true
                    };
                    var instancesOfPanel = panelInstances.Where(pi => pi.BaseDefinition.Id == panel.Id);
                    foreach (var i in instancesOfPanel)
                    {
                        var newInstance = newPanel.CreateInstance(i.Transform, i.Name);
                        newInstance.AdditionalProperties = i.AdditionalProperties;
                        output.Model.AddElement(newInstance);
                    }
                }
            }
            if (!setAnyFacades)
            {
                output.Warnings.Add("No Facades have been set to use Facade Strategy 2. Select a Facade Type in the model to enable Facade Strategy 2.");
            }
            return output;
        }

        private static FacadeStrategy2SettingsValue CreateDefaultPanelSettings()
        {
            return new FacadeStrategy2SettingsValue(false, Colors.Red);
        }
    }
}