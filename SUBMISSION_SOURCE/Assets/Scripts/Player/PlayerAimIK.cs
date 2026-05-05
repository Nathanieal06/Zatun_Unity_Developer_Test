using UnityEngine;

namespace Player
{
    /// <summary>
    /// Manual spine IK that rotates the Spine and Chest bones to point the
    /// equipped weapon's muzzle at WorldCrosshairController.CurrentTargetPoint.
    ///
    /// KEY FIX: Distributes rotation across BOTH spine and chest bones.
    /// Rotating only the chest gives ~30° of range; splitting across spine+chest
    /// gives ~60°, which is enough to align a two-handed rifle with the crosshair.
    ///
    /// spineWeight controls how much of the rotation goes to the Spine bone vs Chest.
    /// e.g. spineWeight = 0.4 → 40% spine, 60% chest (natural-looking split).
    /// </summary>
    public class PlayerAimIK : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The Animator component on the player model.")]
        [SerializeField] private Animator playerAnimator;

        [Header("IK Settings")]
        [Tooltip("How far up/down the spine can bend (degrees).")]
        [SerializeField] private float verticalClamp = 70f;

        [Tooltip("How far left/right the spine can twist (degrees).")]
        [SerializeField] private float horizontalClamp = 70f;

        [Tooltip("How fast the IK blends in/out when aiming starts/stops.")]
        [SerializeField] private float blendSpeed = 20f;

        [Tooltip("Number of iterative fine-correction passes per frame. " +
                 "Higher = more accurate but more expensive. 6 is a good balance.")]
        [SerializeField] [Range(1, 10)] private int correctionIterations = 6;

        [Tooltip("Fraction of rotation applied to the Spine bone (the rest goes to Chest). " +
                 "0 = all chest (may look hunched), 1 = all spine (too rigid). " +
                 "0.35 gives a natural two-handed rifle look.")]
        [SerializeField] [Range(0f, 1f)] private float spineWeight = 0.35f;

        [Tooltip("How fast the spine angles chase the target each frame. " +
                 "Lower = smoother but laggy. 30 is responsive without snapping.")]
        [SerializeField] private float angleChaseSpeed = 30f;

        // Internal state
        private float _ikWeight        = 0f;
        private float _currentPitch    = 0f;
        private float _currentYaw      = 0f;

        // ── Lifecycle ──────────────────────────────────────────────────────

        private void Start()
        {
            if (playerAnimator == null)
                playerAnimator = GetComponentInChildren<Animator>();
        }

        private void LateUpdate()
        {
            if (playerAnimator == null) return;

            bool    isAiming  = InputManager.IsAimHeld;
            Vector3 aimTarget = UI.WorldCrosshairController.CurrentTargetPoint;
            bool    hasTarget = isAiming && aimTarget != Vector3.zero;

            // Blend weight in/out
            float targetWeight = hasTarget ? 1f : 0f;
            _ikWeight = Mathf.MoveTowards(_ikWeight, targetWeight, Time.deltaTime * blendSpeed);

            // Reset angles when fully released so next aim-in starts clean
            if (_ikWeight <= 0.01f)
            {
                _currentPitch = 0f;
                _currentYaw   = 0f;
                return;
            }

            // Require a valid aim target to apply IK
            if (aimTarget == Vector3.zero) return;

            // ── Resolve bones ──────────────────────────────────────────────
            Transform spine = playerAnimator.GetBoneTransform(HumanBodyBones.Spine);
            Transform chest = playerAnimator.GetBoneTransform(HumanBodyBones.Chest);

            // Chest is required; Spine is optional (enhances range if present)
            if (chest == null) return;

            // ── Resolve muzzle ─────────────────────────────────────────────
            WeaponBase weapon = GetComponentInChildren<WeaponBase>();
            if (weapon == null) return;
            Transform muzzle = weapon.MuzzlePoint != null ? weapon.MuzzlePoint : weapon.transform;

            // ── Step 1: Calculate required coarse angles ───────────────────
            Vector3 weaponDir = muzzle.forward;
            Vector3 targetDir = (aimTarget - muzzle.position).normalized;

            // Flatten vectors onto the character's horizontal plane
            Vector3 flatTarget = new Vector3(targetDir.x, 0f, targetDir.z).normalized;
            Vector3 flatWeapon = new Vector3(weaponDir.x, 0f, weaponDir.z).normalized;
            Vector3 flatChar   = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;

            // Vertical (pitch): how much the muzzle needs to tilt up/down
            float targetPitch = Vector3.SignedAngle(flatTarget, targetDir,  transform.right);
            float weaponPitch = Vector3.SignedAngle(flatWeapon, weaponDir,  transform.right);
            float reqPitch    = Mathf.Clamp(targetPitch - weaponPitch, -verticalClamp, verticalClamp);

            // Horizontal (yaw): how much the muzzle needs to swing left/right
            float targetYaw = Vector3.SignedAngle(flatChar, flatTarget, Vector3.up);
            float weaponYaw = Vector3.SignedAngle(flatChar, flatWeapon, Vector3.up);
            float reqYaw    = Mathf.Clamp(targetYaw - weaponYaw, -horizontalClamp, horizontalClamp);

            // Smooth chase so the rotation doesn't snap
            _currentPitch = Mathf.Lerp(_currentPitch, reqPitch, Time.deltaTime * angleChaseSpeed);
            _currentYaw   = Mathf.Lerp(_currentYaw,   reqYaw,   Time.deltaTime * angleChaseSpeed);

            float w = _ikWeight;

            // ── Step 2: Apply rotation split across Spine + Chest ──────────
            // Splitting the angle across two bones gives a more natural posture
            // and doubles the effective rotation range vs rotating chest alone.
            if (spine != null)
            {
                float spineShare = spineWeight;
                spine.rotation = Quaternion.AngleAxis(_currentYaw   * w * spineShare, transform.up)    * spine.rotation;
                spine.rotation = Quaternion.AngleAxis(_currentPitch * w * spineShare, transform.right) * spine.rotation;
            }

            float chestShare = spine != null ? (1f - spineWeight) : 1f;
            chest.rotation = Quaternion.AngleAxis(_currentYaw   * w * chestShare, transform.up)    * chest.rotation;
            chest.rotation = Quaternion.AngleAxis(_currentPitch * w * chestShare, transform.right) * chest.rotation;

            // ── Step 3: Iterative fine-correction ─────────────────────────
            // Each pass rotates the chest by the remaining angle between
            // muzzle.forward and the true direction to target.
            // This fully compensates for camera parallax and bone offset.
            for (int i = 0; i < correctionIterations; i++)
            {
                Vector3 dirToTarget = aimTarget - muzzle.position;
                if (dirToTarget.sqrMagnitude < 0.001f) break;

                Quaternion correction = Quaternion.FromToRotation(muzzle.forward, dirToTarget.normalized);

                // Apply the correction with weight — split again for natural look
                if (spine != null)
                {
                    spine.rotation = Quaternion.Slerp(Quaternion.identity, correction, w * spineWeight)
                                     * spine.rotation;
                }
                chest.rotation = Quaternion.Slerp(Quaternion.identity, correction, w * chestShare)
                                 * chest.rotation;
            }
        }
    }
}
