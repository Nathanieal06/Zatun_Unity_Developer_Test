// Assets/Scripts/Enemies/Zombie/ZombieDissolve.cs
using System.Collections;
using UnityEngine;

/// <summary>
/// Handles spawn (0→1) and death (1→0) dissolve transitions across
/// ALL renderers/materials on the zombie that use a "Dissolve" property.
/// </summary>
public class ZombieDissolve : MonoBehaviour
{
    [Header("Dissolve Settings")]
    [Tooltip("Name of the dissolve float property on your shader.")]
    [SerializeField] private string dissolveProperty = "_DissolveAmount";

    [Tooltip("How long the spawn dissolve takes (seconds).")]
    [SerializeField] private float spawnDuration = 1.5f;

    [Tooltip("How long the death dissolve takes (seconds).")]
    [SerializeField] private float deathDuration = 2f;

    // We will dynamically find the correct property name for each material
    private System.Collections.Generic.Dictionary<Material, string> matProperties = new System.Collections.Generic.Dictionary<Material, string>();

    private void Awake()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        Debug.Log($"[ZombieDissolve] Found {renderers.Length} renderer(s) on {gameObject.name}");

        // We use a list of possible property names to support different shaders
        // _DissolveAmount is the standard for our new custom shader
        string[] possibleNames = new string[] { dissolveProperty, "_DissolveAmount", "_Dissolve", "Dissolve", "Vector1_FEFF47F1" };

        var matList = new System.Collections.Generic.List<Material>();
        foreach (Renderer r in renderers)
        {
            // Accessing .materials automatically creates instances for this specific zombie
            Material[] mats = r.materials; 
            foreach (Material m in mats)
            {
                bool foundProp = false;
                foreach (string prop in possibleNames)
                {
                    if (m.HasProperty(prop))
                    {
                        matProperties[m] = prop;
                        matList.Add(m);
                        foundProp = true;
                        break;
                    }
                }
                
                if (!foundProp)
                {
                    // If no dissolve property is found, we might want to check if the shader name contains "Dissolve"
                    // and log a warning to help the user.
                    if (m.shader.name.Contains("Dissolve"))
                    {
                        Debug.LogWarning($"[ZombieDissolve] Material '{m.name}' uses a dissolve shader but the property name doesn't match our list.");
                    }
                }
            }
        }
        dissolveMaterials = matList.ToArray();
        Debug.Log($"[ZombieDissolve] {dissolveMaterials.Length} dissolve material(s) ready on {gameObject.name}.");
    }

    private void Start()
    {
        // Begin fully dissolved (value = 1), then materialize down to 0
        SetDissolve(1f);
        StartCoroutine(DissolveTransition(1f, 0f, spawnDuration));
    }

    /// <summary>Call this on death to dissolve the zombie away (0 → 1).</summary>
    public IEnumerator DissolveDeath()
    {
        yield return DissolveTransition(0f, 1f, deathDuration);
    }

    private Material[] dissolveMaterials;

    // ── Helpers ───────────────────────────────────────────────────────────

    private void SetDissolve(float value)
    {
        foreach (var kvp in matProperties)
        {
            kvp.Key.SetFloat(kvp.Value, value);
        }
    }

    private IEnumerator DissolveTransition(float from, float to, float duration)
    {
        float elapsed = 0f;
        SetDissolve(from);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Smooth step for a more organic feel
            float smoothT = t * t * (3f - 2f * t);
            SetDissolve(Mathf.Lerp(from, to, smoothT));
            yield return null;
        }
        SetDissolve(to);
    }
}
