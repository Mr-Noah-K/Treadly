using System;
using UnityEngine;

namespace Codex.LanRemoteInput
{
    [Serializable]
    public sealed class LanKeyBinding
    {
        [Tooltip("Leave blank to expose this as its physical KeyCode (for example Space). Set a name to expose an action such as Jump.")]
        [SerializeField] private string action = string.Empty;
        [SerializeField] private KeyCode key = KeyCode.Space;

        public string Action => action;
        public KeyCode Key => key;
        public string ResolvedAction => string.IsNullOrWhiteSpace(action) ? key.ToString() : action.Trim();
    }
}
