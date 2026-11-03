using Codex.LanRemoteInput;
using UnityEngine;

namespace Codex.LanRemoteInput.Samples
{
    public sealed class RemoteCubeController : MonoBehaviour
    {
        [SerializeField] private LanInputReceiver receiver = null;
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float jumpHeight = 1.5f;

        private float verticalVelocity;

        private void Reset()
        {
            receiver = FindObjectOfType<LanInputReceiver>();
        }

        private void OnEnable()
        {
            if (receiver != null)
                receiver.InputChanged += HandleInputChanged;
        }

        private void OnDisable()
        {
            if (receiver != null)
                receiver.InputChanged -= HandleInputChanged;
        }

        private void Update()
        {
            if (receiver == null)
                return;

            float horizontal = 0f;
            if (receiver.GetKey("MoveLeft"))
                horizontal -= 1f;
            if (receiver.GetKey("MoveRight"))
                horizontal += 1f;

            verticalVelocity -= 9.81f * Time.deltaTime;
            Vector3 movement = new Vector3(horizontal * moveSpeed, verticalVelocity, 0f) * Time.deltaTime;
            transform.position += movement;

            if (transform.position.y < 0f)
            {
                Vector3 position = transform.position;
                position.y = 0f;
                transform.position = position;
                verticalVelocity = 0f;
            }
        }

        private void HandleInputChanged(LanInputEvent input)
        {
            if (input.Action == "Jump" && input.Phase == LanInputPhase.Down && transform.position.y <= 0.001f)
                verticalVelocity = Mathf.Sqrt(2f * 9.81f * jumpHeight);
        }
    }
}
