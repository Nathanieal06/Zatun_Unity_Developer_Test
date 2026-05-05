// Assets/Scripts/Pickups/PickupInteractor.cs
using System.Collections.Generic;
using UnityEngine;

public class PickupInteractor : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRadius = 2f;

    private readonly List<BasePickup> nearbyPickups = new List<BasePickup>();
    private BasePickup currentBestPickup;

    private void OnEnable()
    {
        InputManager.OnPickupPressed += TryPickup;
    }

    private void OnDisable()
    {
        InputManager.OnPickupPressed -= TryPickup;
    }

    private void Start()
    {
        SetupInteractionCollider();
    }

    // --- REFRESH: Scan for pickups every 0.2 seconds ──────────────────
    private float scanTimer = 0f;
    private const float scanInterval = 0.2f;

    private void Update()
    {
        scanTimer += Time.deltaTime;
        if (scanTimer >= scanInterval)
        {
            scanTimer = 0f;
            ScanForNearbyPickups();
        }

        // Periodically clean stale/disabled pickups from the list
        nearbyPickups.RemoveAll(p => p == null || !p.gameObject.activeInHierarchy);
    }

    private void ScanForNearbyPickups()
    {
        // Use OverlapSphere to find all colliders in range
        Collider[] hits = Physics.OverlapSphere(transform.position, interactionRadius, ~0, QueryTriggerInteraction.Collide);
        
        bool listChanged = false;
        
        // Add new pickups found in the scan
        foreach (var hit in hits)
        {
            BasePickup pickup = hit.GetComponent<BasePickup>();
            if (pickup != null && pickup.gameObject.activeInHierarchy && !nearbyPickups.Contains(pickup))
            {
                nearbyPickups.Add(pickup);
                listChanged = true;
            }
        }

        // Remove pickups that are no longer in the hits array
        for (int i = nearbyPickups.Count - 1; i >= 0; i--)
        {
            BasePickup p = nearbyPickups[i];
            if (p == null || !p.gameObject.activeInHierarchy)
            {
                nearbyPickups.RemoveAt(i);
                listChanged = true;
                continue;
            }

            bool stillInRange = false;
            foreach (var hit in hits)
            {
                if (hit.gameObject == p.gameObject)
                {
                    stillInRange = true;
                    break;
                }
            }

            if (!stillInRange)
            {
                nearbyPickups.RemoveAt(i);
                listChanged = true;
            }
        }

        if (listChanged)
        {
            RefreshBestPickup();
        }
    }

    private void SetupInteractionCollider()
    {
        // The dynamic scanning in Update() replaces the need for a persistent trigger zone
        // This is more robust for objects that spawn/activate inside the radius.
    }

    private void RefreshBestPickup()
    {
        // Clean nulls and disabled objects
        nearbyPickups.RemoveAll(p => p == null || !p.gameObject.activeInHierarchy);

        if (nearbyPickups.Count == 0)
        {
            currentBestPickup = null;
            GameEvents.RaisePickupExitRange();
            return;
        }

        currentBestPickup = GetNearestPickup();

        if (currentBestPickup != null)
            GameEvents.RaisePickupEnterRange(currentBestPickup.GetPickupLabel());
    }

    private BasePickup GetNearestPickup()
    {
        BasePickup nearest = null;
        float minDist = float.MaxValue;

        foreach (var pickup in nearbyPickups)
        {
            if (pickup == null || !pickup.gameObject.activeInHierarchy) continue;

            float dist = Vector3.Distance(transform.position, pickup.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = pickup;
            }
        }

        return nearest;
    }

    private void TryPickup()
    {
        // Re-clean before attempting pickup
        nearbyPickups.RemoveAll(p => p == null || !p.gameObject.activeInHierarchy);

        if (currentBestPickup == null || !currentBestPickup.gameObject.activeInHierarchy)
        {
            // Debug.Log("[PickupInteractor] Nothing valid to pick up.");
            RefreshBestPickup();
            return;
        }

        // Debug.Log($"[PickupInteractor] Picked up: {currentBestPickup.name}");
        currentBestPickup.OnPickup(gameObject);
        nearbyPickups.Remove(currentBestPickup);
        currentBestPickup = null;
        RefreshBestPickup();
    }
}