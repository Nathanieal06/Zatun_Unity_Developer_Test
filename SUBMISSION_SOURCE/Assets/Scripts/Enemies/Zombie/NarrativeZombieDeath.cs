using UnityEngine;
using System.Collections;

public class NarrativeZombieDeath : MonoBehaviour
{
    [Header("Cutscene Settings")]
    [SerializeField] private string narrativeMessage = "The threat has been neutralized...";
    [SerializeField] private float messageDuration = 5f;
    [SerializeField] private float waitBeforeMessage = 1f;

    private void OnEnable() => GameEvents.OnZombieDied += HandleZombieDied;
    private void OnDisable() => GameEvents.OnZombieDied -= HandleZombieDied;

    private void HandleZombieDied(GameObject zombie)
    {
        // Only trigger if THIS zombie died
        if (zombie == gameObject)
        {
            StartCoroutine(DeathCutsceneRoutine());
        }
    }

    private IEnumerator DeathCutsceneRoutine()
    {
        Debug.Log($"[NarrativeZombieDeath] Starting cutscene for {gameObject.name}");

        // 1. Globally disable AI aggression
        ZombieAI.IsCutsceneActive = true;
        
        // 2. Wait a moment for the death animation to start
        yield return new WaitForSeconds(waitBeforeMessage);

        // 3. Show the narrative message
        GameEvents.RaiseNarrativeMessage(narrativeMessage, messageDuration);

        // 4. Wait for the message to finish
        yield return new WaitForSeconds(messageDuration);

        // 5. Restore AI aggression
        ZombieAI.IsCutsceneActive = false;
        
        Debug.Log("[NarrativeZombieDeath] Cutscene finished.");
    }
}
