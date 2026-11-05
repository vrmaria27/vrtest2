# Launch path: Mac + Meta Quest 3 / 3S

## Recommendation and present state

Use a **native Quest developer alpha**, followed by the reviewed first-area game.
The user confirmed a Mac and Quest on 5 November 2026. A developer diagnostic can
validate the toolchain and controller foundation, but is not the Chelsea game.
No APK has been produced, no Unity compilation has run, and no headset has been
connected to this cloud machine. A GitHub Pages HTML page cannot launch a native
Unity APK.

| Option | What it gives you | Tradeoff | Decision |
|---|---|---|---|
| Native first-area Quest alpha | Offline APK; follows the specified Unity/OpenXR route | Requires local Unity/Android tools and headset validation; real content still needed | **Recommended; source/toolchain work started** |
| WebXR browser prototype | Launch from a headset browser link | Different implementation; reduced MR/native parity; real data/art still needed | Available alternative if requested |
| Full native public release | Reviewed Chelsea experience suitable for distribution | All geography, visual, controller/hand, comfort, licensing and thermal gates first | Follow the alpha; not a shortcut |

## What is blocked, and who can unblock it

| Blocker | Actual evidence | Next action |
|---|---|---|
| Real Chelsea source data | OSM API request CONNECT403; running policy still has zero custom domains although the draft contains additions | Review/save/publish the environment settings, then retry the bounded source download. Alternatively provide a rights-cleared OSM XML extract plus provenance through secure file upload. No API key needed. |
| Approved boundary, references, art and safe street access | Boundary/AOI proposed only; zero completed landmark models, no reviewed public walking targets | After source access, review the boundary and first circuit; create rights-cleared representative architecture and matched views. Do not replace them with a generic city. |
| Native game features | New tabletop/controller diagnostic source only; hands, MR/anchors, safe street mode, route, ambient life and full accessibility still incomplete | Implement and test those milestone features using reviewed first-area content. A controller diagnostic does not complete M2/M3. |
| Unity build toolchain | No Unity editor, Android SDK/NDK/ADB or configured licence on the cloud machine | Install the pinned editor and modules locally on the Mac and activate an eligible licence in Unity Hub. Never send licence/password values in chat. |
| Quest/device validation | Cloud cannot access the user's USB headset | Enable Developer Mode, connect by USB-C and accept USB debugging, then use the install script. Record actual results. |
| Release readiness | No APK, visual recognition, comfort, frame-time, memory or thermal results | Complete the manual/device gates after a real build. No store launch claim yet. |

## 1. Get the source and local editor

Download the repository's `main` branch or clone it:

```sh
git clone --branch main https://github.com/vrmaria27/vrtest2.git
cd vrtest2/chelsea
```

Install **Unity 6000.0.66f2**, revision `b20bc5da3050`, using Unity Hub. Select the
editor build matching your Mac (Apple silicon or Intel), and these modules:
**Android Build Support**, **Android SDK & NDK Tools**, and **OpenJDK**. Open the
editor once and activate your eligible Unity licence locally. Unity Hub/Archive
may offer an installation deep link:
`unityhub://6000.0.66f2/b20bc5da3050`.

Open `chelsea/unity` as the project. Let the pinned packages resolve. Do not upgrade
packages silently. The cloud only inspected package source/API evidence; combined
compatibility and compilation are still unverified.

## 2. Configure Android OpenXR once in the Editor

1. Switch the active Build Profile to **Android**.
2. In **Project Settings → XR Plug-in Management → Android**, enable **OpenXR**
   and **Initialize XR on Startup**.
3. In Android OpenXR settings enable **Meta Quest Support** and the **Oculus Touch
   Controller Profile**. Review the target-device UI for your actual Quest model.
   Set **Render Mode: Single Pass Instanced**.
4. Under **Player → Other Settings → Active Input Handling**, select
   **Input System Package (New)** and accept the restart. Desktop legacy keyboard
   controls are optional and require legacy input enabled; do not assume those
   controls work with New-only input.
5. Resolve actual **OpenXR Project Validation** errors in the editor. In the
   Chelsea menu run **Quest alpha → Check Android OpenXR configuration**.
   The source check is narrower than the package's validation UI; passing it is
   not a device result.

The build method sets IL2CPP, ARM64, Vulkan, Linear colour, a dedicated alpha app
identifier, a single scene and a development APK. It preserves source data as
metres; tabletop scale is a viewing transform. Minimum/target Android API,
Quest device selection and installed SDK/package validation must be reviewed
against the resolved editor and current headset firmware, not guessed here.

## 3. Build the explicitly synthetic diagnostic

From `vrtest2/chelsea` on the Mac:

```sh
bash tools/build/build-alpha-macos.sh --diagnostic
```

By default the script uses:
`/Applications/Unity/Hub/Editor/6000.0.66f2/Unity.app/Contents/MacOS/Unity`.
Set `CHELSEA_UNITY_EDITOR` to the correct executable if your Hub location differs.
Close the same project in the interactive Editor before batch building.

Expected outputs **only after a successful local build**:

- `unity/Builds/Chelsea-Diagnostic-Alpha.apk`
- `unity/Builds/Chelsea-Diagnostic-Alpha.apk.build.json` with checksum/build evidence
- `unity/Builds/diagnostic-build-<run-id>.log`

These outputs do not exist in this cloud checkout. The wrapper requires a unique
current-run success marker and build evidence, so an old APK cannot masquerade
as a new successful build. A failed build preserves its
nonzero status and log. No synthetic APK is labelled as a Chelsea release. The
scene carries a persistent **SYNTHETIC FIXTURE — NOT CHELSEA** notice and disables
street exploration because no safe public targets have been reviewed.

The Editor menu also provides **Create diagnostic launch scene** for source
inspection. Generated scenes/materials live under ignored `Assets/Chelsea/Generated`.
There is a bare tracked camera, one light, a simple VR viewing floor, metric source
meshes and a world-space control menu. There are no invented Chelsea streets or
landmark models. There is no passthrough/anchor or hand-tracking adapter.

## 4. Install and open it on Quest

Enable **Developer Mode** for your Quest using Meta's current developer setup
flow/Meta Horizon app. Complete any account/organisation requirements locally.
Connect the Quest to the Mac by a data-capable USB-C cable. Put on the headset and
accept **USB debugging** from that computer.

```sh
bash tools/build/install-quest-macos.sh
```

The script finds ADB in the pinned Unity Android SDK, requires one authorised
device (or explicit `CHELSEA_DEVICE_SERIAL`), installs the APK and sends the
Android launcher intent for `com.holopolis.chelsea.alpha`. It does not guess a
Unity activity class. No SideQuest account, cloud save or analytics is required.

`CHELSEA_ADB` can override the ADB executable path. If multiple Android devices
are connected, set `CHELSEA_DEVICE_SERIAL` to the intended Quest. An ADB install or
launch message alone does not establish correct in-headset behaviour.

## First device smoke check — not executed here

- Correct persistent synthetic notice, binocular rendering and readable text.
- Head tracking stable and floor/eye height correct for seated/standing use.
- Either controller ray selects a feature/menu; A/X returns/recentres.
- One grip moves/rotates the model; both grips scale within 0.3–4 m.
- Thumbstick steps rotate/scale the model, not the player's body.
- Lighting controls work, settings survive restart, and return remains accessible.
- Focus/tracking loss pauses interaction; held controls do not cause recovery jumps.
- No road is treated as a pavement and no teleport/street control is offered.

Stop if tracking, scale, stereo UI or return misbehaves. Save the build log and
record what happened. Those are implementation defects to fix, not evidence to
mark passed. Device profiling and full first-area visual/comfort review follow.

## After the diagnostic

The next game milestone is reviewed real Duke of York Square geometry and art,
then actual safe street-level exploration and controller/hand parity. Do not
expand the geography or submit a public release based on this diagnostic. See
PLAN.md, MANUAL_QA.md and PERF_REPORT.md for remaining gates.
