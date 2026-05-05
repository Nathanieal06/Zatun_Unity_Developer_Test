using UnityEngine;

/// <summary>
/// A helper script for testing purposes.
/// Allows you to manually check 'Key' booleans in the Inspector to simulate picking up items.
/// </summary>
public class PlayerTestKeys : MonoBehaviour
{
    [Header("Test Key Inventory")]
    [Tooltip("Manually check this in the Inspector to simulate having Key A")]
    public bool hasKeyA;
    
    [Tooltip("Manually check this in the Inspector to simulate having Key B")]
    public bool hasKeyB;

    // Singleton pattern for easy access from other scripts like WinZone or Door scripts
    public static PlayerTestKeys Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    /// <summary>
    /// Helper method to check for a key by index or name if needed.
    /// </summary>
    public bool HasKey(string keyName)
    {
        if (keyName.ToLower().Contains("a")) return hasKeyA;
        if (keyName.ToLower().Contains("b")) return hasKeyB;
        return false;
    }
}
