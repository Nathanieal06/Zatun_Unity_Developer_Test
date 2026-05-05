// Assets/Scripts/Interactables/GateController.cs
using UnityEngine;

/// <summary>
/// Controls the gate/exit point.
/// Inherits from BasePickup to reuse the interaction detection logic.
/// </summary>
public class GateController : BasePickup
{
    [Header("Gate Components")]
    [Tooltip("Drag the GameObject that has the Animator component here.")]
    [SerializeField] private Animator mainGateAnimator;
    
    [Tooltip("If you have more gates (like a double door), add their Animators here.")]
    [SerializeField] private Animator[] additionalGateAnimators;
    
    [Header("Animation Settings")]
    [Tooltip("The name of the Trigger parameter in your Animator Controller.")]
    [SerializeField] private string openAnimationTrigger = "Open";
    
    [Header("Optional Components")]
    [Tooltip("If you have a lock object that should disappear when opened, drag it here.")]
    [SerializeField] private GameObject lockObject;

    [Header("Messages")]
    [SerializeField] private string noKeyMessage = "You need to pick up the key";
    [SerializeField] private string hasKeyPrompt = "Press F to open gate";
    
    private bool isOpened = false;

    public override string GetPickupLabel()
    {
        if (isOpened) return string.Empty;

        // Find the player to check inventory
        PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
        
        if (inventory != null && inventory.HasNormalKey)
        {
            return hasKeyPrompt;
        }
        
        return noKeyMessage;
    }

    public override void OnPickup(GameObject player)
    {
        if (isOpened) return;

        PlayerInventory inventory = player.GetComponent<PlayerInventory>();
        if (inventory != null && inventory.HasNormalKey)
        {
            OpenGate();
        }
    }

    private void OpenGate()
    {
        isOpened = true;
        
        // Trigger main animator
        if (mainGateAnimator != null)
        {
            mainGateAnimator.SetTrigger(openAnimationTrigger);
        }

        // Trigger additional animators
        if (additionalGateAnimators != null)
        {
            foreach (var anim in additionalGateAnimators)
            {
                if (anim != null)
                {
                    anim.SetTrigger(openAnimationTrigger);
                }
            }
        }

        // Hide the lock if assigned
        if (lockObject != null)
        {
            lockObject.SetActive(false);
        }

        // Disable interaction collider after opening
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }
}
