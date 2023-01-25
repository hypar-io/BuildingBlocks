using System;
using System.Linq;
using System.Collections.Generic;
using Elements;
using Elements.Geometry;
using GeometryEx;
using RoomKit;

namespace PlanByProgram
{
    public static class Placer
    {
        private struct SuiteID
        {
            public string name;
            public string number;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="roomDefs"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private static List<Suite> SuiteMaker(PlanByProgramInputs inputs,
                                              List<RoomDefinition> roomDefs,
                                              double ratio = 0.5)
        {
            var suiteIDs = new List<SuiteID>();
            foreach (var roomDef in roomDefs)
            {
                suiteIDs.Add(new SuiteID { name = roomDef.SuiteName, number = roomDef.SuiteNumber });
            }
            suiteIDs = suiteIDs.Distinct().ToList();
            var suites = new List<Suite>();
            int index = 0;
            foreach (var suiteID in suiteIDs)
            {
                var rooms = new List<RoomKit.Room>();
                var suiteRooms = roomDefs.FindAll(d => d.SuiteName == suiteID.name && d.SuiteNumber == suiteID.number);
                var i = 0;
                var colors = Palette.ColorList();
                foreach (var rmDef in suiteRooms)
                {
                    for (var j = 0; j < rmDef.RoomQuantity; j++)
                    {
                        rooms.Add(new RoomKit.Room(rmDef.RoomArea, rmDef.RoomDimensionRatio, 1.0)
                        {
                            Color = colors[i % colors.Count],
                            DesignArea = rmDef.RoomArea,
                            DesignRatio = rmDef.RoomDimensionRatio,
                            Department = rmDef.RoomDepartment,
                            Elevation = 0.0,
                            Height = rmDef.RoomHeight,
                            Name = rmDef.Name,
                            Suite = rmDef.SuiteName,
                            SuiteID = rmDef.SuiteNumber
                        });
                    }
                    i++;
                }
                Suite.SuitePlan plan = Suite.SuitePlan.Reciprocal;
                if (inputs.SuitePlanType == PlanByProgramInputsSuitePlanType.Axis)
                {
                    plan = Suite.SuitePlan.Axis;
                }
                suites.Add(new Suite(suiteID.name, suiteID.number, rooms, ratio, inputs.CorridorWidth, plan));
                index++;
            }
            return suites;
        }

        private static Polygon PlaceAdjacentToFootprint(Polygon suitBox, List<Polygon> footPrints, bool diagonal)
        {
            Polygon footPrint = null;
            foreach (var ftPrint in footPrints)
            {
                footPrint = Shaper.PlaceOrthogonal(ftPrint, suitBox, true, true);
                if (!footPrint.Intersects(footPrints))
                {
                    return footPrint;
                }
                footPrint = Shaper.PlaceOrthogonal(ftPrint, suitBox, true, false);
                if (!footPrint.Intersects(footPrints))
                {
                    return footPrint;
                }
                footPrint = Shaper.PlaceOrthogonal(ftPrint, suitBox, false, true);
                if (!footPrint.Intersects(footPrints))
                {
                    return footPrint;
                }
                footPrint = Shaper.PlaceOrthogonal(ftPrint, suitBox, false, false);
                if (!footPrint.Intersects(footPrints))
                {
                    return footPrint;
                }
                if (!diagonal)
                {
                    continue;
                }
                footPrint = Shaper.PlaceDiagonal(ftPrint, suitBox, Corner.NE);
                if (!footPrint.Intersects(footPrints))
                {
                    return footPrint;
                }
                footPrint = Shaper.PlaceDiagonal(ftPrint, suitBox, Corner.SE);
                if (!footPrint.Intersects(footPrints))
                {
                    return footPrint;
                }
                footPrint = Shaper.PlaceDiagonal(ftPrint, suitBox, Corner.NW);
                if (!footPrint.Intersects(footPrints))
                {
                    return footPrint;
                }
                footPrint = Shaper.PlaceDiagonal(ftPrint, suitBox, Corner.SW);
                if (!footPrint.Intersects(footPrints))
                {
                    return footPrint;
                }
            }
            return footPrint;
        }

        private static Polygon PlaceAdjacentToSuite(Polygon lastBox, Polygon suitBox,
                                                    List<Polygon> footPrints,
                                                    bool northeast, bool minCoord, bool diagonal)
        {
            var footPrint = Shaper.PlaceOrthogonal(lastBox, suitBox, northeast, minCoord);
            if (!footPrint.Intersects(footPrints))
            {
                return footPrint;
            }
            footPrint = Shaper.PlaceOrthogonal(lastBox, suitBox, northeast, !minCoord);
            if (!footPrint.Intersects(footPrints))
            {
                return footPrint;
            }
            footPrint = Shaper.PlaceOrthogonal(lastBox, suitBox, !northeast, minCoord);
            if (!footPrint.Intersects(footPrints))
            {
                return footPrint;
            }
            footPrint = Shaper.PlaceOrthogonal(lastBox, suitBox, !northeast, !minCoord);
            if (!footPrint.Intersects(footPrints))
            {
                return footPrint;
            }
            var corners = new List<Corner>();
            if (!diagonal)
            {
                return null;
            }
            if (northeast)
            {
                corners.Add(Corner.NE);
                corners.Add(Corner.NW);
                corners.Add(Corner.SE);
                corners.Add(Corner.SW);
            }
            else
            {
                corners.Add(Corner.SW);
                corners.Add(Corner.SE);
                corners.Add(Corner.NW);
                corners.Add(Corner.NE);
            }
            footPrint = Shaper.PlaceDiagonal(lastBox, suitBox, corners[0]);
            if (!footPrint.Intersects(footPrints))
            {
                return footPrint;
            }
            footPrint = Shaper.PlaceDiagonal(lastBox, suitBox, corners[1]);
            if (!footPrint.Intersects(footPrints))
            {
                return footPrint;
            }
            footPrint = Shaper.PlaceDiagonal(lastBox, suitBox, corners[2]);
            if (!footPrint.Intersects(footPrints))
            {
                return footPrint;
            }
            footPrint = Shaper.PlaceDiagonal(lastBox, suitBox, corners[3]);
            if (!footPrint.Intersects(footPrints))
            {
                return footPrint;
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="roomDefs"></param>
        /// <returns></returns>
        public static List<Suite> PlaceSuites(PlanByProgramInputs inputs, List<RoomDefinition> roomDefs)
        {
            bool northeast = true;
            bool minCoord = true;
            bool diagonal = inputs.DiagonalAdjacency;
            if (inputs.PrimaryDirection == PlanByProgramInputsPrimaryDirection.Southwest)
            {
                northeast = false;
            }
            if (inputs.CoordinateAdjacency == PlanByProgramInputsCoordinateAdjacency.Maximum)
            {
                minCoord = false;
            }
            var suites = SuiteMaker(inputs, roomDefs, inputs.SuiteRatio);
            var footPrints = new List<Polygon>
            {
                suites.First().CompassCorridor.Box
            };
            var anchorSuite = suites.First();
            var elevation = 0.0;
            var height = 0.0;
            foreach (var suite in suites.Skip(1))
            {
                var testHeight = suite.Rooms.OrderByDescending(r => r.Height).First().Height;
                if (testHeight > height)
                {
                    height = testHeight;
                }
                var footprint = PlaceAdjacentToSuite(anchorSuite.CompassCorridor.Box,
                                                     suite.CompassCorridor.Box,
                                                     footPrints,
                                                     northeast,
                                                     minCoord,
                                                     diagonal);

                if (footprint == null && inputs.MultipleLevels == true)
                {
                    elevation += height + inputs.PlenumHeight;
                    footPrints.Clear();
                    footprint = suite.CompassCorridor.Box;
                    anchorSuite = suite;
                }
                if (footprint == null && inputs.MultipleLevels == false)
                {
                    footprint = PlaceAdjacentToFootprint(suite.CompassCorridor.Box, footPrints, diagonal);
                    if (footprint != null)
                    {
                        height = 0.0;
                    }
                }
                if (footprint == null)
                {
                    continue;
                }
                if (!Shaper.NearEqual(suite.CompassCorridor.AspectRatio, footprint.Compass().AspectRatio))
                {
                    suite.Rotate(Vector3.Origin, 90.0);
                }
                suite.MoveFromTo(suite.CompassCorridor.SW, footprint.Compass().SW);
                suite.Elevation = elevation;
                footPrints.Add(suite.CompassCorridor.Box);
                footPrints = Shaper.Merge(footPrints);
            }
            return suites;
        }
    }
}