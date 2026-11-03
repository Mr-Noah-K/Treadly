# LAN Remote Input for Unity

LAN Remote Input turns another Unity player on the same local network into a lightweight input device. A sender converts local keyboard keys or UI buttons into named actions such as `Jump`, `Fire`, or `MoveLeft`. A receiver raises events and exposes the current pressed state of those actions.

The package has no third-party dependencies. It uses UDP broadcast by default, supports direct unicast, retries state changes, periodically repairs dropped state packets, times out disconnected devices, and can authenticate packets with an HMAC shared secret.

## Use it like Unity's keyboard API

After assigning a `LanInputReceiver` in the Inspector, remote input is one line:

```csharp
if (receiver.GetKey(KeyCode.W)) transform.Translate(Vector3.forward * speed * Time.deltaTime);
if (receiver.GetKeyDown(KeyCode.Space)) Jump();
if (receiver.GetKeyUp(KeyCode.LeftShift)) StopRunning();
```

These `KeyCode` calls merge the receiver computer's local keyboard and all remote keyboards by default, so the change from classic input is usually just `Input` -> `receiver`. Turn off **Include Local Keyboard** on the receiver if you want remote-only input.

To accept only selected devices, pass their IDs (available from `GetConnectedDevices()`):

```csharp
if (receiver.GetKeyDown(KeyCode.Space, phoneId, tabletId)) Jump();
```

The IDs filter remote devices; the local keyboard still participates while **Include Local Keyboard** is enabled.

On the sender, add the key to **Keyboard Bindings** and leave its Action field blank. A blank action forwards the physical `KeyCode`. If you prefer named controls, enter an action such as `Jump` and read it with `receiver.GetKeyDown("Jump")`.

## Requirements

- Unity 2021.3 or newer
- Two devices on the same LAN or Wi-Fi network
- UDP traffic allowed on the configured port (default `47777`)
- For keyboard polling, **Active Input Handling** must include the legacy Input Manager (`Input Manager (Old)` or `Both`). UI/touch calls work with any input backend.

WebGL players are not supported because browsers cannot open arbitrary UDP sockets.

## Install

### From the included folder

1. For the supplied `.tgz`, choose **Window > Package Manager > + > Add package from tarball...** and select `com.codex.lan-remote-input-1.0.0.tgz`.
2. Or copy `LanRemoteInput` into your project's `Packages` folder, for example `Packages/com.codex.lan-remote-input`.
3. Or choose **Window > Package Manager > + > Add package from disk...** and select this package's `package.json`.
4. Optional: in Package Manager, open **LAN Remote Input > Samples** and import **Basic Remote Input**.

## Five-minute setup

### 1. Configure the receiving game

1. Create an empty GameObject named `LAN Input Receiver`.
2. Add `LanInputReceiver`.
3. Keep port `47777`, or choose another port and use the same value on every sender.
4. Set **Shared Secret** to the same non-empty value on all devices and enable **Require Authentication** for normal use.
5. Add a script that reads actions:

```csharp
using Codex.LanRemoteInput;
using UnityEngine;

public sealed class RemotePlayer : MonoBehaviour
{
    [SerializeField] private LanInputReceiver receiver;
    [SerializeField] private float speed = 4f;

    private void Update()
    {
        float x = 0f;
        if (receiver.GetKey(KeyCode.A)) x -= 1f;
        if (receiver.GetKey(KeyCode.D)) x += 1f;
        if (receiver.GetKeyDown(KeyCode.Space)) Debug.Log("Remote jump!");
        transform.Translate(Vector3.right * (x * speed * Time.deltaTime));
    }
}
```

### 2. Configure the sending device

1. Create an empty GameObject named `LAN Input Sender`.
2. Add `LanInputSender`.
3. Use the same port and shared secret as the receiver.
4. Leave **Destination Address** as `255.255.255.255` for automatic LAN broadcast. If the router blocks broadcast, enter the receiver's local IPv4 address, such as `192.168.1.25`.
5. Add keyboard bindings in the Inspector. Leave Action blank for same-keyboard calls such as `receiver.GetKey(KeyCode.A)`, or enter names such as `MoveLeft`, `MoveRight`, and `Jump` for action-based calls.
6. Build and run the sender on the second device.

You can also send touch/UI actions without keyboard polling. Add the sample `LanVirtualActionButton` to a UI object, assign the sender and an action name, or call:

```csharp
sender.SendActionDown("Jump");
sender.SendActionUp("Jump");
```

## Public API

### `LanInputReceiver`

- `InputChanged`: C# event fired when an action changes between up and down.
- `DeviceConnected`: fired when a new sender session is first seen.
- `DeviceDisconnected`: fired after its timeout expires.
- `AnyDevicePressed(action)`: true if any connected device holds the action.
- `GetKey`, `GetKeyDown`, `GetKeyUp`: Input-style overloads for a `KeyCode` or named action, optionally limited to selected remote device IDs. `KeyCode` overloads also include the local keyboard by default.
- `IsPressed(deviceId, action)`: state for one sender.
- `GetConnectedDevices()`: snapshot of known devices.
- `StartListening()` / `StopListening()`: explicit lifecycle control if needed.

The Inspector also exposes matching UnityEvents.

### `LanInputSender`

- `SendActionDown(action)` and `SendActionUp(action)`: ideal for UI buttons.
- `SendActionState(action, pressed)`: explicit state API.
- `SendPulse(action, duration)`: coroutine helper for a short press.

## Networking and security

- UDP is intentionally low latency and connectionless. State-change packets are repeated, and full state snapshots repair occasional packet loss.
- Packet authentication is optional for quick prototypes. Enable **Require Authentication** and use a strong shared secret for real projects.
- HMAC authentication prevents forged/modified packets from devices that do not know the secret. It does **not** encrypt action names or device names.
- Treat a LAN as untrusted. The receiver validates packet size, timestamp, sequence, text length, and signature, but game-authoritative code must still validate what a remote action is allowed to do.
- This is a LAN input transport, not a full game-state networking framework. It does not synchronize GameObjects, physics, scenes, accounts, or matchmaking.

## Troubleshooting

- **No events:** verify both devices use the same port and secret, are on the same subnet, and the receiver is enabled.
- **Broadcast fails:** set the sender's destination to the receiver's LAN IPv4 address. Some guest Wi-Fi networks enable client isolation.
- **Windows prompt:** allow the built player through the private-network firewall. Do not expose the UDP port to the public internet.
- **Keys do nothing:** enable the legacy Input Manager or `Both` under **Project Settings > Player > Active Input Handling**. UI/manual API calls do not require it.
- **Action sticks briefly:** reduce Snapshot Interval or Device Timeout. Very lossy Wi-Fi may need a direct unicast address.
- **Multiple receivers on one machine:** only one process can normally bind the same UDP port. Give each receiver a unique port.

See [Documentation~/index.md](Documentation~/index.md) and the imported Basic Remote Input sample for a fuller walkthrough.
