using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace Player
{
    [RequireComponent(typeof(CinemachineCamera))]
    public class AimCameraController : MonoBehaviour
    {
        [Header("Sensitivity Settings")]
        [SerializeField] private float mouseSensitivity = 0.5f;

        [Header("Rotation Constraints")]
        [SerializeField] private float minPitch = -40f;
        [SerializeField] private float maxPitch = 60f;

        private float yawTarget;
        private float pitchTarget;

        private void Update()
        {
            // Only rotate the aim camera while the player is actively aiming.
            // Without this guard the transform accumulates rotation every frame even
            // when the FreeLook camera is live, causing a snap on the next aim entry.
            if (!InputManager.IsAimHeld) return;
            if (Mouse.current == null) return;

            Vector2 lookInput = Mouse.current.delta.ReadValue();

            if (lookInput.sqrMagnitude < 0.001f) return;

            yawTarget += lookInput.x * mouseSensitivity;
            pitchTarget -= lookInput.y * mouseSensitivity; // Invert Y axis for natural pitch
            
            pitchTarget = Mathf.Clamp(pitchTarget, minPitch, maxPitch);

            // Apply rotation to the camera transform
            transform.rotation = Quaternion.Euler(pitchTarget, yawTarget, 0f);
        }

        /// <summary>
        /// Snaps the Yaw and Pitch to match the given camera's forward direction.
        /// Useful when transitioning into the Aim camera to prevent a jump.
        /// </summary>
        public void SetYawPitchFromCameraForward(Transform sourceCameraTransform)
        {
            Vector3 eulerAngles = sourceCameraTransform.eulerAngles;
            
            pitchTarget = eulerAngles.x;
            yawTarget = eulerAngles.y;

            // Handle euler wrap-around for pitch (e.g., 350 degrees should be -10)
            if (pitchTarget > 180f)
            {
                pitchTarget -= 360f;
            }

            pitchTarget = Mathf.Clamp(pitchTarget, minPitch, maxPitch);
            
            transform.rotation = Quaternion.Euler(pitchTarget, yawTarget, 0f);
        }
    }
}
