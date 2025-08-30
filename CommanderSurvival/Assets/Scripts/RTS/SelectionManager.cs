using System.Collections.Generic;
using UnityEngine;

namespace RTSPrototype.RTS
{
    /// <summary>
    /// Manages unit selection and selection visuals
    /// </summary>
    public class SelectionManager : MonoBehaviour
    {
        [Header("Selection Settings")]
        [SerializeField] private LayerMask selectableLayer = 1 << 6;
        [SerializeField] private Material selectionMaterial;
        [SerializeField] private GameObject selectionBoxPrefab;
        
        public static SelectionManager Instance { get; private set; }
        
        public List<UnitController> SelectedUnits { get; private set; } = new List<UnitController>();
        
        // Selection events
        public System.Action<List<UnitController>> OnSelectionChanged;
        
        private GameObject selectionBox;
        private Vector3 selectionStartPos;
        private bool isSelecting = false;
        
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
            // Subscribe to input events
            if (Core.Input.Instance != null)
            {
                Core.Input.Instance.OnLeftClick += HandleLeftClick;
                Core.Input.Instance.OnDragStart += HandleDragStart;
                Core.Input.Instance.OnDragEnd += HandleDragEnd;
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from input events
            if (Core.Input.Instance != null)
            {
                Core.Input.Instance.OnLeftClick -= HandleLeftClick;
                Core.Input.Instance.OnDragStart -= HandleDragStart;
                Core.Input.Instance.OnDragEnd -= HandleDragEnd;
            }
        }
        
        private void HandleLeftClick(Vector3 worldPosition)
        {
            if (!UnityEngine.Input.GetKey(KeyCode.LeftShift))
            {
                ClearSelection();
            }
            
            // Try to select unit at click position
            UnitController unit = GetUnitAtPosition(worldPosition);
            if (unit != null)
            {
                AddToSelection(unit);
            }
        }
        
        private void HandleDragStart(Vector3 startPos, Vector3 endPos)
        {
            selectionStartPos = startPos;
            isSelecting = true;
            
            if (selectionBox == null && selectionBoxPrefab != null)
            {
                selectionBox = Instantiate(selectionBoxPrefab);
            }
        }
        
        private void HandleDragEnd(Vector3 startPos, Vector3 endPos)
        {
            if (isSelecting)
            {
                SelectUnitsInArea(startPos, endPos);
                isSelecting = false;
                
                if (selectionBox != null)
                {
                    selectionBox.SetActive(false);
                }
            }
        }
        
        private UnitController GetUnitAtPosition(Vector3 position)
        {
            Collider[] colliders = Physics.OverlapSphere(position, 1f, selectableLayer);
            
            foreach (var collider in colliders)
            {
                UnitController unit = collider.GetComponent<UnitController>();
                if (unit != null)
                {
                    return unit;
                }
            }
            
            return null;
        }
        
        private void SelectUnitsInArea(Vector3 startPos, Vector3 endPos)
        {
            if (!UnityEngine.Input.GetKey(KeyCode.LeftShift))
            {
                ClearSelection();
            }
            
            // Create bounds for selection area
            Bounds selectionBounds = new Bounds();
            selectionBounds.SetMinMax(
                new Vector3(Mathf.Min(startPos.x, endPos.x), -100f, Mathf.Min(startPos.z, endPos.z)),
                new Vector3(Mathf.Max(startPos.x, endPos.x), 100f, Mathf.Max(startPos.z, endPos.z))
            );
            
            // Find all units in selection area
            UnitController[] allUnits = FindObjectsOfType<UnitController>();
            foreach (var unit in allUnits)
            {
                if (selectionBounds.Contains(unit.transform.position))
                {
                    AddToSelection(unit);
                }
            }
        }
        
        public void AddToSelection(UnitController unit)
        {
            if (unit != null && !SelectedUnits.Contains(unit))
            {
                SelectedUnits.Add(unit);
                unit.SetSelected(true);
                OnSelectionChanged?.Invoke(SelectedUnits);
            }
        }
        
        public void RemoveFromSelection(UnitController unit)
        {
            if (unit != null && SelectedUnits.Contains(unit))
            {
                SelectedUnits.Remove(unit);
                unit.SetSelected(false);
                OnSelectionChanged?.Invoke(SelectedUnits);
            }
        }
        
        public void ClearSelection()
        {
            foreach (var unit in SelectedUnits)
            {
                if (unit != null)
                {
                    unit.SetSelected(false);
                }
            }
            
            SelectedUnits.Clear();
            OnSelectionChanged?.Invoke(SelectedUnits);
        }
        
        public bool HasSelection()
        {
            return SelectedUnits.Count > 0;
        }
    }
}