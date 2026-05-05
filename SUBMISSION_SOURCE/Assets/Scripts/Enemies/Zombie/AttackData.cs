using UnityEngine;

[System.Serializable]
public class AttackData
{
    public string animTrigger;
    public float damage = 10f;
    public float range = 1.5f;
    public bool isRanged = false;
    
    [Header("Timings")]
    public float cooldown = 2f;
    public float hitboxDelay = 0.4f;
    public float hitboxDuration = 0.3f;
}
