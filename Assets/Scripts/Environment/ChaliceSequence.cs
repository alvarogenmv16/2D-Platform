using UnityEngine;
using UnityEngine.Events;

// Order puzzle: the player strikes chalices, and each correct strike in the
// sequence lights that chalice's flame. A wrong strike snuffs every flame
// lit so far and the sequence starts over from the first chalice. Lighting
// the last one solves it, fires OnSolved (wire it in the Inspector, e.g. to
// activate the reward key - same UnityEvent wiring as KeyManager/BossDoor)
// and locks the puzzle so the flames stay lit.
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

    public UnityEvent OnSolved;

    private int progress;
    private bool isSolved;

    // =========================
    // FUNCTIONS
    // =========================

    public void OnChaliceStruck(ChaliceFlame chalice)
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
}
