using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// Points a rotatable Lumen fixture's beam wherever the building is facing.
    ///
    /// Only ever present on fixtures that are aimable AND running in an install where
    /// cone lights honour their direction -- see <see cref="LumenCompat"/>. Without
    /// that, rotating a cone spins the sprite and leaves the beam pointing at the
    /// floor, so the fixtures are simply not rotatable there.
    ///
    /// No serialised state. The orientation itself lives on Rotatable, which the game
    /// already saves, and this recomputes everything from it at spawn.
    /// </summary>
    public class LumenAimedLight : KMonoBehaviour
    {
#pragma warning disable 649
        [MyCmpReq]
        private Light2D light2D;
#pragma warning restore 649

        protected override void OnSpawn()
        {
            base.OnSpawn();

            // Reapply on load: a rotated fixture comes back from a save with its
            // Rotatable orientation restored but its Light2D at prefab defaults.
            Rotatable rotatable = GetComponent<Rotatable>();
            if (rotatable != null)
            {
                Aim(rotatable.GetOrientation());
            }
        }

        /// <summary>
        /// Called at spawn and from the Rotatable.SetOrientation patch. Sets all three
        /// of the things that have to agree for a light to aim -- see
        /// <see cref="LumenOrientation"/> for why one alone is never enough.
        /// </summary>
        public void Aim(Orientation orientation)
        {
            if (light2D == null)
            {
                return;
            }

            DiscreteShadowCaster.Direction direction = LumenOrientation.ToLightDirection(orientation);

            light2D.LightDirection = direction;
            light2D.Direction = LumenOrientation.ToGlowDirection(direction);
            light2D.Offset = LumenOrientation.ToOffset(direction);

            if (isSpawned)
            {
                // Rebuilds the emitter and re-registers it with the light grid. Without
                // this the new direction sits in pending_emitter_state unapplied.
                light2D.FullRefresh();
            }

            // The sensor derives its trigger area from the light's own geometry, so it
            // follows the beam around -- but only once it recomputes.
            LumenMotionSensor sensor = GetComponent<LumenMotionSensor>();
            if (sensor != null)
            {
                sensor.InvalidateLitCells();
            }
        }
    }
}
