using System;

namespace SketchGrids
{
    /// <summary>
    /// Provides graph edge info
    /// </summary>
    public class LocalEdge
    {
        [Flags]
        private enum VisitDirections
        {
            None,
            Straight,
            Opposite
        }

        /// <summary>
        /// Creates a new instance of Edge class
        /// </summary>
        /// <param name="vertexIndex1">The index of the first vertex</param>
        /// <param name="vertexIndex2">The index of the second vertex</param>
        public LocalEdge(int vertexIndex1, int vertexIndex2)
        {
            visitDirections = VisitDirections.None;
            VertexIndex1 = vertexIndex1;
            VertexIndex2 = vertexIndex2;
        }

        public int VertexIndex1 { get; }
        public int VertexIndex2 { get; }

        /// <summary>
        /// Added record that edge was visited from the vertex index
        /// </summary>
        /// <param name="vertexIndex">The index of the vertex where the edge is visited from</param>
        public void MarkAsVisited(int vertexIndex)
        {
            if (vertexIndex == VertexIndex1)
            {
                visitDirections |= VisitDirections.Straight;
            }
            else if (vertexIndex == VertexIndex2)
            {
                visitDirections |= VisitDirections.Opposite;
            }
        }

        /// <summary>
        /// Returns if this edge is between input vertex indeces
        /// </summary>
        /// <param name="vertexIndex1">The index of the first vertex</param>
        /// <param name="vertexIndex2">The index of the second vertex</param>
        /// <returns>Returns True if edge is between input vertex indeces</returns>
        public bool IsBetweenVertices(int vertexIndex1, int vertexIndex2)
        {
            return (VertexIndex1 == vertexIndex1 && VertexIndex2 == vertexIndex2) ||
                (VertexIndex1 == vertexIndex2 && VertexIndex2 == vertexIndex1);
        }

        /// <summary>
        /// Gets if the edge was visited from the vertex
        /// </summary>
        /// <param name="vertexIndex">The index of the vertex where the edge is visited from</param>
        /// <returns>Returns True if the edge was visited from the vertex</returns>
        public bool IsVisitedFromVertex(int vertexIndex)
        {
            if (VertexIndex1 == vertexIndex)
            {
                return visitDirections.HasFlag(VisitDirections.Straight);
            }

            if (VertexIndex2 == vertexIndex)
            {
                return visitDirections.HasFlag(VisitDirections.Opposite);
            }

            return false;
        }

        private VisitDirections visitDirections;
    }
}