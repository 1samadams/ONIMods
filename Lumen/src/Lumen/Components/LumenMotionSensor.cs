using System.Collections.Generic;
using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// Switches a Lumen light on while a duplicant is within <see cref="sensorRadius"/>
    /// and off again <see cref="lingerSeconds"/> after the last one leaves.
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

        // Configured from LumenLightConfig.DoPostConfigureComplete. Plain public
        // fields, deliberately not [Serialize]: they are prefab configuration, so
        // they come back from the building definition on every load.
        public float sensorRadius = 8f;
        public float lingerSeconds = 5f;

        // Injected by KMonoBehaviour's attribute scan at spawn, which the compiler
        // cannot see -- hence the disabled "never assigned" warning.
#pragma warning disable 649
        [MyCmpReq]
        private Operational operational;
#pragma warning restore 649

        private float lingerRemaining;
        private bool occupied;

        protected override void OnSpawn()
        {
            base.OnSpawn();

            // Start dark. Setting the flag also registers it, which is what makes
            // IsOperational false until a duplicant shows up.
            occupied = false;
            lingerRemaining = 0f;
            operational.SetFlag(OccupiedFlag, false);
        }

        public void Sim200ms(float dt)
        {
            bool duplicantNearby = IsDuplicantNearby();

            if (duplicantNearby)
            {
                lingerRemaining = lingerSeconds;
            }
            else if (lingerRemaining > 0f)
            {
                lingerRemaining -= dt;
            }

            bool shouldBeLit = duplicantNearby || lingerRemaining > 0f;
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

        private bool IsDuplicantNearby()
        {
            Vector3 here = transform.GetPosition();
            float radiusSq = sensorRadius * sensorRadius;

            List<Vector3> minions = MinionPositions.Get();
            for (int i = 0; i < minions.Count; i++)
            {
                // Straight-line distance, so a duplicant on the far side of a wall
                // still trips the sensor. Accepted for now: a real line-of-sight test
                // would need the lit-cell set, which does not exist while the light
                // is off -- the state we are trying to leave.
                if ((minions[i] - here).sqrMagnitude <= radiusSq)
                {
                    return true;
                }
            }

            return false;
        }

        public List<Descriptor> GetDescriptors(GameObject go)
        {
            return new List<Descriptor>
            {
                new Descriptor(
                    string.Format(LumenStrings.SensorDescriptor, sensorRadius, lingerSeconds),
                    string.Format(LumenStrings.SensorDescriptorTooltip, sensorRadius, lingerSeconds)),
            };
        }
    }
}
