# Manual QA — unexecuted gates

No Unity editor, Quest 3/3S, matching visual-source set, or reviewer familiar with
Chelsea was available. All procedures below are **unrun**, not passes.

1. Resolve the pinned Unity packages. Open the project, execute the Editor import
   menu on diagnostic fixture output, check labels and metre orientation. Repeat
   with reviewed real-source output. Confirm generated meshes/materials and no
   missing references; inspect day/overcast/dusk only once those features exist.
2. Before M2: source-match the first-area road graph and footprints. Author
   representative facades and Saatchi exterior from reusable references. Review
   matched viewpoints, human scale, pavement alignment, clipping and LOD behavior.
3. Before M3: implement/test MR and viewing-room placement, controller/hand parity,
   two-hand scale, one-handed alternatives, enlarged selection and finder, safe
   public-pavement teleport, snap turn, fade and persistent return. No smooth motion.
4. Test seated and standing, left/right hand, text scale, audio/haptics, focus pause,
   tracking loss and recovery. Ensure return is always reachable.
5. Test saved viewpoints/settings and visited locations across relaunch, fully
   offline, with no analytics or keys.
6. Ask a reviewer familiar with Chelsea to identify five priority places in
   unlabelled views. Record actual answers and comfort feedback. Do not count an
   agent's assessment or synthetic geometry toward this gate.
7. Run the on-device captures described in PERF_REPORT.md before claiming release.

Do not expand geography until the first-area M4 acceptance gate is recorded.
