using System;
using UnityEngine.Events;

namespace Codex.LanRemoteInput
{
    public enum LanInputPhase
    {
        Down,
        Up
    }

    [Serializable]
    public sealed class LanInputEvent
    {
        public string DeviceId { get; internal set; }
        public string SessionId { get; internal set; }
        public string DeviceName { get; internal set; }
        public string Action { get; internal set; }
        public LanInputPhase Phase { get; internal set; }
        public string RemoteAddress { get; internal set; }
        public long Sequence { get; internal set; }
        public long ReceivedAtUnixMilliseconds { get; internal set; }
    }

    [Serializable]
    public sealed class LanDeviceInfo
    {
        public string DeviceId { get; internal set; }
        public string SessionId { get; internal set; }
        public string DeviceName { get; internal set; }
        public string RemoteAddress { get; internal set; }
        public long LastSeenUnixMilliseconds { get; internal set; }
    }

    [Serializable]
    public sealed class LanInputUnityEvent : UnityEvent<LanInputEvent>
    {
    }

    [Serializable]
    public sealed class LanDeviceUnityEvent : UnityEvent<LanDeviceInfo>
    {
    }
}
