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

## Passive Guidance Calibration

Use a named tool profile to save results. Tune tractor steering first. Verify dimensions, enable geometry, choose Curve Response between 50% and 200%, then tick **I have verified the profile dimensions** on **Calibration**.

1. **Start Calibration**: drive normally in working soil at working speed. Record at least 30 valid straight seconds and 8 settled seconds in each physical curve direction. Allow longer runs (about a minute straight and 20 seconds each way) to collect enough usable samples. Gentle transitions do not count as straight or settled curves. Do not intentionally make abrupt steering inputs to force a fit.
2. **Review Results**: recording stops. The assistant estimates a first-order response time and delay from actual tractor and tool error on straights, and a curve factor from their separation in settled bends. It rejects insufficient movement, weak fits, insufficient curve off-tracking, and substantially different left/right results. Receiving no proposal is a valid result; a consistently centred tool may not need calibration or supply enough excitation.
3. **Test Settings**: temporarily applies bounded suggestions for Correction Interval, Correction Strength, Heading Sensitivity and Curve Response. Each change is restricted to 25% of the previous value and the calibration range. Tracking, Acquire, dimensions and offset limits are not changed. The saved profile remains untouched during this trial.
4. Repeat comparable driving, then **Finish Test**. Requires at least 20 valid straight seconds and 5 seconds per curve direction. At least 70% of each category must match the baseline speed and curvature bands. Approval requires at least 5% lower matched tool-error RMS overall, with no direction worsening by more than 10% plus 1 cm. This is an observational comparison, not proof of causality or a guarantee under other conditions.
5. **Save Calibration** writes only the proposed tuning fields, calibration timestamp, geometry signature, simulation label and measured summary into the current tool profile. **Restore Previous** discards the temporary proposal. Closing the tool configuration panel also discards it. Changing profiles, relevant dimensions, manual settings or simulator mode invalidates the session.

Collection is at up to 10 Hz and supports GNSS updates at 5 Hz or faster. It requires engaged forward passive guidance, both steering buttons on, sections on, fresh tool position and heading, and RTK on tractor and tool (or the built-in simulator). U-turns, reverse, missing data and offset saturation are excluded. The first two seconds after engagement or resumption are excluded. Pauses split the recording so delay/derivative estimates cannot bridge gaps.

**Export Calibration Log** writes recording and trial samples to Twol's Logs directory. Simulator calibrations are explicitly marked and should not be interpreted as soil-response measurements. Recalibrate in actual working conditions when appropriate. This is a bounded calibration assistant, not continuously self-adjusting machine learning. Timing estimates are uncertain for nonlinear slip, changing soil or speed; a weak fit leaves settings unchanged.

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
