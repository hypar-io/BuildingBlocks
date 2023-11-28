using Elements;
using Elements.Geometry;
using Elements.Spatial.AdaptiveGrid;
using System.Collections.Generic;
using System;
using System.Linq;
using GridVertex = Elements.Spatial.AdaptiveGrid.Vertex;
using AdaptiveGraphRouting = Elements.Spatial.AdaptiveGrid.AdaptiveGraphRouting;

namespace EmergencyEgress
{
    public static class EmergencyEgress
    {
        private const double OffsetFromWall = 0.5;
        private const double VisualizationHeight = 1.5;
        private const double RoomToWallTolerance = 1e-3;
        private static Material EgressMaterial = new Material("Exit Plan", new Color("Red"));

        /// <summary>
        /// The EmergencyEgress function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A EmergencyEgressOutputs instance containing computed results and the model with any new elements.</returns>
        public static EmergencyEgressOutputs Execute(Dictionary<string, Model> inputModels, EmergencyEgressInputs input)
        {
            var output = new EmergencyEgressOutputs();
            if (!inputModels.TryGetValue("Space Planning Zones", out Model spaceZonesModel))
            {
                output.Errors.Add("The model output named 'Space Planning Zones' could not be found. Check the upstream functions for errors.");
                return output;
            }

            if (!inputModels.TryGetValue("Circulation", out Model circulationModel))
            {
                output.Errors.Add("The model output named 'Circulation' could not be found. Check the upstream functions for errors.");
                return output;
            }

            var corridors = circulationModel.AllElementsOfType<CirculationSegment>();
            var rooms = spaceZonesModel.AllElementsOfType<SpaceBoundary>();

            List<Door> doors = null;
            if (inputModels.TryGetValue("Doors", out Model doorsModel))
            {
                doors = doorsModel.AllElementsOfType<Door>().ToList();
            }

            List<WallCandidate> walls = null;
            if (inputModels.TryGetValue("Interior Partitions", out Model wallsModel))
            {
                walls = wallsModel.AllElementsOfType<WallCandidate>().ToList();
            }

            var corridorsByLevel = corridors.GroupBy(c => c.Level);
            var roomsByLevel = rooms.GroupBy(r => r.Level);

            foreach (var levelRooms in roomsByLevel)
            {
                var levelCorridors = corridorsByLevel.Where(c => c.Key == levelRooms.Key).FirstOrDefault();
                if (levelCorridors == null)
                {
                    continue;
                }

                var centerlines = new List<(CirculationSegment Segment, Polyline Centerline)>();
                foreach (var item in levelCorridors)
                {
                    var centerLine = CorridorCenterLine(item);
                    if (centerLine != null && centerLine.Vertices.Count > 1)
                    {
                        centerlines.Add((item, centerLine));
                    }
                }

                AdaptiveGrid grid = new AdaptiveGrid(new Transform());

                foreach (var line in centerlines)
                {
                    grid.AddVertices(line.Centerline.Vertices,
                        AdaptiveGrid.VerticesInsertionMethod.ConnectAndSelfIntersect);
                }

                Intersect(centerlines, grid);

                var exits = LinkExits(input.ExitLocations, grid);
                if (exits == null || !exits.Any())
                {
                    continue;
                }

                var roomInfos = new List<List<RoomEvacuationVariant>>();
                foreach (var room in levelRooms)
                {
                    roomInfos.Add(AddRoom(room, centerlines, walls, doors, grid));
                }

                var alg = new AdaptiveGraphRouting(grid, new RoutingConfiguration());

                var tree = alg.BuildSimpleNetwork(
                    roomInfos.SelectMany(r => r.Select(e => e.Exit)).Select(l => new RoutingVertex(l.Id, 0)).ToList(),
                    exits, null);

                //Grid visualization for debug purposes
                /*var elements = alg.RenderElements(
                    new List<AdaptiveGraphRouting.RoutingHintLine>(),
                    grid.GetVertices().Select(v => v.Point).ToList());
                output.Model.AddElements(elements);*/

                var bestExits = ChooseRoutes(grid, tree, roomInfos);
                output.Model.AddElements(Visualize(grid, bestExits, tree));
            }

            return output;
        }

        private static Polyline CorridorCenterLine(CirculationSegment corridor)
        {
            double offsetDistance = corridor.Geometry.GetOffset();
            var corridorPolyline = corridor.Geometry.Polyline;
            if (!offsetDistance.ApproximatelyEquals(0))
            {
                corridorPolyline = corridorPolyline.OffsetOpen(offsetDistance);
            }
            corridorPolyline = corridorPolyline.TransformedPolyline(corridor.Transform);
            return corridorPolyline;
        }


        /// <summary>
        /// Create connection edges between corridors.
        /// Corridors itself are represented as middle lines without width.
        /// For each line points are found with other corridors and itself that are closer that their combined width.
        /// </summary>
        /// <param name="centerlines">Corridor segments with precalculated center lines.</param>
        /// <param name="grid">AdaptiveGrid to insert new vertices and edge into.</param>
        private static void Intersect(
            List<(CirculationSegment Segment, Polyline Centerline)> centerlines,
            AdaptiveGrid grid)
        {
            foreach (var item in centerlines)
            {
                var leftVertices = item.Centerline.Vertices;
                foreach (var candidate in centerlines)
                {
                    var rightVertices = candidate.Centerline.Vertices;
                    var maxDistance = item.Segment.Geometry.GetWidth() + candidate.Segment.Geometry.GetWidth();
                    for (int i = 0; i < leftVertices.Count - 1; i++)
                    {
                        Vector3 closestLeftItem = Vector3.Origin, closestRightItem = Vector3.Origin;
                        int closestLeftProximity = -1, closestRightProximity = -1;
                        double closestDistance = double.PositiveInfinity;

                        Action<Line, Vector3, int, int, bool> check =
                            (Line line, Vector3 point, int leftIndex, int rightIndex, bool left) =>
                            {
                                if (CanConnect(point, line, Math.Min(maxDistance, closestDistance), out var closest, out var d))
                                {
                                    closestDistance = d;
                                    (closestLeftItem, closestRightItem) = left ? (closest, point) : (point, closest);
                                    closestLeftProximity = leftIndex;
                                    closestRightProximity = rightIndex;
                                }
                            };

                        for (int j = 0; j < rightVertices.Count - 1; j++)
                        {
                            if (item == candidate && Math.Abs(i - j) < 2)
                            {
                                continue;
                            }

                            Line leftLine = new Line(leftVertices[i], leftVertices[i + 1]);
                            Line rightLine = new Line(rightVertices[j], rightVertices[j + 1]);
                            if (!leftLine.Intersects(rightLine, out var intersection))
                            {
                                check(rightLine, leftLine.Start, i, j, false);
                                check(rightLine, leftLine.End, i, j, false);
                                check(leftLine, rightLine.Start, i, j, true);
                                check(leftLine, rightLine.End, i, j, true);
                            }
                            else
                            {
                                closestLeftItem = intersection;
                                closestRightItem = intersection;
                                closestLeftProximity = i;
                                closestRightProximity = j;
                            }
                        }

                        if (closestLeftProximity == -1 || closestRightProximity == -1)
                        {
                            continue;
                        }

                        bool leftExist = grid.TryGetVertexIndex(closestLeftItem, out var leftId);
                        bool rightExist = grid.TryGetVertexIndex(closestRightItem, out var rightId);
                        if (leftExist && rightExist)
                        {
                            if (leftId != rightId)
                            {
                                grid.AddEdge(leftId, rightId);
                            }
                        }
                        else
                        {
                            GridVertex leftVertex = null;
                            if (!leftExist)
                            {
                                grid.TryGetVertexIndex(leftVertices[closestLeftProximity], out var leftCon);
                                grid.TryGetVertexIndex(leftVertices[closestLeftProximity + 1], out var rightCon);
                                var segment = new Line(leftVertices[closestLeftProximity], leftVertices[closestLeftProximity + 1]);
                                var vertex = grid.GetVertex(leftCon);
                                while (vertex.Id != rightCon)
                                {
                                    GridVertex otherVertex = null;
                                    Edge edge = null;
                                    foreach (var e in vertex.Edges)
                                    {
                                        otherVertex = grid.GetVertex(e.OtherVertexId(vertex.Id));
                                        var edgeDirection = (otherVertex.Point - vertex.Point).Unitized();
                                        if (edgeDirection.Dot(segment.Direction()).ApproximatelyEquals(1))
                                        {
                                            edge = e;
                                            break;
                                        }
                                    }

                                    if (edge == null)
                                    {
                                        throw new Exception("End edge is not reached");
                                    }

                                    var edgeLine = new Line(vertex.Point, otherVertex.Point);
                                    if (edgeLine.PointOnLine(closestLeftItem))
                                    {
                                        leftVertex = grid.CutEdge(edge, closestLeftItem);
                                        break;
                                    }

                                    vertex = otherVertex;
                                }
                            }
                            else
                            {
                                leftVertex = grid.GetVertex(leftId);
                            }

                            if (!rightExist)
                            {
                                grid.TryGetVertexIndex(rightVertices[closestRightProximity], out var leftCon);
                                grid.TryGetVertexIndex(rightVertices[closestRightProximity + 1], out var rightCon);
                                var vertex = grid.GetVertex(leftCon);
                                var connections = new List<GridVertex>();
                                if (!closestLeftItem.IsAlmostEqualTo(closestRightItem, grid.Tolerance))
                                {
                                    connections.Add(leftVertex);
                                }

                                var segment = new Line(rightVertices[closestRightProximity], rightVertices[closestRightProximity + 1]);
                                while (vertex.Id != rightCon)
                                {
                                    GridVertex otherVertex = null;
                                    Edge edge = null;
                                    foreach (var e in vertex.Edges)
                                    {
                                        otherVertex = grid.GetVertex(e.OtherVertexId(vertex.Id));
                                        var edgeDirection = (otherVertex.Point - vertex.Point).Unitized();
                                        if (edgeDirection.Dot(segment.Direction()).ApproximatelyEquals(1))
                                        {
                                            edge = e;
                                            break;
                                        }
                                    }

                                    if (edge == null)
                                    {
                                        throw new Exception("End edge is not reached");
                                    }

                                    var edgeLine = new Line(vertex.Point, otherVertex.Point);
                                    if (edgeLine.PointOnLine(closestRightItem))
                                    {
                                        connections.Add(vertex);
                                        connections.Add(otherVertex);
                                        grid.AddVertex(closestRightItem,
                                                       new ConnectVertexStrategy(connections.ToArray()),
                                                       cut: false);
                                        grid.RemoveEdge(edge);
                                        break;
                                    }

                                    vertex = otherVertex;
                                }
                            }
                            else if (leftVertex.Id != rightId)
                            {
                                grid.AddEdge(leftVertex.Id, rightId);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add SpaceBoundary, representing a room, to the grid.
        /// There are no defined exits. in the room. Every segment middle point is considered.
        /// This is very simple approaches that ignores voids or obstacles inside room and won't work for complex rooms.
        /// </summary>
        /// <param name="room">Room geometry.</param>
        /// <param name="centerlines">Corridor segments with precalculated center lines.</param>
        /// <param name="grid">AdaptiveGrid to insert new vertices and edge into.</param>
        /// <returns></returns>
        private static List<RoomEvacuationVariant> AddRoom(
            SpaceBoundary room,
            List<(CirculationSegment Segment, Polyline Centerline)> centerlines,
            List<WallCandidate>? walls,
            List<Door>? doors,
            AdaptiveGrid grid)
        {
            var roomExits = new List<RoomEvacuationVariant>();
            //Center of every segment in room boundary is checked against corridors
            var perimeter = room.Boundary.Perimeter.CollinearPointsRemoved().TransformedPolygon(room.Transform);
            foreach (var roomEdge in perimeter.Segments())
            {
                //exitVertex is already added to the grid by `FindExit`
                var exitVertex = FindRoomExit(roomEdge, centerlines, walls, doors, grid);
                if (exitVertex != null)
                {
                    //If it's close enough to corridors - it's two furthest corners are added.
                    //Note that this is done for every possible exit, so in the end there will be
                    //a lot of possible combinations of exit to it's corners in the grid,
                    //not connected one with another.
                    var twoFurthest = perimeter.Vertices.OrderBy(v =>
                        (v - exitVertex.Point).Length()).TakeLast(2);
                    var corners = new List<(GridVertex Vertex, Vector3 ExactPosition)>();
                    foreach (var furthest in twoFurthest)
                    {
                        //Two reasons why room corners edges are inset from the corners:
                        //1)For better visualizations.
                        //2)To prevent vertex unifications if room boundaries overlap.
                        var direction = (furthest - exitVertex.Point).Unitized();
                        var shrinked = furthest - direction * OffsetFromWall;
                        var leaf = grid.AddVertex(shrinked, new ConnectVertexStrategy(exitVertex), cut: false);
                        corners.Add((leaf, furthest));
                    }
                    roomExits.Add(new RoomEvacuationVariant(exitVertex, corners));
                }
            }
            return roomExits;
        }

        /// <summary>
        /// Find if edge middle point is close enough to any corridor to be considered connected.
        /// If point is closer then half corridor width then it's connected to closest point by new edge.
        /// </summary>
        /// <param name="roomEdge">Line representing room wall.</param>
        /// <param name="centerlines">Corridor segments with precalculated center lines.</param>
        /// <param name="grid">AdaptiveGrid to insert new vertices and edge into.</param>
        /// <returns>New Vertex on room edge midpoint.</returns>
        private static GridVertex FindRoomExit(
            Line roomEdge,
            List<(CirculationSegment Segment, Polyline Centerline)> centerlines,
            List<WallCandidate>? walls,
            List<Door>? doors,
            AdaptiveGrid grid)
        {
            var door = doors?.FirstOrDefault(d => roomEdge.PointOnLine(d.Transform.Origin, false, RoomToWallTolerance));
            var wall = walls?.FirstOrDefault(w => w.Line.PointOnLine(roomEdge.Start, true, RoomToWallTolerance) &&
                                             w.Line.PointOnLine(roomEdge.End, true, RoomToWallTolerance));
            
            // There are doors in the workflow and this segment is a wall without a door.
            if (wall != null && doors != null && door == null)
            {
                return null;
            }

            var midpoint = door?.Transform.Origin ?? roomEdge.Mid();

            foreach (var line in centerlines)
            {
                for (int i = 0; i < line.Centerline.Vertices.Count - 1; i++)
                {
                    var segment = new Line(line.Centerline.Vertices[i], line.Centerline.Vertices[i + 1]);
                    if (CanConnect(midpoint, segment, line.Segment.Geometry.GetWidth() / 2 + 0.1, out var closest, out _))
                    {
                        GridVertex exitVertex = null;
                        grid.TryGetVertexIndex(segment.Start, out var id);
                        var vertex = grid.GetVertex(id);

                        //We know corridor line but it can already be split into several edges.
                        //Need to find exact edge to insert new vertex into.
                        //First vertex corresponding start of the segment is found.
                        //Then, edges that do in the same direction as segment is traversed
                        //until target edge is found or end vertex is reached.
                        //This is much faster than traverse every single edge in the grid.
                        if (vertex.Point.IsAlmostEqualTo(closest, grid.Tolerance))
                        {
                            exitVertex = vertex;
                        }
                        else
                        {
                            while (!vertex.Point.IsAlmostEqualTo(segment.End, grid.Tolerance))
                            {
                                Edge edge = null;
                                GridVertex otherVertex = null;
                                foreach (var e in vertex.Edges)
                                {
                                    otherVertex = grid.GetVertex(e.OtherVertexId(vertex.Id));
                                    var edgeDirection = (otherVertex.Point - vertex.Point).Unitized();
                                    if (edgeDirection.Dot(segment.Direction()).ApproximatelyEquals(1))
                                    {
                                        edge = e;
                                        break;
                                    }
                                }

                                if (edge == null)
                                {
                                    break;
                                }

                                if (otherVertex.Point.IsAlmostEqualTo(closest, grid.Tolerance))
                                {
                                    exitVertex = otherVertex;
                                    break;
                                }

                                var edgeLine = new Line(vertex.Point, otherVertex.Point);
                                if (edgeLine.PointOnLine(closest))
                                {
                                    exitVertex = grid.AddVertex(closest,
                                                                new ConnectVertexStrategy(vertex, otherVertex),
                                                                cut: false);
                                    grid.RemoveEdge(edge);
                                }
                                vertex = otherVertex;
                            }
                        }

                        if (exitVertex != null)
                        {
                            if (!exitVertex.Point.IsAlmostEqualTo(midpoint, grid.Tolerance))
                            {
                                return grid.AddVertex(midpoint, new ConnectVertexStrategy(exitVertex), cut: false);
                            }
                            else
                            {
                                return exitVertex;
                            }
                        }
                    }
                }
            }
            return null;
        }


        /// <summary>
        /// Add exit points to the grid that are close enough to any of existing edges.
        /// </summary>
        /// <param name="exits">Exit points positions.</param>
        /// <param name="grid">AdaptiveGrid to insert new vertices and edge into.</param>
        /// <returns>Ids of exit vertices that are added to the grid.</returns>
        private static List<ulong> LinkExits(IList<Vector3> exits, AdaptiveGrid grid)
        {
            List<ulong> exitVertices = new List<ulong>();
            foreach (var exit in exits)
            {
                var edge = grid.ClosestEdge(exit, out var closest);
                var startVertex = grid.GetVertex(edge.StartId);
                var endVertex = grid.GetVertex(edge.EndId);

                var exitOnLevel = new Vector3(exit.X, exit.Y, closest.Z);
                var distance = exitOnLevel.DistanceTo(closest);

                //TODO: do something better.
                //This is a way to filter exit points that are near other levels.
                //It's done by distance but there should be a better way.
                if (distance < 2)
                {
                    GridVertex closestVertex;
                    if (closest.IsAlmostEqualTo(startVertex.Point, grid.Tolerance))
                    {
                        closestVertex = startVertex;
                    }
                    else if (closest.IsAlmostEqualTo(endVertex.Point, grid.Tolerance))
                    {
                        closestVertex = endVertex;
                    }
                    else
                    {
                        closestVertex = grid.CutEdge(edge, closest);
                    }

                    //Snap to existing vertex if it's close enough.
                    if (distance < 0.1)
                    {
                        exitVertices.Add(closestVertex.Id);
                    }
                    else
                    {
                        var vertex = grid.AddVertex(exitOnLevel, new ConnectVertexStrategy(closestVertex), cut: false);
                        exitVertices.Add(vertex.Id);
                    }
                }
            }
            return exitVertices.Distinct().ToList();
        }

        /// <summary>
        /// For each room find exit that provides smallest distance for it's furthest corner.
        /// </summary>
        /// <param name="grid">AdaptiveGrid to traverse.</param>
        /// <param name="tree">Traveling tree from rooms corners to exits.</param>
        /// <param name="roomInfos">Combinations of exits and their corresponding corners for each room.</param>
        /// <returns>Most distance efficient exit information for each room.</returns>
        public static List<RoomEvacuationVariant> ChooseRoutes(
            AdaptiveGrid grid,
            IDictionary<ulong, TreeNode> tree,
            List<List<RoomEvacuationVariant>> roomInfos)
        {
            List<RoomEvacuationVariant> bestExits = new List<RoomEvacuationVariant>();
            foreach (var roomExits in roomInfos)
            {
                if (roomExits.Count == 1)
                {
                    bestExits.Add(roomExits[0]);
                }
                else
                {
                    double bestLength = double.PositiveInfinity;
                    RoomEvacuationVariant bestExit = null;
                    foreach (var exit in roomExits)
                    {
                        double accumulatedLength = 0;
                        var current = tree[exit.Exit.Id];
                        while (current.Trunk != null)
                        {
                            var p0 = grid.GetVertex(current.Id).Point;
                            var p1 = grid.GetVertex(current.Trunk.Id).Point;
                            accumulatedLength += p0.DistanceTo(p1);
                            current = current.Trunk;
                        }

                        var maxCornerDistance = exit.Corners.Max(c => c.ExactPosition.DistanceTo(exit.Exit.Point));
                        accumulatedLength += maxCornerDistance;
                        if (accumulatedLength < bestLength)
                        {
                            bestLength = accumulatedLength;
                            bestExit = exit;
                        }
                    }

                    if (bestExit != null)
                    {
                        bestExits.Add(bestExit);
                    }
                }
            }
            return bestExits;
        }

        private static List<ModelCurve> Visualize(
            AdaptiveGrid grid,
            List<RoomEvacuationVariant> inputs,
            IDictionary<ulong, TreeNode> tree)
        {
            Dictionary<Edge, double> accumulatedDistances = new Dictionary<Edge, double>();
            List<ModelCurve> visualizations = new List<ModelCurve>();
            var t = new Transform(0, 0, VisualizationHeight);
            foreach (var input in inputs)
            {
                //Distances are caches in Dictionary. It's mostly to draw edge lines only once as
                //distance calculations are cheap.
                CalculateDistanceRecursive(grid, input.Exit, tree, accumulatedDistances);

                double distanceOutside = 0;
                var node = tree[input.Exit.Id];
                if (node != null && node.Trunk != null)
                {
                    var outgoingEdge = input.Exit.GetEdge(node.Trunk.Id);
                    distanceOutside = accumulatedDistances[outgoingEdge];
                }

                //Draw only furthest corner line except for the case if several corners are on the same distance.
                var ordered = input.Corners.OrderByDescending(c => c.ExactPosition.DistanceTo(input.Exit.Point));
                var enumerator = ordered.GetEnumerator();
                enumerator.MoveNext();
                var corner = enumerator.Current;
                var firstDistance = corner.ExactPosition.DistanceTo(input.Exit.Point);
                var distance = firstDistance;
                while (distance / firstDistance > 1 - 1e-3)
                {
                    //Draw shrinker version of the corner
                    var shape = new Line(input.Exit.Point, corner.Vertex.Point);
                    var modelCurve = new ModelCurve(shape, EgressMaterial, t);
                    //Attached accumulated distance information to corner lines.
                    modelCurve.AdditionalProperties["Distance"] = distance + distanceOutside;
                    visualizations.Add(modelCurve);

                    if (!enumerator.MoveNext())
                    {
                        break;
                    }

                    corner = enumerator.Current;
                    distance = corner.ExactPosition.DistanceTo(input.Exit.Point);
                }
            }

            foreach (var item in accumulatedDistances)
            {
                var start = grid.GetVertex(item.Key.StartId);
                var end = grid.GetVertex(item.Key.EndId);
                var shape = new Line(start.Point, end.Point);
                var modelCurve = new ModelCurve(shape, EgressMaterial, t);
                visualizations.Add(modelCurve);
            }

            return visualizations;
        }

        private static double CalculateDistanceRecursive(
            AdaptiveGrid grid,
            GridVertex head,
            IDictionary<ulong, TreeNode> tree,
            Dictionary<Edge, double> accumulatedDistances)
        {
            var node = tree[head.Id];
            if (node == null || node.Trunk == null)
            {
                return 0;
            }

            var edge = head.GetEdge(node.Trunk.Id);
            if (accumulatedDistances.TryGetValue(edge, out double distance))
            {
                return distance;
            }

            var tail = grid.GetVertex(node.Trunk.Id);
            var d = CalculateDistanceRecursive(grid, tail, tree, accumulatedDistances);
            d += tail.Point.DistanceTo(head.Point);
            accumulatedDistances[edge] = d;
            return d;
        }

        private static bool CanConnect(Vector3 point,
                                       Line segment,
                                       double maxDistance,
                                       out Vector3 closest,
                                       out double dist)
        {
            dist = point.DistanceTo(segment, out closest);
            var dot = (closest - point).Unitized().Dot(segment.Direction());
            bool aligned = dot.ApproximatelyEquals(0) || Math.Abs(dot).ApproximatelyEquals(1);
            return dist < maxDistance && aligned;
        }
    }
}