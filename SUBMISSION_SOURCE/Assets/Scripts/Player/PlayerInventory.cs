// Assets/Scripts/Player/PlayerInventory.cs
using UnityEngine;

/// <summary>
/// Simple inventory to track non-weapon items like keys.
/// Attached to the Player GameObject.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory Data")]
    [SerializeField] private bool hasNormalKey = false;
    [SerializeField] private bool hasVictoryKey = false;
    
    public bool HasNormalKey => hasNormalKey;
    public bool HasVictoryKey => hasVictoryKey;

    public void AddNormalKey()
    {
        hasNormalKey = true;
        Debug.Log("[PlayerInventory] Normal Key added.");
    }

    public void AddVictoryKey()
    {
        hasVictoryKey = true;
        Debug.Log("[PlayerInventory] VICTORY KEY added!");
    }

    public void RemoveNormalKey() => hasNormalKey = false;
    public void RemoveVictoryKey() => hasVictoryKey = false;
}
