using System.Collections.Generic;
using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// Switches a Lumen light on while a duplicant is standing somewhere the light
    /// would actually illuminate, and off again <see cref="lingerSeconds"/> after the
    /// last one leaves.
    ///
    /// It does this by driving one <see cref="Operational.Flag"/> rather than by
    /// touching Light2D, the animation or the power draw directly. That single flag is
    /// enough because of how the vanilla chain is wired (verified against build 744825):
    ///
    ///   Operational.SetFlag(false)
    ///     -> UpdateOperational() sees a false flag, so IsOperational = false
    ///     -> SetActive(false), and EnergyConsumer.WattsUsed returns 0 when !IsActive
    ///     -> Trigger(OperationalChanged, false)
    ///          -> Light2D.OnOperationalChangedDelegate sets Light2D.enabled = false
    ///             (it has autoRespondToOperational = true), removing the emitter
    ///             from the light grid
    ///          -> LightController transitions on -> off and plays the "off" anim
    ///
    /// So one flag gets the correct visuals, the correct light grid state and a
    /// genuine zero-watt draw, all through code Klei already maintains.
    ///
    /// No field here is [Serialize] and the class is not ISaveLoadable, so it writes
    /// nothing to the save file. Operational.Flags is likewise not serialised, so a
    /// save made with this mod installed contains no trace of it.
    /// </summary>
    public class LumenMotionSensor : KMonoBehaviour, ISim200ms, IGameObjectEffectDescriptor
    {
        /// <summary>
        /// Shared by every Lumen light. Operational.Flags is keyed by reference, so
        /// this must be a single static instance -- a per-instance Flag would add a
        /// new dictionary entry on every call and never resolve to the same key.
        /// </summary>
        private static readonly Operational.Flag OccupiedFlag =
            new Operational.Flag("lumen_occupied", Operational.Flag.Type.Requirement);

        /// <summary>
        /// Scratch buffer for <see cref="DiscreteShadowCaster.GetVisibleCells"/>.
        /// Static and reused: rebuilding lit cells must not allocate.
        /// </summary>
        private static readonly List<int> scratchCells = new List<int>(512);

        /// <summary>
        /// How often the lit-cell set is recomputed. Only walls being built or dug out
        /// can invalidate it, which is rare, so this is deliberately lazy rather than
        /// hooked into the solid-change partitioner.
        /// </summary>
        private const float LitCellRefreshSeconds = 5f;

        // Configured from LumenLightConfig.DoPostConfigureComplete. Plain public
        // fields, deliberately not [Serialize]: they are prefab configuration, so
        // they come back from the building definition on every load.

        /// <summary>
        /// Extra straight-line reach *beyond* the lit area, in tiles. Zero means the
        /// fixture triggers strictly on what it lights, which is what every Lumen
        /// light except the Sentry wants. The Sentry sets this above zero on purpose,
        /// so it sees duplicants coming and lights the corridor before they arrive.
        /// </summary>
        public float extraSensorRadius;

        public float lingerSeconds = 5f;

#pragma warning disable 649
        // Injected by KMonoBehaviour's attribute scan at spawn, which the compiler
        // cannot see -- hence the disabled "never assigned" warning.
        [MyCmpReq]
        private Operational operational;

        [MyCmpReq]
        private Light2D light2D;
#pragma warning restore 649

        /// <summary>
        /// The cells this fixture would light if it were on. Computed with the same
        /// shadow caster the light grid itself uses, so it accounts for walls and for
        /// the cone's real shape -- and it works while the light is off, which is the
        /// state we need to decide out of.
        /// </summary>
        private readonly HashSet<int> litCells = new HashSet<int>();

        private float sinceLitCellRefresh = float.MaxValue;
        private float lingerRemaining;
        private bool occupied;

        protected override void OnSpawn()
        {
            base.OnSpawn();

            RebuildLitCells();

            // Start dark. Setting the flag also registers it, which is what makes
            // IsOperational false until a duplicant shows up.
            occupied = false;
            lingerRemaining = 0f;
            operational.SetFlag(OccupiedFlag, false);
        }

        public void Sim200ms(float dt)
        {
            sinceLitCellRefresh += dt;
            if (sinceLitCellRefresh >= LitCellRefreshSeconds)
            {
                RebuildLitCells();
            }

            bool duplicantPresent = IsDuplicantPresent();

            if (duplicantPresent)
            {
                lingerRemaining = lingerSeconds;
            }
            else if (lingerRemaining > 0f)
            {
                lingerRemaining -= dt;
            }

            bool shouldBeLit = duplicantPresent || lingerRemaining > 0f;
            if (shouldBeLit == occupied)
            {
                return;
            }

            // Only touch Operational on an actual edge. SetFlag is cheap but it
            // triggers events that ripple into the light grid, so calling it every
            // tick would churn LightGridManager for no reason.
            occupied = shouldBeLit;
            operational.SetFlag(OccupiedFlag, shouldBeLit);
        }

        /// <summary>
        /// Asks the game which cells this fixture would light.
        ///
        /// This replaced a plain distance check, which was wrong in both directions:
        /// the sensor described a sphere around the fixture while the light describes
        /// a downward cone. A radius small enough not to catch duplicants sideways
        /// through a wall never reached the floor underneath, and one large enough to
        /// reach the floor fired constantly on people it was not lighting.
        ///
        /// Using the shadow caster removes the guesswork: the trigger condition is now
        /// exactly "this duplicant would be lit". That also lines the mod up with the
        /// work speed bonus, which Workable grants on Grid.LightIntensity at
        /// Grid.PosToCell(worker.gameObject) -- the same cell this tests.
        /// </summary>
        private void RebuildLitCells()
        {
            sinceLitCellRefresh = 0f;
            litCells.Clear();

            if (light2D == null)
            {
                return;
            }

            int origin = Grid.PosToCell(transform.GetPosition() + (Vector3)light2D.Offset);
            if (!Grid.IsValidCell(origin))
            {
                return;
            }

            scratchCells.Clear();
            DiscreteShadowCaster.GetVisibleCells(
                origin,
                scratchCells,
                (int)light2D.Range,
                light2D.Width,
                light2D.LightDirection,
                light2D.shape);

            for (int i = 0; i < scratchCells.Count; i++)
            {
                litCells.Add(scratchCells[i]);
            }
        }

        private bool IsDuplicantPresent()
        {
            Vector3 here = transform.GetPosition();
            float extraSq = extraSensorRadius * extraSensorRadius;
            bool hasExtraReach = extraSensorRadius > 0f;

            List<Vector3> minions = MinionPositions.Get();
            for (int i = 0; i < minions.Count; i++)
            {
                Vector3 position = minions[i];

                // The duplicant's own cell is their feet, which is also the cell the
                // game reads for the light work speed bonus.
                int cell = Grid.PosToCell(position);
                if (Grid.IsValidCell(cell) && litCells.Contains(cell))
                {
                    return true;
                }

                // Sentry-style early warning. Deliberately a raw straight-line radius
                // that ignores walls: the point is to notice someone approaching from
                // outside the lit area.
                if (hasExtraReach && (position - here).sqrMagnitude <= extraSq)
                {
                    return true;
                }
            }

            return false;
        }

        public List<Descriptor> GetDescriptors(GameObject go)
        {
            string text = ((extraSensorRadius > 0f)
                ? string.Format(LumenStrings.SensorDescriptorExtended, extraSensorRadius, lingerSeconds)
                : string.Format(LumenStrings.SensorDescriptor, lingerSeconds));

            return new List<Descriptor>
            {
                new Descriptor(text, text),
            };
        }
    }
}
