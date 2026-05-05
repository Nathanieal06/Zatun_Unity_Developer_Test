using UnityEngine;
using Unity.Cinemachine;

namespace Player
{
    public class CameraSwitcher : MonoBehaviour
    {
        [Header("Cinemachine References")]
        [Tooltip("The default Freelook/ThirdPerson camera.")]
        [SerializeField] private CinemachineCamera freelookCamera;
        
        [Tooltip("The over-the-shoulder Aim camera.")]
        [SerializeField] private CinemachineCamera aimCamera;

        [Header("Priorities")]
        [SerializeField] private int activePriority = 20;
        [SerializeField] private int inactivePriority = 10;

        private AimCameraController aimCameraController;
        private CinemachineInputAxisController freelookInputAxisController;
        private Camera mainCam;

        private void Start()
        {
            mainCam = Camera.main;

            if (aimCamera != null)
            {
                aimCameraController = aimCamera.GetComponent<AimCameraController>();
            }

            if (freelookCamera != null)
            {
                freelookInputAxisController = freelookCamera.GetComponent<CinemachineInputAxisController>();
            }

            // Start in free look state
            SetAimState(false);
        }

        private void OnEnable()
        {
            InputManager.OnAimStarted += HandleAimStarted;
            InputManager.OnAimCanceled += HandleAimCanceled;
        }

        private void OnDisable()
        {
            InputManager.OnAimStarted -= HandleAimStarted;
            InputManager.OnAimCanceled -= HandleAimCanceled;
        }

        private void HandleAimStarted()
        {
            // Snap Aim camera rotation so it doesn't wildly spin when enabled
            if (aimCameraController != null && mainCam != null)
            {
                aimCameraController.SetYawPitchFromCameraForward(mainCam.transform);
            }

            // Disable free look mouse input so it freezes its orbit while aiming
            if (freelookInputAxisController != null)
            {
                freelookInputAxisController.enabled = false;
            }

            SetAimState(true);
        }

        private void HandleAimCanceled()
        {
            // Sync FreeLook camera rotation to where we finished aiming so it doesn't snap back
            if (freelookCamera != null && freelookInputAxisController != null && mainCam != null)
            {
                Vector3 euler = mainCam.transform.eulerAngles;
                float yaw = euler.y;
                float pitch = euler.x;
                if (pitch > 180f) pitch -= 360f;

                // Sync PanTilt if present
                var panTilt = freelookCamera.GetComponent<CinemachinePanTilt>();
                if (panTilt != null)
                {
                    panTilt.PanAxis.Value = yaw;
                    panTilt.TiltAxis.Value = pitch;
                }

                // Sync OrbitalFollow if present
                var orbitalFollow = freelookCamera.GetComponent<CinemachineOrbitalFollow>();
                if (orbitalFollow != null)
                {
                    orbitalFollow.HorizontalAxis.Value = yaw;
                    // Note: OrbitalFollow vertical axis is often a normalized [0,1] value or pitch.
                    // We'll sync the vertical axis if the property is accessible, but Horizontal is the most critical for not snapping.
                    // The property name in v3 is usually VerticalAxis or similar, but just syncing horizontal is usually enough to prevent the jarring snap.
                    // To be safe we just sync HorizontalAxis here.
                }
                
                freelookInputAxisController.enabled = true;
            }

            SetAimState(false);
        }

        private void SetAimState(bool isAiming)
        {
            if (aimCamera != null)
            {
                aimCamera.Priority = isAiming ? activePriority : inactivePriority;
            }

            if (freelookCamera != null)
            {
                freelookCamera.Priority = isAiming ? inactivePriority : activePriority;
            }

            // Notify UI and other systems that the aim state has changed
            GameEvents.RaiseAimStateChanged(isAiming);
        }
    }
}
