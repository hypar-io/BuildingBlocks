using Elements;
using Elements.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using GeometryEx;

namespace LevelsByEnvelope
{
    public class LevelMaker
    {

        public LevelMaker(List<Envelope> envelopes, double stdHeight, double grdHeight, double mchHeight)
        {
            Envelopes = new List<Envelope> ();
            Envelopes.AddRange(envelopes.OrderBy(e => e.Elevation));
            Levels = new List<Level>();
            GradeLevels(stdHeight, grdHeight);
            HighLevels(stdHeight, mchHeight);
            MidLevels(stdHeight);
        }

        private List<Envelope> Envelopes { get; set; }
        public List<Level> Levels { get; private set; }

        /// <summary>
        /// Creates Levels within the subgrade and ongrade Envelopes.
        /// </summary>
        /// <param name="stdHeight">Desired height for repeating Levels.</param>
        /// <param name="grdHeight">Desired height for first Level above grade.</param>
        private void GradeLevels(double stdHeight, double grdHeight)
        {
            // Add subgrade Levels.
            var subs = Envelopes.Where(e => e.Elevation < 0.0).ToList();
            foreach (var env in subs)
            {
                Levels.AddRange(MakeLevels(env, stdHeight, true, true));
            }

            // Add lobby Envelope Levels.
            var envelope = Envelopes.Where(e => e.Elevation >= 0.0).ToList().First();
            if (subs.Count == 0) // if no subgrade levels, add the first Level
            {
                MakeLevel(envelope, envelope.Elevation);
            }
            if (envelope.Height >= grdHeight + (stdHeight * 2))
            {
                // Temporary Envelope to populate levels above the lobby.
                envelope = new Envelope(envelope.Profile, grdHeight, envelope.Height - grdHeight,
                                        Vector3.ZAxis, 0.0, envelope.Transform, null, envelope.Representation,
                                        Guid.NewGuid(), "");
                Levels.AddRange(MakeLevels(envelope, stdHeight, false, true));
            }
            else
            {
                MakeLevel(envelope, envelope.Height);
            }
            Levels = Levels.OrderBy(l => l.Elevation).ToList();
        }

        /// <summary>
        /// Creates levels in the highest Envelope, including a higher mechanical Level below the top of the Envelope.
        /// </summary>
        /// <param name="stdHeight">Desired height for repeating Levels.</param>
        /// <param name="mchHeight">Desired height for mechanical Levels</param>
        private void HighLevels(double stdHeight, double mchHeight)
        {
            // Add mechanical level and roof level to highest Envelope.
            var envelope = Envelopes.Last();
            var bldgHeight = envelope.Elevation + envelope.Height;
            MakeLevel(envelope, bldgHeight);

            // Create temporary envelope to populate the region beneath the Mechanical level.
            envelope = new Envelope(envelope.Profile.Perimeter, envelope.Elevation, envelope.Height - mchHeight,
                                    Vector3.ZAxis, 0.0, envelope.Transform, null, envelope.Representation,
                                    Guid.NewGuid(), "");
            Levels.AddRange(MakeLevels(envelope, stdHeight, false, true));
            Levels = Levels.OrderBy(l => l.Elevation).ToList();
        }

        /// <summary>
        /// Creates Levels in middle-height Envelopes.
        /// </summary>
        /// <param name="stdHeight">Desired height for repeating Levels.</param>
        private void MidLevels(double stdHeight)
        {
            // Remove completed Levels from Envelope list.
            var envelopes = new List<Envelope>();
            envelopes.AddRange(Envelopes.Where(e => e.Elevation >= 0.0).Skip(1).ToList());
            envelopes = envelopes.SkipLast(1).ToList();

            // Add standard height Levels.
            foreach (var envelope in envelopes)
            {
                //Skip the last level so we don't get redundant levels at the top of one envelope and the bottom of the next.
                Levels.AddRange(MakeLevels(envelope, stdHeight, false, true));
            }
            Levels = Levels.OrderBy(l => l.Elevation).ToList();
        }

        /// <summary>
        /// Creates a Level at the specified height if it falls within the vertical volume of the supplied Envelope.
        /// </summary>
        /// <param name="envelope">Envelope from which to derive the Level perimeter.</param>
        /// <param name="elevation">Elevation of the proposed Level.</param>
        /// <returns>A Level or null if no eligible envelope is found.</returns>
        public bool MakeLevel(Envelope envelope, double elevation)
        {
            if (elevation < envelope.Elevation || elevation > envelope.Elevation + envelope.Height)
            {
                return false;
            }
            Levels.Add(new Level(Vector3.Origin, Vector3.ZAxis, elevation, envelope.Profile.Perimeter, Guid.NewGuid(), ""));
            Levels = Levels.OrderBy(l => l.Elevation).ToList();
            return true;
        }

        /// <summary>
        /// Creates Levels within the supplied Envelope, starting at Envelope.Elevation and proceeding upward at equal intervals until placing the last Level at Envelope.Elevation + Envelope.Height.
        /// </summary>
        /// <param name="envelope">Envelope that will encompass the new Levels.</param>
        /// <param name="interval">Desired vertical distance between Levels.</param>
        /// <returns>A List of Levels ordered from lowest Elevation to highest.</returns>
        public List<Level> MakeLevels (Envelope envelope, double interval, bool first = true, bool last = true)
        {
            var levels = new List<Level>();
            if (first)
            {
                levels.Add(new Level(Vector3.Origin, 
                                     Vector3.ZAxis, 
                                     envelope.Elevation, 
                                     Math.Abs(envelope.Profile.Perimeter.Area()),        
                                     envelope.Profile.Perimeter, 
                                     Guid.NewGuid(), ""));
            };
            var openHeight = envelope.Height;
            var stdHeight = openHeight / Math.Floor(openHeight / interval) - 1;
            var atHeight = envelope.Elevation + stdHeight;
            while (openHeight > stdHeight * 2)
            {
                levels.Add(new Level(Vector3.Origin, 
                                     Vector3.ZAxis, 
                                     atHeight,
                                     Math.Abs(envelope.Profile.Perimeter.Area()), 
                                     envelope.Profile.Perimeter, 
                                     Guid.NewGuid(), ""));
                openHeight -= stdHeight;
                atHeight += stdHeight;
            }
            if (last)
            {
                levels.Add(new Level(Vector3.Origin, 
                                     Vector3.ZAxis, 
                                     envelope.Elevation + envelope.Height,
                                     Math.Abs(envelope.Profile.Perimeter.Area()),
                                     envelope.Profile.Perimeter, 
                                     Guid.NewGuid(), ""));
            }
            return levels;
        }
    }
}