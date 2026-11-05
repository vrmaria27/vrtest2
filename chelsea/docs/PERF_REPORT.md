# Performance evidence

**Unverified — no Unity build, APK, Quest 3, Quest 3S or profiler session.**
The .NET test run is not VR performance evidence. Diagnostic mesh budget checks
are per-file structural checks, not draw-call, frame-time, memory or thermal tests.

Required future capture: build/version, device, refresh/quality mode, duration,
source snapshot, location, visible triangles, draw calls, active agents,
CPU-main/GPU frame-time p50/p95/p99, memory, allocations and cold-start time.
Capture busiest King's Road, detailed landmark, riverfront, full-tabletop and
repeated scale transitions. Run a >=30 min thermal session on each headset.

Targets from the brief remain unchanged: sustained 90 Hz (explicit 72 Hz mode may
be separately documented), p99 CPU/GPU each <10 ms at 90 Hz, typical/peak 1.0/1.3 M
visible triangles, <=300 draw calls, 0 B/frame application hot-path allocations,
<2.5 GB memory, <10 s cold start, APK <500 MB. None have been measured here.
