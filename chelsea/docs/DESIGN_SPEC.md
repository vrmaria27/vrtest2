HOLOPOLIS: CHELSEA, LONDON
Implementation specification — Chelsea replica only

1. MISSION
You are the lead engineer, technical artist and XR interaction designer for a geographically grounded recreation of Chelsea, London, for Meta Quest 3, with Quest 3S compatibility.

Build a living miniature of a real neighbourhood that the player can place on a table, inspect, and enter at street level. The central experience is recognising Chelsea: its street network, riverfront, architecture, gardens, landmarks, proportions and atmosphere.

Pitch: hold Chelsea in your hands, then step into its streets.

This document replaces the original fictional city-building specification. For this release, replication and exploration are the entire scope. Do not implement zoning, city growth, taxation, coal plants, resource management, disasters, population tiers, Charter Cards, a campaign or an invented endgame landmark. Movement, lighting and ambient street life support the replica; they are not a city-management simulation.

Priorities, in order: recognisable geography; convincing street-level scale and architecture; comfortable interaction; sustained headset performance; a small, complete release. “Realistic” means evidence-based layout and appearance, not a promise of photogrammetric or survey-grade accuracy.

2. DESIGN PILLARS
• A specific place. Every major street, junction and landmark belongs to the selected Chelsea area. Generic London scenery is insufficient.
• Two useful scales. Tabletop mode reveals the neighbourhood; street mode reveals its character. Both are core requirements.
• Evidence before invention. Build from reusable geographic data and rights-cleared visual references. Record estimates and gaps.
• Quiet life. Pedestrians, traffic, trees, water and sound make the place feel inhabited without overwhelming exploration.
• Quality before coverage. Complete one coherent area before extending the map. Do not replace accuracy with more streets.

3. GEOGRAPHIC SCOPE
One map only: a deliberately bounded Chelsea study area. This is a product boundary, not a claim about the definitive administrative or historic boundary of Chelsea.

Proposed release coverage:
• East: Sloane Square and the Royal Hospital/Chelsea Bridge Road side of the neighbourhood.
• North: the King's Road corridor, including buildings fronting both sides and short side-street returns.
• West: Beaufort Street and the Chelsea approach to Battersea Bridge.
• South: Cheyne Walk, Chelsea Embankment and the north bank of the Thames, with enough river surface to establish the setting.

M0 must turn these descriptive limits into one reviewed GeoJSON polygon. Include the required landmarks below; resolve edge cases explicitly. Do not use a rectangular crop as permission to add unrelated neighbourhoods.

Battersea, Knightsbridge, South Kensington, Fulham and Chelsea Harbour are not explorable areas in this release. Bridge spans and distant opposite-bank silhouettes may appear as simplified context, but must not add southern-bank streets, buildings with interaction, or missions. Chelsea Harbour is excluded to keep this first scope compact; do not silently extend westwards to it.

First playable area: Duke of York Square and its immediate King's Road frontage. Select a contiguous approximately 300–500 m walking circuit after reviewing source coverage. Complete its geometry, representative façades, interaction and performance before expanding.

Expand within the same approved polygon in this order:
1. Duke of York Square and the Sloane Square connection.
2. Royal Hospital Road and the Royal Hospital exterior setting.
3. Chelsea Physic Garden perimeter, Cheyne Walk and Albert Bridge's Chelsea approach.
4. Remaining King's Road frontage and western connecting streets.

Do not present incomplete areas as finished. Maintain a coverage map showing complete, approximate and unavailable areas.

4. HARD SCOPE FENCE
Included:
• One real-world Chelsea map, with one geographic coordinate system and a frozen source-data snapshot.
• Tabletop MR; a simple full-VR viewing room; street-level exploration.
• Actual road curves, junctions, pavements, river edge, building footprints and public-space layouts where source data supports them.
• Original or properly licensed exterior building models, with distinct landmark models and reusable local architecture kits.
• Eight priority landmark/settings: Sloane Square, Duke of York Square, Saatchi Gallery exterior, Royal Hospital Chelsea exterior, Chelsea Physic Garden exterior/perimeter, Chelsea Old Church exterior, Albert Bridge, Battersea Bridge.
• King's Road, Royal Hospital Road, Cheyne Walk and Chelsea Embankment as priority connecting corridors; verify exact connectivity and names during research.
• Ambient left-driving traffic, pedestrians, limited river boats and birds.
• Day, overcast and dusk lighting; a fixed season to keep vegetation and references coherent.
• Landmark information cards, a small location finder, optional guided walk, saved viewpoints and comfort settings.

Excluded:
• Other cities, fictional maps or procedural street layouts.
• Building interiors, private-home access, invented interior views or named residents.
• Editable roads, demolition, zoning, economic simulation and disasters.
• Multiplayer, accounts, cloud saves, ads, purchases, live navigation, live traffic and streamed map tiles.
• A full transport network, functional Underground stations, exact bus timetables or a simulated London population.
• Photorealistic reconstruction of every façade, changing seasons and dynamic weather systems.

Cut order: guided-walk narration or extra text; optional decorative props; distant contextual scenery; nonessential ambient variety; geographic coverage at the western edge. Record any boundary reduction and relabel the release. Never cut the recognisable first area, reference provenance, street-level mode, controller parity or honest performance verification.

5. REFERENCE RESEARCH AND GOOGLE MAPS REVIEW
Before final modelling, conduct a documented reference review. Google Maps and Street View are desired supplementary viewing/checking tools where accessible and permitted; they are not the default asset or geometry source.

For every priority location:
1. Record a reference URL, access date, visible imagery date when available, and the intended view or route.
2. Inspect street character, junction relationships, relative massing, rooflines, entrances, paving, trees, street furniture and landmark sightlines.
3. Identify discrepancies between references taken at different times. Choose and document a coherent snapshot; do not claim live accuracy.
4. Obtain equivalent rights-cleared evidence for any detail used to create the shipped model: reusable geodata, own photographs, licensed photographs or other sources permitting that use.
5. Record an implementation checklist and confidence rating: verified, estimated or unavailable.
6. Compare rendered views against the approved references from matching positions and directions; log differences before polishing materials.

Google content is not automatically licensed for reconstruction or redistribution. Do not scrape Google Maps, extract its meshes, trace a geometry dataset from its imagery, download Street View panoramas, bake screenshots into materials, or bundle Google imagery. A proposed Google API/content integration requires a separately documented permitted-use and technical assessment; it is not part of this offline release. Check current terms for the exact intended use. If visual comparison itself cannot be established as permitted for the workflow, use rights-cleared alternatives.

Use open or appropriately licensed geographic sources, such as an OpenStreetMap extract, for the underlying road/building dataset. Record attribution and applicable database obligations, including distribution obligations for derivative databases where required. Publicly accessible photographs and council documents are not automatically reusable textures or geometry sources.

Useful supplementary research: RBKC conservation-area appraisals, official landmark pages, and the user's own street photographs. Council appraisals are particularly useful for understanding local architectural character; check reuse terms separately.

Do not use Google user reviews as architectural evidence. If consulted for experiential context, treat them as subjective, dated observations; do not reproduce review text or reviewer identities.

Research outputs:
• docs/REFERENCE_REGISTER.csv: location ID, source URL, provider, observation date, imagery date, permitted use/license, geographic coverage, confidence and notes.
• docs/GEOGRAPHY.md: study boundary, projection, units, origin, source snapshot and limitations.
• docs/ACCURACY_REVIEW.md: checkpoint, expected feature, observed result, discrepancy, confidence and status.
• docs/ATTRIBUTION.md and an in-app attribution view.

If Google Maps/Street View or source downloads are unavailable, say so explicitly. Continue with accessible sources and list the missing review. Never claim a Google Maps review occurred because a search result or link was found.

6. WORLD DATA AND ACCURACY
Remove the original 96×96 grid and 8 m cells. They would force Chelsea into an artificial street pattern.

Use georeferenced vector roads, building footprints and land-use polygons. Store source coordinates and convert them into a documented local metric coordinate system. World units are metres; tabletop scale is only a viewing transform. Preserve north orientation and a stable origin.

Represent roads as connected curves/segments, buildings as footprints with height and roof metadata, and pavements as explicit walkable geometry. Separate geographic truth from presentation and ambient-agent routing. Use spatial chunks for loading and rendering without changing the underlying layout.

Source priorities:
• Road connectivity and footprints: licensed geographic dataset.
• Heights and storeys: explicit source values where available; otherwise documented estimates from permitted references.
• Landmark silhouette and façade composition: individually researched models.
• Generic infill: parameterised kits matching the relevant block, with estimated details labelled internally.

Proposed acceptance targets, to validate against source quality in M0:
• All priority streets connect in the correct order; no missing critical junctions or fabricated shortcuts.
• At least 20 distributed checkpoints across the release area, compared with the chosen geodata baseline; target horizontal discrepancy no greater than 3 m where the baseline supports it. This measures fidelity to the source, not guaranteed ground truth.
• Buildings with reliable height evidence: target within 10% or 1 m, whichever is greater. Estimated buildings carry no accuracy claim.
• Priority landmarks: correct location, orientation, major massing and defining silhouette, confirmed by visual review.
• No systematic street widening or footprint snapping for gameplay convenience. Use interaction aids instead.

Record source completeness and uncertainty. Never infer that a building has been measured merely because it has a precise numeric height in the model.

7. VISUAL DIRECTION
Create a convincing, restrained Chelsea miniature with enough architectural detail to survive a street-level visit. Preserve local colours and proportions; do not colour buildings by gameplay zones.

Architecture kit: London brick and stucco terraces, mansion-block forms where observed, shopfronts, sash-window rhythms, parapets, pitched roofs, chimney stacks, basement lightwells, entrance steps, iron railings and garden walls. Use each only where supported by the selected street references.

Landmarks need bespoke mesh silhouettes. A generic block with a label is not a finished landmark. The bridge structures must be recognisable from both riverfront and tabletop views.

Prioritise coherent façade rhythm, rooflines, pavements and tree placement over tiny decorative details. Use readable street names at close range. Business names and shopfront identities must be sourced, dated and appropriately cleared; otherwise use unobtrusive generic shopfront treatment and record the approximation.

Use London-appropriate road markings, crossings, kerbs and left-side traffic. Trees should reflect the chosen season. Water should resemble the Thames rather than tropical blue water. Avoid exaggerated saturation, cartoon construction animation and invented skyscrapers.

For each building family, generate three LODs. Preserve silhouettes across transitions. Use reusable materials and trim atlases, with bespoke exceptions for important landmarks. Licensed meshes/textures are allowed when they materially improve fidelity; the original code-only-art restriction is removed. Commit provenance and generation/import settings.

8. PLAYER JOURNEY AND CONTROLS
First 90 seconds:
• Launch into mode selection and comfortable placement.
• Reveal the already-built Chelsea model, with restrained location labels and an orientation marker.
• Invite the player to rotate and enlarge it.
• Highlight Duke of York Square as the first street-level destination.
• Offer a comfortable fade into a verified walkable viewpoint, then a short optional route.

Hands and controllers have full parity. Two-hand grab moves, rotates and scales the tabletop world. Pinch/trigger selects a location. Grip grabs; a simple wrist menu provides Locations, View, Lighting, Route, Settings and Return.

Default tabletop width: approximately 1.2 m, adjustable from 0.3–4 m. Do not promise that every small building is selectable at the default scale. Provide enlarged hit areas, a location finder and zoom-to-selection.

Street mode: teleport to verified public walkable surfaces and snap-turn. Use a fade between tabletop and human scale; no automatic zoom-flight through buildings. No smooth artificial locomotion in this release. A persistent accessible action returns to the tabletop. Restrict teleport through walls, into traffic lanes, private gardens and inaccessible courtyards. Grounds are walkable only where access and geometry have been verified; otherwise show the exterior.

Accessibility: seated/standing, left/right hand, one-handed controls, text scale, colour-independent cues, adjustable audio and haptics, focus-loss pause and tracking-loss recovery. Never use visual realism as a reason to make controls difficult.

9. AMBIENT LIFE AND EXPLORATION
Ambient traffic is illustrative, not live or a calibrated transport model. Follow the verified road graph, left-side lanes, junction rules and known restrictions included in the dataset. Model safe-looking crossings and avoid vehicles clipping through kerbs or pedestrians.

Use bounded pools with initial maximum active counts of 150 vehicles, 250 pedestrians and 6 boats; tune down after device profiling. Different viewpoints may use different densities. Freeze or simplify distant agents.

Provide a short optional discovery walk linking verified public viewpoints. Cards contain concise sourced facts, with dates where facts may change. Do not invent local history, opening hours or public access rights. A missed source means omit the claim or mark it for verification.

Local state stores settings, saved viewpoints and visited locations. No resident profiles, real-person tracking or external analytics.

10. TECH STACK AND REPOSITORY
Unity 6 LTS, choosing a version currently supported by the selected Meta XR packages; verify and pin versions at implementation time. URP Forward, Vulkan, IL2CPP, ARM64 and OpenXR. Quest 3 is primary; profile Quest 3S separately before claiming compatibility/performance.

Use Meta-compatible input, passthrough, anchors and spatial audio adapters. Avoid guessing SDK APIs. Isolate all SDK calls behind interfaces and document unverified integration points.

Keep geodata ingestion, coordinate conversion, validation and ambient routing in testable C# libraries. Separate these from Unity presentation. Use an Editor build pipeline for geodata conversion, chunk generation, mesh generation/import and LOD checks. A small bootstrap scene is sufficient; do not force bespoke landmark authoring into unreadable procedural code.

Suggested repository:
chelsea/
  AGENTS.md  PLAN.md  README.md
  docs/  DESIGN_SPEC.md  GEOGRAPHY.md  REFERENCE_REGISTER.csv
         ACCURACY_REVIEW.md  ATTRIBUTION.md  TECH_DECISIONS.md
         DECISIONS.md  PERF_REPORT.md  MANUAL_QA.md  VERIFICATION.md
  data/  boundary/  source_manifest/  processed/  landmarks/  routes/
  unity/Assets/Chelsea/
         Core/  World/  Render/  XR/  UI/  Audio/  Editor/  Tests/
  dotnet/  Chelsea.Core/  Chelsea.Core.Tests/
  tools/  import/  validation/  build/

Research/import tools may access approved sources during development. The installed experience must run offline after installation. Do not ship API keys. Target an APK below 500 MB; record any justified revision after measuring the first area. Version-pin source extracts so builds do not silently change with live map data.

11. RENDERING AND AUDIO
Use static chunk batching, instanced repeated props, appropriate occlusion, distance LOD and a near-detail ring for street mode. No per-building Update loops. Pool agents and effects.

Initial rendering configuration: single-pass stereo, 4× MSAA if affordable, one shadow-casting directional light, baked/vertex ambient shading, simple glass reflections and inexpensive water. Avoid full-screen effects and high-overdraw foliage. Verify multiview support in custom shaders.

Audio should convey a real neighbourhood: restrained traffic, footsteps, leaves, birds and river ambience. Use rights-cleared recordings or original synthesis. Scale attenuation between tabletop and street views. No mandatory music, recorded private conversations or unlicensed recordings.

12. PERFORMANCE BUDGETS
These are targets requiring device evidence, not guarantees:
• Quest 3 default: sustained 90 Hz; an explicitly selected 72 Hz quality mode is acceptable if documented. Do not call sustained 72 Hz “locked 90”.
• At 90 Hz: CPU main-thread and GPU frame times each target under 10 ms at p99, with headroom against the 11.1 ms frame interval.
• Visible triangles: approximately 1.0 M typical, 1.3 M peak.
• Draw calls: no more than 300 initially.
• Steady-state managed allocation: 0 B/frame in the application hot path; investigate SDK allocations separately.
• Total memory: target below 2.5 GB, checked on both target devices.
• Cold start to interaction: target below 10 s; scene transitions must not leave the player in a frozen image.
• Thermal test: at least 30 minutes on-device, covering both modes.

Profile the busiest King's Road view, a detailed landmark, the riverfront, full-map tabletop view and repeated mode transitions. Include p50/p95/p99 frame times, memory, active agents, device model, build version and test duration. Reduce decorative density and distant detail before compromising landmark shape or street layout. If targets fail, reduce coverage/detail and retest; do not claim an adaptive-quality controller guarantees performance.

13. MILESTONES
M0 — Scope and evidence. Finalise the boundary polygon, source/licence manifest, reference checklist, source availability and first-area route. Pin stack versions where verifiable. Exit: implementable source plan; missing evidence explicitly listed; no fictional replacement map.

M1 — Geographic foundation. Implement import, coordinate conversion, street/building geometry, chunking and validation for the first area. Exit: tested transform and connectivity; first-area source comparison completed; geometry displayed in Unity where available.

M2 — Recognisable first area. Complete the Duke of York Square/King's Road circuit, representative façades, relevant landmark exterior, paving, trees and lighting. Exit: matched-view accuracy review and representative headset performance capture. If no headset is available, mark that gate blocked rather than passed.

M3 — MR and street exploration. Placement, scale, selection, controller/hand parity, teleport, return, accessibility and tracking recovery. Exit: a new user can place the model and enter the first area in two minutes, with no inaccessible return path.

M4 — First-area polish and review. Ambient life, audio, labels and settings; user recognition/comfort review. Exit: the first area is recognisable and enjoyable to explore. Prepare a reviewable build before asking for feedback. Do not expand geography until this gate is accepted.

M5 — Chelsea expansion. Extend only within the approved polygon in the order in Section 3. Exit: priority landmarks and routes complete; distributed accuracy checkpoints reviewed; incomplete areas identified.

M6 — Optimisation and final QA. LOD, memory, thermal, accessibility, licensing and offline checks. Exit: all release gates measured or explicitly blocked; no unresolved placeholder landmark models.

M7 — Release. Deliver the APK when build tools are available, installation instructions, source/import pipeline, attribution, coverage/limitations and final verification report. Never fabricate a build or device result.

14. QUALITY AND VERIFICATION GATES
Geography: all priority places correctly positioned; main junctions connected; no invented roads or unexplained missing landmarks; coordinate and boundary tests pass.

Visuals: no floating façades, clipped pavements, z-fighting, obvious LOD popping, broken materials or inconsistent human scale. Review matched viewpoints in day, overcast and dusk.

Recognition: ask a reviewer familiar with Chelsea to identify at least five priority locations from unlabelled views. Record actual results; do not substitute the agent's opinion for user review.

Comfort: seated and one-handed use, controller parity, reliable tracking recovery, safe teleport targets and immediate return to tabletop.

Tests: coordinate fixtures, boundary clipping, source-ID stability, road connectivity, invalid geometry handling, deterministic asset generation, save/load and mesh budgets. Unity smoke test: boot, place model, select location, enter street mode, teleport, return and reload settings. Device QA: long session, focus loss, tracking loss, memory and thermal profiling.

Evidence: every completed feature is Verified with method/result, or Unverified with the missing tool, source or device named. “Approximate” describes fidelity; it does not mean tested.

15. OPERATING RULES AND REPORTING
Make routine reversible implementation decisions autonomously and record context, decision and consequence in docs/DECISIONS.md. Do not expand scope to compensate for missing references. Do not claim source access, Google review, builds or headset tests that did not happen.

Keep commits small. Exclude Unity Library/, Temp/, logs and generated builds from source control; use appropriate binary asset storage. Maintain source attribution beside imported assets and data. No unknown-license assets in the release.

At each milestone update PLAN.md and report, in under 300 words: Done; Verified; Unverified; Accuracy gaps; Decisions; Next steps. If Unity or device access is unavailable, continue independent core/data work while leaving the corresponding release gates open.

16. KICKOFF INSTRUCTION FOR THE IMPLEMENTING AGENT
Read this complete brief. Inspect the existing repository before changing it. Create/update AGENTS.md, PLAN.md and docs/DESIGN_SPEC.md using this scope.

Begin with M0 and the first-area portion of M1 only. Research available sources, define the Chelsea boundary, establish the Google Maps/Street View review status, document reusable data and asset permissions, and build the geographic import foundation. Do not build an entire generic city first. Do not start economy, zoning or disaster systems.

Stop at that reviewable milestone with the boundary, sources, initial geometry or tests, and an honest verification report. Subsequent work should follow the milestone gates.

REFERENCE STARTING POINTS
Checked for this specification on 5 November 2026. These links are a starting point for implementation research; this specification does not certify that a street-by-street Google Maps review has already occurred.
• Google Maps terms: https://www.google.com/help/terms_maps/
• Google geographic-content guidelines: https://about.google/brand-resource-center/products-and-services/geo-guidelines/
• OpenStreetMap copyright and licence: https://www.openstreetmap.org/copyright
• OSM attribution guidelines: https://osmfoundation.org/wiki/Attribution_Guidelines
• RBKC conservation areas: https://www.rbkc.gov.uk/planning-and-building-control/heritage-and-conservation/conservation-areas
• RBKC conservation-area maps: https://www.rbkc.gov.uk/planning-and-building-control/heritage-and-conservation/conservation-area-maps
