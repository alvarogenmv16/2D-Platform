using UnityEngine;

// Gives a single-frame walk pose some life: while walking, the sprite
// bounces once per step and tilts slightly back and forth, so it reads as
// steps instead of sliding. Goes on Visuals (the sprite's own transform,
// never the physics root), and only touches its local position/rotation -
// the Animator here only animates the sprite, so they don't conflict.
// Whoever drives the movement calls SetWalking (e.g. BeastAI).
public class WalkBob : MonoBehaviour
{
    [SerializeField] private float bobHeight = 0.15f;      // world units at the top of a step
    [SerializeField] private float stepsPerSecond = 2.2f;
    [SerializeField] private float tiltDegrees = 1.5f;     // lean forward/back per step
    [SerializeField] private float settleSpeed = 12f;      // how fast it returns to rest

    private bool isWalking;
    private float walkTime;
    private Vector3 basePosition;
    private Quaternion baseRotation;

    private void Awake()
    {
        basePosition = transform.localPosition;
        baseRotation = transform.localRotation;
    }

    public void SetWalking(bool walking)
    {
        isWalking = walking;
    }

    private void LateUpdate()
    {
        if (!isWalking)
        {
            // Ease back to rest instead of snapping mid-step.
            walkTime = 0f;
            float k = 1f - Mathf.Exp(-settleSpeed * Time.deltaTime);
            transform.localPosition = Vector3.Lerp(transform.localPosition, basePosition, k);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, baseRotation, k);
            return;
        }

        walkTime += Time.deltaTime;
        float phase = walkTime * stepsPerSecond * Mathf.PI;

        // |sin| = one bounce per step; plain sin for the tilt so it rocks
        // one way on one step and the other way on the next.
        float bob = Mathf.Abs(Mathf.Sin(phase)) * bobHeight;
        float tilt = Mathf.Sin(phase) * tiltDegrees;

        transform.localPosition = basePosition + Vector3.up * bob;
        transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, tilt);
    }
}
