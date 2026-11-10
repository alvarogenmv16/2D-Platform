using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Visual warning for FakeKeyBlock's explosion, split from the block itself
// so the trap logic (damage, timing) stays in FakeKeyBlock and this only
// draws. Everything is sized from the block's real explosionRadius, so the
// warning always matches the actual damage area - no separate sprite to
// keep in sync by hand.
//
// Fuse (BeginTelegraph): a ring at the exact blast radius + a translucent
// disc growing from the center to that ring over the fuse time (reaching
// the ring = boom), while the block shakes and flashes faster and faster.
// Blast (PlayExplosion): block hides, ring shockwave expands and fades,
// a Light2D flash, a burst of particles and a camera impulse.
[RequireComponent(typeof(SpriteRenderer))]
public class FakeKeyBlockTelegraph : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [Header("Danger zone")]
    [SerializeField] private Color dangerColor = new Color(1f, 0.25f, 0.2f, 1f);
    [SerializeField] private float ringWidth = 0.08f;
    [SerializeField] private float fillMaxAlpha = 0.3f;
    // Unlit so the warning reads the same in dark and lit areas of the level.
    [SerializeField] private Material unlitMaterial;
    // Frontmost sorting layer, so Ground tiles never cover the warning.
    [SerializeField] private string sortingLayerName = "Hazard";

    [Header("Block shake / flash during the fuse")]
    [SerializeField] private float maxShakeMagnitude = 0.08f;
    [SerializeField] private Color flashColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private float flashFrequencyStart = 3f; // flashes per second when the fuse starts
    [SerializeField] private float flashFrequencyEnd = 12f; // ... and right before the blast

    [Header("Explosion")]
    [SerializeField] private float explosionFxDuration = 0.6f;
    [SerializeField] private Color explosionLightColor = new Color(1f, 0.45f, 0.3f, 1f);
    [SerializeField] private float explosionLightIntensity = 4f;
    [SerializeField] private Color particleColorA = new Color(1f, 0.5f, 0.3f, 1f);
    [SerializeField] private Color particleColorB = new Color(0.4f, 0.95f, 1f, 1f); // crystal cyan
    [SerializeField] private int particleCount = 40;
    [SerializeField] private Material particleMaterial;
    [SerializeField] private CinemachineImpulseSource impulseSource; // optional screen shake

    private const int RingSegments = 64;

    private SpriteRenderer blockRenderer;
    private Color blockBaseColor;
    private Vector3 blockOrigin;
    private float radius;

    // FX live on their own root, NOT parented to the block: the block's
    // transform is what shakes, and this way the ring stays perfectly still.
    private GameObject fxRoot;
    private LineRenderer ring;
    private SpriteRenderer fill;
    private Coroutine fuseRoutine;

    private static Sprite discSprite;

    // =========================
    // START
    // =========================
    private void Awake()
    {
        blockRenderer = GetComponent<SpriteRenderer>();
        blockBaseColor = blockRenderer.color;
    }

    // =========================
    // FUNCTIONS
    // =========================

    // Called by FakeKeyBlock the moment it's hit. duration = fuse length.
    public void BeginTelegraph(float explosionRadius, float duration)
    {
        radius = explosionRadius;
        blockOrigin = transform.position;

        CreateDangerZone();
        fuseRoutine = StartCoroutine(FuseRoutine(duration));
    }

    // Called by FakeKeyBlock right before it applies the damage. Returns
    // how long the block must stay alive so the FX can finish.
    public float PlayExplosion()
    {
        if (fuseRoutine != null) StopCoroutine(fuseRoutine);

        // Back to the exact origin: FakeKeyBlock measures the blast from
        // transform.position, which the shake has been moving around.
        transform.position = blockOrigin;
        blockRenderer.enabled = false;

        if (fxRoot == null) CreateDangerZone();

        SpawnLight();
        SpawnParticles();
        impulseSource?.GenerateImpulse();

        StartCoroutine(ExplosionRoutine());
        return explosionFxDuration;
    }

    private IEnumerator FuseRoutine(float duration)
    {
        float elapsed = 0f;
        float flashPhase = 0f;

        while (elapsed < duration)
        {
            float t = duration > 0f ? elapsed / duration : 1f;

            // Fill grows to the ring exactly when the fuse runs out - doubles
            // as a "time left" bar. Eased in so the last moment feels urgent.
            float fillT = t * t;
            SetFill(radius * fillT, fillMaxAlpha);

            // Ring pulses faster as time runs out.
            float frequency = Mathf.Lerp(flashFrequencyStart, flashFrequencyEnd, t);
            flashPhase += frequency * Time.deltaTime;
            float pulse = 0.5f + 0.5f * Mathf.Sin(flashPhase * Mathf.PI * 2f);
            SetRing(radius, Mathf.Lerp(0.6f, 1f, pulse), ringWidth);

            blockRenderer.color = Color.Lerp(blockBaseColor, flashColor, pulse * t);
            transform.position = blockOrigin + (Vector3)(Random.insideUnitCircle * maxShakeMagnitude * t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        SetFill(radius, fillMaxAlpha);
        transform.position = blockOrigin;
    }

    private IEnumerator ExplosionRoutine()
    {
        float elapsed = 0f;

        while (elapsed < explosionFxDuration)
        {
            float t = elapsed / explosionFxDuration;
            float fade = 1f - t;

            // Shockwave: ring pushes a bit past the real radius and fades.
            SetRing(radius * Mathf.Lerp(1f, 1.3f, t), fade, ringWidth * Mathf.Lerp(1f, 3f, t));
            // Fill flashes bright on the blast frame, then fades out.
            SetFill(radius, Mathf.Lerp(0.6f, 0f, t));

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(fxRoot);
    }

    private void CreateDangerZone()
    {
        fxRoot = new GameObject($"{name}_ExplosionFX");
        fxRoot.transform.position = blockOrigin;

        Material material = unlitMaterial != null ? unlitMaterial : blockRenderer.sharedMaterial;

        // Translucent disc
        GameObject fillObject = new GameObject("DangerFill");
        fillObject.transform.SetParent(fxRoot.transform, false);
        fill = fillObject.AddComponent<SpriteRenderer>();
        fill.sprite = GetDiscSprite();
        fill.sharedMaterial = material;
        fill.sortingLayerName = sortingLayerName;
        fill.sortingOrder = 0;

        // Ring at the exact blast radius
        GameObject ringObject = new GameObject("DangerRing");
        ringObject.transform.SetParent(fxRoot.transform, false);
        ring = ringObject.AddComponent<LineRenderer>();
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = RingSegments;
        ring.sharedMaterial = material;
        ring.sortingLayerName = sortingLayerName;
        ring.sortingOrder = 1;
        ring.numCornerVertices = 2;

        SetFill(0f, fillMaxAlpha);
        SetRing(radius, 1f, ringWidth);
    }

    private void SetRing(float ringRadius, float alpha, float width)
    {
        if (ring == null) return;

        for (int i = 0; i < RingSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / RingSegments;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * ringRadius);
        }

        Color color = dangerColor;
        color.a = alpha;
        ring.startColor = color;
        ring.endColor = color;
        ring.widthMultiplier = width;
    }

    private void SetFill(float fillRadius, float alpha)
    {
        if (fill == null) return;

        // Disc sprite is 1 unit across, so scale = diameter.
        fill.transform.localScale = Vector3.one * fillRadius * 2f;

        Color color = dangerColor;
        color.a = alpha;
        fill.color = color;
    }

    private void SpawnLight()
    {
        GameObject lightObject = new GameObject("ExplosionLight");
        lightObject.transform.SetParent(fxRoot.transform, false);

        Light2D light = lightObject.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = explosionLightColor;
        light.pointLightInnerRadius = 0f;
        light.pointLightOuterRadius = radius * 1.5f;
        light.intensity = explosionLightIntensity;

        StartCoroutine(FadeLight(light));
    }

    private IEnumerator FadeLight(Light2D light)
    {
        float elapsed = 0f;

        while (elapsed < explosionFxDuration && light != null)
        {
            light.intensity = Mathf.Lerp(explosionLightIntensity, 0f, elapsed / explosionFxDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void SpawnParticles()
    {
        GameObject particlesObject = new GameObject("ExplosionParticles");
        particlesObject.transform.SetParent(fxRoot.transform, false);

        ParticleSystem particles = particlesObject.AddComponent<ParticleSystem>();
        // A freshly added ParticleSystem starts playing with default
        // settings, and duration can't be changed while it plays.
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        float lifetime = explosionFxDuration * 0.9f;

        ParticleSystem.MainModule main = particles.main;
        main.duration = explosionFxDuration;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
        // Fast enough that the outermost pieces reach the edge of the
        // danger zone - the burst itself shows how far the blast goes.
        main.startSpeed = new ParticleSystem.MinMaxCurve(radius * 0.8f / lifetime, radius * 1.6f / lifetime);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.25f);
        main.startColor = new ParticleSystem.MinMaxGradient(particleColorA, particleColorB);
        main.gravityModifier = 0.4f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)particleCount) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        ParticleSystemRenderer particleRenderer = particlesObject.GetComponent<ParticleSystemRenderer>();
        if (particleMaterial != null) particleRenderer.sharedMaterial = particleMaterial;
        particleRenderer.sortingLayerName = sortingLayerName;
        particleRenderer.sortingOrder = 2;

        particles.Play();
    }

    // Procedural soft-edged disc, generated once and shared by every block.
    private static Sprite GetDiscSprite()
    {
        if (discSprite != null) return discSprite;

        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                // Slightly stronger toward the edge, soft 1-2px rim.
                float alpha = Mathf.Clamp01((1f - distance) * 40f) * Mathf.Lerp(0.6f, 1f, distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();

        discSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return discSprite;
    }

    private void OnDestroy()
    {
        // Safety net: never leave the FX behind if the block goes away
        // some other way (scene change, etc.).
        if (fxRoot != null) Destroy(fxRoot);
    }
}
