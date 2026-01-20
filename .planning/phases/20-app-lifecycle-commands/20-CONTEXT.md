# Phase 20: App Lifecycle Commands - Context

**Gathered:** 2026-01-20
**Status:** Ready for planning

<vision>
## How This Should Work

The CLI should be able to fully manage ChronoView's lifecycle — launch it, stop it, restart it, and check its status. The vision is a full lifecycle manager that enables test-executor agents to work completely autonomously without manual app launching.

For launching: use `dotnet run --project ChronoView/ChronoView.csproj` directly. No need to find compiled executables — just run the project as you would from the command line.

For status detection: use process-based detection. Look for running ChronoView processes and windows rather than maintaining persistent state files. The CLI should be stateless and discover what's running in real-time.

Keep crash handling simple — no sophisticated crash detection or recovery needed. The focus is on reliable launch and clean shutdown.

</vision>

<essential>
## What Must Be Nailed

- **Reliable launch** — The core requirement for test-executor independence. Agents must be able to launch ChronoView without manual intervention.
- **Clean termination** — Equally critical. Tests need clean environments, so the app must be fully stopped between test runs.

Both launch and termination are non-negotiable for the testing workflow to work.

</essential>

<specifics>
## Specific Ideas

- **Launch command:** Use `dotnet run --project ChronoView/ChronoView.csproj`
- **State tracking:** Process detection only — no persistent state files
- **Crash handling:** Keep it simple — basic launch/stop without sophisticated crash detection
- **Multi-instance:** The CLI should be aware of multiple instances (full lifecycle manager), but process detection makes this feasible

</specifics>

<notes>
## Additional Context

This phase enables test-executor agents to run completely autonomously. Currently, agents require manual app launching before tests can run. After this phase, the same CLI that automates UI will also control app lifecycle.

The user emphasized "full lifecycle manager" but then clarified to keep crash handling simple. The balance is: comprehensive coverage (launch/stop/restart/status) with straightforward implementation (process detection, no state files).

</notes>

---

*Phase: 20-app-lifecycle-commands*
*Context gathered: 2026-01-20*
