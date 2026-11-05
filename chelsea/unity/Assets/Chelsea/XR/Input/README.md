# Controller developer adapter — source only

`ChelseaControllerInput` is a controller-only developer foundation. It uses
Unity's built-in XR Input APIs, with no speculative Meta SDK calls. It is **not**
proof of working Quest input, MR, hand parity, comfort or frame timing: no Unity
Editor, Android build or headset was available to execute it.

The Editor launch generator configures this component with `ChelseaExperience`,
a bare camera under a tracking-origin Transform, and an explicitly referenced
URP material for rays. Do not add a second head-pose driver to this camera. The
adapter updates head pose in Update and before render. It retries input-subsystem
discovery if the loader is still starting and observes the actual origin mode
centrally. An accepted floor-origin request must be observed before first-session
placement; it never adds a device eye offset to floor-relative poses. The model
and menu are recentered once after the first valid session pose, not on a brief
head-tracking loss. Device-origin fallback adds a 1.65 m eye offset. This fallback
is a developer default, not validated seated calibration. Actual floor height,
seated setup and tracking recovery require a headset test.

## Controller mapping (both hands)

- Trigger: ray-select a menu action or source feature.
- Grip one controller: translate and rotate the tabletop model.
- Grip both controllers: move the midpoint, rotate and scale the model. The
  application clamps tabletop width to 0.3–4 m.
- Thumbstick left/right: rotate the model by 30 degrees per deflection.
- Thumbstick up/down: increase/decrease model width by 0.15 m per deflection.
- Primary button (A/X on typical Touch controllers): return/recenter the tabletop
  and its persistent menu. This does not enter street mode.

A line ray is drawn for each tracked controller. Loss of head tracking, focus or
pause freezes interactions. Each controller rearms independently after tracking
loss or reappearance and must return to neutral, so a held trigger/grip is not
replayed as a selection or grab. The other controller remains available for
one-handed operation. Controller loss ends that controller's grip; grip count
changes rebaseline the model transform instead of jumping. A provider origin
update also clears gesture baselines. No player locomotion is implemented.

## Desktop inspection mapping

The recommended native Android configuration uses **Input System (New)**. The
old-input desktop fallback is compiled/executed only with
`ENABLE_LEGACY_INPUT_MANAGER`. Under New-only input it stays disabled and reports
that desktop controls are unavailable, rather than calling an unsupported API.
For optional **Editor-only inspection**, enable Both and restore New before the
Android preflight/build. With XR inactive on a non-mobile platform, the enabled
legacy fallback maps:

- Left mouse click selects a menu action or feature.
- Right-drag moves the model on its current horizontal plane.
- Q/E rotates the model by 30 degrees.
- +/− changes tabletop width.
- Space returns/recenters the model and menu.

Android without an active XR session pauses interaction instead of silently
presenting a desktop simulation as working headset input.

## Verified API evidence

API signatures were inspected in official UnityCsReference commit
`bf6028387a4fc2ea6d41aa7d8998e774655aedf6` on 5 November 2026:

- `Modules/XR/ScriptBindings/XRInput.bindings.cs`:
  `GetDeviceAtXRNode`, `CommonUsages.isTracked`, poses, trigger/grip/primary buttons
  and primary axis. SHA-256:
  `41ccefb3a19e2bc95a0c7d8b3924d432af7fb02268e5f6da98d96fd5788940c2`.
- `Modules/XR/Subsystems/Input/XRInputSubsystem.bindings.cs`:
  `TrySetTrackingOriginMode` and `GetTrackingOriginMode`. SHA-256:
  `cc5a29da753f195ace8399cfd86896e937589137a4bf9f5a92610287160294dc`.

Those source signatures support implementation choices; they are not a compiled
Unity result. OpenXR interaction profiles, actual ray orientation, shader
inclusion, tracking latency, device compatibility and all controls need the
pinned editor/device smoke test. Hand tracking, passthrough, anchors, haptics and
street teleport remain unimplemented, and `SupportsPassthrough` returns false.
