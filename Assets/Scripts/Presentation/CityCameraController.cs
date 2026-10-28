using UnityEngine;
using UnityEngine.InputSystem;

namespace EpochCity
{
    /// <summary>
    /// Stadtkamera für das neue Input System.
    /// WASD / Pfeiltasten: bewegen · Q / E: drehen · Mausrad: zoomen
    /// Mittlere Maustaste halten + Maus bewegen: drehen und neigen
    /// </summary>
    public class CityCameraController : MonoBehaviour
    {
        [Header("Fokuspunkt")]
        public Vector3 focus = new Vector3(64f, 0f, 64f);
        public Vector2 boundsMin = new Vector2(0f, 0f);
        public Vector2 boundsMax = new Vector2(128f, 128f);

        [Header("Bewegung")]
        public float panSpeed = 1.2f;          // wird mit dem Abstand skaliert
        public float rotateKeySpeed = 90f;      // Grad pro Sekunde
        public float mouseRotateSpeed = 0.2f;

        [Header("Zoom")]
        public float distance = 40f;
        public float minDistance = 8f;
        public float maxDistance = 150f;
        public float zoomStep = 4f;

        [Header("Winkel")]
        public float yaw = 45f;
        public float pitch = 55f;
        public float minPitch = 25f;
        public float maxPitch = 85f;

        private void LateUpdate()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            float dt = Time.unscaledDeltaTime;

            if (kb != null)
            {
                // Bewegung relativ zur Blickrichtung auf der Bodenebene
                var move = Vector2.zero;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;

                var flatRot = Quaternion.Euler(0f, yaw, 0f);
                var dir = flatRot * new Vector3(move.x, 0f, move.y);
                focus += dir * (panSpeed * distance * dt);

                if (kb.qKey.isPressed) yaw -= rotateKeySpeed * dt;
                if (kb.eKey.isPressed) yaw += rotateKeySpeed * dt;
            }

            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                    distance = Mathf.Clamp(distance - Mathf.Sign(scroll) * zoomStep, minDistance, maxDistance);

                if (mouse.middleButton.isPressed)
                {
                    var delta = mouse.delta.ReadValue();
                    yaw += delta.x * mouseRotateSpeed;
                    pitch = Mathf.Clamp(pitch - delta.y * mouseRotateSpeed, minPitch, maxPitch);
                }
            }

            focus.x = Mathf.Clamp(focus.x, boundsMin.x, boundsMax.x);
            focus.z = Mathf.Clamp(focus.z, boundsMin.y, boundsMax.y);

            var rot = Quaternion.Euler(pitch, yaw, 0f);
            transform.SetPositionAndRotation(focus + rot * new Vector3(0f, 0f, -distance), rot);
        }
    }
}
