using System.Collections;
using UnityEngine;

// TempleScene: the three companions gathered before the final boss. Each
// keeps its own dialogue (CompanionInteraction); once the player has gone
// through ALL of their conversations, they grant the reward - a much
// stronger jump (PlayerMovement.UpgradeJump), enough to leap over the Beast.
//
// Needs no wiring: it picks up every CompanionInteraction in the scene and
// the scene's DialogueUI by itself (the companions are prefab instances).
public class CompanionGathering : MonoBehaviour
{
    [Header("Reward")]
    // x1.35 impulse: first jump ~2.5 -> ~4.5 units high, double jump ~3.8 ->
    // ~6.9 - clears the Beast (~5 units tall) and its bite/charge hitbox.
    [SerializeField] private float jumpForceMultiplier = 1.35f;
    // x1.4 duration: dash ~2.25 -> ~3.15 units long, same speed.
    [SerializeField] private float dashDurationMultiplier = 1.4f;

    [Header("Message")]
    [SerializeField] private string rewardMessage = "Your companions lend you their strength. Your jump and dash are now much more powerful!";
    // Lets the last companion's closing line be read before it's replaced.
    [SerializeField] private float messageDelay = 2f;
    [SerializeField] private float messageDuration = 4f;
    [SerializeField] private AudioClip rewardSound; // optional

    private CompanionInteraction[] companions;
    private DialogueUI dialogueUI;
    private bool rewarded;

    private void Start()
    {
        companions = FindObjectsByType<CompanionInteraction>(FindObjectsSortMode.None);
        dialogueUI = FindFirstObjectByType<DialogueUI>(FindObjectsInactive.Include);

        foreach (CompanionInteraction companion in companions)
        {
            companion.OnConversationCompleted.AddListener(CheckAllCompleted);
        }
    }

    private void OnDestroy()
    {
        if (companions == null) return;

        foreach (CompanionInteraction companion in companions)
        {
            if (companion != null) companion.OnConversationCompleted.RemoveListener(CheckAllCompleted);
        }
    }

    private void CheckAllCompleted()
    {
        if (rewarded) return;

        foreach (CompanionInteraction companion in companions)
        {
            if (companion != null && !companion.HasCompletedConversation) return;
        }

        rewarded = true;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.TryGetComponent(out PlayerMovement movement))
        {
            movement.UpgradeJump(jumpForceMultiplier);
            movement.UpgradeDash(dashDurationMultiplier);
        }

        StartCoroutine(ShowRewardMessage());
    }

    private IEnumerator ShowRewardMessage()
    {
        yield return new WaitForSeconds(messageDelay);

        if (rewardSound != null && Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(rewardSound, Camera.main.transform.position);
        }

        if (dialogueUI == null) yield break;

        dialogueUI.Show(rewardMessage);

        // Closes itself: the player may already be out of every companion's
        // trigger, and then nothing else would hide the box.
        yield return new WaitForSeconds(messageDuration);
        dialogueUI.Hide();
    }
}
