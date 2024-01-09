using Elements.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SketchGrids
{
    public class VectorEqualityComparer : IEqualityComparer<Vector3>
    {
        public bool Equals([AllowNull] Vector3 x, [AllowNull] Vector3 y)
        {
            return x.IsAlmostEqualTo(y);
        }

        public int GetHashCode([DisallowNull] Vector3 obj)
        {
            // Stolen from the vector 3 class, but using
            // less precision to enforce matches.
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Math.Round(obj.X, 5).GetHashCode();
                hash = hash * 23 + Math.Round(obj.Y, 5).GetHashCode();
                hash = hash * 23 + Math.Round(obj.Z, 5).GetHashCode();
                return hash;
            }
        }
    }
}