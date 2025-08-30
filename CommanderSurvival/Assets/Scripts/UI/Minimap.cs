using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RTSPrototype.UI
{
    /// <summary>
    /// Displays a minimap showing unit positions and map overview
    /// </summary>
    public class Minimap : MonoBehaviour
    {
        [Header("Minimap Settings")]
        [SerializeField] private Camera minimapCamera;
        [SerializeField] private RawImage minimapDisplay;
        [SerializeField] private RectTransform minimapContainer;
        [SerializeField] private float mapSize = 100f;
        [SerializeField] private float cameraHeight = 50f;
        [SerializeField] private LayerMask minimapLayers = -1;
        
        [Header("Unit Indicators")]
        [SerializeField] private GameObject unitIconPrefab;
        [SerializeField] private Color friendlyUnitColor = Color.blue;
        [SerializeField] private Color enemyUnitColor = Color.red;
        [SerializeField] private Color neutralUnitColor = Color.yellow;
        [SerializeField] private float iconSize = 3f;
        [SerializeField] private bool showUnitIcons = true;
        
        [Header("Camera Indicator")]
        [SerializeField] private Image cameraIndicator;
        [SerializeField] private Color cameraIndicatorColor = Color.white;
        [SerializeField] private bool showCameraView = true;
        
        [Header("Update Settings")]
        [SerializeField] private float updateInterval = 0.1f;
        [SerializeField] private bool enableMinimapClick = true;
        
        private Camera playerCamera;
        private RenderTexture minimapTexture;
        private List<MinimapIcon> unitIcons = new List<MinimapIcon>();
        private float lastUpdateTime;
        private Vector2 minimapCenter;
        
        [System.Serializable]
        private class MinimapIcon
        {
            public Transform unit;
            public Image icon;
            public UnitType unitType;
        }
        
        private enum UnitType
        {
            Friendly,
            Enemy,
            Neutral
        }
        
        private void Awake()
        {
            if (minimapCamera == null)
            {
                SetupMinimapCamera();
            }
            
            if (minimapDisplay == null)
            {
                minimapDisplay = GetComponent<RawImage>();
            }
            
            if (minimapContainer == null)
            {
                minimapContainer = GetComponent<RectTransform>();
            }
        }
        
        private void Start()
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindObjectOfType<Camera>();
            }
            
            SetupMinimap();
            
            // Subscribe to input events for minimap clicking
            if (enableMinimapClick && Core.Input.Instance != null)
            {
                // Note: This would need additional setup for UI input handling
            }
        }
        
        private void Update()
        {
            if (Time.time >= lastUpdateTime + updateInterval)
            {
                UpdateMinimap();
                lastUpdateTime = Time.time;
            }
            
            UpdateCameraIndicator();
        }
        
        private void SetupMinimapCamera()
        {
            GameObject cameraObj = new GameObject("MinimapCamera");
            cameraObj.transform.SetParent(transform);
            
            minimapCamera = cameraObj.AddComponent<Camera>();
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = mapSize * 0.5f;
            minimapCamera.transform.position = new Vector3(0, cameraHeight, 0);
            minimapCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
            minimapCamera.cullingMask = minimapLayers;
            minimapCamera.depth = -10; // Render before main camera
            
            // Create render texture
            minimapTexture = new RenderTexture(256, 256, 16);
            minimapCamera.targetTexture = minimapTexture;
        }
        
        private void SetupMinimap()
        {
            if (minimapTexture != null && minimapDisplay != null)
            {
                minimapDisplay.texture = minimapTexture;
            }
            
            if (minimapContainer != null)
            {
                minimapCenter = minimapContainer.rect.center;
            }
            
            // Create camera indicator if it doesn't exist
            if (cameraIndicator == null && showCameraView)
            {
                GameObject indicatorObj = new GameObject("CameraIndicator");
                indicatorObj.transform.SetParent(minimapContainer);
                
                cameraIndicator = indicatorObj.AddComponent<Image>();
                cameraIndicator.color = cameraIndicatorColor;
                cameraIndicator.raycastTarget = false;
                
                RectTransform indicatorRect = indicatorObj.GetComponent<RectTransform>();
                indicatorRect.sizeDelta = new Vector2(20, 20);
                indicatorRect.anchoredPosition = Vector2.zero;
            }
        }
        
        private void UpdateMinimap()
        {
            UpdateMinimapCamera();
            UpdateUnitIcons();
        }
        
        private void UpdateMinimapCamera()
        {
            if (minimapCamera != null && playerCamera != null)
            {
                // Position minimap camera above the player camera's focus area
                Vector3 cameraPos = playerCamera.transform.position;
                cameraPos.y = cameraHeight;
                minimapCamera.transform.position = cameraPos;
            }
        }
        
        private void UpdateUnitIcons()
        {
            if (!showUnitIcons) return;
            
            // Find all units in the scene
            RTS.UnitController[] allUnits = FindObjectsOfType<RTS.UnitController>();
            
            // Remove icons for destroyed units
            for (int i = unitIcons.Count - 1; i >= 0; i--)
            {
                if (unitIcons[i].unit == null)
                {
                    if (unitIcons[i].icon != null)
                    {
                        Destroy(unitIcons[i].icon.gameObject);
                    }
                    unitIcons.RemoveAt(i);
                }
            }
            
            // Update existing icons and create new ones
            foreach (var unit in allUnits)
            {
                if (unit == null || !unit.IsAlive) continue;
                
                MinimapIcon existingIcon = unitIcons.Find(icon => icon.unit == unit.transform);
                
                if (existingIcon != null)
                {
                    // Update existing icon position
                    UpdateIconPosition(existingIcon);
                }
                else
                {
                    // Create new icon
                    CreateUnitIcon(unit);
                }
            }
        }
        
        private void CreateUnitIcon(RTS.UnitController unit)
        {
            if (unitIconPrefab == null || minimapContainer == null) return;
            
            GameObject iconObj = Instantiate(unitIconPrefab, minimapContainer);
            Image iconImage = iconObj.GetComponent<Image>();
            
            if (iconImage == null)
            {
                iconImage = iconObj.AddComponent<Image>();
            }
            
            // Determine unit type and color
            UnitType unitType = DetermineUnitType(unit);
            Color iconColor = GetUnitColor(unitType);
            iconImage.color = iconColor;
            iconImage.raycastTarget = false;
            
            // Set icon size
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.sizeDelta = Vector2.one * iconSize;
            
            // Create minimap icon entry
            MinimapIcon newIcon = new MinimapIcon
            {
                unit = unit.transform,
                icon = iconImage,
                unitType = unitType
            };
            
            unitIcons.Add(newIcon);
            UpdateIconPosition(newIcon);
        }
        
        private void UpdateIconPosition(MinimapIcon icon)
        {
            if (icon.unit == null || icon.icon == null || minimapContainer == null) return;
            
            // Convert world position to minimap position
            Vector3 worldPos = icon.unit.position;
            Vector2 minimapPos = WorldToMinimapPosition(worldPos);
            
            RectTransform iconRect = icon.icon.GetComponent<RectTransform>();
            iconRect.anchoredPosition = minimapPos;
        }
        
        private Vector2 WorldToMinimapPosition(Vector3 worldPosition)
        {
            if (minimapCamera == null || minimapContainer == null) return Vector2.zero;
            
            Vector3 cameraPos = minimapCamera.transform.position;
            Vector2 relativePos = new Vector2(
                worldPosition.x - cameraPos.x,
                worldPosition.z - cameraPos.z
            );
            
            // Normalize to minimap bounds
            float mapHalfSize = minimapCamera.orthographicSize;
            relativePos.x = (relativePos.x / mapHalfSize) * (minimapContainer.rect.width * 0.5f);
            relativePos.y = (relativePos.y / mapHalfSize) * (minimapContainer.rect.height * 0.5f);
            
            return relativePos;
        }
        
        private void UpdateCameraIndicator()
        {
            if (!showCameraView || cameraIndicator == null || playerCamera == null) return;
            
            Vector3 cameraWorldPos = playerCamera.transform.position;
            Vector2 cameraMinimapPos = WorldToMinimapPosition(cameraWorldPos);
            
            RectTransform indicatorRect = cameraIndicator.GetComponent<RectTransform>();
            indicatorRect.anchoredPosition = cameraMinimapPos;
            
            // Rotate indicator to match camera rotation
            float cameraRotation = playerCamera.transform.eulerAngles.y;
            indicatorRect.rotation = Quaternion.Euler(0, 0, -cameraRotation);
        }
        
        private UnitType DetermineUnitType(RTS.UnitController unit)
        {
            // Simple implementation - can be expanded with team/faction system
            if (unit.CompareTag("Player") || unit.CompareTag("Friendly"))
            {
                return UnitType.Friendly;
            }
            else if (unit.CompareTag("Enemy"))
            {
                return UnitType.Enemy;
            }
            else
            {
                return UnitType.Neutral;
            }
        }
        
        private Color GetUnitColor(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.Friendly: return friendlyUnitColor;
                case UnitType.Enemy: return enemyUnitColor;
                case UnitType.Neutral: return neutralUnitColor;
                default: return Color.white;
            }
        }
        
        public void OnMinimapClick(Vector2 clickPosition)
        {
            if (!enableMinimapClick || playerCamera == null || minimapContainer == null) return;
            
            // Convert minimap click to world position
            Vector2 localClick = clickPosition - minimapContainer.rect.center;
            
            float mapHalfSize = minimapCamera.orthographicSize;
            float worldX = (localClick.x / (minimapContainer.rect.width * 0.5f)) * mapHalfSize;
            float worldZ = (localClick.y / (minimapContainer.rect.height * 0.5f)) * mapHalfSize;
            
            Vector3 worldClickPos = new Vector3(
                minimapCamera.transform.position.x + worldX,
                0,
                minimapCamera.transform.position.z + worldZ
            );
            
            // Move camera to clicked position
            Core.CameraController cameraController = playerCamera.GetComponent<Core.CameraController>();
            if (cameraController != null)
            {
                cameraController.SetCameraPosition(new Vector3(worldClickPos.x, playerCamera.transform.position.y, worldClickPos.z));
            }
        }
        
        public void SetMapSize(float size)
        {
            mapSize = size;
            if (minimapCamera != null)
            {
                minimapCamera.orthographicSize = mapSize * 0.5f;
            }
        }
        
        public void SetShowUnitIcons(bool show)
        {
            showUnitIcons = show;
            
            foreach (var icon in unitIcons)
            {
                if (icon.icon != null)
                {
                    icon.icon.gameObject.SetActive(show);
                }
            }
        }
        
        public void SetUpdateInterval(float interval)
        {
            updateInterval = Mathf.Max(0.01f, interval);
        }
        
        private void OnDestroy()
        {
            if (minimapTexture != null)
            {
                minimapTexture.Release();
            }
        }
    }
}