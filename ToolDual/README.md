# ToolDual implement IMU heading

ToolDual can use a BNO08x IMU mounted on the implement when operating with a
single GNSS antenna. The IMU heading lets Twol estimate the implement's working
point from its antenna position and configured antenna-to-tool geometry. GNSS
course over ground remains the fallback when the IMU heading is unavailable.

## Hardware and build

- This sketch expects an Adafruit BNO08x compatible sensor, such as the BNO085,
  connected over I2C to the Teensy 4.1 `Wire` bus (SDA pin 18, SCL pin 19).
- Install the **Adafruit BNO08x** Arduino library and its dependencies before
  compiling. The sketch still runs without a connected sensor, but the library
  must be installed to build it.
- Mount the IMU rigidly with its Z axis vertical and its forward axis aligned
  with the implement's forward direction. Keep it away from magnetic
  interference from steel, motors, and high-current wiring.
- Configure the Tool settings in Twol, including the antenna offsets and
  `dualHeadingOffset`, to match the installed antenna and sensor orientation.
  Check heading against a surveyed direction before using it for field work.

## Data path

The sketch sends `$PANDA` over the ToolDual Ethernet UDP position stream on port
19999 in single-GNSS mode. PANDA heading, roll, pitch, and yaw rate are in
degrees and degrees per second. Twol accepts an IMU heading only while it is
fresh (500 ms), and applies the configured heading offset. When no calibrated,
recent IMU heading is available, Twol falls back to GNSS course over ground.

The existing dual-GNSS `$PAOGI` path is unchanged. This feature only changes
straight passive guidance reference-point selection; it does not add tool
centered U-turn behavior.
