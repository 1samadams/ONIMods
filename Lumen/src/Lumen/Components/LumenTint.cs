using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// Applies a colour multiply to the building's reused vanilla kanim.
    ///
    /// This is what makes five buildings sharing three Klei anims read as five
    /// distinct fixtures without any custom art. It has to run at spawn rather than
    /// at prefab-configure time: KBatchedAnimController.TintColour writes through to
    /// batchInstanceData, which does not exist until the controller is actually
    /// batched, so setting it on the inactive prefab template is silently dropped.
    ///
    /// No serialised state -- the colour comes from the building definition every
    /// time the object spawns.
    /// </summary>
    public class LumenTint : KMonoBehaviour
    {
        public Color32 colour = new Color32(255, 255, 255, 255);

        protected override void OnSpawn()
        {
            base.OnSpawn();

            KBatchedAnimController controller = GetComponent<KBatchedAnimController>();
            if (controller != null)
            {
                controller.TintColour = colour;
            }
        }
    }
}
