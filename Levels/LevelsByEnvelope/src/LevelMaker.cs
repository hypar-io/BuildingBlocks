using Elements;
using Elements.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using GeometryEx;

namespace LevelsByEnvelope
{
    public static class LevelMaker
    {
        /// <summary>
        /// Finds the envelope in Envelopes that encompasses the supplied elevation and creates a Level at the elevation.
        /// </summary>
        /// <param name="elevation">Elevation at which to identify the encompassing envelope.</param>
        /// <returns>A Level or null if no eligible envelope is found.</returns>
        public static Level MakeLevel(Envelope envelope, double atHeight)
        {
            if (atHeight >= envelope.Elevation && atHeight <= envelope.Elevation + envelope.Height)
            {
                return new Level(Vector3.Origin, Vector3.ZAxis, atHeight, envelope.Profile.Perimeter, Guid.NewGuid(), "");
            }
            return null;
        }

        /// <summary>
        /// Finds the envelope in Envelopes that encompasses the supplied elevation and creates a Level at the elevation.
        /// </summary>
        /// <param name="elevation">Elevation at which to identify the encompassing envelope.</param>
        /// <returns>A Level or null if no eligible envelope is found.</returns>
        internal static List<Level> MakeLevels (Envelope envelope, double interval, bool top = true)
        {
            var levels = new List<Level>
            {
                new Level(Vector3.Origin, Vector3.ZAxis, envelope.Elevation, envelope.Profile.Perimeter, Guid.NewGuid(), "")
            };
            var openHeight = envelope.Height;
            var stdHeight = openHeight / Math.Floor(openHeight / interval) - 1;
            var atHeight = envelope.Elevation + stdHeight;
            while (openHeight > stdHeight * 2)
            {
                levels.Add(new Level(Vector3.Origin, Vector3.ZAxis, atHeight, envelope.Profile.Perimeter, Guid.NewGuid(), ""));
                openHeight -= stdHeight;
                atHeight += stdHeight;
            }
            if (top)
            {
                atHeight = envelope.Elevation + envelope.Height;
                levels.Add(new Level(Vector3.Origin, Vector3.ZAxis, atHeight, envelope.Profile.Perimeter, Guid.NewGuid(), ""));
            }
            return levels.OrderBy(l => l.Elevation).ToList();
        }
    }
}