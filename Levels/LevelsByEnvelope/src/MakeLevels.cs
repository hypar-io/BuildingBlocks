using Elements;
using Elements.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using GeometryEx;

namespace LevelsByEnvelope
{
    public class MakeLevels
    {
        private List<Envelope> Envelopes;

        public MakeLevels (List<Envelope> envelopes)
        {
            Envelopes = envelopes;
        }

        /// <summary>
        /// Finds the envelope in Envelopes that encompasses the supplied elevation and creates a Level at the elevation.
        /// </summary>
        /// <param name="elevation">Elevation at which to identify the encompassing envelope.</param>
        /// <returns>A Level or null if no eligible envelope is found.</returns>
        public Level MakeLevel (double elevation)
        {
            foreach (var envelope in Envelopes)
            {
                if (elevation >= envelope.Elevation && elevation <= envelope.Elevation + envelope.Height)
                {
                    return new Level(Vector3.Origin, Vector3.ZAxis, elevation, envelope.Profile.Perimeter, Guid.NewGuid(), "");
                }
            }
            return null;
        }
    }
}