using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// Maps a building's <see cref="Orientation"/> onto the three separate things a
    /// Light2D needs in order to actually aim.
    ///
    /// Those three are genuinely independent and all must agree, which is the whole
    /// subtlety of aiming a light:
    ///
    ///   LightDirection (DiscreteShadowCaster.Direction) -> which CELLS are lit
    ///   Direction      (Vector2)                        -> which way the GLOW is
    ///                                                      drawn, via LightBuffer's
    ///                                                      _LightDirectionAngle
    ///   Offset         (Vector2)                        -> where the emitter sits
    ///
    /// Set only the first and the light lights the right tiles while the glow points
    /// the old way; set only the second and it looks right and lights the wrong
    /// tiles.
    ///
    /// Orientation is Neutral=0, R90=1, R180=2, R270=3, NumRotations=4, FlipH=5,
    /// FlipV=6 (verified in Assembly-CSharp). NumRotations is a counter, never a real
    /// value, so it is not mapped.
    /// </summary>
    internal static class LumenOrientation
    {
        /// <summary>
        /// How far from the building's transform the emitter sits along its facing
        /// axis. Purely cosmetic: it moves where the glow is drawn from, and at this
        /// magnitude it never changes which cell Grid.PosToCell picks as the origin.
        /// </summary>
        private const float AimOffset = 0.35f;

        private const float CrossOffset = 0.05f;

        public static DiscreteShadowCaster.Direction ToLightDirection(Orientation orientation)
        {
            switch (orientation)
            {
                case Orientation.R90:
                    return DiscreteShadowCaster.Direction.West;
                case Orientation.R180:
                case Orientation.FlipV:
                    return DiscreteShadowCaster.Direction.North;
                case Orientation.R270:
                    return DiscreteShadowCaster.Direction.East;
                default:
                    // Neutral and FlipH both leave a ceiling fixture pointing down,
                    // which is also the sane fallback for anything unexpected.
                    return DiscreteShadowCaster.Direction.South;
            }
        }

        /// <summary>Unit vector for the glow shader.</summary>
        public static Vector2 ToGlowDirection(DiscreteShadowCaster.Direction direction)
        {
            switch (direction)
            {
                case DiscreteShadowCaster.Direction.North:
                    return new Vector2(0f, 1f);
                case DiscreteShadowCaster.Direction.East:
                    return new Vector2(1f, 0f);
                case DiscreteShadowCaster.Direction.West:
                    return new Vector2(-1f, 0f);
                default:
                    return new Vector2(0f, -1f);
            }
        }

        /// <summary>Emitter position, nudged along the facing axis.</summary>
        public static Vector2 ToOffset(DiscreteShadowCaster.Direction direction)
        {
            switch (direction)
            {
                case DiscreteShadowCaster.Direction.North:
                    return new Vector2(CrossOffset, AimOffset);
                case DiscreteShadowCaster.Direction.East:
                    return new Vector2(AimOffset, CrossOffset);
                case DiscreteShadowCaster.Direction.West:
                    return new Vector2(0f - AimOffset, CrossOffset);
                default:
                    return new Vector2(CrossOffset, 0f - AimOffset);
            }
        }
    }
}
