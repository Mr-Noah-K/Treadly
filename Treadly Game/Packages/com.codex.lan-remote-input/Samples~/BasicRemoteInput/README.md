# Basic Remote Input sample

This sample provides two small scripts. It intentionally does not include a scene so it can be dropped into either a 2D or 3D project without changing render-pipeline assets.

## Receiver scene

1. Use **GameObject > LAN Remote Input > Receiver**.
2. Create a Cube at `(0, 0, 0)` and add `RemoteCubeController`.
3. Assign the receiver to the controller.
4. Set the same non-empty Shared Secret on the receiver and sender, then enable Require Authentication on the receiver.

## Sender scene

1. Use **GameObject > LAN Remote Input > Sender**.
2. Add these keyboard bindings:
   - A -> MoveLeft
   - D -> MoveRight
   - Space -> Jump
3. Match the receiver's port and Shared Secret.
4. Leave Destination Address as `255.255.255.255` or enter the receiver computer's IPv4 address.

For touch controls, create UI buttons and attach `LanVirtualActionButton` to each. Assign the sender and one of the action names above.

Run the receiver in the Editor and the sender as a player on another computer or phone. The remote keys/buttons will move and jump the cube.

For the shortest possible API, leave each Action field blank and use `receiver.GetKey(KeyCode.A)`, `receiver.GetKey(KeyCode.D)`, or `receiver.GetKeyDown(KeyCode.Space)` in your gameplay script.
