using UnityEngine;

namespace RTSPrototype.Core
{
    /// <summary>
    /// Handles input processing for RTS controls
    /// </summary>
    public class Input : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private LayerMask groundLayer = 1;
        [SerializeField] private LayerMask unitLayer = 1 << 6;
        
        public static Input Instance { get; private set; }
        
        // Input events
        public System.Action<Vector3> OnLeftClick;
        public System.Action<Vector3> OnRightClick;
        public System.Action<Vector3, Vector3> OnDragStart;
        public System.Action<Vector3, Vector3> OnDragEnd;
        public System.Action<Vector2> OnCameraPan;
        public System.Action<float> OnCameraZoom;
        
        private Camera playerCamera;
        private bool isDragging = false;
        private Vector3 dragStartPosition;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindObjectOfType<Camera>();
            }
        }
        
        private void Update()
        {
            HandleMouseInput();
            HandleKeyboardInput();
        }
        
        private void HandleMouseInput()
        {
            // Left mouse button
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                Vector3 worldPos = GetWorldPosition(UnityEngine.Input.mousePosition);
                OnLeftClick?.Invoke(worldPos);
                
                dragStartPosition = worldPos;
                isDragging = true;
            }
            
            if (UnityEngine.Input.GetMouseButtonUp(0) && isDragging)
            {
                Vector3 worldPos = GetWorldPosition(UnityEngine.Input.mousePosition);
                OnDragEnd?.Invoke(dragStartPosition, worldPos);
                isDragging = false;
            }
            
            // Right mouse button
            if (UnityEngine.Input.GetMouseButtonDown(1))
            {
                Vector3 worldPos = GetWorldPosition(UnityEngine.Input.mousePosition);
                OnRightClick?.Invoke(worldPos);
            }
            
            // Mouse wheel for zoom
            float scroll = UnityEngine.Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                OnCameraZoom?.Invoke(scroll);
            }
        }
        
        private void HandleKeyboardInput()
        {
            // WASD camera movement
            Vector2 movement = Vector2.zero;
            
            if (UnityEngine.Input.GetKey(KeyCode.W)) movement.y += 1;
            if (UnityEngine.Input.GetKey(KeyCode.S)) movement.y -= 1;
            if (UnityEngine.Input.GetKey(KeyCode.A)) movement.x -= 1;
            if (UnityEngine.Input.GetKey(KeyCode.D)) movement.x += 1;
            
            if (movement.magnitude > 0.01f)
            {
                OnCameraPan?.Invoke(movement);
            }
        }
        
        private Vector3 GetWorldPosition(Vector3 screenPosition)
        {
            Ray ray = playerCamera.ScreenPointToRay(screenPosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
            {
                return hit.point;
            }
            
            // Fallback to plane at y=0
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }
            
            return Vector3.zero;
        }
    }
}