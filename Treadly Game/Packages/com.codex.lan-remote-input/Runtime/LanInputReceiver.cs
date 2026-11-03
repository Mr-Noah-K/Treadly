using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace Codex.LanRemoteInput
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1000)]
    [AddComponentMenu("LAN Remote Input/LAN Input Receiver")]
    public sealed class LanInputReceiver : MonoBehaviour
    {
        [Header("Network")]
        [SerializeField, Range(1, 65535)] private int listenPort = 47777;
        [SerializeField] private bool requireAuthentication = false;
        [SerializeField] private string sharedSecret = string.Empty;

        [Header("Reliability")]
        [SerializeField, Min(1f)] private float maxPacketAgeSeconds = 10f;
        [SerializeField, Min(1f)] private float deviceTimeoutSeconds = 5f;
        [SerializeField, Range(1, 4096)] private int maxMessagesPerFrame = 256;

        [Header("Keyboard API")]
        [Tooltip("When enabled, KeyCode queries merge this computer's keyboard with remote devices.")]
        [SerializeField] private bool includeLocalKeyboard = true;

        [Header("Events")]
        [SerializeField] private LanInputUnityEvent onInputChanged = new LanInputUnityEvent();
        [SerializeField] private LanDeviceUnityEvent onDeviceConnected = new LanDeviceUnityEvent();
        [SerializeField] private LanDeviceUnityEvent onDeviceDisconnected = new LanDeviceUnityEvent();

        private readonly ConcurrentQueue<ReceivedDatagram> pendingDatagrams = new ConcurrentQueue<ReceivedDatagram>();
        private readonly Dictionary<string, DeviceState> devices = new Dictionary<string, DeviceState>(StringComparer.Ordinal);
        private readonly List<string> removalBuffer = new List<string>();
        private UdpClient udpClient;
        private Thread receiveThread;
        private volatile bool isListening;
        private float nextTimeoutCheck;

        public event Action<LanInputEvent> InputChanged;
        public event Action<LanDeviceInfo> DeviceConnected;
        public event Action<LanDeviceInfo> DeviceDisconnected;

        public int ListenPort => listenPort;
        public bool IsListening => isListening;
        public LanInputUnityEvent OnInputChanged => onInputChanged;
        public LanDeviceUnityEvent OnDeviceConnected => onDeviceConnected;
        public LanDeviceUnityEvent OnDeviceDisconnected => onDeviceDisconnected;

        private void OnEnable()
        {
            StartListening();
        }

        private void OnDisable()
        {
            StopListening();
        }

        private void Update()
        {
            foreach (DeviceState device in devices.Values)
            {
                device.PressedThisFrame.Clear();
                device.ReleasedThisFrame.Clear();
            }

            int processed = 0;
            while (processed < maxMessagesPerFrame && pendingDatagrams.TryDequeue(out ReceivedDatagram datagram))
            {
                ProcessDatagram(datagram);
                processed++;
            }

            if (Time.unscaledTime >= nextTimeoutCheck)
            {
                nextTimeoutCheck = Time.unscaledTime + 1f;
                RemoveTimedOutDevices(CurrentUnixMilliseconds());
            }
        }

        public void StartListening()
        {
            if (isListening)
                return;

            if (requireAuthentication && string.IsNullOrEmpty(sharedSecret))
            {
                Debug.LogError("LAN Input Receiver requires authentication, but its Shared Secret is empty.", this);
                return;
            }

            try
            {
                udpClient = new UdpClient(listenPort);
                udpClient.Client.ReceiveTimeout = 500;
                isListening = true;
                receiveThread = new Thread(ReceiveLoop)
                {
                    IsBackground = true,
                    Name = $"LAN Remote Input Receiver :{listenPort}"
                };
                receiveThread.Start();
            }
            catch (Exception exception)
            {
                isListening = false;
                udpClient?.Close();
                udpClient = null;
                Debug.LogError($"LAN Input Receiver could not listen on UDP port {listenPort}: {exception.Message}", this);
            }
        }

        public void StopListening()
        {
            if (!isListening && udpClient == null)
                return;

            isListening = false;
            udpClient?.Close();
            udpClient = null;

            if (receiveThread != null && receiveThread.IsAlive)
                receiveThread.Join(750);
            receiveThread = null;

            while (pendingDatagrams.TryDequeue(out _))
            {
            }

            long now = CurrentUnixMilliseconds();
            foreach (DeviceState device in devices.Values)
                DisconnectDevice(device, now);
            devices.Clear();
        }

        public bool AnyDevicePressed(string action)
        {
            return GetKey(action);
        }

        /// <summary>Returns true while any remote device holds a named binding.</summary>
        public bool GetKey(string binding)
        {
            return Query(binding, null, QueryKind.Held);
        }

        /// <summary>Returns true while one of the selected device IDs holds a named binding.</summary>
        public bool GetKey(string binding, params string[] selectedDeviceIds)
        {
            return Query(binding, selectedDeviceIds, QueryKind.Held);
        }

        /// <summary>Returns true while any remote device holds the forwarded physical key.</summary>
        public bool GetKey(KeyCode key)
        {
            return GetLocalKey(key, QueryKind.Held) || GetKey(key.ToString());
        }

        /// <summary>Returns true while one of the selected device IDs holds the forwarded physical key.</summary>
        public bool GetKey(KeyCode key, params string[] selectedDeviceIds)
        {
            return GetLocalKey(key, QueryKind.Held) || GetKey(key.ToString(), selectedDeviceIds);
        }

        /// <summary>Returns true on the frame a named remote binding is pressed.</summary>
        public bool GetKeyDown(string binding)
        {
            return Query(binding, null, QueryKind.Down);
        }

        public bool GetKeyDown(string binding, params string[] selectedDeviceIds)
        {
            return Query(binding, selectedDeviceIds, QueryKind.Down);
        }

        public bool GetKeyDown(KeyCode key)
        {
            return GetLocalKey(key, QueryKind.Down) || GetKeyDown(key.ToString());
        }

        public bool GetKeyDown(KeyCode key, params string[] selectedDeviceIds)
        {
            return GetLocalKey(key, QueryKind.Down) || GetKeyDown(key.ToString(), selectedDeviceIds);
        }

        /// <summary>Returns true on the frame a named remote binding is released.</summary>
        public bool GetKeyUp(string binding)
        {
            return Query(binding, null, QueryKind.Up);
        }

        public bool GetKeyUp(string binding, params string[] selectedDeviceIds)
        {
            return Query(binding, selectedDeviceIds, QueryKind.Up);
        }

        public bool GetKeyUp(KeyCode key)
        {
            return GetLocalKey(key, QueryKind.Up) || GetKeyUp(key.ToString());
        }

        public bool GetKeyUp(KeyCode key, params string[] selectedDeviceIds)
        {
            return GetLocalKey(key, QueryKind.Up) || GetKeyUp(key.ToString(), selectedDeviceIds);
        }

        private bool GetLocalKey(KeyCode key, QueryKind queryKind)
        {
            if (!includeLocalKeyboard)
                return false;

#if ENABLE_LEGACY_INPUT_MANAGER
            switch (queryKind)
            {
                case QueryKind.Down:
                    return Input.GetKeyDown(key);
                case QueryKind.Up:
                    return Input.GetKeyUp(key);
                default:
                    return Input.GetKey(key);
            }
#else
            return false;
#endif
        }

        public bool IsPressed(string deviceId, string action)
        {
            return !string.IsNullOrEmpty(deviceId) &&
                   !string.IsNullOrEmpty(action) &&
                   devices.TryGetValue(deviceId, out DeviceState device) &&
                   device.PressedActions.Contains(action);
        }

        private bool Query(string binding, string[] selectedDeviceIds, QueryKind queryKind)
        {
            if (string.IsNullOrWhiteSpace(binding))
                return false;

            string normalized = binding.Trim();
            foreach (DeviceState device in devices.Values)
            {
                if (!IsSelected(device.DeviceId, selectedDeviceIds))
                    continue;

                HashSet<string> source;
                switch (queryKind)
                {
                    case QueryKind.Down:
                        source = device.PressedThisFrame;
                        break;
                    case QueryKind.Up:
                        source = device.ReleasedThisFrame;
                        break;
                    default:
                        source = device.PressedActions;
                        break;
                }

                if (source.Contains(normalized))
                    return true;
            }

            return false;
        }

        private static bool IsSelected(string deviceId, string[] selectedDeviceIds)
        {
            if (selectedDeviceIds == null || selectedDeviceIds.Length == 0)
                return true;

            for (int i = 0; i < selectedDeviceIds.Length; i++)
            {
                if (string.Equals(deviceId, selectedDeviceIds[i], StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        public IReadOnlyList<LanDeviceInfo> GetConnectedDevices()
        {
            var snapshot = new List<LanDeviceInfo>(devices.Count);
            foreach (DeviceState device in devices.Values)
                snapshot.Add(CreateDeviceInfo(device));
            return snapshot;
        }

        private void ReceiveLoop()
        {
            while (isListening)
            {
                try
                {
                    IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = udpClient.Receive(ref endpoint);
                    if (data.Length <= LanInputProtocol.MaxPacketBytes)
                    {
                        pendingDatagrams.Enqueue(new ReceivedDatagram
                        {
                            Data = data,
                            RemoteAddress = endpoint.Address.ToString(),
                            ReceivedAtUnixMilliseconds = CurrentUnixMilliseconds()
                        });
                    }
                }
                catch (SocketException exception)
                {
                    if (!isListening)
                        break;
                    if (exception.SocketErrorCode != SocketError.TimedOut &&
                        exception.SocketErrorCode != SocketError.Interrupted)
                    {
                        Debug.LogWarning($"LAN Input Receiver socket error: {exception.Message}", this);
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception exception)
                {
                    if (isListening)
                        Debug.LogWarning($"LAN Input Receiver stopped reading a packet: {exception.Message}", this);
                }
            }
        }

        private void ProcessDatagram(ReceivedDatagram datagram)
        {
            long maxAge = (long)(Mathf.Max(1f, maxPacketAgeSeconds) * 1000f);
            if (!LanInputProtocol.TryDecode(
                    datagram.Data,
                    sharedSecret,
                    requireAuthentication,
                    datagram.ReceivedAtUnixMilliseconds,
                    maxAge,
                    out LanInputPacket packet,
                    out _))
            {
                return;
            }

            bool isNewDevice = !devices.TryGetValue(packet.DeviceId, out DeviceState device);
            if (isNewDevice)
            {
                device = new DeviceState(packet.DeviceId, packet.SessionId);
                devices.Add(packet.DeviceId, device);
            }
            else if (!string.Equals(device.SessionId, packet.SessionId, StringComparison.Ordinal))
            {
                ReleasePressedActions(device, datagram.ReceivedAtUnixMilliseconds);
                device.BeginSession(packet.SessionId);
                isNewDevice = true;
            }

            if (packet.Sequence <= device.LastSequence)
                return;

            device.LastSequence = packet.Sequence;
            device.DeviceName = packet.DeviceName;
            device.RemoteAddress = datagram.RemoteAddress;
            device.LastSeenUnixMilliseconds = datagram.ReceivedAtUnixMilliseconds;

            if (isNewDevice)
            {
                LanDeviceInfo info = CreateDeviceInfo(device);
                DeviceConnected?.Invoke(info);
                onDeviceConnected?.Invoke(info);
            }

            if (string.IsNullOrEmpty(packet.Action))
                return;

            bool wasPressed = device.PressedActions.Contains(packet.Action);
            if (wasPressed == packet.Pressed)
                return;

            if (packet.Pressed)
            {
                device.PressedActions.Add(packet.Action);
                device.PressedThisFrame.Add(packet.Action);
            }
            else
            {
                device.PressedActions.Remove(packet.Action);
                device.ReleasedThisFrame.Add(packet.Action);
            }

            RaiseInput(device, packet.Action, packet.Pressed, packet.Sequence, datagram.ReceivedAtUnixMilliseconds);
        }

        private void RemoveTimedOutDevices(long now)
        {
            long timeout = (long)(Mathf.Max(1f, deviceTimeoutSeconds) * 1000f);
            removalBuffer.Clear();

            foreach (KeyValuePair<string, DeviceState> pair in devices)
            {
                if (now - pair.Value.LastSeenUnixMilliseconds > timeout)
                    removalBuffer.Add(pair.Key);
            }

            foreach (string deviceId in removalBuffer)
            {
                DeviceState device = devices[deviceId];
                DisconnectDevice(device, now);
                devices.Remove(deviceId);
            }
        }

        private void DisconnectDevice(DeviceState device, long now)
        {
            ReleasePressedActions(device, now);
            LanDeviceInfo info = CreateDeviceInfo(device);
            DeviceDisconnected?.Invoke(info);
            onDeviceDisconnected?.Invoke(info);
        }

        private void ReleasePressedActions(DeviceState device, long now)
        {
            if (device.PressedActions.Count == 0)
                return;

            string[] actions = new string[device.PressedActions.Count];
            device.PressedActions.CopyTo(actions);
            device.PressedActions.Clear();
            foreach (string action in actions)
            {
                device.ReleasedThisFrame.Add(action);
                RaiseInput(device, action, false, device.LastSequence, now);
            }
        }

        private void RaiseInput(DeviceState device, string action, bool pressed, long sequence, long receivedAt)
        {
            var input = new LanInputEvent
            {
                DeviceId = device.DeviceId,
                SessionId = device.SessionId,
                DeviceName = device.DeviceName,
                Action = action,
                Phase = pressed ? LanInputPhase.Down : LanInputPhase.Up,
                RemoteAddress = device.RemoteAddress,
                Sequence = sequence,
                ReceivedAtUnixMilliseconds = receivedAt
            };

            InputChanged?.Invoke(input);
            onInputChanged?.Invoke(input);
        }

        private static LanDeviceInfo CreateDeviceInfo(DeviceState device)
        {
            return new LanDeviceInfo
            {
                DeviceId = device.DeviceId,
                SessionId = device.SessionId,
                DeviceName = device.DeviceName,
                RemoteAddress = device.RemoteAddress,
                LastSeenUnixMilliseconds = device.LastSeenUnixMilliseconds
            };
        }

        private static long CurrentUnixMilliseconds()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private sealed class ReceivedDatagram
        {
            public byte[] Data;
            public string RemoteAddress;
            public long ReceivedAtUnixMilliseconds;
        }

        private sealed class DeviceState
        {
            public readonly string DeviceId;
            public readonly HashSet<string> PressedActions = new HashSet<string>(StringComparer.Ordinal);
            public readonly HashSet<string> PressedThisFrame = new HashSet<string>(StringComparer.Ordinal);
            public readonly HashSet<string> ReleasedThisFrame = new HashSet<string>(StringComparer.Ordinal);
            public string SessionId;
            public string DeviceName;
            public string RemoteAddress;
            public long LastSequence = -1;
            public long LastSeenUnixMilliseconds;

            public DeviceState(string deviceId, string sessionId)
            {
                DeviceId = deviceId;
                SessionId = sessionId;
            }

            public void BeginSession(string sessionId)
            {
                SessionId = sessionId;
                LastSequence = -1;
                PressedActions.Clear();
                PressedThisFrame.Clear();
            }
        }

        private enum QueryKind
        {
            Held,
            Down,
            Up
        }
    }
}
