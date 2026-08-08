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

        protected override void OnSpawn()
        {
            base.OnSpawn();

            KBatchedAnimController controller = GetComponent<KBatchedAnimController>();
            if (controller == null)
            {
                return;
            }

            controller.TintColour = colour;

            if (!Mathf.Approximately(animScaleMultiplier, 1f))
            {
                // Multiplied rather than assigned so this composes with anything else
                // that has adjusted the scale. OnSpawn runs once per instance, so this
                // cannot compound.
                controller.animScale *= animScaleMultiplier;
            }

            LumenSymbolDump.DumpOnce(controller);
        }
    }

    /// <summary>
    /// TEMPORARY DIAGNOSTIC. Logs the symbol names inside each kanim a Lumen fixture
    /// uses, once per build, the first time such a building spawns.
    ///
    /// Reason it exists: SetSymbolTint, SetSymbolVisiblity, SetSymbolScale and
    /// SetSymbolOverride all address parts of a sprite by symbol name, and those names
    /// live in the binary build file rather than in Assembly-CSharp -- they cannot be
    /// read by decompiling. Knowing them is the difference between tinting a whole
    /// fixture and tinting just its lens.
    ///
    /// Caveat: ONI stores symbols hashed. If HashCache happens to know the original
    /// strings these come out readable; if not, they come out as numbers and symbols
    /// have to be identified by tinting them one at a time and looking. The hash is
    /// logged alongside the name either way, because the hash is what the setters
    /// actually take.
    ///
    /// DELETE THIS once the symbol names are recorded in CLAUDE.md.
    /// </summary>
    internal static class LumenSymbolDump
    {
        private static readonly HashSet<string> dumped = new HashSet<string>();

        public static void DumpOnce(KBatchedAnimController controller)
        {
            KAnimFile[] files = controller.AnimFiles;
            if (files == null)
            {
                return;
            }

            foreach (KAnimFile file in files)
            {
                if (file == null || !dumped.Add(file.name))
                {
                    continue;
                }

                try
                {
                    KAnim.Build build = file.GetData()?.build;
                    if (build?.symbols == null)
                    {
                        UnityEngine.Debug.Log("[Lumen] symbols " + file.name + ": build not loaded.");
                        continue;
                    }

                    StringBuilder line = new StringBuilder();
                    line.Append("[Lumen] symbols ").Append(file.name)
                        .Append(" (").Append(build.symbols.Length).Append("): ");

                    for (int i = 0; i < build.symbols.Length; i++)
                    {
                        if (i > 0)
                        {
                            line.Append(", ");
                        }

                        line.Append(build.symbols[i].hash.ToString());
                    }

                    UnityEngine.Debug.Log(line.ToString());
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogWarning(
                        "[Lumen] could not read symbols for " + file.name + ": " + e.Message);
                }
            }
        }
    }
}
