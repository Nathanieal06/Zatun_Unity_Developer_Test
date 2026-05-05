// Assets/Scripts/Interactables/SpawnerInteractable.cs
using UnityEngine;

/// <summary>
/// An interactable object that spawns (activates or instantiates) another object when used.
/// Useful for 'Praying' to reveal a key, or 'Searching' to find an item.
/// </summary>
public class SpawnerInteractable : BasePickup
{
    [Header("Spawner Settings")]
    [Tooltip("The object (e.g. the Key) that will be activated/spawned when the player interacts.")]
    [SerializeField] private GameObject objectToSpawn;

    [Tooltip("Optional: If assigned, the object will move to this position and rotation when activated.")]
    [SerializeField] private Transform spawnPoint;
    
    [Tooltip("Message shown in the prompt, e.g. 'Press F to Pray'")]
    [SerializeField] private string interactPrompt = "Pray";

    [Tooltip("If true, it will instantiate a new copy. If false, it will just activate the reference object in the scene.")]
    [SerializeField] private bool instantiateAsNew = false;

    [Header("Post-Interaction")]
    [Tooltip("If true, this interactable will disappear after one use.")]
    [SerializeField] private bool destroyOnUse = true;

    public override string GetPickupLabel()
    {
        return $"Press F to {interactPrompt}";
    }

    public override void OnPickup(GameObject player)
    {
        if (objectToSpawn != null)
        {
            Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
            Quaternion rot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

            if (instantiateAsNew)
            {
                Instantiate(objectToSpawn, pos, rot);
                Debug.Log($"[SpawnerInteractable] {gameObject.name} instantiated a new {objectToSpawn.name}");
            }
            else
            {
                objectToSpawn.transform.position = pos;
                objectToSpawn.transform.rotation = rot;
                objectToSpawn.SetActive(true);
                Debug.Log($"[SpawnerInteractable] {gameObject.name} activated {objectToSpawn.name} in scene.");
            }
        }
        else
        {
            Debug.LogError($"[SpawnerInteractable] {gameObject.name} has no Object To Spawn assigned!");
        }

        if (destroyOnUse)
        {
            ConsumePickup();
        }
    }
}
