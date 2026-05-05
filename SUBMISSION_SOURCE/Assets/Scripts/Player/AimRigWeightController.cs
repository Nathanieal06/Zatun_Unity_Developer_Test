using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Player
{
    /// <summary>
    /// Attach this to your Player.
    /// Manages aim IK constraint weights for both Pistol and Rifle.
    /// - Only the active weapon's constraint blends in/out with aiming.
    /// - The inactive weapon's constraint is immediately forced to 0.
    /// - isRifleEquipped is driven automatically via GameEvents.OnWeaponEquipped.
    /// </summary>
    public class AimRigWeightController : MonoBehaviour
    {
        [Header("Aim Constraints")]
        [Tooltip("MultiAimConstraint used when the Pistol is equipped.")]
        [SerializeField] private MultiAimConstraint pistolAimConstraint;

        [Tooltip("MultiAimConstraint used when the Rifle is equipped.")]
        [SerializeField] private MultiAimConstraint rifleAimConstraint;

        [Header("Left Hand IK (Rifle Only)")]
        [Tooltip("TwoBoneIKConstraint that pulls the left hand to the rifle barrel.")]
        [SerializeField] private TwoBoneIKConstraint leftHandIKConstraint;

        [Header("Blend Settings")]
        [Tooltip("How fast the rig weight blends in/out (higher = snappier).")]
        [SerializeField] private float blendSpeed = 20f;

        // Driven automatically by GameEvents.OnWeaponEquipped.
        // You can also toggle this manually in the Inspector during Play Mode for testing.
        [Header("Debug / State")]
        [SerializeField] private bool isRifleEquipped = false;
        [SerializeField] private bool isPistolEquipped = false;

        [Tooltip("Enable to print constraint weights and weapon events to the Console.")]
        [SerializeField] private bool debugMode = false;

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
#if UNITY_ANIMATION_RIGGING
            // AUTO-FIX: The crazy twisting happens because the RifleAim constraint
            // is targeting the wrong bone (or has inverted axes like -Z and -Y).
            // We FORCE it to perfectly mirror the pistol constraint, since the pistol works.
            if (pistolAimConstraint != null && rifleAimConstraint != null)
            {
                var pData = pistolAimConstraint.data;
                var rData = rifleAimConstraint.data;

                // Force it to match the pistol setup exactly to prevent crazy rotations
                bool needsFix = (rData.constrainedObject != pData.constrainedObject) ||
                                (rData.aimAxis != pData.aimAxis) ||
                                (rData.upAxis != pData.upAxis);

                if (needsFix)
                {
                    // Debug.LogWarning($"[AimRig] Auto-fixing RifleAim! Forcing it to perfectly match the Pistol setup to stop the crazy twisting.");
                    
                    rData.constrainedObject = pData.constrainedObject;
                    rData.aimAxis           = pData.aimAxis;
                    rData.upAxis            = pData.upAxis;
                    rData.worldUpType       = pData.worldUpType;
                    rData.worldUpObject     = pData.worldUpObject;
                    rData.constrainedAxes   = pData.constrainedAxes;
                    
                    rifleAimConstraint.data = rData;

                    var rigBuilder = GetComponentInChildren<RigBuilder>();
                    if (rigBuilder != null) rigBuilder.Build();
                }
            }

            // AUTO-FIX: Ensure the left hand actually grabs the target properly
            // by forcing the position and rotation weights to 1.
            if (leftHandIKConstraint != null)
            {
                var lData = leftHandIKConstraint.data;
                if (lData.targetPositionWeight < 1f || lData.targetRotationWeight < 1f)
                {
                    // Debug.LogWarning("[AimRig] Auto-fixing Left Hand IK Constraint: Forcing Target Position & Rotation Weights to 1.0.");
                    lData.targetPositionWeight = 1f;
                    lData.targetRotationWeight = 1f;
                    leftHandIKConstraint.data = lData;

                    var rigBuilder = GetComponentInChildren<RigBuilder>();
                    if (rigBuilder != null) rigBuilder.Build();
                }
            }
#endif
        }

        private void OnEnable()
        {
            GameEvents.OnWeaponEquipped += HandleWeaponEquipped;
        }

        private void OnDisable()
        {
            GameEvents.OnWeaponEquipped -= HandleWeaponEquipped;
        }

        /// <summary>
        /// Listens to the existing GameEvents system to know which weapon is active.
        /// Fires whenever PlayerEquipment or WeaponController raises OnWeaponEquipped.
        /// </summary>
        private void HandleWeaponEquipped(WeaponType type)
        {
            isRifleEquipped = (type == WeaponType.Rifle);
            isPistolEquipped = (type == WeaponType.Pistol);

            if (debugMode)
            {
                // Debug.Log($"[AimRig] OnWeaponEquipped received: {type} | isRifleEquipped={isRifleEquipped} | isPistolEquipped={isPistolEquipped}");
                // Debug.Log($"[AimRig] pistolAimConstraint = {(pistolAimConstraint == null ? "NULL ❌" : pistolAimConstraint.name + " ✅")}");
                // Debug.Log($"[AimRig] rifleAimConstraint  = {(rifleAimConstraint  == null ? "NULL ❌" : rifleAimConstraint.name  + " ✅")}");
            }

            // When switching weapons, immediately zero out the constraint that is
            // no longer active so bones snap back without waiting for the lerp.
            if (!isPistolEquipped && pistolAimConstraint != null)
                pistolAimConstraint.weight = 0f;

            if (!isRifleEquipped)
            {
                if (rifleAimConstraint != null) rifleAimConstraint.weight = 0f;
                if (leftHandIKConstraint != null) leftHandIKConstraint.weight = 0f;
            }
        }

        // ── Update ─────────────────────────────────────────────────────────

        private void LateUpdate()
        {
            float targetWeight = InputManager.IsAimHeld ? 1f : 0f;

            // Pistol constraint — active only when pistol IS equipped
            if (pistolAimConstraint != null)
            {
                if (!isPistolEquipped)
                {
                    pistolAimConstraint.weight = 0f;
                }
                else
                {
                    pistolAimConstraint.weight = Mathf.Lerp(
                        pistolAimConstraint.weight, targetWeight, Time.deltaTime * blendSpeed);

                    if (pistolAimConstraint.weight < 0.01f) pistolAimConstraint.weight = 0f;
                    if (pistolAimConstraint.weight > 0.99f) pistolAimConstraint.weight = 1f;
                }
            }

            // Rifle constraint — active only when rifle IS equipped AND aiming
            if (rifleAimConstraint != null)
            {
                if (!isRifleEquipped)
                {
                    rifleAimConstraint.weight = 0f;
                }
                else
                {
                    rifleAimConstraint.weight = Mathf.Lerp(
                        rifleAimConstraint.weight, targetWeight, Time.deltaTime * blendSpeed);

                    if (rifleAimConstraint.weight < 0.01f) rifleAimConstraint.weight = 0f;
                    if (rifleAimConstraint.weight > 0.99f) rifleAimConstraint.weight = 1f;
                }
            }

            // Left Hand IK — active whenever rifle is equipped (not just when aiming)
            if (leftHandIKConstraint != null)
            {
                if (!isRifleEquipped)
                {
                    leftHandIKConstraint.weight = 0f;
                }
                else
                {
                    leftHandIKConstraint.weight = Mathf.Lerp(
                        leftHandIKConstraint.weight, 1f, Time.deltaTime * blendSpeed);
                        
                    if (leftHandIKConstraint.weight < 0.01f) leftHandIKConstraint.weight = 0f;
                    if (leftHandIKConstraint.weight > 0.99f) leftHandIKConstraint.weight = 1f;
                }
            }

            // ── Debug logging (once per second to avoid spam) ──────────────
            if (debugMode && Time.frameCount % 60 == 0)
            {
                /*
                Debug.Log($"[AimRig] isRifle={isRifleEquipped} | isPistol={isPistolEquipped} | isAiming={InputManager.IsAimHeld} | " +
                          $"pistolW={(pistolAimConstraint != null ? pistolAimConstraint.weight.ToString("F2") : "NO CONSTRAINT")} | " +
                          $"rifleW={(rifleAimConstraint  != null ? rifleAimConstraint.weight.ToString("F2")  : "NO CONSTRAINT")}");
                */
            }
        }
    }
}
