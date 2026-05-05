// Assets/Scripts/Player/DamageFeedback.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles visual feedback when the player takes damage.
/// Listens to GameEvents — completely decoupled from PlayerHealth.
/// Attach to: any GameObject (recommend: Player or UI root).
/// Requires: A full-screen UI Image assigned in Inspector.
/// </summary>
public class DamageFeedback : MonoBehaviour
{
    [Header("Screen Flash Settings")]
    [Tooltip("The full-screen red overlay Image in the Canvas")]
    [SerializeField] private Image flashOverlay;

    [Tooltip("Peak alpha of the flash (0 = invisible, 1 = fully opaque)")]
    [SerializeField, Range(0f, 1f)] private float flashMaxAlpha = 0.4f;

    [Tooltip("Total time for the flash animation")]
    [SerializeField] private float flashDuration = 0.3f;

    private Coroutine flashCoroutine;

    private void OnEnable()
    {
        GameEvents.OnPlayerDamaged += HandleDamageTaken;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerDamaged -= HandleDamageTaken;
    }

    private void Start()
    {
        // Ensure overlay starts invisible
        if (flashOverlay != null)
            SetAlpha(0f);
    }

    private void HandleDamageTaken(float amount)
    {
        // Cancel any in-progress flash before starting a new one
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // Fade IN quickly
        float halfDuration = flashDuration * 0.5f;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Lerp(0f, flashMaxAlpha, elapsed / halfDuration));
            yield return null;
        }

        // Fade OUT slowly
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Lerp(flashMaxAlpha, 0f, elapsed / halfDuration));
            yield return null;
        }

        SetAlpha(0f);
        flashCoroutine = null;
    }

    private void SetAlpha(float alpha)
    {
        if (flashOverlay == null) return;
        var c = flashOverlay.color;
        c.a = alpha;
        flashOverlay.color = c;
    }
}