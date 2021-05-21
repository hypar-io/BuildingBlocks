using Elements;
using Elements.Geometry;
using Elements.Geometry.Solids;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LevelsByEnvelope
{
    public class LevelMaker
    {

        public LevelMaker(List<Envelope> envelopes, double stdHeight, double grdHeight, double pntHeight)
        {
            Envelopes = new List<Envelope>();
            Envelopes.AddRange(envelopes.OrderBy(e => e.Elevation));
            Levels = new List<Level>();
            LevelPerimeters = new List<LevelPerimeter>();
            SubGradeLevels(stdHeight);
            GradeLevels(stdHeight, grdHeight);
            HighLevels(stdHeight, pntHeight);
            MidLevels(stdHeight);
            LevelVolumes = MakeLevelVolumes();

        }

        private List<Envelope> Envelopes { get; set; }
        public List<Level> Levels { get; private set; }
        public List<LevelPerimeter> LevelPerimeters { get; private set; }
        public List<LevelVolume> LevelVolumes { get; private set; }



        /// <summary>
        /// Creates Levels within the subgrade and ongrade Envelopes.
        /// </summary>
        /// <param name="stdHeight">Desired height for repeating Levels.</param>
        /// <param name="grdHeight">Desired height for first Level above grade.</param>
        private void GradeLevels(double stdHeight, double grdHeight)
        {
            var envelopes = Envelopes.Where(e => e.Elevation >= 0.0).ToList();
            if (envelopes.Count() == 0)
            {
                return;
            }
            var envelope = envelopes.First();
            var subs = Envelopes.Where(e => e.Elevation < 0.0).ToList();
            if (subs.Count() == 0) // if no subgrade levels created, add the first Level.
            {
                MakeLevel(envelope, envelope.Elevation);
            }
            if (envelope.Height >= grdHeight + (stdHeight * 2))
            {
                // Temporary Envelope to populate levels above the lobby.
                envelope = new Envelope(envelope.Profile, grdHeight, envelope.Height - grdHeight,
                                        Vector3.ZAxis, 0.0, new Transform(0.0, 0.0, grdHeight), null, null,
                                        false, Guid.NewGuid(), "");
                MakeLevels(envelope, stdHeight, true, true);
            }
            else
            {
                MakeLevel(envelope, envelope.Height);
            }
            Levels = Levels.OrderBy(l => l.Elevation).ToList();
            LevelPerimeters = LevelPerimeters.OrderBy(l => l.Elevation).ToList();
        }

        /// <summary>
        /// Creates levels in the highest Envelope, including a higher mechanical Level below the top of the Envelope.
        /// </summary>
        /// <param name="stdHeight">Desired height for repeating Levels.</param>
        /// <param name="pntHeight">Desired height for mechanical Levels</param>
        private void HighLevels(double stdHeight, double pntHeight)
        {
            if (Envelopes.Where(e => e.Elevation >= 0.0).ToList().Count() < 2)
            {
                return;
            }
            // Add penthouse level and roof level to highest Envelope.
            var envelope = Envelopes.Last();
            var bldgHeight = envelope.Elevation + envelope.Height;
            MakeLevel(envelope, bldgHeight);

            // Create temporary envelope to populate the region beneath the penthouse level.
            envelope = new Envelope(envelope.Profile.Perimeter, envelope.Elevation, envelope.Height - pntHeight,
                                    Vector3.ZAxis, 0.0, envelope.Transform, null, envelope.Representation,
                                    false, Guid.NewGuid(), "");
            MakeLevels(envelope, stdHeight, false, true);
            Levels = Levels.OrderBy(l => l.Elevation).ToList();
            LevelPerimeters = LevelPerimeters.OrderBy(l => l.Elevation).ToList();
        }

        /// <summary>
        /// Creates Levels in middle-height Envelopes.
        /// </summary>
        /// <param name="stdHeight">Desired height for repeating Levels.</param>
        private void MidLevels(double stdHeight)
        {
            if (Envelopes.Where(e => e.Elevation >= 0.0).ToList().Count() < 3)
            {
                return;
            }
            // Remove completed Levels from Envelope list.
            var envelopes = new List<Envelope>();
            envelopes.AddRange(Envelopes.Where(e => e.Elevation >= 0.0).Skip(1).ToList());
            envelopes = envelopes.SkipLast(1).ToList();

            // Add standard height Levels.
            foreach (var envelope in envelopes)
            {
                //Skip the last level so we don't get redundant levels at the top of one envelope and the bottom of the next.
                MakeLevels(envelope, stdHeight, false, true);
            }
            Levels = Levels.OrderBy(l => l.Elevation).ToList();
            LevelPerimeters = LevelPerimeters.OrderBy(l => l.Elevation).ToList();
        }

        /// <summary>
        /// Creates Levels within the subgrade Envelopes.
        /// </summary>
        /// <param name="stdHeight">Desired height for repeating Levels.</param>
        /// <param name="grdHeight">Desired height for first Level above grade.</param>
        private void SubGradeLevels(double stdHeight)
        {
            // Add subgrade Levels.
            var subs = Envelopes.Where(e => e.Elevation < 0.0).ToList();
            foreach (var env in subs)
            {
                MakeLevels(env, stdHeight, true, true);
            }
            Levels = Levels.OrderBy(l => l.Elevation).ToList();
            LevelPerimeters = LevelPerimeters.OrderBy(l => l.Elevation).ToList();
        }

        /// <summary>
        /// Creates a Level at the specified height if it falls within the vertical volume of the supplied Envelope.
        /// </summary>
        /// <param name="envelope">Envelope from which to derive the Level perimeter.</param>
        /// <param name="elevation">Elevation of the proposed Level.</param>
        /// <returns>A Level or null if no eligible envelope is found.</returns>
        public bool MakeLevel(Envelope envelope, double elevation)
        {
            var perimeter = envelope.Profile.Perimeter;
            if (perimeter.IsClockWise())
            {
                perimeter = perimeter.Reversed();
            }
            if (elevation < envelope.Elevation || elevation > envelope.Elevation + envelope.Height)
            {
                return false;
            }
            Levels.Add(new Level(elevation, Guid.NewGuid(), ""));
            LevelPerimeters.Add(new LevelPerimeter(perimeter.Area(), elevation, perimeter, Guid.NewGuid(), ""));
            Levels = Levels.OrderBy(l => l.Elevation).ToList();
            LevelPerimeters = LevelPerimeters.OrderBy(l => l.Elevation).ToList();
            return true;
        }

        /// <summary>
        /// Creates Levels within the supplied Envelope, starting at Envelope.Elevation and proceeding upward at equal intervals until placing the last Level at Envelope.Elevation + Envelope.Height.
        /// </summary>
        /// <param name="envelope">Envelope that will encompass the new Levels.</param>
        /// <param name="interval">Desired vertical distance between Levels.</param>
        /// <returns>A List of Levels ordered from lowest Elevation to highest.</returns>
        public void MakeLevels(Envelope envelope, double interval, bool first = true, bool last = true)
        {
            var perimeter = envelope.Profile.Perimeter;
            if (perimeter.IsClockWise())
            {
                perimeter = perimeter.Reversed();
            }
            if (first)
            {
                Levels.Add(new Level(envelope.Elevation, Guid.NewGuid(), ""));
                LevelPerimeters.Add(new LevelPerimeter(perimeter.Area(), envelope.Elevation, perimeter, Guid.NewGuid(), ""));
            };
            var openHeight = envelope.Height;
            var stdHeight = Math.Abs(openHeight / Math.Floor(openHeight / interval));
            var atHeight = envelope.Elevation + stdHeight;
            while (openHeight >= stdHeight * 2)
            {
                Levels.Add(new Level(atHeight, Guid.NewGuid(), ""));
                LevelPerimeters.Add(new LevelPerimeter(perimeter.Area(), atHeight, perimeter, Guid.NewGuid(), ""));
                openHeight -= stdHeight;
                atHeight += stdHeight;
            }
            if (last)
            {
                Levels.Add(new Level(envelope.Elevation + envelope.Height, Guid.NewGuid(), ""));
                LevelPerimeters.Add(new LevelPerimeter(perimeter.Area(), envelope.Elevation + envelope.Height, perimeter, Guid.NewGuid(), ""));
            }
        }

        private List<LevelVolume> MakeLevelVolumes()
        {
            List<LevelVolume> volumes = new List<LevelVolume>();
            for (int i = 0; i < LevelPerimeters.Count - 1; i++)
            {
                var thisLevelPerimeter = LevelPerimeters[i];
                var nextLevelPerimeter = LevelPerimeters[i + 1];
                var levelHeight = nextLevelPerimeter.Elevation - thisLevelPerimeter.Elevation;
                if (levelHeight > 0.01)
                {
                    volumes.Add(
                        new LevelVolume(
                            thisLevelPerimeter.Perimeter,
                            levelHeight,
                            thisLevelPerimeter.Area,
                            new Transform(0, 0, thisLevelPerimeter.Elevation),
                            BuiltInMaterials.Glass,
                            new Representation(
                                new[] { new Extrude(thisLevelPerimeter.Perimeter, levelHeight, Vector3.ZAxis, false) }
                                ),
                            false,
                            Guid.NewGuid(),
                            thisLevelPerimeter.Name));
                }

            }
            return volumes;
        }
    }
}