# Passive Guidance closed-loop harness

Run the actual `CGuidance.Guidance` routine against an independent tractor/trailer plant. This extends the isolated tests in `tests/PassiveGuidance`; it does not replace the Windows desktop simulator or field testing.

## Run on Windows

In the packaged Windows ZIP, extract the entire folder and double-click **RunHarness.cmd**. The published executable is self-contained; Visual Studio and a .NET installation are unnecessary. The console runs the cases and opens `harness-output/index.html` in your browser. The executable was cross-published on Linux; native Windows launch has not been executed here.

From a source checkout, double-click `tests/PassiveHarness/RunHarness.cmd` with the .NET 8 SDK installed, or run:

```sh
dotnet run --project tests/PassiveHarness -c Release -- --output harness-output
```

Default seeds are 42, 43, 44. The full run writes an interactive report, `summary.json`, per-episode CSV traces, and calibration recording/trial CSVs. Exit **1** means a check failed; the report still opens. Known calibration performance failures are intentionally retained. Do not interpret successful compilation as all guidance checks passing.

For a quick regression run that omits calibration, the higher-speed/tow-between cases, and additional fault cases:

```sh
dotnet run --project tests/PassiveHarness -c Release -- --quick --seeds 42 --output harness-quick
```

For your own dimensions and tuning, copy `example.json`, edit it, then:

```sh
dotnet run --project tests/PassiveHarness -c Release -- --config my-machine.json --seeds 42,43,44 --output my-results
```

The packaged executable accepts the same arguments. Units are metres, seconds, m/s, and degrees where named. `Drawbar` is a positive physical length; the adapter converts it to Twol's negative trailing-hitch setting. `WorkingBar` is behind the tool pivot and should match `Tuning.PivotToToolDistance`. Antenna fore/aft and lateral mounting are simulated from the tuning fields; roll is level. `TrueDrawbar`, if supplied, changes the simulated implement's actual length without changing the assumed profile, allowing a dimension-error experiment. Keep unrelated inputs fixed for comparisons. The suite's fixed accuracy targets are not automatically relaxed for your configuration.

## What is production code

The project directly compiles these repository files, not rewritten equivalents:

- `CGuidance.cs`: acquisition, button/sections/GPS gating, Pure Pursuit, curve preview, combined limits and offset slew, and the calibration sampling connection.
- `CPassiveController.cs`, `CPassiveGeometry.cs`, `CPassiveCalibration.cs`.
- `CNMEA.cs`: coordinate conversion and receiver data model.
- `CSettings.cs` and `vec3.cs`.

The only functional production change for the harness is optional clock injection in `CGuidance`. The ordinary constructor still uses UTC wall time. Compiled source texts are embedded and SHA-256 hashes are recorded in the report, so the tested code can be identified even in a standalone executable.

`DesktopAdapter.cs` substitutes WinForms and device dependencies. It supplies a **fixed configurable Pure Pursuit look-ahead**, primitive distance/angle operations, and simulated GNSS fields. It does not exercise the desktop `CVehicle.UpdateGoalPointDistance` model, graphics, network I/O, firmware, physical actuators, or raw-packet parsing (the existing regression tests cover the parser separately). `Settings` is static in production, so scenarios run sequentially and reset it between independent experiments. No user profile is loaded or saved, no UDP packets are sent, and no steering hardware is addressed.

## Plant and scenarios

The independent plant integrates a bicycle tractor at 50 Hz with commanded steering delay, first-order actuator response, steering-rate and angle limits. One or two differential trailer stages follow the hitch; optional lateral soil slip is an external velocity. Steering decisions use production guidance at 5/10 Hz with seeded GNSS and heading noise. Working-bar ground-truth error is scored by a separate geometric projector, rather than trusting the controller's own reported error.

Cases include displaced-tool engagement, soil draft, right/left curves, S bends, course/displacement/IMU heading modes, 5 Hz GNSS, 4 m/s travel, tow-between geometry, insufficient offset capacity, button/section transitions, stale tool GPS, and U-turn inhibition. The U-turn case checks that passive correction is disabled; no U-turn planning is added or tested. At the default speed, the 25 m-radius bends are ordinary route curves, not headland turns.

The geometry comparisons keep the source profile's legacy curve base at its default 5 and sensitivity 100, while the geometry model uses verified dimensions at 100%. They do **not** establish superiority over a manually optimised legacy controller. Repeat with measured dimensions and actual legacy gain for that comparison.

## Declared checks

- All commands finite; steering ≤35°; target offset within its effective limit.
- Active fixed-limit offset slew ≤ `0.20 × strength / 100` m/s. Inhibit resets are exempt.
- Straight engagement last-15s RMS ≤0.10 m; opposite-side overshoot ≤0.15 m.
- Settled soil-draft RMS ≤0.12 m.
- Nominal curve RMS ≤0.20 m; inside-cut p95 ≤0.35 m; first-15s exit peak ≤0.30 m.
- Geometry curve RMS at least 30% below the declared legacy reference.
- Buttons off, sections off, stale GPS and U-turns inhibit passive offsets. Resumption must reacquire normally. Sections cannot override a button that remains off.
- Calculated heading/IMU fusion actually exercised; identical seed replay produces identical full-rate traces.

These are explicit engineering regression targets for the simulated fixture, not guaranteed agricultural accuracy specifications. The limited-offset case is expected to cut inside and must report saturation; its accuracy is not compared with the nominal curve target.

## Calibration validation and current finding

Each seed records a straight/left/right route with modest time-varying soil draft, calls production Review Results, temporarily tests the actual proposal on the same route and noise seed, then calls Finish Test. The harness checks that rejected results cannot save and Restore removes the guidance override. It records the candidate's errors, settings, estimated response, and fit quality.

At the initial baseline, all three seeds produced proposals that **worsened curve RMS** (approximately 14–16 cm before, 18 cm after). The existing validation correctly rejected them. The full run therefore has **three performance failures**, even though its gating, bounds, rejection and restore checks pass. Automatic tuning is not established by this harness. The calibration estimator needs further work before relying on its proposals. A plausible cause to investigate is the curve-factor fit using tractor and tool positions at different curve phases, plus soil draft confounding the first-order response fit; this is a hypothesis, not a verified diagnosis.

The earlier 91 isolated checks still pass. Those checks demonstrate mathematical identities and controlled input cases; they do not contradict the closed-loop calibration failure.

## Reproducibility and boundaries

The baseline report records every source hash, configuration, seed and full-rate trace. Report replay is reduced to 2 Hz; scoring uses full-rate samples. A byte-identical replay check covers the guidance/plant path, but report generation timestamps are intentionally real time. HTML is self-contained and makes no external requests.

This model excludes terrain roll, GNSS multipath/latency beyond the explicit dropout case, hydraulic deadband, wheel slip of the tractor, implement flexibility, real boundary clearance, and reverse dynamics. It cannot certify a machine. First use its results to reject regressions and bad calibration proposals, then validate remaining behaviour with the desktop simulator and actual equipment.
