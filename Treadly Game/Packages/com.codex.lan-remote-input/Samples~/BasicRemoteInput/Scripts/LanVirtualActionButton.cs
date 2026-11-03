using Codex.LanRemoteInput;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Codex.LanRemoteInput.Samples
{
    public sealed class LanVirtualActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private LanInputSender sender = null;
        [SerializeField] private string action = "Jump";

        private bool pressed;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (sender == null || pressed)
                return;
            pressed = true;
            sender.SendActionDown(action);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Release();
        }

        private void OnDisable()
        {
            Release();
        }

        private void Release()
        {
            if (!pressed)
                return;
            pressed = false;
            if (sender != null)
                sender.SendActionUp(action);
        }
    }
}
