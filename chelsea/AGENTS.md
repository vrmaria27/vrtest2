# Chelsea implementation rules

Read docs/DESIGN_SPEC.md before implementation. Work in this existing checkout;
each cloud task is isolated. Do not create a worktree unless explicitly requested.

The user subsequently requested a native launch path on Mac + Quest. M0/M1
remain open; native toolchain/controller diagnostics are now authorised alongside
that foundation. A diagnostic must not be described as a completed game.
First-area game scope and geographic review gates still apply. No economy, zoning, fictional city, interiors,
or expansion before the specified review gates. Source downloads, boundary
approval, matched visual reviews, Unity builds and device tests are separate gates.
Never call synthetic fixtures a Chelsea replica or mark a gate passed without evidence.

Use metres, WGS84 source coordinates and the documented stable origin. Preserve
source IDs, provenance, uncertainty and raw-data hashes. Do not infer at-grade road
junctions from intersecting coordinates. Do not use estimated roads as teleport
surfaces. No Google imagery/mesh extraction, unknown-license assets or API keys.

Use /workspace/.tools/dotnet/dotnet when dotnet is not on PATH. Run
`bash tools/validation/verify.sh` from chelsea after core/import changes. Unity must
also be opened and tested in its pinned editor before claiming presentation works.
Keep Library, Temp, builds, local source downloads and derived data out of Git.
Update PLAN.md and docs/VERIFICATION.md with actual results and remaining gates.
