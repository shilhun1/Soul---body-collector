namespace SmilingEclipse.STMImporter
{
    using UnityEngine;
    using UnityEngine.InputSystem; 

    public class UIDragZoom : MonoBehaviour
    {
        public RectTransform target;
        public Canvas canvas;        // seu canvas
        public float zoomSpeed = 0.1f;
        public float minZoom = 0.5f;
        public float maxZoom = 2.0f;
        private float zoom = 1f;

        private bool dragging = false;
        private Vector2 lastMousePos;

        private float dragTimer = 0.15f;

        private void Start()
        {
        }

        void Update()
        {
            HandleDrag();
            HandleZoom();
        }

        void HandleDrag()
        {
            if (Mouse.current.rightButton.isPressed)
            {

            }


            if (dragging)
            {
                dragTimer += Time.deltaTime;
                if (dragTimer > 0.15f)
                {
                    Vector2 currentMousePos = Mouse.current.position.ReadValue();
                    Vector2 delta = currentMousePos - lastMousePos;
                    lastMousePos = currentMousePos;

                    // Ajusta delta pelo scale do Canvas (importante em ScreenSpace-Camera)
                    float scaleFactor = canvas.scaleFactor;
                    float force = canvas.renderMode == RenderMode.ScreenSpaceCamera ? 0.01f : 1f;

                    // move no espaço local do target
                    target.position += (Vector3)delta * force * scaleFactor;
                }

            }
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                dragging = true;
                lastMousePos = Mouse.current.position.ReadValue();
            }
            else if (Mouse.current.rightButton.wasReleasedThisFrame)
            {
                dragging = false;

                dragTimer = 0f;
            }
        }

        void HandleZoom()
        {
            float scroll = Mouse.current.scroll.ReadValue().y; if (Mathf.Abs(scroll) > 0.01f)
            {
                zoom = Mathf.Clamp(zoom + scroll * zoomSpeed * Time.deltaTime, minZoom, maxZoom);
                canvas.scaleFactor = zoom;
            }
        }
    }
}