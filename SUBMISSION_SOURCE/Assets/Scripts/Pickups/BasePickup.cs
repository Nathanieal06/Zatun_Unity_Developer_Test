// Assets/Scripts/Pickups/BasePickup.cs
using UnityEngine;

/// <summary>
/// Abstract base class for all pickups.
/// Subclasses must implement: GetPickupLabel() and OnPickup().
/// 
/// Architecture note: We use an abstract class (not interface) because
/// all pickups share common behavior (floating, prompt label) in addition
/// to the abstract methods. An interface would be better if we only needed
/// the contract without shared implementation.
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class BasePickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    [Tooltip("Label shown in the pickup prompt UI")]
    [SerializeField] protected string pickupLabel = "Item";

    [Header("Visual")]
    [SerializeField] private bool floatInPlace = false;
    [SerializeField] private float floatSpeed = 1.5f;
    [SerializeField] private float floatHeight = 0.3f;
    [SerializeField] private bool rotateInPlace = false;
    [SerializeField] private float rotationSpeed = 60f;

    private Vector3 startPosition;

    protected virtual void Awake()
    {
        var col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
        else
        {
            // Debug.LogWarning($"[BasePickup] {gameObject.name} has no Collider — add one so PickupInteractor can detect it.");
        }

        startPosition = transform.position;
        // Debug.Log($"[BasePickup] {gameObject.name} initialized");
    }

    protected virtual void Update()
    {
        // Sinusoidal float (only if enabled)
        if (floatInPlace)
        {
            float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        // Slow rotation (only if enabled)
        if (rotateInPlace)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Returns the display text shown in the pickup prompt UI.
    /// Override in subclasses to customize per item.
    /// </summary>
    public virtual string GetPickupLabel() => $"Press F to pick up {pickupLabel}";

    /// <summary>
    /// Called when the player successfully picks up this item.
    /// Implement the actual pickup effect in subclasses.
    /// </summary>
    public abstract void OnPickup(GameObject player);

    /// <summary>
    /// Default pickup handling: disable the object.
    /// Override to pool objects instead of disabling for better performance.
    /// </summary>
    protected virtual void ConsumePickup()
    {
        gameObject.SetActive(false);
        // Future: return to object pool here
    }
}