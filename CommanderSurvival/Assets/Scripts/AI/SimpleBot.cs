using UnityEngine;

namespace RTSPrototype.AI
{
    /// <summary>
    /// Simple AI behavior for automated unit control
    /// </summary>
    public class SimpleBot : MonoBehaviour
    {
        [Header("AI Settings")]
        [SerializeField] private AIState initialState = AIState.Idle;
        [SerializeField] private float thinkInterval = 1f;
        [SerializeField] private float aggroRange = 10f;
        [SerializeField] private float patrolRadius = 15f;
        [SerializeField] private bool debugMode = false;
        
        [Header("Behavior Settings")]
        [SerializeField] private float fleeHealthThreshold = 0.3f;
        [SerializeField] private float pursuitDistance = 20f;
        [SerializeField] private float returnToPatrolDistance = 25f;
        
        public enum AIState
        {
            Idle,
            Patrol,
            Attack,
            Pursue,
            Flee,
            ReturnToPatrol
        }
        
        public AIState CurrentState { get; private set; }
        
        private RTS.UnitController unitController;
        private Targeting targeting;
        private Vector3 patrolCenter;
        private Vector3 currentPatrolTarget;
        private float lastThinkTime;
        private float lastStateChange;
        
        // AI events
        public System.Action<AIState, AIState> OnStateChanged;
        
        private void Awake()
        {
            unitController = GetComponent<RTS.UnitController>();
            targeting = GetComponent<Targeting>();
            
            if (targeting == null)
            {
                targeting = gameObject.AddComponent<Targeting>();
            }
        }
        
        private void Start()
        {
            patrolCenter = transform.position;
            currentPatrolTarget = GetRandomPatrolPoint();
            
            ChangeState(initialState);
            
            // Subscribe to events
            if (targeting != null)
            {
                targeting.OnTargetAcquired += OnTargetAcquired;
                targeting.OnTargetLost += OnTargetLost;
            }
            
            if (unitController != null)
            {
                unitController.OnHealthChanged += OnHealthChanged;
            }
        }
        
        private void Update()
        {
            if (Time.time >= lastThinkTime + thinkInterval)
            {
                Think();
                lastThinkTime = Time.time;
            }
            
            ExecuteCurrentState();
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (targeting != null)
            {
                targeting.OnTargetAcquired -= OnTargetAcquired;
                targeting.OnTargetLost -= OnTargetLost;
            }
            
            if (unitController != null)
            {
                unitController.OnHealthChanged -= OnHealthChanged;
            }
        }
        
        private void Think()
        {
            if (unitController == null || !unitController.IsAlive) return;
            
            AIState newState = CurrentState;
            
            // Check for flee condition
            if (ShouldFlee())
            {
                newState = AIState.Flee;
            }
            // Check for attack condition
            else if (targeting.CurrentTarget != null && ShouldAttack())
            {
                newState = AIState.Attack;
            }
            // Check for pursuit condition
            else if (targeting.CurrentTarget != null && ShouldPursue())
            {
                newState = AIState.Pursue;
            }
            // Check if we should return to patrol
            else if (ShouldReturnToPatrol())
            {
                newState = AIState.ReturnToPatrol;
            }
            // Default to patrol if no other conditions
            else if (CurrentState == AIState.Idle)
            {
                newState = AIState.Patrol;
            }
            
            if (newState != CurrentState)
            {
                ChangeState(newState);
            }
        }
        
        private void ExecuteCurrentState()
        {
            switch (CurrentState)
            {
                case AIState.Idle:
                    ExecuteIdle();
                    break;
                case AIState.Patrol:
                    ExecutePatrol();
                    break;
                case AIState.Attack:
                    ExecuteAttack();
                    break;
                case AIState.Pursue:
                    ExecutePursue();
                    break;
                case AIState.Flee:
                    ExecuteFlee();
                    break;
                case AIState.ReturnToPatrol:
                    ExecuteReturnToPatrol();
                    break;
            }
        }
        
        private void ExecuteIdle()
        {
            // Do nothing, wait for conditions to change
        }
        
        private void ExecutePatrol()
        {
            if (Vector3.Distance(transform.position, currentPatrolTarget) < 2f)
            {
                currentPatrolTarget = GetRandomPatrolPoint();
            }
            
            unitController.SetDestination(currentPatrolTarget);
        }
        
        private void ExecuteAttack()
        {
            if (targeting.CurrentTarget != null)
            {
                unitController.AttackTarget(targeting.CurrentTarget);
            }
            else
            {
                ChangeState(AIState.Patrol);
            }
        }
        
        private void ExecutePursue()
        {
            if (targeting.CurrentTarget != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, targeting.CurrentTarget.position);
                
                if (distanceToTarget <= unitController.GetComponent<RTS.UnitController>() != null ? 5f : aggroRange)
                {
                    ChangeState(AIState.Attack);
                }
                else if (distanceToTarget > pursuitDistance)
                {
                    ChangeState(AIState.ReturnToPatrol);
                }
                else
                {
                    unitController.SetDestination(targeting.CurrentTarget.position);
                }
            }
            else
            {
                ChangeState(AIState.ReturnToPatrol);
            }
        }
        
        private void ExecuteFlee()
        {
            Vector3 fleeDirection = Vector3.zero;
            
            if (targeting.CurrentTarget != null)
            {
                fleeDirection = (transform.position - targeting.CurrentTarget.position).normalized;
            }
            else
            {
                fleeDirection = (transform.position - patrolCenter).normalized;
            }
            
            Vector3 fleeDestination = transform.position + fleeDirection * 10f;
            unitController.SetDestination(fleeDestination);
            
            // Switch to return to patrol after fleeing for a bit
            if (Time.time - lastStateChange > 3f)
            {
                ChangeState(AIState.ReturnToPatrol);
            }
        }
        
        private void ExecuteReturnToPatrol()
        {
            float distanceToCenter = Vector3.Distance(transform.position, patrolCenter);
            
            if (distanceToCenter > patrolRadius)
            {
                unitController.SetDestination(patrolCenter);
            }
            else
            {
                ChangeState(AIState.Patrol);
            }
        }
        
        private bool ShouldFlee()
        {
            if (unitController == null) return false;
            
            float healthRatio = unitController.Health / unitController.MaxHealth;
            return healthRatio <= fleeHealthThreshold && targeting.CurrentTarget != null;
        }
        
        private bool ShouldAttack()
        {
            if (targeting.CurrentTarget == null) return false;
            
            float distanceToTarget = Vector3.Distance(transform.position, targeting.CurrentTarget.position);
            return distanceToTarget <= aggroRange;
        }
        
        private bool ShouldPursue()
        {
            if (targeting.CurrentTarget == null) return false;
            
            float distanceToTarget = Vector3.Distance(transform.position, targeting.CurrentTarget.position);
            return distanceToTarget > aggroRange && distanceToTarget <= pursuitDistance;
        }
        
        private bool ShouldReturnToPatrol()
        {
            if (targeting.CurrentTarget != null) return false;
            
            float distanceToCenter = Vector3.Distance(transform.position, patrolCenter);
            return distanceToCenter > returnToPatrolDistance;
        }
        
        private Vector3 GetRandomPatrolPoint()
        {
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            Vector3 randomPoint = patrolCenter + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            // TODO: Add NavMesh sampling to ensure valid patrol points
            
            return randomPoint;
        }
        
        private void ChangeState(AIState newState)
        {
            AIState previousState = CurrentState;
            CurrentState = newState;
            lastStateChange = Time.time;
            
            OnStateChanged?.Invoke(previousState, newState);
            
            if (debugMode)
            {
                Debug.Log($"{name}: State changed from {previousState} to {newState}");
            }
        }
        
        private void OnTargetAcquired(Transform target)
        {
            if (debugMode)
            {
                Debug.Log($"{name}: Target acquired - {target.name}");
            }
        }
        
        private void OnTargetLost(Transform target)
        {
            if (debugMode)
            {
                Debug.Log($"{name}: Target lost - {target.name}");
            }
        }
        
        private void OnHealthChanged(RTS.UnitController unit, float newHealth)
        {
            // React to health changes
            if (newHealth / unit.MaxHealth <= fleeHealthThreshold)
            {
                if (CurrentState != AIState.Flee)
                {
                    ChangeState(AIState.Flee);
                }
            }
        }
        
        public void SetPatrolCenter(Vector3 center)
        {
            patrolCenter = center;
            currentPatrolTarget = GetRandomPatrolPoint();
        }
        
        public void SetAggroRange(float range)
        {
            aggroRange = Mathf.Max(0, range);
        }
        
        public void SetPatrolRadius(float radius)
        {
            patrolRadius = Mathf.Max(1, radius);
        }
        
        public void ForceState(AIState state)
        {
            ChangeState(state);
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw patrol center and radius
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(patrolCenter, patrolRadius);
            
            // Draw aggro range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, aggroRange);
            
            // Draw current patrol target
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(currentPatrolTarget, 1f);
            Gizmos.DrawLine(transform.position, currentPatrolTarget);
            
            // Draw state info
            if (debugMode)
            {
                Vector3 textPos = transform.position + Vector3.up * 3f;
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(textPos, CurrentState.ToString());
                #endif
            }
        }
    }
}