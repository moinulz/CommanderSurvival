using UnityEngine;
using UnityEngine.UI;

namespace RTSPrototype.UI
{
    /// <summary>
    /// Displays unit health above units
    /// </summary>
    public class HealthBar : MonoBehaviour
    {
        [Header("Health Bar Settings")]
        [SerializeField] private Canvas healthCanvas;
        [SerializeField] private Image healthBarFill;
        [SerializeField] private Image backgroundBar;
        [SerializeField] private Text healthText;
        [SerializeField] private float showDistance = 20f;
        [SerializeField] private bool alwaysShow = false;
        [SerializeField] private bool showOnlyWhenDamaged = true;
        
        [Header("Visual Settings")]
        [SerializeField] private Color fullHealthColor = Color.green;
        [SerializeField] private Color lowHealthColor = Color.red;
        [SerializeField] private Color criticalHealthColor = Color.red;
        [SerializeField] private float lowHealthThreshold = 0.5f;
        [SerializeField] private float criticalHealthThreshold = 0.2f;
        [SerializeField] private Vector3 offset = Vector3.up * 2f;
        
        private RTS.UnitController unitController;
        private Camera playerCamera;
        private float maxHealth;
        private float currentHealth;
        private bool isVisible = false;
        
        private void Awake()
        {
            unitController = GetComponentInParent<RTS.UnitController>();
            
            if (healthCanvas == null)
            {
                healthCanvas = GetComponent<Canvas>();
            }
            
            if (healthCanvas != null)
            {
                healthCanvas.worldCamera = Camera.main;
            }
        }
        
        private void Start()
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindObjectOfType<Camera>();
            }
            
            if (unitController != null)
            {
                maxHealth = unitController.MaxHealth;
                currentHealth = unitController.Health;
                
                // Subscribe to health changes
                unitController.OnHealthChanged += OnHealthChanged;
                unitController.OnUnitDestroyed += OnUnitDestroyed;
            }
            
            InitializeHealthBar();
            UpdateHealthBar();
        }
        
        private void Update()
        {
            UpdateVisibility();
            UpdatePosition();
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (unitController != null)
            {
                unitController.OnHealthChanged -= OnHealthChanged;
                unitController.OnUnitDestroyed -= OnUnitDestroyed;
            }
        }
        
        private void InitializeHealthBar()
        {
            if (healthCanvas == null)
            {
                // Create canvas if it doesn't exist
                GameObject canvasObj = new GameObject("HealthBarCanvas");
                canvasObj.transform.SetParent(transform);
                canvasObj.transform.localPosition = offset;
                
                healthCanvas = canvasObj.AddComponent<Canvas>();
                healthCanvas.renderMode = RenderMode.WorldSpace;
                healthCanvas.worldCamera = playerCamera;
                
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.dynamicPixelsPerUnit = 10f;
                
                // Create background
                if (backgroundBar == null)
                {
                    GameObject bgObj = new GameObject("Background");
                    bgObj.transform.SetParent(canvasObj.transform);
                    bgObj.transform.localPosition = Vector3.zero;
                    bgObj.transform.localScale = Vector3.one;
                    
                    backgroundBar = bgObj.AddComponent<Image>();
                    backgroundBar.color = Color.black;
                    
                    RectTransform bgRect = bgObj.GetComponent<RectTransform>();
                    bgRect.sizeDelta = new Vector2(100, 10);
                }
                
                // Create health fill
                if (healthBarFill == null)
                {
                    GameObject fillObj = new GameObject("HealthFill");
                    fillObj.transform.SetParent(canvasObj.transform);
                    fillObj.transform.localPosition = Vector3.zero;
                    fillObj.transform.localScale = Vector3.one;
                    
                    healthBarFill = fillObj.AddComponent<Image>();
                    healthBarFill.color = fullHealthColor;
                    healthBarFill.type = Image.Type.Filled;
                    healthBarFill.fillMethod = Image.FillMethod.Horizontal;
                    
                    RectTransform fillRect = fillObj.GetComponent<RectTransform>();
                    fillRect.sizeDelta = new Vector2(100, 10);
                }
                
                // Create health text (optional)
                if (healthText == null && alwaysShow)
                {
                    GameObject textObj = new GameObject("HealthText");
                    textObj.transform.SetParent(canvasObj.transform);
                    textObj.transform.localPosition = Vector3.zero;
                    textObj.transform.localScale = Vector3.one;
                    
                    healthText = textObj.AddComponent<Text>();
                    healthText.text = $"{currentHealth:F0}/{maxHealth:F0}";
                    healthText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    healthText.fontSize = 8;
                    healthText.color = Color.white;
                    healthText.alignment = TextAnchor.MiddleCenter;
                    
                    RectTransform textRect = textObj.GetComponent<RectTransform>();
                    textRect.sizeDelta = new Vector2(100, 15);
                    textRect.anchoredPosition = new Vector2(0, 15);
                }
            }
        }
        
        private void UpdateVisibility()
        {
            bool shouldShow = alwaysShow;
            
            if (!shouldShow && playerCamera != null)
            {
                float distance = Vector3.Distance(transform.position, playerCamera.transform.position);
                shouldShow = distance <= showDistance;
            }
            
            if (!shouldShow && showOnlyWhenDamaged && unitController != null)
            {
                shouldShow = unitController.Health < unitController.MaxHealth;
            }
            
            if (shouldShow != isVisible)
            {
                isVisible = shouldShow;
                if (healthCanvas != null)
                {
                    healthCanvas.gameObject.SetActive(isVisible);
                }
            }
        }
        
        private void UpdatePosition()
        {
            if (healthCanvas != null && playerCamera != null)
            {
                // Make health bar face camera
                Vector3 lookDirection = playerCamera.transform.position - transform.position;
                lookDirection.y = 0; // Keep it horizontal
                
                if (lookDirection != Vector3.zero)
                {
                    healthCanvas.transform.rotation = Quaternion.LookRotation(-lookDirection);
                }
                
                // Update position
                healthCanvas.transform.position = transform.position + offset;
            }
        }
        
        private void UpdateHealthBar()
        {
            if (unitController == null) return;
            
            float healthRatio = currentHealth / maxHealth;
            
            // Update fill amount
            if (healthBarFill != null)
            {
                healthBarFill.fillAmount = healthRatio;
                
                // Update color based on health ratio
                if (healthRatio <= criticalHealthThreshold)
                {
                    healthBarFill.color = criticalHealthColor;
                }
                else if (healthRatio <= lowHealthThreshold)
                {
                    healthBarFill.color = Color.Lerp(lowHealthColor, fullHealthColor, 
                        (healthRatio - criticalHealthThreshold) / (lowHealthThreshold - criticalHealthThreshold));
                }
                else
                {
                    healthBarFill.color = Color.Lerp(lowHealthColor, fullHealthColor, 
                        (healthRatio - lowHealthThreshold) / (1f - lowHealthThreshold));
                }
            }
            
            // Update text
            if (healthText != null)
            {
                healthText.text = $"{currentHealth:F0}/{maxHealth:F0}";
            }
        }
        
        private void OnHealthChanged(RTS.UnitController unit, float newHealth)
        {
            currentHealth = newHealth;
            UpdateHealthBar();
        }
        
        private void OnUnitDestroyed(RTS.UnitController unit)
        {
            // Hide health bar when unit is destroyed
            if (healthCanvas != null)
            {
                healthCanvas.gameObject.SetActive(false);
            }
        }
        
        public void SetHealthBarSettings(bool alwaysVisible, bool onlyWhenDamaged, float distance)
        {
            alwaysShow = alwaysVisible;
            showOnlyWhenDamaged = onlyWhenDamaged;
            showDistance = distance;
        }
        
        public void SetColors(Color full, Color low, Color critical)
        {
            fullHealthColor = full;
            lowHealthColor = low;
            criticalHealthColor = critical;
            UpdateHealthBar();
        }
        
        public void SetThresholds(float lowThreshold, float criticalThreshold)
        {
            lowHealthThreshold = Mathf.Clamp01(lowThreshold);
            criticalHealthThreshold = Mathf.Clamp01(criticalThreshold);
            UpdateHealthBar();
        }
    }
}