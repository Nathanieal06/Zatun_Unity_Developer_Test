// Assets/Scripts/Equipment/GunRecoil.cs
using UnityEngine;

/// <summary>
/// Axis along which the recoil kick is applied.
/// All directions are relative to the gun's OWN orientation (world-space transform axes),
/// so BackwardZ always means "away from where the barrel points" regardless of how the
/// gun model is rotated inside its parent rig.
/// </summary>
public enum RecoilAxis
{
    /// <summary>Gun moves backward along its own -forward (barrel pushes away from target). Classic rifle kick.</summary>
    BackwardZ,
    /// <summary>Gun moves along its own +up  (muzzle-rise feel).</summary>
    UpY,
    /// <summary>Gun moves along its own -up  (muzzle dip).</summary>
    DownY,
    /// <summary>Gun moves along its own +right.</summary>
    RightX,
    /// <summary>Gun moves along its own -right (left kick).</summary>
    LeftX,
    /// <summary>Use the <c>customAxis</c> vector below — interpreted in the gun's LOCAL space and normalised automatically.</summary>
    Custom,
}

/// <summary>
/// Attach this to the gun GameObject (same object as WeaponPistol / WeaponRifle).
/// On each shot it nudges the gun backward along its local -Z axis by <recoilDistance>
/// and then smoothly springs it back to its rest position.
///
/// Usage:
///   Call  GunRecoil.Kick()  from WeaponPistol/WeaponRifle after a successful shot,
///   OR enable <listenToGameEvent> to have it react to GameEvents.OnShotFired automatically.
/// </summary>
public class GunRecoil : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────

    [Header("Recoil Kick")]

    [Tooltip("Which local-space axis the gun is kicked along when fired.\n" +
             "BackwardZ = classic rifle push-back.\n" +
             "UpY      = muzzle-rise only.\n" +
             "Custom   = use the vector below.")]
    [SerializeField] private RecoilAxis recoilAxis = RecoilAxis.BackwardZ;

    [Tooltip("Only used when Recoil Axis is set to Custom. Will be normalised at runtime.")]
    [SerializeField] private Vector3 customAxis = Vector3.back;

    [Tooltip("How far the gun moves along the chosen axis on each shot, in metres.")]
    [SerializeField] private float recoilDistance = 0.06f;

    [Tooltip("How quickly the position snaps back to rest (higher = snappier).")]
    [SerializeField] private float returnSpeed = 12f;

    [Tooltip("Maximum accumulated recoil offset (caps rapid fire kick).")]
    [SerializeField] private float maxRecoilDistance = 0.15f;

    [Header("Auto-Listen (optional)")]
    [Tooltip("If true, reacts to GameEvents.OnShotFired for the matching weapon type " +
             "automatically — no code changes in WeaponPistol/Rifle needed.")]
    [SerializeField] private bool listenToGameEvent = true;

    [Tooltip("Which weapon type this gun belongs to (only used when listenToGameEvent = true).")]
    [SerializeField] private WeaponType weaponType;

    // ── Private ────────────────────────────────────────────────────────────

    private Vector3 _restLocalPos;     // position at scene start / on enable
    private float   _currentOffset;   // how far we are currently displaced

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        _restLocalPos = transform.localPosition;
    }

    private void OnEnable()
    {
        _restLocalPos   = transform.localPosition;
        _currentOffset  = 0f;

        if (listenToGameEvent)
            GameEvents.OnShotFired += HandleShotFired;
    }

    private void OnDisable()
    {
        if (listenToGameEvent)
            GameEvents.OnShotFired -= HandleShotFired;
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Trigger one recoil kick. Call this directly from WeaponPistol / WeaponRifle
    /// if you prefer explicit control instead of using listenToGameEvent.
    /// </summary>
    public void Kick()
    {
        _currentOffset = Mathf.Min(_currentOffset + recoilDistance, maxRecoilDistance);
    }

    // ── Update ─────────────────────────────────────────────────────────────

    private void LateUpdate()
    {
        // Smoothly return offset toward 0
        _currentOffset = Mathf.Lerp(_currentOffset, 0f, Time.deltaTime * returnSpeed);

        // Get kick direction in the gun's own world-space, then convert to parent-local
        // space so the localPosition offset is always physically correct regardless of
        // how the gun model is rotated inside its parent rig.
        Vector3 worldDir   = GetWorldKickDirection();
        Vector3 parentLocalDir = transform.parent != null
            ? transform.parent.InverseTransformDirection(worldDir)
            : worldDir;  // no parent → world IS local

        transform.localPosition = _restLocalPos - parentLocalDir * _currentOffset;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the world-space unit direction the gun should be pushed toward on kick.
    /// Uses the gun's own transform axes so orientation is always correct.
    /// </summary>
    private Vector3 GetWorldKickDirection()
    {
        switch (recoilAxis)
        {
            // -forward = gun moves away from what it's aiming at (classic recoil)
            case RecoilAxis.BackwardZ: return -transform.forward;
            case RecoilAxis.UpY:       return  transform.up;
            case RecoilAxis.DownY:     return -transform.up;
            case RecoilAxis.RightX:    return  transform.right;
            case RecoilAxis.LeftX:     return -transform.right;
            case RecoilAxis.Custom:
                Vector3 n = customAxis.normalized;
                if (n == Vector3.zero) n = Vector3.back;
                // customAxis is expressed in the gun's own local space → convert to world
                return transform.TransformDirection(n);
            default: return -transform.forward;
        }
    }

    // ── Event Handlers ─────────────────────────────────────────────────────

    private void HandleShotFired(WeaponType type)
    {
        if (type == weaponType)
            Kick();
    }
}
