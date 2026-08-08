using System;

namespace Lumen
{
    /// <summary>
    /// What the rest of the mod is allowed to assume about the running game.
    ///
    /// Exists for one question: can a cone light point anywhere other than down?
    /// In stock Oxygen Not Included the answer is no.
    /// DiscreteShadowCaster.GetVisibleCells scans Octant.S_SE and S_SW for
    /// LightShape.Cone unconditionally and never reads the direction it was passed --
    /// only ScanQuad honours it. So setting Light2D.LightDirection on a cone does
    /// precisely nothing.
    ///
    /// The "Rotate Everything" mod replaces that method with a prefix that picks the
    /// octant pair from the direction, which makes cones genuinely aimable for every
    /// light in the game, Lumen's included. That is what these fixtures rotate on top
    /// of; without it, rotating a cone light would spin the sprite while the beam
    /// stayed pointing at the floor, which is worse than not offering rotation at all.
    /// </summary>
    internal static class LumenCompat
    {
        private const string RotateEverythingAssembly = "rotate_everything";

        private static bool? conesAreDirectional;

        /// <summary>
        /// True when something in this install has made cone lights honour their
        /// direction.
        ///
        /// Resolved lazily and cached. Lazily because mod load order is not
        /// guaranteed: this is first read while building definitions are created,
        /// which happens well after every mod assembly is loaded, whereas reading it
        /// during OnLoad could race.
        /// </summary>
        public static bool ConesAreDirectional
        {
            get
            {
                if (!conesAreDirectional.HasValue)
                {
                    conesAreDirectional = IsAssemblyLoaded(RotateEverythingAssembly);
                    UnityEngine.Debug.Log(conesAreDirectional.Value
                        ? "[Lumen] Rotate Everything detected; Lumen lights will be rotatable in 4 directions."
                        : "[Lumen] Rotate Everything not detected; Lumen lights stay fixed, because a stock cone " +
                          "light cannot aim anywhere but down.");
                }

                return conesAreDirectional.Value;
            }
        }

        private static bool IsAssemblyLoaded(string name)
        {
            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Equals(assembly.GetName().Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
