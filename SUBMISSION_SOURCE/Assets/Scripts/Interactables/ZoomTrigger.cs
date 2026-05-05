// Assets/Scripts/Interactables/ZoomTrigger.cs
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Triggers a cinematic camera zoom and a narrative message when the player enters.
/// Can be configured to only trigger under certain conditions (e.g. if a door is locked).
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class ZoomTrigger : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("The Cinemachine Camera to activate (give it a higher priority than the default camera).")]
    [SerializeField] private CinemachineCamera zoomCamera;
    [SerializeField] private int activePriority = 30;
    [SerializeField] private int inactivePriority = 5;

    [Header("Narrative Settings")]
    [TextArea(3, 10)]
    [SerializeField] private string narrativeMessage = "Take gun and knife by pressing F";
    [SerializeField] private float messageDuration = 5f;

    [Header("Sequence Events")]
    [Tooltip("Optional: An object to activate once this sequence ends.")]
    [SerializeField] private GameObject objectToActivateOnComplete;

    [Header("Conditions")]
    [Tooltip("If true, this sequence will only trigger if the player does NOT have the key.")]
    [SerializeField] private bool onlyTriggerIfMissingKey = false;
    [SerializeField] private bool triggerOnlyOnce = true;
    
    private bool hasTriggered = false;
    private PlayerMovement playerMovement;
    private float originalAnimatorSpeed = 1f;

    private void Awake()
    {
        var col = GetComponent<BoxCollider>();
        if (col != null) col.isTrigger = true;

        if (zoomCamera != null)
        {
            zoomCamera.Priority = inactivePriority;
        }

        // Hide the activation target initially
        if (objectToActivateOnComplete != null)
        {
            objectToActivateOnComplete.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnlyOnce) return;

        // Check if the collider belongs to the player
        if (other.CompareTag("Player") || other.GetComponent<Player.CameraSwitcher>() != null)
        {
            // Condition Check: If we only want this for locked doors, skip if player has key
            if (onlyTriggerIfMissingKey)
            {
                PlayerInventory inv = other.GetComponent<PlayerInventory>();
                if (inv == null) inv = other.GetComponentInParent<PlayerInventory>();
                
                if (inv != null && inv.HasNormalKey)
                {
                    // Player has the key, skip this zoom sequence
                    return;
                }
            }

            playerMovement = other.GetComponent<PlayerMovement>();
            if (playerMovement == null) playerMovement = other.GetComponentInParent<PlayerMovement>();
            
            TriggerSequence();
        }
    }

    private void TriggerSequence()
    {
        hasTriggered = true;
        
        // Globally disable zombie aggression during cutscenes
        ZombieAI.IsCutsceneActive = true;

        // Freeze player
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
            
            if (playerMovement.PlayerAnimator != null)
            {
                originalAnimatorSpeed = playerMovement.PlayerAnimator.speed;
                playerMovement.PlayerAnimator.speed = 0f;
            }
        }

        // Activate camera
        if (zoomCamera != null)
        {
            zoomCamera.Priority = activePriority;
            Invoke(nameof(EndSequence), messageDuration);
        }

        // Show message
        GameEvents.RaiseNarrativeMessage(narrativeMessage, messageDuration);
    }

    private void EndSequence()
    {
        // Re-enable zombie aggression
        ZombieAI.IsCutsceneActive = false;

        // Restore camera
        if (zoomCamera != null)
        {
            zoomCamera.Priority = inactivePriority;
        }

        // Unfreeze player
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
            
            if (playerMovement.PlayerAnimator != null)
            {
                playerMovement.PlayerAnimator.speed = originalAnimatorSpeed;
            }
        }

        // Activate next object in the chain
        if (objectToActivateOnComplete != null)
        {
            objectToActivateOnComplete.SetActive(true);
        }
    }
}
