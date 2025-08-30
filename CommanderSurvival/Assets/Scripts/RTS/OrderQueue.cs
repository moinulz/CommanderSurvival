using System.Collections.Generic;
using UnityEngine;

namespace RTSPrototype.RTS
{
    /// <summary>
    /// Manages and queues orders for units
    /// </summary>
    public class OrderQueue : MonoBehaviour
    {
        [System.Serializable]
        public class Order
        {
            public OrderType type;
            public Vector3 targetPosition;
            public Transform targetUnit;
            public float priority = 1f;
            public bool isComplete = false;
            
            public Order(OrderType orderType, Vector3 position)
            {
                type = orderType;
                targetPosition = position;
            }
            
            public Order(OrderType orderType, Transform target)
            {
                type = orderType;
                targetUnit = target;
                targetPosition = target.position;
            }
        }
        
        public enum OrderType
        {
            Move,
            Attack,
            Patrol,
            Hold,
            Stop
        }
        
        [Header("Order Settings")]
        [SerializeField] private int maxQueueSize = 10;
        [SerializeField] private bool showOrderQueue = true;
        
        public static OrderQueue Instance { get; private set; }
        
        private Queue<Order> globalOrderQueue = new Queue<Order>();
        
        // Order events
        public System.Action<Order> OnOrderIssued;
        public System.Action<Order> OnOrderCompleted;
        
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
                Core.Input.Instance.OnRightClick += HandleRightClick;
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from input events
            if (Core.Input.Instance != null)
            {
                Core.Input.Instance.OnRightClick -= HandleRightClick;
            }
        }
        
        private void HandleRightClick(Vector3 worldPosition)
        {
            if (SelectionManager.Instance != null && SelectionManager.Instance.HasSelection())
            {
                // Check if clicking on an enemy unit for attack order
                Collider[] colliders = Physics.OverlapSphere(worldPosition, 1f);
                Transform targetUnit = null;
                
                foreach (var collider in colliders)
                {
                    UnitController unit = collider.GetComponent<UnitController>();
                    if (unit != null && !SelectionManager.Instance.SelectedUnits.Contains(unit))
                    {
                        targetUnit = unit.transform;
                        break;
                    }
                }
                
                OrderType orderType = targetUnit != null ? OrderType.Attack : OrderType.Move;
                
                if (targetUnit != null)
                {
                    IssueOrderToSelectedUnits(new Order(orderType, targetUnit));
                }
                else
                {
                    IssueOrderToSelectedUnits(new Order(orderType, worldPosition));
                }
            }
        }
        
        public void IssueOrderToSelectedUnits(Order order)
        {
            if (SelectionManager.Instance == null) return;
            
            foreach (var unit in SelectionManager.Instance.SelectedUnits)
            {
                if (unit != null)
                {
                    IssueOrderToUnit(unit, order);
                }
            }
        }
        
        public void IssueOrderToUnit(UnitController unit, Order order)
        {
            if (unit == null || order == null) return;
            
            bool queueOrder = UnityEngine.Input.GetKey(KeyCode.LeftShift);
            
            if (!queueOrder)
            {
                unit.ClearOrders();
            }
            
            unit.AddOrder(order);
            OnOrderIssued?.Invoke(order);
            
            Debug.Log($"Order issued to {unit.name}: {order.type} at {order.targetPosition}");
        }
        
        public void AddToGlobalQueue(Order order)
        {
            if (globalOrderQueue.Count >= maxQueueSize)
            {
                globalOrderQueue.Dequeue(); // Remove oldest order
            }
            
            globalOrderQueue.Enqueue(order);
        }
        
        public Order GetNextGlobalOrder()
        {
            if (globalOrderQueue.Count > 0)
            {
                return globalOrderQueue.Dequeue();
            }
            
            return null;
        }
        
        public void ClearGlobalQueue()
        {
            globalOrderQueue.Clear();
        }
        
        public void CompleteOrder(Order order)
        {
            if (order != null)
            {
                order.isComplete = true;
                OnOrderCompleted?.Invoke(order);
            }
        }
        
        public void IssueStopOrder()
        {
            IssueOrderToSelectedUnits(new Order(OrderType.Stop, Vector3.zero));
        }
        
        public void IssueHoldOrder()
        {
            IssueOrderToSelectedUnits(new Order(OrderType.Hold, Vector3.zero));
        }
        
        public void IssuePatrolOrder(Vector3 startPos, Vector3 endPos)
        {
            // Create patrol order between two points
            Order patrolOrder = new Order(OrderType.Patrol, startPos);
            IssueOrderToSelectedUnits(patrolOrder);
        }
        
        private void OnDrawGizmos()
        {
            if (!showOrderQueue) return;
            
            // Visualize global order queue
            int index = 0;
            foreach (var order in globalOrderQueue)
            {
                Gizmos.color = GetOrderColor(order.type);
                Gizmos.DrawWireSphere(order.targetPosition + Vector3.up * index * 0.5f, 0.5f);
                index++;
            }
        }
        
        private Color GetOrderColor(OrderType orderType)
        {
            switch (orderType)
            {
                case OrderType.Move: return Color.green;
                case OrderType.Attack: return Color.red;
                case OrderType.Patrol: return Color.yellow;
                case OrderType.Hold: return Color.blue;
                case OrderType.Stop: return Color.gray;
                default: return Color.white;
            }
        }
    }
}