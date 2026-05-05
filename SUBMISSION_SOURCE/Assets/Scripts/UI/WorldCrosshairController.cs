using UnityEngine;
using UnityEngine.UI;
#if UNITY_ANIMATION_RIGGING
using UnityEngine.Animations.Rigging;
#endif

namespace UI
{
    public class WorldCrosshairController : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Maximum distance to place the crosshair if no object is hit.")]
        [SerializeField] private float maxDistance = 100f;



        [Tooltip("Optional Sprite for the crosshair. Replaces the green square.")]
        [SerializeField] private Sprite crosshairSprite;

        [Tooltip("Size of the crosshair image on screen.")]
        [SerializeField] private float crosshairSize = 32f;

        [Tooltip("Color of the auto-generated crosshair dot.")]
        [SerializeField] private Color crosshairColor = Color.green;

        [Tooltip("Tag of the player GameObject to ignore when raycasts.")]
        [SerializeField] private string playerTag = "Player";

        [Header("Rigging")]
        [Tooltip("Optional: Assign an existing Transform here (like an Empty GameObject). It will be moved to the crosshair location so you can assign it as a target in Animation Rigging.")]
        [SerializeField] private Transform rigTarget;

        [Tooltip("Optional: Assign the Rig component(s) that drive aim IK. Their weight will be set to 0/1 when aiming stops/starts instead of toggling the target GameObject.")]
        [SerializeField] private UnityEngine.Object[] aimRigs;

        [Tooltip("Time in seconds to blend the IK rig weight in/out. Set to 0 for instant snap.")]
        [SerializeField] [Range(0f, 0.5f)] private float rigBlendTime = 0.15f;

        [Tooltip("Seconds after aiming starts during which the crosshair is locked to screen-center so it doesn't drift during the camera transition.")]
        [SerializeField] [Range(0f, 0.5f)] private float crosshairSettleTime = 0.12f;

        [Tooltip("If true, generates a green square at the hit point. Disable this if you are using your own Rig Target visual.")]
        [SerializeField] private bool showDebugVisual = true;

        /// <summary>World-space point the crosshair is hitting. Returns Vector3.zero when not aiming.</summary>
        public static Vector3 CurrentTargetPoint { get; private set; }

        private Camera mainCamera;
        private RectTransform crosshairTransform;
        private Transform playerRoot;

        // Tracks whether a click just happened so the crosshair hides that frame.
        private bool _hideThisFrame = false;

        // Current blended weight of any assigned aimRigs (0 = off, 1 = fully aimed).
        private float _currentRigWeight = 0f;

        // Timer used to pin crosshair to screen-center immediately after aim-in starts.
        // While > 0 the crosshair stays at viewport (0.5, 0.5) regardless of hit point.
        private float _crosshairSettleTimer = 0f;

        private static WorldCrosshairController instance;

        private void Awake()
        {
            // 1. Destroy duplicate instances of the controller script
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }
            instance = this;

            // 2. Destroy any lingering/ghost canvases from previous runs
            GameObject[] ghostCanvases = GameObject.FindGameObjectsWithTag("Untagged");
            foreach (var go in ghostCanvases)
            {
                if (go.name == "AutoWorldCrosshairCanvas")
                    Destroy(go);
            }
        }

        private void Start()
        {
            mainCamera = Camera.main;

            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
                playerRoot = playerObj.transform.root;
            else
                Debug.LogWarning($"[WorldCrosshairController] Could not find a GameObject tagged '{playerTag}'. " +
                                 "Raycasts may hit the player's own colliders.");

            if (showDebugVisual)
                GenerateCrosshairUI();

            // Subscribe to attack events to hide crosshair on click
            InputManager.OnAttackPressed += HandleAttackPressed;
        }

        private void OnDestroy()
        {
            // Unsubscribe to prevent memory leaks
            InputManager.OnAttackPressed -= HandleAttackPressed;
        }

        private void HandleAttackPressed()
        {
            _hideThisFrame = true;
        }

        private void GenerateCrosshairUI()
        {
            GameObject canvasObj = new GameObject("AutoWorldCrosshairCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            GameObject crosshairObj = new GameObject("CrosshairImage");
            crosshairObj.transform.SetParent(canvasObj.transform, false);

            crosshairTransform = crosshairObj.AddComponent<RectTransform>();
            crosshairTransform.sizeDelta = new Vector2(crosshairSize, crosshairSize);
            crosshairTransform.anchoredPosition = Vector2.zero;

            UnityEngine.UI.Image img = crosshairObj.AddComponent<UnityEngine.UI.Image>();
            if (crosshairSprite != null)
            {
                img.sprite = crosshairSprite;
                img.color = Color.white; // Show image clearly
            }
            else
            {
                img.color = crosshairColor; // Fallback to green
            }
        }

        private void Update()
        {
            // Input is now handled via HandleAttackPressed event callback
            // to support the New Input System without errors.
        }

        private void LateUpdate()
        {
            if (mainCamera == null) return;

            bool isAiming = InputManager.IsAimHeld;

            // Count down the settle timer while aiming; reset it when aim is released.
            if (isAiming)
            {
                if (_crosshairSettleTimer > 0f)
                    _crosshairSettleTimer -= Time.deltaTime;
            }
            else
            {
                // Arm the timer for the next aim-in.
                _crosshairSettleTimer = crosshairSettleTime;
            }

            bool crosshairSettling = isAiming && (_crosshairSettleTimer > 0f);

            // Issue #2 — Hide on the exact frame aiming stops OR on click frame.
            // _hideThisFrame is set by HandleAttackPressed (called by InputManager event,
            // which fires in the same frame as the input — before LateUpdate).
            // showCrosshair is false the instant IsAimHeld goes false. No delay.
            bool showCrosshair = isAiming && !_hideThisFrame;
            _hideThisFrame = false; // reset every frame

            // Apply immediately — this frame, not next frame.
            if (crosshairTransform != null)
                crosshairTransform.gameObject.SetActive(showCrosshair);

            // While the camera is still blending in, pin the crosshair to screen center
            // so it stays perfectly still instead of drifting with the transitioning camera.
            if (crosshairSettling && crosshairTransform != null && showCrosshair)
            {
                crosshairTransform.position = new Vector3(
                    Screen.width  * 0.5f,
                    Screen.height * 0.5f,
                    0f);
            }


            // ── IK Rig weight blend ───────────────────────────────────────────
            // IMPORTANT: Do NOT call rigTarget.gameObject.SetActive(false) to stop IK.
            // Unity Animation Rigging retains the last solved bone pose even when the
            // target is deactivated. We must drive rig.weight to 0 to release the bones.
            float targetRigWeight = isAiming ? 1f : 0f;
            if (rigBlendTime > 0f)
                _currentRigWeight = Mathf.MoveTowards(_currentRigWeight, targetRigWeight, Time.deltaTime / rigBlendTime);
            else
                _currentRigWeight = targetRigWeight;

            SetAimRigWeights(_currentRigWeight);
            // ─────────────────────────────────────────────────────────────────

            if (!isAiming)
            {
                // Clear the static target so PlayerAimIK's blend-out ends cleanly.
                CurrentTargetPoint = Vector3.zero;

                // Move the rigTarget to a neutral position (e.g., 2m in front of the player)
                // so that during the blend-out, the gun doesn't point at a distant wall.
                if (rigTarget != null && playerRoot != null)
                {
                    rigTarget.position = playerRoot.position + playerRoot.forward * 2f + Vector3.up * 1.5f;
                    rigTarget.rotation = playerRoot.rotation;
                }
                return;
            }

            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            bool foundHit = false;
            foreach (var hit in hits)
            {
                if (playerRoot != null && hit.transform.root == playerRoot) continue;

                CurrentTargetPoint = hit.point;

                if (crosshairTransform != null && showCrosshair && !crosshairSettling)
                {
                    Vector3 screenPoint = mainCamera.WorldToScreenPoint(hit.point);
                    if (screenPoint.z > 0)
                    {
                        crosshairTransform.position = screenPoint;
                    }
                    else
                    {
                        crosshairTransform.gameObject.SetActive(false);
                    }
                }

                if (rigTarget != null)
                {
                    rigTarget.position = hit.point;
                    rigTarget.rotation = Quaternion.LookRotation(-hit.normal);
                }

                foundHit = true;
                break;
            }

            if (!foundHit)
            {
                Vector3 targetPosition = ray.origin + ray.direction * maxDistance;
                CurrentTargetPoint = targetPosition;

                if (crosshairTransform != null && showCrosshair && !crosshairSettling)
                {
                    Vector3 screenPoint = mainCamera.WorldToScreenPoint(targetPosition);
                    if (screenPoint.z > 0)
                    {
                        crosshairTransform.position = screenPoint;
                    }
                    else
                    {
                        crosshairTransform.gameObject.SetActive(false);
                    }
                }

                if (rigTarget != null)
                {
                    rigTarget.position = targetPosition;
                    rigTarget.rotation = Quaternion.LookRotation(-mainCamera.transform.forward);
                }
            }
        }

        /// <summary>
        /// Sets the weight on all assigned aimRigs components.
        /// Supports UnityEngine.Animations.Rigging.Rig (if the package is present)
        /// as a plain UnityEngine.Object reference, resolved at runtime via reflection
        /// so the script compiles even when the Animation Rigging package is absent.
        /// </summary>
        private void SetAimRigWeights(float weight)
        {
            if (aimRigs == null) return;
            foreach (var rigObj in aimRigs)
            {
                if (rigObj == null) continue;

#if UNITY_ANIMATION_RIGGING
                if (rigObj is UnityEngine.Animations.Rigging.Rig rig)
                {
                    rig.weight = weight;
                    continue;
                }
                if (rigObj is UnityEngine.Animations.Rigging.MultiAimConstraint mac)
                {
                    mac.weight = weight;
                    continue;
                }
                if (rigObj is UnityEngine.Animations.Rigging.TwoBoneIKConstraint tbik)
                {
                    tbik.weight = weight;
                    continue;
                }
#endif
                // Fallback: try to set 'weight' via reflection for any other constraint type.
                var weightProp = rigObj.GetType().GetProperty("weight");
                weightProp?.SetValue(rigObj, weight);
            }
        }
    }
}
