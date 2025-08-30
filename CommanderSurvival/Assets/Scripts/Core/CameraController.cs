using UnityEngine;

namespace RTSPrototype.Core
{
    /// <summary>
    /// Controls the RTS camera movement and positioning
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private float panSpeed = 10f;
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float maxZoom = 20f;
        [SerializeField] private Vector2 panLimit = new Vector2(50f, 50f);
        
        [Header("Smoothing")]
        [SerializeField] private float panSmoothness = 0.1f;
        [SerializeField] private float zoomSmoothness = 0.1f;
        
        private Camera playerCamera;
        private Vector3 targetPosition;
        private float targetZoom;
        private Vector3 velocity = Vector3.zero;
        private float zoomVelocity = 0f;
        
        private void Awake()
        {
            playerCamera = GetComponent<Camera>();
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }
        
        private void Start()
        {
            targetPosition = transform.position;
            targetZoom = playerCamera.orthographicSize;
            
            // Subscribe to input events
            if (Input.Instance != null)
            {
                Input.Instance.OnCameraPan += HandlePan;
                Input.Instance.OnCameraZoom += HandleZoom;
            }
        }
        
        private void Update()
        {
            UpdateCameraPosition();
            UpdateCameraZoom();
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from input events
            if (Input.Instance != null)
            {
                Input.Instance.OnCameraPan -= HandlePan;
                Input.Instance.OnCameraZoom -= HandleZoom;
            }
        }
        
        private void HandlePan(Vector2 direction)
        {
            Vector3 panDirection = new Vector3(direction.x, 0, direction.y);
            panDirection = transform.TransformDirection(panDirection);
            panDirection.y = 0; // Keep camera at same height
            
            targetPosition += panDirection * panSpeed * Time.deltaTime;
            ClampCameraPosition();
        }
        
        private void HandleZoom(float zoomDelta)
        {
            targetZoom -= zoomDelta * zoomSpeed;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }
        
        private void UpdateCameraPosition()
        {
            transform.position = Vector3.SmoothDamp(
                transform.position, 
                targetPosition, 
                ref velocity, 
                panSmoothness
            );
        }
        
        private void UpdateCameraZoom()
        {
            playerCamera.orthographicSize = Mathf.SmoothDamp(
                playerCamera.orthographicSize,
                targetZoom,
                ref zoomVelocity,
                zoomSmoothness
            );
        }
        
        private void ClampCameraPosition()
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, -panLimit.x, panLimit.x);
            targetPosition.z = Mathf.Clamp(targetPosition.z, -panLimit.y, panLimit.y);
        }
        
        public void SetCameraPosition(Vector3 position)
        {
            targetPosition = position;
            ClampCameraPosition();
        }
        
        public void SetZoom(float zoom)
        {
            targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        }
        
        public void FocusOnPosition(Vector3 position)
        {
            Vector3 focusPosition = position;
            focusPosition.y = targetPosition.y; // Maintain current height
            SetCameraPosition(focusPosition);
        }
    }
}