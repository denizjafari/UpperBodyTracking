# Phase 2 — Sensor input layer

## What landed in this phase

```
Assets/Scripts/Input/
├── TrackingMode.cs              # enum IMU_UDP / Controllers / Simulator
├── UdpPacket.cs                 # wire format (matches RPi JSON)
├── JointSnapshot.cs             # latest angle + timestamp per joint
├── ISensorInputAdapter.cs       # adapter contract
├── UdpSensorAdapter.cs          # background-thread UDP listener
├── ControllerSensorAdapter.cs   # dev-only synthetic angles from XR controller
└── SimulatorSensorAdapter.cs    # sine-wave generator for unit tests

Assets/Scripts/Managers/
└── SensorInputManager.cs        # adapter swap, timeout detection, fan-out

Assets/Settings/
├── network.json                 # UDP port + timeouts
└── rom_calibration.json         # placed here for Phase 7 to pick up
```

## Setup in the Editor
1. On `[Managers]`, add `SensorInputManager`.
2. Add a child GameObject `[Adapters]` and attach all three adapter components:
   `UdpSensorAdapter`, `ControllerSensorAdapter`, `SimulatorSensorAdapter`.
3. Drag those three into the `SensorInputManager`'s slots.
4. On `ControllerSensorAdapter`, drag `OVRCameraRig/CenterEyeAnchor` into `headRef`.

## Wire format (UDP, RPi → Unity, port 5005)

```json
{
  "ts_ms": 1745136540123,
  "joint": "shoulder_flexion",
  "angle_deg": 87.4,
  "valid": true,
  "compensation": { "detected": false, "type": null, "severity": 0 }
}
```
One JSON object per datagram, one joint per packet. Joints use the constants in
`VRRehab.Data.JointKeys`.

## Threading model
- UDP receive runs on a dedicated `Thread` (background, IsBackground=true).
- Packets land in a `ConcurrentQueue<UdpPacket>`; drained on the main thread in
  `Update()` so all listener callbacks happen on the main thread.
- Adapter `End()` is called in `OnDisable` so app pause/resume cycles release
  the socket cleanly.

## Verification gate 2
- [ ] In the Editor with `Simulator` mode, `Services.Sensors.OnPacket` fires
      ~50 Hz across five joints.
- [ ] In `IMU_UDP` mode, sending 1000 packets from a laptop produces < 5 drops
      and < 50 ms median latency (verify with a logged `(now - ts_ms)` histogram).
- [ ] Yanking the sender for 2 s raises `OnSensorTimeout` for the affected joint;
      resuming raises `OnSensorRecovered` within one packet.
- [ ] Switching `TrackingMode` at runtime via `SensorInputManager.SwitchTo(...)`
      cleanly swaps adapters; no GC.Alloc spike in the Profiler.
- [ ] `git tag phase-2-input && git push --tags`.
