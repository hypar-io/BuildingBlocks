using Elements;
using Elements.Geometry;
using Elements.Geometry.Solids;
using System;
using System.Collections.Generic;

namespace Elements
{
    public class Warnings
    {
        public static Material WarningMaterial = new Material(
            "Warnings", new Color(1, 0, 0, 0.7),
            specularFactor: 1, glossinessFactor: 1,
            unlit: false, texture: "", doubleSided: false, repeatTexture: false,
            normalTexture: "", interpolateTexture: false, id: Guid.NewGuid());
        public const string DefaultName = "Warning Message";
        private const double DefaultSideLength = 0.3;

        public static WarningMessage TextWarning(
            string message, string name = null, string stackTrace = null)
        {
            return new WarningMessage(message, stackTrace, name: name ?? DefaultName);
        }

        public static WarningMessage WarningAtPolygon(
            string message, Polygon boundary,
            string name = null, string stackTrace = null)
        {
            const double warningHeightRaise = 0.02;
            var extrude = new Lamina(boundary, false);
            var warning = new WarningMessage(message,
                                             stackTrace,
                                             new Transform().Moved(z: warningHeightRaise),
                                             WarningMaterial,
                                             new Representation(new[] { extrude }),
                                             false,
                                             Guid.NewGuid(),
                                             name ?? DefaultName);
            return warning;
        }
    }
}
