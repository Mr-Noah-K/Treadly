# LAN Remote Input manual

## Data flow

```text
Keyboard / touch UI
        |
        v
LanInputSender -- UDP broadcast or unicast --> LanInputReceiver
                                                   |
                                                   +--> InputChanged event
                                                   +--> AnyDevicePressed(...)
                                                   +--> IsPressed(deviceId, ...)
```

Each sender owns its local action state. The receiving player is authoritative about whether a remote action is permitted to affect the game.

## Input-style API

Leave the sender binding's Action blank to forward its physical key. The receiver then mirrors Unity's classic input calls:

```csharp
if (receiver.GetKey(KeyCode.W)) MoveForward();
if (receiver.GetKeyDown(KeyCode.Space)) Jump();
if (receiver.GetKeyUp(KeyCode.E)) CloseInteraction();
```

`KeyCode` overloads merge local and remote keyboard state when **Include Local Keyboard** is enabled (the default). Named-action string overloads read remote actions only.

Passing no IDs aggregates every connected sender, like multiple keyboards controlling the same game. Pass one or more stable device IDs to filter the query:

```csharp
if (receiver.GetKey(KeyCode.W, playerOnePhoneId)) MovePlayerOne();
if (receiver.GetKeyDown("Fire", playerOnePhoneId, playerOneTabletId)) FireForPlayerOne();
```

Device IDs filter the remote senders only. Disable **Include Local Keyboard** if a selected-device query must exclude the receiver computer's own keyboard.

## Recommended production settings

| Setting | Recommended starting value | Purpose |
|---|---:|---|
| Port | `47777` | Must match on sender and receiver |
| Shared Secret | unique non-empty value | Signs packets with HMAC-SHA256 |
| Require Authentication | on | Rejects unsigned packets |
| Snapshot Interval | `0.5` seconds | Repairs missed UDP state changes |
| Device Timeout | `5` seconds | Releases held actions after disconnect |
| Max Packet Age | `10` seconds | Rejects stale/replayed packets |

## Receiver event example

```csharp
private void OnEnable()
{
    receiver.InputChanged += HandleInput;
    receiver.DeviceDisconnected += HandleDisconnect;
}

private void OnDisable()
{
    receiver.InputChanged -= HandleInput;
    receiver.DeviceDisconnected -= HandleDisconnect;
}

private void HandleInput(LanInputEvent message)
{
    if (message.Action == "Fire" && message.Phase == LanInputPhase.Down)
        TryFireFor(message.DeviceId);
}
```

## Per-device players

Use `DeviceId` as the stable local identity and `SessionId` as the current app-run identity. On a sender restart, the receiver releases old held actions and accepts the new session immediately.

```csharp
foreach (LanDeviceInfo device in receiver.GetConnectedDevices())
{
    bool accelerate = receiver.IsPressed(device.DeviceId, "Accelerate");
    UpdateKart(device.DeviceId, accelerate);
}
```

## Touch and controller-style UI

Import the Basic Remote Input sample. Add `LanVirtualActionButton` to a UI element that receives pointer events. Set its sender reference and action. Pointer down sends a press; pointer up, pointer exit, disable, and application focus loss all send a release.

## Lifecycle

`LanInputSender` and `LanInputReceiver` start in `OnEnable` and clean up in `OnDisable`. The receiver performs socket reads on a background thread and dispatches Unity-facing events on the main thread. It limits work per frame using **Max Messages Per Frame**.

## Platform notes

- Windows, macOS, Linux, Android, and iOS support UDP sockets, subject to OS firewall and local-network permissions.
- On iOS, add a Local Network Usage Description when distributing an app that accesses the LAN. Bonjour service declaration may be needed depending on App Store behavior and discovery choices; this package uses broadcast rather than Bonjour.
- Android builds need the normal internet/network permissions Unity supplies when networking APIs are used.
- WebGL is unsupported.

## Protocol notes

The wire format is a small versioned UTF-8 record. User text fields are Base64-encoded, and the canonical record can be signed using HMAC-SHA256. Receivers reject malformed, oversized, stale, out-of-order, or incorrectly signed datagrams. This protocol is meant for same-LAN input and is not exposed as a general internet service.
