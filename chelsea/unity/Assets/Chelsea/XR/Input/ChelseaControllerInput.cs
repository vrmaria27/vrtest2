using System.Collections.Generic;
using Chelsea.Runtime;
using Chelsea.XR;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace Chelsea.Input
{
    /// <summary>
    /// Controller-only developer adapter using Unity's XR device API. It does not
    /// implement hands, passthrough, anchors, safe teleport or headset validation.
    /// Attach to a bare camera rig; do not also attach another head-pose driver.
    /// </summary>
    public sealed class ChelseaControllerInput : MonoBehaviour, IXrSession
    {
        [SerializeField] private ChelseaExperience experience;
        [SerializeField] private Camera headCamera;
        [SerializeField] private Transform trackingOrigin;
        [SerializeField] private Material rayMaterial;
        [SerializeField] private bool enableDesktopControls = true;
        [SerializeField] private float rayLengthMetres = 4f;
        [SerializeField] private float fallbackEyeHeightMetres = 1.65f;

        private readonly List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
        private LineRenderer leftRay;
        private LineRenderer rightRay;
        private Material ownedRayMaterial;
        private bool focused = true;
        private bool paused;
        private bool previousXrActive;
        private bool leftArmed;
        private bool rightArmed;
        private bool leftWasTracked;
        private bool rightWasTracked;
        private bool useFloorOrigin;
        private XRInputSubsystem activeSubsystem;
        private TrackingOriginModeFlags actualOriginMode;
        private TrackingOriginModeFlags requestedOriginMode;
        private float nextSubsystemDiscovery;
        private bool needsInitialPlacement;
        private bool leftTrigger;
        private bool rightTrigger;
        private bool leftPrimary;
        private bool rightPrimary;
        private Vector2 leftAxis;
        private Vector2 rightAxis;
        private int previousGrabCount;
        private bool previousSingleLeft;
        private Vector3 previousGripPosition;
        private float previousGripYaw;
        private float scaleStartDistance;
        private float scaleStartWidth;
        private bool desktopDragging;
        private Vector3 previousDesktopPoint;

        public bool HasFocus => focused && !paused;
        public bool IsTracking { get; private set; }
        public bool IsXrActive { get; private set; }
        public bool ControllerControlsArmed => leftArmed || rightArmed;
        public bool SupportsPassthrough => false;
        public bool TrySetPassthrough(bool enabled) => false;
        public string AdapterStatus
        {
            get
            {
                if (IsXrActive) return IsTracking
                    ? "Controller developer adapter; device validation pending"
                    : "XR tracking/origin unavailable; interactions paused";
                if (DesktopAllowed) return "Desktop inspection controls; not a headset session";
#if !ENABLE_LEGACY_INPUT_MANAGER
                if (!Application.isMobilePlatform && enableDesktopControls)
                    return "Desktop controls unavailable: legacy input disabled (Both is optional for Editor inspection only)";
#endif
                return "XR session unavailable; interactions paused";
            }
        }
        private bool DesktopAllowed
        {
            get
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                return enableDesktopControls && !Application.isMobilePlatform;
#else
                return false;
#endif
            }
        }

        // Called by the Editor launch-scene generator. Does not create edit-time objects.
        public void Configure(ChelseaExperience application, Camera camera, Material controllerRayMaterial = null)
        {
            experience = application;
            headCamera = camera;
            trackingOrigin = camera != null ? camera.transform.parent : null;
            rayMaterial = controllerRayMaterial;
        }

        private void OnEnable()
        {
            Application.onBeforeRender += BeforeRender;
            focused = Application.isFocused;
            previousXrActive = false;
        }

        private void Start()
        {
            if (headCamera == null) headCamera = Camera.main;
            if (trackingOrigin == null && headCamera != null) trackingOrigin = headCamera.transform.parent;
            if (experience == null || headCamera == null || trackingOrigin == null)
            {
                Debug.LogError("Chelsea input requires an experience and a camera under a tracking-origin transform.", this);
                enabled = false;
                return;
            }
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (rayMaterial != null || shader != null)
            {
                ownedRayMaterial = rayMaterial != null ? new Material(rayMaterial) : new Material(shader);
                ownedRayMaterial.name = "Chelsea controller ray (runtime)";
                ownedRayMaterial.SetColor("_BaseColor", new Color(.72f, .91f, .81f));
                leftRay = CreateRay("Left controller ray");
                rightRay = CreateRay("Right controller ray");
            }
            else Debug.LogWarning("URP Unlit unavailable; controller rays are hidden. Restore the pinned render pipeline.", this);
        }

        private LineRenderer CreateRay(string label)
        {
            var rayObject = new GameObject(label);
            rayObject.transform.SetParent(transform, false);
            var line = rayObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = .0025f;
            line.endWidth = .001f;
            line.sharedMaterial = ownedRayMaterial;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            return line;
        }

        private void Update()
        {
            if (experience == null || headCamera == null || trackingOrigin == null) return;
            IsXrActive = XRSettings.isDeviceActive;
            if (IsXrActive && !previousXrActive)
            {
                needsInitialPlacement = true;
                nextSubsystemDiscovery = 0;
                ResetInput();
            }
            if (!IsXrActive && previousXrActive) { ReleaseSubsystem(); ResetInput(); }
            previousXrActive = IsXrActive;
            if (IsXrActive)
            {
                IsTracking = RefreshTrackingOrigin() && UpdateHeadPose();
                experience.SetTracking(IsTracking && HasFocus);
                if (!IsTracking || !HasFocus)
                {
                    HideRays();
                    ResetInput();
                    return;
                }
                if (needsInitialPlacement)
                {
                    // Start may precede the first tracked pose. Place once using the
                    // actual XR head, not the editor camera's initial coordinates.
                    experience.ReturnToTabletop();
                    needsInitialPlacement = false;
                }
                var left = Sample(XRNode.LeftHand);
                var right = Sample(XRNode.RightHand);
                // Each hand rearms independently after tracking/focus loss. A
                // reconnecting held trigger/grip cannot become a new action, and
                // the other hand remains usable for one-handed operation.
                ArmController(ref left, ref leftWasTracked, ref leftArmed);
                ArmController(ref right, ref rightWasTracked, ref rightArmed);
                UpdateRay(left, leftRay, left.Trigger && !leftTrigger);
                UpdateRay(right, rightRay, right.Trigger && !rightTrigger);
                // A/X returns are deliberately processed even when content is not ready.
                if ((left.Tracked && left.Primary && !leftPrimary) || (right.Tracked && right.Primary && !rightPrimary))
                    experience.ReturnToTabletop();
                if (!experience.IsInteractionPaused)
                {
                    ApplyAxis(left, leftAxis);
                    ApplyAxis(right, rightAxis);
                    ApplyGrabs(left, right);
                }
                else previousGrabCount = 0;
                leftTrigger = left.Trigger; rightTrigger = right.Trigger;
                leftPrimary = left.Primary; rightPrimary = right.Primary;
                leftAxis = left.Axis; rightAxis = right.Axis;
            }
            else
            {
                HideRays();
                previousGrabCount = 0;
                IsTracking = DesktopAllowed;
                experience.SetTracking(DesktopAllowed && HasFocus);
                if (DesktopAllowed && HasFocus) UpdateDesktop();
                else desktopDragging = false;
            }
        }

        private bool RefreshTrackingOrigin()
        {
            if (activeSubsystem != null && !activeSubsystem.running) ReleaseSubsystem();
            if (activeSubsystem == null)
            {
                // isDeviceActive can precede the input subsystem's running state.
                // Retry discovery centrally, without per-frame list allocation.
                if (Time.unscaledTime < nextSubsystemDiscovery) return false;
                nextSubsystemDiscovery = Time.unscaledTime + .5f;
                subsystems.Clear();
                SubsystemManager.GetSubsystems(subsystems);
                foreach (var subsystem in subsystems)
                {
                    if (!subsystem.running) continue;
                    activeSubsystem = subsystem;
                    activeSubsystem.trackingOriginUpdated += TrackingOriginUpdated;
                    if (subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor))
                        requestedOriginMode = TrackingOriginModeFlags.Floor;
                    else if (subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Device))
                        requestedOriginMode = TrackingOriginModeFlags.Device;
                    else requestedOriginMode = TrackingOriginModeFlags.Unknown;
                    break;
                }
            }
            if (activeSubsystem == null) return false;
            var mode = activeSubsystem.GetTrackingOriginMode();
            if (mode != actualOriginMode)
            {
                actualOriginMode = mode;
                ResetInput();
            }
            // Use the observed current mode, never the requested mode. A floor
            // request can complete asynchronously; adding an eye offset to a
            // floor-relative pose would otherwise double the player's height.
            useFloorOrigin = (mode & TrackingOriginModeFlags.Floor) != 0;
            bool knownMode = useFloorOrigin || (mode & TrackingOriginModeFlags.Device) != 0;
            // At session startup, don't place against the old device origin while
            // an accepted floor-origin request is still completing.
            bool initialOriginReady = !needsInitialPlacement || requestedOriginMode == TrackingOriginModeFlags.Unknown
                || (mode & requestedOriginMode) != 0;
            return knownMode && initialOriginReady;
        }

        private void TrackingOriginUpdated(XRInputSubsystem subsystem)
        {
            // Drop gesture baselines when the provider recentres its coordinates.
            // Do not move the model merely because of brief head tracking loss.
            ResetInput();
        }

        private void ReleaseSubsystem()
        {
            if (activeSubsystem != null) activeSubsystem.trackingOriginUpdated -= TrackingOriginUpdated;
            activeSubsystem = null;
            actualOriginMode = TrackingOriginModeFlags.Unknown;
            requestedOriginMode = TrackingOriginModeFlags.Unknown;
            useFloorOrigin = false;
        }

        private void BeforeRender()
        {
            if (!IsXrActive || !HasFocus || headCamera == null || trackingOrigin == null) return;
            // Late head-pose update; button edges and app actions stay in Update.
            if (RefreshTrackingOrigin()) UpdateHeadPose();
        }

        private bool UpdateHeadPose()
        {
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (!TryPose(head, out var position, out var rotation)) return false;
            if (!useFloorOrigin) position.y += fallbackEyeHeightMetres;
            headCamera.transform.localPosition = position;
            headCamera.transform.localRotation = rotation;
            return true;
        }

        private static bool TryPose(InputDevice device, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            if (!device.isValid) return false;
            if (!device.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) || !tracked) return false;
            return device.TryGetFeatureValue(CommonUsages.devicePosition, out position)
                && device.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation)
                && Finite(position) && Finite(rotation);
        }

        private ControllerSample Sample(XRNode node)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (!TryPose(device, out var position, out var rotation)) return default;
            if (!useFloorOrigin) position.y += fallbackEyeHeightMetres;
            device.TryGetFeatureValue(CommonUsages.triggerButton, out bool trigger);
            device.TryGetFeatureValue(CommonUsages.gripButton, out bool grip);
            device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primary);
            device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis);
            return new ControllerSample
            {
                Tracked = true, Trigger = trigger, Grip = grip, Primary = primary, Axis = axis,
                Position = trackingOrigin.TransformPoint(position), Rotation = trackingOrigin.rotation * rotation
            };
        }

        private void UpdateRay(ControllerSample sample, LineRenderer line, bool select)
        {
            if (line != null) line.enabled = sample.Tracked;
            if (!sample.Tracked) return;
            var ray = new Ray(sample.Position, sample.Rotation * Vector3.forward);
            var end = ray.GetPoint(rayLengthMetres);
            bool hit = Physics.Raycast(ray, out var result, rayLengthMetres, ~0, QueryTriggerInteraction.Collide);
            if (hit) end = result.point;
            if (line != null) { line.SetPosition(0, ray.origin); line.SetPosition(1, end); }
            if (select && hit) Activate(result.collider);
        }

        private static bool Neutral(ControllerSample sample) => !sample.Trigger && !sample.Grip
            && !sample.Primary && sample.Axis.sqrMagnitude < .04f;

        private static void ArmController(ref ControllerSample sample, ref bool wasTracked, ref bool armed)
        {
            if (!sample.Tracked) { wasTracked = false; armed = false; return; }
            if (!wasTracked) { wasTracked = true; armed = false; }
            if (armed) return;
            if (Neutral(sample)) armed = true;
            // Retain the pose for the visible ray while suppressing held actions.
            sample.Trigger = sample.Grip = sample.Primary = false;
            sample.Axis = Vector2.zero;
        }

        private static void Activate(Collider collider)
        {
            var action = collider.GetComponentInParent<ExperienceActionTarget>();
            if (action != null) { action.Activate(); return; }
            var location = collider.GetComponentInParent<ExperienceSelectionTarget>();
            if (location != null) location.Select();
        }

        private void ApplyAxis(ControllerSample sample, Vector2 previous)
        {
            if (!sample.Tracked) return;
            if (Mathf.Abs(sample.Axis.x) > .7f && Mathf.Abs(previous.x) <= .7f)
                experience.RotateTabletop(Mathf.Sign(sample.Axis.x) * 30f);
            if (Mathf.Abs(sample.Axis.y) > .7f && Mathf.Abs(previous.y) <= .7f)
                experience.SetTabletopWidth(experience.TabletopWidthMetres + Mathf.Sign(sample.Axis.y) * .15f);
        }

        private void ApplyGrabs(ControllerSample left, ControllerSample right)
        {
            bool leftGrab = left.Tracked && left.Grip;
            bool rightGrab = right.Tracked && right.Grip;
            int count = (leftGrab ? 1 : 0) + (rightGrab ? 1 : 0);
            if (count == 0) { previousGrabCount = 0; return; }
            Vector3 position;
            float yaw;
            float distance = 0;
            if (count == 2)
            {
                position = (left.Position + right.Position) * .5f;
                var separation = right.Position - left.Position;
                distance = separation.magnitude;
                yaw = Mathf.Atan2(separation.x, separation.z) * Mathf.Rad2Deg;
            }
            else
            {
                var controller = leftGrab ? left : right;
                position = controller.Position;
                var forward = controller.Rotation * Vector3.forward;
                yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            }
            bool baselineChanged = previousGrabCount != count || (count == 1 && previousSingleLeft != leftGrab);
            if (baselineChanged)
            {
                scaleStartDistance = distance;
                scaleStartWidth = experience.TabletopWidthMetres;
            }
            else
            {
                experience.MoveTabletop(position - previousGripPosition);
                experience.RotateTabletop(Mathf.DeltaAngle(previousGripYaw, yaw));
                if (count == 2)
                {
                    if (scaleStartDistance >= .08f && distance >= .08f)
                        experience.SetTabletopWidth(scaleStartWidth * distance / scaleStartDistance);
                    else { scaleStartDistance = distance; scaleStartWidth = experience.TabletopWidthMetres; }
                }
            }
            previousGripPosition = position;
            previousGripYaw = yaw;
            previousGrabCount = count;
            previousSingleLeft = leftGrab;
        }

        private void UpdateDesktop()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.Input.GetKeyDown(KeyCode.Space)) experience.ReturnToTabletop();
            if (experience.IsInteractionPaused) return;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Q)) experience.RotateTabletop(-30);
            if (UnityEngine.Input.GetKeyDown(KeyCode.E)) experience.RotateTabletop(30);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Equals) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadPlus))
                experience.SetTabletopWidth(experience.TabletopWidthMetres + .15f);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Minus) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadMinus))
                experience.SetTabletopWidth(experience.TabletopWidthMetres - .15f);
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                var ray = headCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, rayLengthMetres, ~0, QueryTriggerInteraction.Collide)) Activate(hit.collider);
            }
            if (experience.WorldRoot == null) return;
            // Right-drag moves the model on its horizontal plane, not the player.
            var plane = new Plane(Vector3.up, experience.WorldRoot.position);
            var screenRay = headCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            if (UnityEngine.Input.GetMouseButton(1) && plane.Raycast(screenRay, out var distance))
            {
                var point = screenRay.GetPoint(distance);
                if (desktopDragging) experience.MoveTabletop(point - previousDesktopPoint);
                previousDesktopPoint = point;
                desktopDragging = true;
            }
            else desktopDragging = false;
#endif
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            focused = hasFocus;
            if (!HasFocus)
            {
                HideRays(); ResetInput(); IsTracking = false;
                if (experience != null) experience.SetTracking(false);
            }
        }

        private void OnApplicationPause(bool isPaused)
        {
            paused = isPaused;
            if (!HasFocus)
            {
                HideRays(); ResetInput(); IsTracking = false;
                if (experience != null) experience.SetTracking(false);
            }
        }

        private void ResetInput()
        {
            previousGrabCount = 0;
            leftArmed = rightArmed = false;
            leftWasTracked = rightWasTracked = false;
            leftTrigger = rightTrigger = leftPrimary = rightPrimary = false;
            leftAxis = rightAxis = Vector2.zero;
            desktopDragging = false;
        }

        private void HideRays()
        {
            if (leftRay != null) leftRay.enabled = false;
            if (rightRay != null) rightRay.enabled = false;
        }

        private void OnDisable()
        {
            Application.onBeforeRender -= BeforeRender;
            ReleaseSubsystem();
            HideRays();
            ResetInput();
            IsTracking = false;
            if (experience != null) experience.SetTracking(false);
        }

        private void OnDestroy()
        {
            if (leftRay != null) Destroy(leftRay.gameObject);
            if (rightRay != null) Destroy(rightRay.gameObject);
            if (ownedRayMaterial != null) Destroy(ownedRayMaterial);
        }

        private static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
        private static bool Finite(Quaternion value) => Finite(value.x) && Finite(value.y) && Finite(value.z) && Finite(value.w);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private struct ControllerSample
        {
            public bool Tracked;
            public bool Trigger;
            public bool Grip;
            public bool Primary;
            public Vector2 Axis;
            public Vector3 Position;
            public Quaternion Rotation;
        }
    }
}
