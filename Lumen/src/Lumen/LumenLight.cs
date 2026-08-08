using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// Everything that distinguishes one Lumen light from another.
    ///
    /// The five buildings share a single <see cref="LumenLightConfig"/> implementation
    /// and differ only by the values in this record, so adding a sixth light means
    /// adding one entry here plus a four-line config subclass -- not a sixth copy of
    /// the building code.
    /// </summary>
    public sealed class LumenLight
    {
        /// <summary>Prefab ID. Also the uppercased key under STRINGS.BUILDINGS.PREFABS.</summary>
        public string Id;

        public string Name;
        public string Description;
        public string Effect;

        public int Width;
        public int Height;

        /// <summary>
        /// A *vanilla* kanim. Custom art would need a real kanim pipeline (Spriter +
        /// a kanim packer + ModUtil.AddKAnim); these five instead reuse Klei's light
        /// anims and separate themselves visually with <see cref="Tint"/>. Only anims
        /// driven by LightController are safe here, because LightController plays the
        /// literal anim names "on" and "off" -- a kanim without those states (e.g.
        /// mercurylight_kanim, which has its own state machine) will not animate.
        /// </summary>
        public string Anim;

        public BuildLocationRule BuildLocation;
        public string[] Materials;
        public float[] Mass;

        /// <summary>Colour multiplied over the reused kanim so each light reads distinct.</summary>
        public Color32 Tint;

        /// <summary>
        /// Size multiplier on the reused kanim, 1 being vanilla size.
        ///
        /// Deliberately tracks <see cref="Range"/>: a fixture that lights further
        /// looks bigger, so the size is information rather than decoration. It is also
        /// the only silhouette difference available without custom art or knowledge of
        /// the kanim's internal symbol names.
        /// </summary>
        public float AnimScale;

        /// <summary>
        /// Colour applied to <see cref="LensSymbols"/> only, on top of
        /// <see cref="Tint"/>. Alpha 0 means "not set".
        ///
        /// The point is to keep the housing a common colour across the range while
        /// each fixture's lens carries its identity, so they read as one product line
        /// rather than as the same lamp dyed four ways.
        /// </summary>
        public Color32 LensTint;

        /// <summary>
        /// kanim symbols treated as "the lens". Verified by dumping the builds:
        ///   ceilinglight_kanim: generator_light_bloom, light_off, place, temp_base, ui
        ///   floorlamp_kanim:    ui, beam, cord, place, pole, shade, feet, handle, light
        /// </summary>
        public string[] LensSymbols;

        // --- Light2D emitter settings ---
        public LightShape Shape;
        public float Range;
        public float Angle;
        public int Lux;
        public Color LightColour;
        public Vector2 Offset;

        /// <summary>Decor is the vanilla per-building value, not something the mod invents.</summary>
        public EffectorValues Decor;

        /// <summary>Default watts. Overridable per building in config.json.</summary>
        public float Watts;

        /// <summary>
        /// Extra straight-line detection reach *beyond* the area the fixture lights,
        /// in tiles. Zero for everything but the Sentry.
        ///
        /// Zero does not mean "no sensor". The sensor's baseline is the fixture's own
        /// lit area, computed from <see cref="Range"/> and <see cref="Shape"/> by the
        /// game's shadow caster, so it needs no tuning and cannot drift out of step
        /// with the light. This field only exists for the Sentry's early warning.
        /// </summary>
        public float ExtraSensorRadius;

        /// <summary>Default seconds the light stays on after the last duplicant leaves.</summary>
        public float LingerSeconds;

        /// <summary>
        /// Whether this fixture can be rotated to aim its beam.
        ///
        /// Only meaningful for <see cref="LightShape.Cone"/>: a Circle is
        /// rotation-invariant, so offering rotation on one would be a control that
        /// visibly does nothing. Also requires cone lights to honour their direction
        /// at all -- see <see cref="LumenCompat.ConesAreDirectional"/>.
        /// </summary>
        public bool Aimable;
    }
}
