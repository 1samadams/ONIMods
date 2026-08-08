using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// Everything that makes one Lumen fixture look different from another while they
    /// share a vanilla kanim: a colour tint and a size multiplier.
    ///
    /// Both have to be applied at spawn rather than at prefab-configure time.
    /// <c>TintColour</c> writes through to <c>batchInstanceData</c>, which does not
    /// exist until the controller is batched, so setting it on the inactive prefab
    /// template is silently dropped.
    ///
    /// No serialised state -- both values come from the building definition on every
    /// spawn.
    /// </summary>
    public class LumenAppearance : KMonoBehaviour
    {
        public Color32 colour = new Color32(255, 255, 255, 255);

        /// <summary>
        /// Multiplier on <c>KBatchedAnimController.animScale</c> (which defaults to
        /// 0.005f). Purely visual -- the building still occupies its declared cells.
        ///
        /// This is the cheapest real differentiator available without custom art:
        /// size reads as a different fixture far more strongly than colour does, and
        /// it needs no knowledge of the kanim's internals. animScale is read inside
        /// GetTransformMatrix() on every render, so assigning it at spawn is enough --
        /// no refresh call needed.
        /// </summary>
        public float animScaleMultiplier = 1f;

        /// <summary>
        /// Colour for <see cref="lensSymbols"/> only, layered over
        /// <see cref="colour"/>. Alpha 0 means unset.
        /// </summary>
        public Color32 lensColour;

        /// <summary>kanim symbols treated as the lens. May be null.</summary>
        public string[] lensSymbols;

        protected override void OnSpawn()
        {
            base.OnSpawn();

            KBatchedAnimController controller = GetComponent<KBatchedAnimController>();
            if (controller == null)
            {
                return;
            }

            controller.TintColour = colour;
            ApplyLensTint(controller);

            if (!Mathf.Approximately(animScaleMultiplier, 1f))
            {
                // Multiplied rather than assigned so this composes with anything else
                // that has adjusted the scale. OnSpawn runs once per instance, so this
                // cannot compound.
                controller.animScale *= animScaleMultiplier;
            }
        }

        /// <summary>
        /// Tints just the lens, leaving the housing on <see cref="colour"/>.
        ///
        /// Symbol tints live on the controller instance rather than on an animation,
        /// so they survive the fixture switching between its "on" and "off" anims and
        /// only need applying once.
        /// </summary>
        private void ApplyLensTint(KBatchedAnimController controller)
        {
            if (lensSymbols == null || lensColour.a == 0)
            {
                return;
            }

            foreach (string symbol in lensSymbols)
            {
                // Naming a symbol the build does not have is a no-op, not an error,
                // so a kanim swap cannot break this.
                controller.SetSymbolTint(new KAnimHashedString(symbol), lensColour);
            }
        }
    }
}
