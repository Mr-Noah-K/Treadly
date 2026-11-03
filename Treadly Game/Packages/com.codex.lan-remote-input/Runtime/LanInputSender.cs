using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace Codex.LanRemoteInput
{
    [DisallowMultipleComponent]
    [AddComponentMenu("LAN Remote Input/LAN Input Sender")]
    public sealed class LanInputSender : MonoBehaviour
    {
        private const string DeviceIdPlayerPrefsKey = "Codex.LanRemoteInput.DeviceId";

        [Header("Identity")]
        [SerializeField] private string deviceName = string.Empty;

        [Header("Network")]
        [SerializeField] private string destinationAddress = "255.255.255.255";
        [SerializeField, Range(1, 65535)] private int destinationPort = 47777;
        [SerializeField] private string sharedSecret = string.Empty;

        [Header("Keyboard Bindings")]
        [SerializeField] private List<LanKeyBinding> bindings = new List<LanKeyBinding>();

        [Header("Reliability")]
        [SerializeField, Range(1, 5)] private int transitionCopies = 3;
        [SerializeField, Min(0.1f)] private float snapshotInterval = 0.5f;
        [SerializeField, Min(0.25f)] private float heartbeatInterval = 1f;

        private readonly Dictionary<string, bool> actionStates = new Dictionary<string, bool>(StringComparer.Ordinal);
        private UdpClient udpClient;
        private IPEndPoint destination;
        private string deviceId;
        private string sessionId;
        private long sequence;
        private float nextSnapshotTime;
        private float nextHeartbeatTime;

        public string DeviceId => deviceId;
        public string SessionId => sessionId;
        public bool IsSending => udpClient != null;

        private void OnEnable()
        {
            StartSending();
        }

        private void OnDisable()
        {
            ReleaseAllActions();
            StopSending();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                ReleaseAllActions();
        }

        private void Update()
        {
            PollKeyboard();

            if (Time.unscaledTime >= nextSnapshotTime)
            {
                nextSnapshotTime = Time.unscaledTime + Mathf.Max(0.1f, snapshotInterval);
                SendSnapshot();
            }

            if (Time.unscaledTime >= nextHeartbeatTime)
            {
                nextHeartbeatTime = Time.unscaledTime + Mathf.Max(0.25f, heartbeatInterval);
                SendPacket(string.Empty, false, 1);
            }
        }

        public void StartSending()
        {
            if (udpClient != null)
                return;

            if (!IPAddress.TryParse(destinationAddress, out IPAddress address))
            {
                Debug.LogError($"LAN Input Sender destination '{destinationAddress}' is not a valid IP address.", this);
                return;
            }

            if (address.AddressFamily != AddressFamily.InterNetwork)
            {
                Debug.LogError("LAN Input Sender currently requires an IPv4 destination address.", this);
                return;
            }

            try
            {
                deviceId = LoadOrCreateDeviceId();
                sessionId = Guid.NewGuid().ToString("N");
                sequence = 0;
                destination = new IPEndPoint(address, destinationPort);
                udpClient = new UdpClient();
                udpClient.EnableBroadcast = true;
                nextSnapshotTime = Time.unscaledTime;
                nextHeartbeatTime = Time.unscaledTime;
            }
            catch (Exception exception)
            {
                udpClient?.Close();
                udpClient = null;
                Debug.LogError($"LAN Input Sender could not start: {exception.Message}", this);
            }
        }

        public void StopSending()
        {
            udpClient?.Close();
            udpClient = null;
            destination = null;
            actionStates.Clear();
        }

        public void SendActionDown(string action)
        {
            SendActionState(action, true);
        }

        public void SendActionUp(string action)
        {
            SendActionState(action, false);
        }

        public void SendActionState(string action, bool pressed)
        {
            action = NormalizeAction(action);
            if (action == null)
                return;

            if (actionStates.TryGetValue(action, out bool previous) && previous == pressed)
                return;

            actionStates[action] = pressed;
            SendPacket(action, pressed, transitionCopies);
        }

        public void SendPulse(string action, float duration = 0.1f)
        {
            StartCoroutine(PulseRoutine(action, Mathf.Max(0.01f, duration)));
        }

        private IEnumerator PulseRoutine(string action, float duration)
        {
            SendActionDown(action);
            yield return new WaitForSecondsRealtime(duration);
            SendActionUp(action);
        }

        private void PollKeyboard()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            for (int i = 0; i < bindings.Count; i++)
            {
                LanKeyBinding binding = bindings[i];
                if (binding == null)
                    continue;

                string action = NormalizeAction(binding.ResolvedAction);
                if (action == null)
                    continue;

                bool pressed = Input.GetKey(binding.Key);
                if (!actionStates.TryGetValue(action, out bool previous) || previous != pressed)
                {
                    actionStates[action] = pressed;
                    SendPacket(action, pressed, transitionCopies);
                }
            }
#endif
        }

        private void SendSnapshot()
        {
            foreach (KeyValuePair<string, bool> state in actionStates)
                SendPacket(state.Key, state.Value, 1);
        }

        private void ReleaseAllActions()
        {
            if (udpClient == null || actionStates.Count == 0)
                return;

            var actions = new List<string>(actionStates.Keys);
            foreach (string action in actions)
            {
                if (!actionStates[action])
                    continue;
                actionStates[action] = false;
                SendPacket(action, false, transitionCopies);
            }
        }

        private void SendPacket(string action, bool pressed, int copies)
        {
            if (udpClient == null || destination == null)
                return;

            var packet = new LanInputPacket
            {
                DeviceId = deviceId,
                SessionId = sessionId,
                DeviceName = GetSafeDeviceName(),
                Action = action,
                Pressed = pressed,
                Sequence = sequence++,
                SentAtUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            try
            {
                byte[] data = LanInputProtocol.Encode(packet, sharedSecret);
                int sendCount = Mathf.Clamp(copies, 1, 5);
                for (int i = 0; i < sendCount; i++)
                    udpClient.Send(data, data.Length, destination);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"LAN Input Sender could not send '{action}': {exception.Message}", this);
            }
        }

        private static string NormalizeAction(string action)
        {
            if (string.IsNullOrWhiteSpace(action))
                return null;

            string normalized = action.Trim();
            if (normalized.Length > LanInputProtocol.MaxTextLength)
            {
                Debug.LogWarning($"LAN input action names cannot exceed {LanInputProtocol.MaxTextLength} characters.");
                return null;
            }

            return normalized;
        }

        private string GetSafeDeviceName()
        {
            string value = string.IsNullOrWhiteSpace(deviceName) ? SystemInfo.deviceName : deviceName.Trim();
            if (string.IsNullOrEmpty(value))
                value = "Unity Device";
            return value.Length <= LanInputProtocol.MaxTextLength
                ? value
                : value.Substring(0, LanInputProtocol.MaxTextLength);
        }

        private static string LoadOrCreateDeviceId()
        {
            string id = PlayerPrefs.GetString(DeviceIdPlayerPrefsKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(id))
                return id;

            id = Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(DeviceIdPlayerPrefsKey, id);
            PlayerPrefs.Save();
            return id;
        }
    }
}
