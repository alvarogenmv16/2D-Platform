using UnityEngine;
using UnityEngine.Events;

// Order puzzle: the player strikes chalices, and each correct strike in the
// sequence lights that chalice's flame. A wrong strike snuffs every flame
// lit so far and the sequence starts over from the first chalice. Lighting
// the last one solves it, fires OnSolved (wire it in the Inspector, e.g. to
// light a flame on the level's door - same UnityEvent wiring as
// KeyManager/BossDoor) and locks the puzzle so the flames stay lit and
// further strikes do nothing.
//
// One swing = one chalice: if the sword box overlaps several chalices in
// the same swing, only the one closest to the player counts. Without this
// a single swing between two chalices would light one and snuff everything
// with the other, depending on overlap order.
//
// Setup: put the ChaliceFlame objects as children of this one, then drag
// them into correctOrder in the order they must be struck. Their position
// in the Hierarchy doesn't matter - only this array defines the solution.
public class ChaliceSequence : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private ChaliceFlame[] correctOrder;

    [Header("Sound")]
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip correctSound; // a flame lights up
    [SerializeField] private AudioClip wrongSound; // every flame goes out
    [SerializeField] private AudioClip solvedSound; // last flame lit

    [Header("Reward")]
    // Objects switched on the moment the puzzle is solved - e.g. a Key left
    // INACTIVE in the scene, so the key only appears once the sequence is
    // done. Plain field instead of an OnSolved listener so it can't be
    // forgotten; OnSolved stays available for anything else.
    [SerializeField] private GameObject[] revealOnSolved;

    public UnityEvent OnSolved;

    private int progress;
    private bool isSolved;

    // Strike collected during this frame's swing, resolved in LateUpdate
    // once every chalice the swing touched has reported in.
    private ChaliceFlame pendingStrike;
    private float pendingDistance;
    private Transform player;

    // =========================
    // FUNCTIONS
    // =========================

    // Called by ChaliceFlame.Break (the sword hit). Only queues the strike -
    // the closest chalice of this swing is the one that actually counts.
    public void OnChaliceStruck(ChaliceFlame chalice)
    {
        if (isSolved) return;

        float distance = DistanceToPlayer(chalice);
        if (pendingStrike == null || distance < pendingDistance)
        {
            pendingStrike = chalice;
            pendingDistance = distance;
        }
    }

    private void LateUpdate()
    {
        if (pendingStrike == null) return;

        ChaliceFlame chalice = pendingStrike;
        pendingStrike = null;
        ResolveStrike(chalice);
    }

    private void ResolveStrike(ChaliceFlame chalice)
    {
        if (isSolved || correctOrder == null || correctOrder.Length == 0) return;

        // Hitting a flame that's already burning neither helps nor punishes -
        // otherwise a stray double swing would wipe out correct progress.
        if (chalice.IsLit) return;

        if (chalice == correctOrder[progress])
        {
            chalice.SetLit(true);
            progress++;

            if (progress >= correctOrder.Length)
            {
                isSolved = true;
                sfxPlayer?.Play(solvedSound);
                RevealRewards();
                OnSolved?.Invoke();
            }
            else
            {
                sfxPlayer?.Play(correctSound);
            }
        }
        else
        {
            ResetSequence();
        }
    }

    private void RevealRewards()
    {
        if (revealOnSolved == null) return;

        foreach (GameObject reward in revealOnSolved)
        {
            if (reward != null)
            {
                reward.SetActive(true);
            }
        }
    }

    private void ResetSequence()
    {
        foreach (ChaliceFlame chalice in correctOrder)
        {
            if (chalice != null)
            {
                chalice.SetLit(false);
            }
        }

        progress = 0;
        sfxPlayer?.Play(wrongSound);
    }

    private float DistanceToPlayer(ChaliceFlame chalice)
    {
        // Looked up lazily (and again if lost): the Player that survives a
        // scene transition replaces this scene's local one.
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null) return 0f;
            player = playerObject.transform;
        }

        return Vector2.Distance(player.position, chalice.transform.position);
    }
}
