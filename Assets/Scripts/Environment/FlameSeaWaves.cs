using UnityEngine;

// Makes a single-frame flame sprite read as living fire: each layer sways
// sideways and bobs up and down on its own sine wave, out of phase with the
// others (e.g. a mirrored back layer moving against the front one). Purely
// visual - moves only the layers' local positions, never the root, so it
// doesn't interfere with RisingFlameSea's movement or hitbox.
public class FlameSeaWaves : MonoBehaviour
{
    [SerializeField] private Transform[] layers;
    [SerializeField] private float swayAmplitude = 0.6f;
    [SerializeField] private float swaySpeed = 0.8f;
    [SerializeField] private float bobAmplitude = 0.15f;
    [SerializeField] private float bobSpeed = 2.3f;

    private Vector3[] basePositions;

    private void Start()
    {
        basePositions = new Vector3[layers.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] != null) basePositions[i] = layers[i].localPosition;
        }
    }

    private void Update()
    {
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == null) continue;

            // Alternate layers move in opposite directions.
            float phase = i * Mathf.PI;
            float x = Mathf.Sin(Time.time * swaySpeed + phase) * swayAmplitude;
            float y = Mathf.Sin(Time.time * bobSpeed + phase * 0.5f) * bobAmplitude;

            layers[i].localPosition = basePositions[i] + new Vector3(x, y, 0f);
        }
    }
}
