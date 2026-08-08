using System.Collections.Generic;
using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// One shared snapshot of where every live duplicant is, rebuilt at most once per
    /// frame.
    ///
    /// Without this, N lights each walk the duplicant list every 200 ms and call
    /// transform.GetPosition() M times -- N*M transform reads per tick for data that
    /// is identical across all N callers. With it, the list is walked once and the
    /// lights read a plain Vector3 array.
    /// </summary>
    internal static class MinionPositions
    {
        private static readonly List<Vector3> positions = new List<Vector3>(64);
        private static int cachedFrame = -1;

        /// <summary>
        /// Positions of all live duplicants. The returned list is reused between
        /// calls -- read it, do not retain it.
        /// </summary>
        public static List<Vector3> Get()
        {
            int frame = Time.frameCount;
            if (cachedFrame == frame)
            {
                return positions;
            }

            cachedFrame = frame;
            positions.Clear();

            List<MinionIdentity> minions = Components.LiveMinionIdentities.Items;
            for (int i = 0; i < minions.Count; i++)
            {
                MinionIdentity minion = minions[i];
                // Cmps compacts on removal but a destroyed-this-frame entry can still
                // be present; the Unity null check catches it.
                if (minion != null)
                {
                    positions.Add(minion.transform.GetPosition());
                }
            }

            return positions;
        }
    }
}
