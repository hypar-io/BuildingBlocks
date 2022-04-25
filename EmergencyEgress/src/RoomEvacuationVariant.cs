using System.Collections.Generic;
using System;
using Elements.Geometry;
using GridVertex = Elements.Spatial.AdaptiveGrid.Vertex;

namespace EmergencyEgress
{
    public class RoomEvacuationVariant
    {
        public RoomEvacuationVariant(
            GridVertex exit, 
            List<(GridVertex Vertex, Vector3 ExactPosition)> corners)
        {
            Exit = exit;
            Corners = corners;
        }

        public GridVertex Exit
        {
            get; private set;
        }

        public List<(GridVertex Vertex, Vector3 ExactPosition)> Corners
        {
            get; private set;
        }
    }
}