# Developer launch runtime — source implementation

`ChelseaExperience` boots the explicit bundled geographic TextAsset in a native
Unity scene. It retains the TextAsset reference, validates schema/origin/provenance
and mesh structure, builds diagnostic geometry, scales the viewing transform to a
1.2 m tabletop by default (0.3–4 m range), and provides world-space controls.

Controls expose return/recenter, selection, tabletop translation/rotation/scale,
Day/Overcast/Dusk lighting and source information. A small versioned offline file
stores width, yaw and lighting; malformed values fall back to defaults. Focus and
tracking state suspend interactions. No per-feature Update methods exist.

All BoxColliders are selection aids. They do not certify public access or become
teleport destinations. There is no street mode or route: the menu explicitly says
it is blocked. Synthetic fixtures are persistently labelled NOT CHELSEA. Real OSM
source is diagnostic, not a completed replica. No facade/landmark models are
invented. Source tags and estimated heights are not measured height evidence.

Controller/XR pose adapters are a separate assembly. No Meta SDK calls,
passthrough, anchors, hands, or controller/hand parity are claimed. This source
has not been compiled or rendered in Unity, installed as an APK, or tested on a
Quest device. Actual package resolution, TextMesh readability, focus/tracking
recovery, interaction comfort, settings persistence and performance need the
pinned Unity Editor and hardware.
