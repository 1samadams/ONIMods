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
    ///   Offset         (Vector2)                        -> which CELL the beam is
    ///                                                      cast FROM, and where the
    ///                                                      glow is drawn from
    ///
    /// Set only the first and the light lights the right tiles while the glow points
    /// the old way; set only the second and it looks right and lights the wrong
    /// tiles. Get the third wrong and the whole beam moves a tile -- see
    /// <see cref="ToOffset"/>.
    ///
    /// Orientation is Neutral=0, R90=1, R180=2, R270=3, NumRotations=4, FlipH=5,
    /// FlipV=6 (verified in Assembly-CSharp). NumRotations is a counter, never a real
    /// value, so it is not mapped.
    /// </summary>
    internal static class LumenOrientation
    {
        /// <summary>
        /// How far the emitter leans along the facing axis, away from the fixture's
        /// mounting point. Only ever a lean: <see cref="ToOffset"/> clamps the result
        /// back inside the fixture's own cell, so raising this cannot move the beam.
        /// </summary>
        private const float AimOffset = 0.35f;

        /// <summary>
        /// How far the clamped emitter position is kept from its cell's edges, in
        /// tiles. Matches the 0.05 fudge Grid.PosToCell itself applies, so a rounding
        /// wobble cannot tip the origin into a neighbouring cell.
        /// </summary>
        private const float CellMargin = 0.05f;

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

        /// <summary>
        /// Emitter position: the fixture's own mounting point, leaned along the facing
        /// axis, and clamped so it can never leave the cell that mounting point is in.
        ///
        /// The clamp is the whole point of this method. Light2D.Offset is **not**
        /// cosmetic -- its setter recomputes
        /// `origin = Grid.PosToCell(transform.position + Offset)`, and `origin` is the
        /// cell DiscreteShadowCaster casts the entire cone from. (It positions the glow
        /// too, in LightBuffer.LateUpdate; it does both jobs, not just the pretty one.)
        ///
        /// A building's transform sits at the BOTTOM of its footprint, which is why
        /// every vanilla offset is positive-Y: CEILINGLIGHT_OFFSET is +0.65 on a 1x1,
        /// FLOORLAMP_OFFSET +1.5 on a 1x2, SUNLAMP_OFFSET +3.5 on a 2x4. So a
        /// downward-aiming offset of -0.35 does not nudge the glow down a little, it
        /// moves the emitter into the cell BELOW -- dropping the whole beam one tile
        /// and leaving the fixture's own cell dark. That shipped and was play-tested as
        /// "the bulb is at zero lumens". The clamp makes it unrepresentable.
        /// </summary>
        /// <param name="direction">Where the fixture is aimed.</param>
        /// <param name="baseOffset">
        /// The fixture's configured mounting point (LumenLight.Offset) -- a
        /// vanilla-derived value that is already in the correct cell.
        /// </param>
        /// <param name="position">The building's world position.</param>
        public static Vector2 ToOffset(
            DiscreteShadowCaster.Direction direction, Vector2 baseOffset, Vector3 position)
        {
            // Leaning along the glow vector rather than a second direction table means
            // the emitter and the glow shader cannot disagree about which way is which.
            Vector2 aimed = baseOffset + AimOffset * ToGlowDirection(direction);

            Vector3 anchor = position + (Vector3)baseOffset;

            // The world-space box Grid.PosToCell maps to the anchor's cell. Y is
            // asymmetric because PosToCell is `(int)(pos.y + 0.05f)`, not a plain
            // floor; the truncation matches a floor here because ONI grid coordinates
            // are never negative.
            float minX = (int)anchor.x;
            float minY = (int)(anchor.y + CellMargin) - CellMargin;

            float x = Mathf.Clamp(position.x + aimed.x, minX + CellMargin, minX + 1f - CellMargin);
            float y = Mathf.Clamp(position.y + aimed.y, minY + CellMargin, minY + 1f - CellMargin);

            return new Vector2(x - position.x, y - position.y);
        }
    }
}
