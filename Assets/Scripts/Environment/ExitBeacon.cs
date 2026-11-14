using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Makes a LevelExit easy to spot: a spinning vortex at the exit with a
// column of light rising above it, a purple glow on its surroundings, all
// pulsing gently. When the object gets activated (e.g. by an arena
// controller once the boss dies) it opens up from nothing instead of
// popping in.
//
// Hierarchy (Prefabs/LevelPortal.prefab):
//   LevelPortal (root) - BoxCollider2D (trigger), LevelExit, ExitBeacon, AudioSource
//     Beam    - SpriteRenderer (tall column of light, drawn behind)
//     Vortex  - SpriteRenderer (the swirl)
public class ExitBeacon : MonoBehaviour
{
    [SerializeField] private Transform vortex;
    [SerializeField] private SpriteRenderer beam;

    [Header("Motion")]
    [SerializeField] private float spinSpeed = -120f;       // degrees per second
    [SerializeField] private float pulseSpeed = 2.5f;
    [SerializeField, Range(0f, 0.5f)] private float pulseAmount = 0.08f;
    [SerializeField] private float appearDuration = 1.2f;

    [Header("Glow")]
    [SerializeField] private Color lightColor = new Color(0.7f, 0.35f, 1f);
    [SerializeField] private float lightIntensity = 1.6f;
    [SerializeField] private float lightRadius = 6f;

    [Header("Sound")]
    [SerializeField] private AudioClip appearSound;
    [SerializeField, Range(0f, 1f)] private float appearVolume = 1f;

    private Vector3 vortexScale;
    private Vector3 beamScale;
    private Vector3 beamPosition;
    private float beamHeight; // local units, at full size
    private float beamAlpha;
    private Light2D glow;
    private float appear; // 0..1

    private void Awake()
    {
        if (vortex != null) vortexScale = vortex.localScale;
        if (beam != null)
        {
            beamScale = beam.transform.localScale;
            beamPosition = beam.transform.localPosition;
            beamHeight = beam.sprite != null ? beam.sprite.bounds.size.y * beamScale.y : 0f;
            beamAlpha = beam.color.a;
        }

        glow = gameObject.AddComponent<Light2D>();
        glow.lightType = Light2D.LightType.Point;
        glow.color = lightColor;
        glow.pointLightInnerRadius = 0f;
        glow.pointLightOuterRadius = lightRadius;
        glow.intensity = 0f;
    }

    private void OnEnable()
    {
        StartCoroutine(Appear());
    }

    private IEnumerator Appear()
    {
        if (appearSound != null && TryGetComponent(out AudioSource source))
        {
            source.PlayOneShot(appearSound, appearVolume);
        }

        for (float t = 0f; t < appearDuration; t += Time.deltaTime)
        {
            float k = t / appearDuration;
            appear = 1f - (1f - k) * (1f - k); // ease out
            yield return null;
        }
        appear = 1f;
    }

    private void Update()
    {
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;

        if (vortex != null)
        {
            vortex.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
            vortex.localScale = vortexScale * (appear * pulse);
        }

        if (beam != null)
        {
            // The column grows upwards from the vortex while appearing.
            // Its pivot is centred, so shift it down by the missing half to
            // keep the bottom end anchored at the vortex.
            beam.transform.localScale = new Vector3(beamScale.x * pulse, beamScale.y * appear, beamScale.z);
            beam.transform.localPosition = beamPosition - new Vector3(0f, beamHeight * (1f - appear) * 0.5f, 0f);
            Color c = beam.color;
            c.a = beamAlpha * appear * Mathf.Lerp(0.75f, 1f, (pulse - 1f + pulseAmount) / (2f * Mathf.Max(0.0001f, pulseAmount)));
            beam.color = c;
        }

        if (glow != null)
        {
            glow.intensity = lightIntensity * appear * pulse;
        }
    }
}
