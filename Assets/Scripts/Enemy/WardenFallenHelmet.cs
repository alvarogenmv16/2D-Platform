using UnityEngine;

// The Warden's helmet, knocked off at the phase change (spawned by WardenAI).
// Shoots up and to one side spinning, falls, bounces a couple of times on the
// floor and comes to rest lying on its side - and stays in the arena for the
// rest of the fight as scenery.
//
// Purely visual: no collider, no Rigidbody. It's moved by hand and only
// raycasts against the Ground layer to find the floor (and arena walls).
public class WardenFallenHelmet : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;

    [Header("Launch")]
    [SerializeField] private Vector2 launchVelocity = new Vector2(1.5f, 11f); // x is mirrored to the chosen side
    [SerializeField] private float spinSpeed = 720f;                          // degrees per second
    [SerializeField] private float gravity = 28f;

    [Header("Bounce")]
    [SerializeField] private float bounciness = 0.35f;
    [SerializeField] private float bounceFriction = 0.55f;
    [SerializeField] private float minBounceSpeed = 2.5f;

    [Header("Rest")]
    // Height of its centre above the floor once lying on its side.
    [SerializeField] private float restHeight = 0.55f;
    [SerializeField] private float settleTime = 0.25f;

    private Vector2 velocity;
    private float spin;
    private bool resting;
    private float settleTimer;
    private float settleFrom, settleTo;
    private ContactFilter2D solidGroundFilter;
    private readonly RaycastHit2D[] hits = new RaycastHit2D[4];

    private void Awake()
    {
        solidGroundFilter = new ContactFilter2D();
        solidGroundFilter.SetLayerMask(groundLayer);
        solidGroundFilter.useTriggers = false;
    }

    // direction: +1 flies to the right, -1 to the left.
    // sizeMultiplier: the Warden's own scale in the scene, so the helmet is
    // exactly as big as it was on its head. The whole motion (speeds,
    // gravity, resting height) scales with it, so it moves the same way at
    // any size.
    public void Launch(float direction, float sizeMultiplier = 1f)
    {
        size = Mathf.Max(0.01f, sizeMultiplier);
        transform.localScale *= size;

        float dir = direction >= 0f ? 1f : -1f;
        velocity = new Vector2(launchVelocity.x * dir, launchVelocity.y) * size;
        spin = -spinSpeed * dir;
        resting = false;
    }

    private float size = 1f;

    private void Update()
    {
        if (resting)
        {
            Settle();
            return;
        }

        float dt = Time.deltaTime;
        velocity.y -= gravity * size * dt;
        Vector2 position = transform.position;
        Vector2 step = velocity * dt;
        float rest = restHeight * size;

        // Arena wall: bounce back the other way.
        if (step.x != 0f && Physics2D.Raycast(position, new Vector2(Mathf.Sign(step.x), 0f), solidGroundFilter, hits, Mathf.Abs(step.x) + 0.4f * size) > 0)
        {
            velocity.x = -velocity.x * bounceFriction;
            step.x = 0f;
        }

        // Floor (only while falling).
        if (velocity.y < 0f && Physics2D.Raycast(position, Vector2.down, solidGroundFilter, hits, -step.y + rest) > 0)
        {
            transform.position = new Vector3(position.x + step.x, hits[0].point.y + rest, transform.position.z);

            if (-velocity.y > minBounceSpeed * size)
            {
                velocity.y = -velocity.y * bounciness;
                velocity.x *= bounceFriction;
                spin *= 0.5f;
            }
            else
            {
                StartSettling();
            }
            return;
        }

        transform.position += (Vector3)step;
        transform.Rotate(0f, 0f, spin * dt);
    }

    private void StartSettling()
    {
        resting = true;
        settleTimer = 0f;
        // Lies down on whichever side is closest.
        settleFrom = transform.eulerAngles.z;
        float z = Mathf.Repeat(settleFrom, 360f);
        settleTo = Mathf.Abs(Mathf.DeltaAngle(z, 90f)) < Mathf.Abs(Mathf.DeltaAngle(z, 270f)) ? 90f : 270f;
    }

    private void Settle()
    {
        if (settleTimer >= settleTime) return;

        settleTimer += Time.deltaTime;
        float t = Mathf.Clamp01(settleTimer / settleTime);
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(settleFrom, settleTo, 1f - (1f - t) * (1f - t)));
    }
}
