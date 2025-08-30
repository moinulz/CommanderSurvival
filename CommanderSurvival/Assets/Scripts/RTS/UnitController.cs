using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace RTSPrototype.RTS
{
    /// <summary>
    /// Controls individual unit behavior and movement
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class UnitController : MonoBehaviour
    {
        [Header("Unit Stats")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private float attackDamage = 20f;
        [SerializeField] private float attackRange = 5f;
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private float movementSpeed = 5f;
        
        [Header("Visual Settings")]
        [SerializeField] private GameObject selectionIndicator;
        [SerializeField] private Renderer unitRenderer;
        [SerializeField] private Material selectedMaterial;
        [SerializeField] private Material defaultMaterial;
        
        public bool IsSelected { get; private set; } = false;
        public bool IsAlive { get; private set; } = true;
        public float Health => currentHealth;
        public float MaxHealth => maxHealth;
        
        private NavMeshAgent navAgent;
        private Queue<OrderQueue.Order> orderQueue = new Queue<OrderQueue.Order>();
        private OrderQueue.Order currentOrder;
        private float lastAttackTime;
        private Transform currentTarget;
        
        // Unit events
        public System.Action<UnitController> OnUnitDestroyed;
        public System.Action<UnitController, float> OnHealthChanged;
        
        private void Awake()
        {
            navAgent = GetComponent<NavMeshAgent>();
            navAgent.speed = movementSpeed;
            
            currentHealth = maxHealth;
            
            if (unitRenderer == null)
            {
                unitRenderer = GetComponent<Renderer>();
            }
        }
        
        private void Start()
        {
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(false);
            }
        }
        
        private void Update()
        {
            if (!IsAlive) return;
            
            ProcessCurrentOrder();
            UpdateMovement();
        }
        
        private void ProcessCurrentOrder()
        {
            // Get next order if current is complete or null
            if (currentOrder == null || currentOrder.isComplete)
            {
                if (orderQueue.Count > 0)
                {
                    currentOrder = orderQueue.Dequeue();
                }
                else
                {
                    currentOrder = null;
                    return;
                }
            }
            
            // Execute current order
            switch (currentOrder.type)
            {
                case OrderQueue.OrderType.Move:
                    ExecuteMoveOrder();
                    break;
                case OrderQueue.OrderType.Attack:
                    ExecuteAttackOrder();
                    break;
                case OrderQueue.OrderType.Patrol:
                    ExecutePatrolOrder();
                    break;
                case OrderQueue.OrderType.Hold:
                    ExecuteHoldOrder();
                    break;
                case OrderQueue.OrderType.Stop:
                    ExecuteStopOrder();
                    break;
            }
        }
        
        private void ExecuteMoveOrder()
        {
            navAgent.SetDestination(currentOrder.targetPosition);
            
            if (Vector3.Distance(transform.position, currentOrder.targetPosition) < 1f)
            {
                CompleteCurrentOrder();
            }
        }
        
        private void ExecuteAttackOrder()
        {
            if (currentOrder.targetUnit == null)
            {
                CompleteCurrentOrder();
                return;
            }
            
            float distanceToTarget = Vector3.Distance(transform.position, currentOrder.targetUnit.position);
            
            if (distanceToTarget > attackRange)
            {
                // Move closer to target
                navAgent.SetDestination(currentOrder.targetUnit.position);
            }
            else
            {
                // Stop and attack
                navAgent.SetDestination(transform.position);
                
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    PerformAttack(currentOrder.targetUnit);
                    lastAttackTime = Time.time;
                }
            }
        }
        
        private void ExecutePatrolOrder()
        {
            // Simple patrol implementation - move to target position
            navAgent.SetDestination(currentOrder.targetPosition);
            
            if (Vector3.Distance(transform.position, currentOrder.targetPosition) < 1f)
            {
                CompleteCurrentOrder();
            }
        }
        
        private void ExecuteHoldOrder()
        {
            navAgent.SetDestination(transform.position);
            CompleteCurrentOrder();
        }
        
        private void ExecuteStopOrder()
        {
            navAgent.SetDestination(transform.position);
            ClearOrders();
            CompleteCurrentOrder();
        }
        
        private void UpdateMovement()
        {
            // Update any movement-related logic
            if (navAgent.velocity.magnitude > 0.1f)
            {
                // Unit is moving
                transform.LookAt(transform.position + navAgent.velocity.normalized);
            }
        }
        
        private void PerformAttack(Transform target)
        {
            UnitController targetUnit = target.GetComponent<UnitController>();
            if (targetUnit != null)
            {
                targetUnit.TakeDamage(attackDamage);
                Debug.Log($"{name} attacks {target.name} for {attackDamage} damage");
            }
        }
        
        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;
            
            currentHealth = Mathf.Max(0, currentHealth - damage);
            OnHealthChanged?.Invoke(this, currentHealth);
            
            if (currentHealth <= 0)
            {
                Die();
            }
        }
        
        public void Heal(float amount)
        {
            if (!IsAlive) return;
            
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(this, currentHealth);
        }
        
        private void Die()
        {
            IsAlive = false;
            OnUnitDestroyed?.Invoke(this);
            
            // Remove from selection if selected
            if (IsSelected && SelectionManager.Instance != null)
            {
                SelectionManager.Instance.RemoveFromSelection(this);
            }
            
            // Disable components
            navAgent.enabled = false;
            
            // TODO: Play death animation, drop loot, etc.
            
            Destroy(gameObject, 2f); // Destroy after 2 seconds
        }
        
        public void AddOrder(OrderQueue.Order order)
        {
            orderQueue.Enqueue(order);
        }
        
        public void ClearOrders()
        {
            orderQueue.Clear();
            currentOrder = null;
        }
        
        private void CompleteCurrentOrder()
        {
            if (currentOrder != null)
            {
                OrderQueue.Instance?.CompleteOrder(currentOrder);
                currentOrder = null;
            }
        }
        
        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(selected);
            }
            
            if (unitRenderer != null)
            {
                unitRenderer.material = selected ? selectedMaterial : defaultMaterial;
            }
        }
        
        public void SetDestination(Vector3 destination)
        {
            ClearOrders();
            AddOrder(new OrderQueue.Order(OrderQueue.OrderType.Move, destination));
        }
        
        public void AttackTarget(Transform target)
        {
            ClearOrders();
            AddOrder(new OrderQueue.Order(OrderQueue.OrderType.Attack, target));
        }
    }
}