# Passive guidance tuning and implement heading

Open **Tool Steer Configuration → Passive** in the compact left panel. The right setup window does not need to be expanded for tuning. The inner pages are Response, Limits, Geometry, Calibration, and Diagnostics. Manual adjustments are live and save when leaving the tab or closing the panel. Calibration trials use a temporary override and are only saved with Save Calibration.

These controls follow the documented *purposes* of commercial tuning controls. Twol's equations and values are its own; this is not a reproduction of Deere or Trimble's proprietary controller.

| Control | Actual effect |
| --- | --- |
| Tracking Sensitivity | Strength of response to implement cross-track error. |
| Heading Sensitivity | Response to lateral motion implied by implement heading. Damps a returning implement and increases response when it is heading away. Works with look-ahead off. |
| Acquire Sensitivity | Extra correction strength when implement error is large, blending into ordinary tracking near the line. Does not change engagement thresholds. |
| Curve Response | The single visible curve multiplier, 0–300%. With verified geometry enabled, 100% uses the dimension-based prediction. Without it, 100% preserves the old combined Curve Base Gain × Curve Sensitivity setting. The old controls are hidden and their XML fields remain for compatibility. |
| Correction Interval | Actual seconds between corrections, 0–25 in 0.1-second steps. Zero means continuous. Old profiles retain their Passive Adj numeric value, which is now interpreted as seconds rather than update counts. |
| Correction Strength | Independent correction size/rate limit. At 100, up to 0.20 m per correction and 0.20 m/s; accumulated correction remains bounded. |
| Straight Maximum Offset | Bounds the combined target offset on straight paths, 0.1–3 m. Default 1 m. |
| Curve Maximum Offset | Separate bound, 0.1–3 m. Old profiles inherit the straight limit until explicitly changed. The effective limit blends as curvature increases between radius 200 m and 25 m, including the upcoming bend. Calibration does not increase these limits. |
| Tool Look-ahead | Optional extra heading preview, 0–2 seconds. Zero disables extra preview, not heading damping. |
| Retry early | Shortens the hold when the implement remains more than 8 cm off line and is moving away, has crossed the line, or is not improving. Minimum response delay is 0.8–2 seconds, depending on interval. A tool returning toward the line keeps its hold. |

For an initial comparison with the previous holding behavior, use interval **4 seconds**, sensitivities and strength **100**, maximum offset **1 m**, look-ahead **0**, and early retry **on**. Existing profiles retain their existing sensitivities, curve gain, look-ahead, and interval. Tune one setting at a time.

Passive correction remains in the Pure Pursuit controller. Both tractor autosteer and the tool-steer button must be on. Sections off temporarily suspend correction and cannot reactivate a button that is off. It resets with invalid/stale implement position, sections off, or tractor speed below 2 km/h. Engagement still requires the tractor within 10 cm and 1.5 degrees of its line. U-turn correction remains disabled; U-turn path generation and steering are unchanged. This does not add an active implement reverse controller.

## Heading selection

1. Fresh dual GNSS baseline heading has priority. Mounting alignment uses the existing Dual Heading Offset.
2. With single GNSS and IMU, relative yaw is anchored to GNSS course or calculated displacement course. The IMU supplies quick turn changes; GNSS adjusts drift slowly. **Fusion currently runs in Twol**, where tractor reverse state is available. Raw yaw alone is never treated as true north.
3. Without IMU, use fresh receiver course, or calculate course from an antenna displacement window of at least 0.5 m for RTK fix and 1 m otherwise. Calculated speed allows a position-only receiver to work without a VTG speed sentence.
4. At insufficient motion or without a trustworthy heading, use position-only passive correction at the antenna point. Do not fabricate orientation.

Single-antenna course describes travel direction and cannot independently measure implement side-slip or stationary physical orientation. In reverse, course is converted to an estimated forward-facing body heading before applying antenna geometry; travel prediction then follows the reverse direction. A direction change resets the heading estimator. The existing tractor reverse indication is used.

Position receipt freshness is 1.5 seconds; dual/course freshness is 1 second; IMU freshness is 0.5 seconds. IMU fusion can bridge up to 2 seconds without a fresh GNSS reference, then falls back. Sender sample age is checked separately from DGPS correction age.

## ToolDual firmware

Use the updated `ToolDual/ToolDual.ino` source with its companion files. **The existing `.hex` has not been rebuilt and does not contain these changes.** Build for your existing Teensy board using NativeEthernet and Adafruit BNO08x plus its dependencies. Set `TOOL_USE_BNO08X` to `0` for a build without the BNO08x library; single-GNSS operation also works with it enabled and no IMU found.

The BNO08x connects to `ImuWire` (Wire, SCL 19 / SDA 18 on the existing Teensy configuration); firmware probes I2C addresses 0x4A then 0x4B. Mount the IMU rigidly with Z up, X forward, and Y left. Confirm the displayed/logged turn direction and roll sign after mounting. The game rotation vector supplies relative yaw, avoiding reliance on a magnetic heading next to steel. Roll zero and invert-roll use the existing settings.

Ethernet still sends PAOGI for dual GNSS and PANDA for single GNSS, now on every available position fix. Optional trailing fields 16–19 carry heading source (0 none, 1 dual, 2 relative IMU yaw), validity, heading sample age in milliseconds, and GPS course. All angles use degrees. Empty IMU fields mean unavailable. Legacy sentences remain accepted. Dual dropout stops blocking position packets and permits heading fallback. F9P heading validity now checks the full flags word including `relPosHeadingValid`.

## Geometry and calibration checks

Tune tractor steering first. Measure antenna lateral offset and height, implement pivot/rotation point to antenna, and pivot to working point with the implement straight and in its normal working position. Verify fore/aft signs in the existing Antenna page. Zero roll on a level reference and check it again facing the opposite direction. The profile copy now preserves roll zero correctly.

Make forward passes on a straight reference line in both directions. Check the **implement working point**, rather than just its antenna or the tractor, for repeatable centering. Correct geometry/mounting errors before increasing sensitivity. Compare a return-to-line case with a sustained side-draft case, then curves. Calibration reuses measured dimensions; it does not learn or rewrite them.

Diagnostics shows heading source, tool error, error rate, applied tractor target offset, engagement, and last correction reason. Optional recording writes a CSV once per second into Twol's existing Logs directory and shows the filename. Recording ends when the panel closes. This supports comparing settings without guessing from steering-wheel movement alone.

## Geometry-based curve compensation

On **Geometry**, check the profile hitch type, signed tractor hitch length, trailing drawbar length, working-bar-to-pivot distance, tow-between length (if applicable), and width. Also check the separate antenna-to-pivot/working-point offsets on the existing setup page: these must describe the same working point. Only then enable **Use verified profile geometry**. Start Curve Response at **100%**; it is a percentage of a different model, not the former numeric base gain (for example, 24.3).

Existing profiles deliberately keep geometry disabled. Their old base gain and sensitivity are retained as a combined legacy reference behind the single Curve Response control. Enabling geometry replaces that reference with the measured-dimension model. If you disable it, the original reference is still available.

The concentric steady-turn model relates requested working-bar radius `Rbar` and tractor pivot radius `Rvehicle`:

`Rvehicle² = Rbar² + drawbar² + towBetweenDrawbar² - tractorHitch² - workingBarFromPivot²`

For rigid tools, only the tractor hitch/working-centre displacement term is used. Curvature is measured from path positions at spaced stations rather than relying on point headings. Preview blends local curvature with curvature ahead of the tractor; its distance is `clamp(0.5 × geometry length + speed in m/s, 2, 15)` metres. The geometric radius relationship is speed-independent, while execution preview accounts for motion. It is an approximation for ordinary curve tracking, not a full predicted swept-path optimiser. Lateral side draft is left to measured tool-error feedback. The existing strength slew bound still applies, and feedback cannot accumulate behind a saturated geometric offset.

Articulated tractors and front-mounted trailing arrangements are not supported by this first geometry calibration. Invalid dimensions disable geometric output and leave the legacy fallback. It does not add articulation constraints, full-footprint boundary planning, minimum-headland optimisation, or working-bar-centred U-turns.

## Passive Guidance Calibration — refined workflow

Use a named tool profile to save results. Verify dimensions and antenna offsets, enable geometry, choose Curve Response within 50–200%, then tick **I have verified the profile dimensions**. The stages are separate:

- **Straight response (timing only)** estimates response delay/time constant from straight data. A proposal requires training R² ≥0.80 and chronologically held-out R² ≥0.65. If tracking is already within 8 cm RMS, the fit is weak, or the interval is outside 0.8–8 seconds, it retains manual timing. Any timing proposal changes **Correction Interval only**, by at most 15%; it does not retune Strength, Heading or Curve Response. A strong fit does not guarantee improved control: independent testing remains required.
- **Curve response (settled bends)** changes **Curve Response only**, by at most 25%. It does not change Interval, Strength or Heading. The geometry separation validates the model, while actual residual tool error determines whether current feedback already supplies adequate compensation. Small/uncertain adjustments are declined: the change must exceed both the uncertainty band and ten percentage points. A retained setting is not a claim that it is globally optimal.

Start with straight response, retain current timing if evidence is weak, then start a separate curve calibration. Use normal working soil and speed, without abrupt steering inputs to force a fit.

1. **Start Calibration** records the selected stage. Straight response needs at least 30 valid straight seconds. Curve response also needs settled left and right bends, not just briefly touching each direction.
2. **Review Results** estimates only the selected control family. Curve samples are excluded unless the tractor, tool and preview all share the same bend. They must remain in that condition for at least `max(3 seconds, profile tractor-to-working-bar length / speed)`, with stable tool error and offset. Four near-one-second blocks per direction are required; partial fragments do not count as independent settled observations.
3. The paired-bend fit separates a constant side-draft intercept from curvature-dependent separation. That intercept is diagnostic; it is **not** a new antenna offset or a change to machine dimensions. Error/offset transients, phase changes, large uncertainty and inconsistent model ranges produce no proposal. **Continue Recording** adds another pass without discarding previous data or lowering confidence thresholds; at most 12,000 samples are retained.
4. If a material proposal is supported, **Record Comparison Pass** drives a separate route using the current saved settings. Press **Finish Comparison Pass** after sufficient data: 20 straight seconds and, for curve calibration, 5 seconds each way. The proposal is not applied during comparison recording.
5. **Test Settings** temporarily applies the proposal. Repeat the comparison route, speed and working conditions, then press **Finish Test**. Test cannot begin before the comparison recording is complete. Training data is not reused as its performance baseline.
6. Approval requires at least 5% lower matched RMS for the selected stage: straight RMS for timing, opposing-curve RMS for Curve Response. The existing non-regression checks remain across every relevant category: no direction (including straights during curve calibration) may worsen by more than 10% plus 1 cm. At least 70% of each test category must match the comparison speed and curvature bands. These are observational checks, not proof of causality under all conditions.
7. **Save Calibration** writes the tested proposal and its measured summary to the current named implement profile. **Restore Previous** discards the override. Closing the configuration panel also restores saved tuning. Changing profiles, dimensions, settings or simulator mode invalidates recording, comparison and trial together. Nothing is saved when evidence is insufficient or a trial is rejected.

Collection is at up to 10 Hz and supports GNSS updates at 5 Hz or faster. It requires engaged forward passive guidance, both steering buttons on, sections on, fresh tool position and heading, and RTK on tractor and tool (or the built-in simulator). U-turns, reverse, missing data and offset saturation are excluded. The first two seconds after engagement or resumption are excluded. Pauses split the recording so fits cannot bridge gaps. Timing confidence can remain weak in variable soil; retaining current settings is then the intended result.

**Export Calibration Log** writes `record`, `comparison` and `test` samples to Twol's Logs directory. It includes tractor/tool/preview curvature, required settling time, and the reason each curve-fit sample was used or excluded. Simulator calibrations remain explicitly marked; they are not measurements of real soil response.

The closed-loop harness keeps the original short-curve failure recording as a regression case. It separately exercises 50%, 75%, 100%, 125% and 150% starting Curve Response, with proposals frozen before validation on unseen noise seeds, changed speed, curve radius and soil phase. A paired current-settings validation pass and candidate pass share those unseen conditions. One additional recording pass is permitted for uncertain fits; the threshold is unchanged. Moderate settings may be retained rather than being advertised as optimised.

## Validation

The complete Twol C# source was compiled with C# 7.1 against .NET Framework 4.8.1 reference assemblies and the declared OpenTK 3.3.3 dependencies, with no compiler errors. This is a source compilation check, not a packaged Visual Studio build or Windows simulator run.

Production controller, heading resolver, NMEA parser, and settings are tested by `tests/PassiveGuidance` (requires .NET 8 SDK):

```sh
dotnet run --project tests/PassiveGuidance/PassiveGuidance.csproj
```

Host firmware tests compile actual sentence-builder and F9P-decoder source using small hardware stubs:

```sh
g++ -std=c++11 -I tests/FirmwareHeading tests/FirmwareHeading/Test.cpp -o /tmp/twol-heading-test
/tmp/twol-heading-test
```

Regression tests include geometric circle identities, numerical trailing-tool replays in both directions, 5/10 Hz response estimation, legacy XML compatibility, saturation handling, trial/restore/save lifecycle, invalid-data rejection, and worse/unmatched trial rejection. These do not replace a Teensy firmware build, sensor mounting checks, field calibration, or the Windows desktop simulator. No ready-to-flash firmware or new desktop executable is included.
