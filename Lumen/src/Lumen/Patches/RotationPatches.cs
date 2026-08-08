using System.Reflection;
using HarmonyLib;

namespace Lumen
{
    /// <summary>
    /// Keeps a rotated Lumen fixture's beam pointed where the building is facing.
    ///
    /// Scoped by <b>component presence</b>, not by name: only objects carrying
    /// <see cref="LumenAimedLight"/> are touched, and that component is only ever
    /// added to Lumen buildings. Vanilla lights, and other mods' lights, pass straight
    /// through.
    /// </summary>
    [HarmonyPatch(typeof(Rotatable), "SetOrientation")]
    public static class RotatableSetOrientationPatch
    {
        public static void Postfix(Rotatable __instance, Orientation new_orientation)
        {
            LumenAimedLight aimed = __instance.GetComponent<LumenAimedLight>();
            if (aimed != null)
            {
                aimed.Aim(new_orientation);
            }
        }
    }

    /// <summary>
    /// Keeps the placement preview's cone pointed the same way the finished fixture
    /// will point.
    ///
    /// This is the fix for the reported "preview cone points up" bug, and the reason
    /// it happens is worth recording, because it is not our bug and not Klei's:
    ///
    ///   - Stock GetVisibleCells ignores direction for cones, so nothing ever needed
    ///     to set LightShapePreview.direction. It is left at its default, which is
    ///     Direction.North -- enum value 0.
    ///   - Rotate Everything replaces GetVisibleCells with a version that DOES read
    ///     direction for cones, globally, for every light in the game.
    ///   - It only updates LightShapePreview.direction for prefabs whose name starts
    ///     with "CeilingLight".
    ///
    /// So with that mod installed, every cone light it does not special-case suddenly
    /// previews as a cone pointing North -- straight up. That is why the vanilla Sun
    /// Lamp does it too, why the vanilla Ceiling Light does not, and why the Lumen
    /// Floor Lamp was unaffected: it is a Circle, and circles are rotation-invariant.
    ///
    /// This patch does for Lumen prefabs what that mod does for the Ceiling Light. It
    /// is harmless when the fixture is unrotatable, and harmless without the rotation
    /// mod, where the direction is ignored for cones anyway.
    /// </summary>
    [HarmonyPatch(typeof(LightShapePreview), "Update")]
    public static class LightShapePreviewUpdatePatch
    {
        /// <summary>
        /// LightShapePreview only regenerates its preview when the cursor changes
        /// cell, so changing the direction alone would not show up until you moved a
        /// tile. Resetting this private field forces a rebuild on the next Update.
        /// </summary>
        private static readonly FieldInfo PreviousCellField =
            AccessTools.Field(typeof(LightShapePreview), "previousCell");

        public static void Prefix(LightShapePreview __instance)
        {
            Rotatable rotatable = __instance.GetComponent<Rotatable>();
            if (rotatable == null)
            {
                return;
            }

            // The preview prefab is named "<PrefabID>Preview", so it carries no
            // LumenAimedLight and has to be matched by name.
            KPrefabID prefabId = __instance.GetComponent<KPrefabID>();
            if (prefabId == null || !LumenLights.IsLumenPrefab(prefabId.PrefabTag.Name))
            {
                return;
            }

            DiscreteShadowCaster.Direction direction =
                LumenOrientation.ToLightDirection(rotatable.GetOrientation());

            if (direction != __instance.direction)
            {
                __instance.direction = direction;
                PreviousCellField?.SetValue(__instance, -1);
            }
        }
    }
}
