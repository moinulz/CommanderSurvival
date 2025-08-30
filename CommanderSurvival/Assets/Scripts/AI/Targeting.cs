using System.Collections.Generic;
using UnityEngine;

namespace RTSPrototype.AI
{
    /// <summary>
    /// Handles target acquisition and prioritization for AI units
    /// </summary>
    public class Targeting : MonoBehaviour
    {
        [Header("Targeting Settings")]
        [SerializeField] private float detectionRange = 15f;
        [SerializeField] private float targetUpdateInterval = 0.5f;
        [SerializeField] private LayerMask enemyLayers = 1 << 6;
        [SerializeField] private bool autoTarget = true;
        
        [Header("Target Priorities")]
        [SerializeField] private List<TargetPriority> targetPriorities = new List<TargetPriority>();
        
        [System.Serializable]
        public class TargetPriority
        {
            public string targetTag = "Unit";
            public float priority = 1f;
            public float bonusRange = 0f; // Additional range for this target type
        }
        
        public Transform CurrentTarget { get; private set; }
        public List<Transform> PotentialTargets { get; private set; } = new List<Transform>();
        
        private RTS.UnitController unitController;
        private float lastTargetUpdate;
        private Collider[] detectionBuffer = new Collider[50];
        
        // Targeting events
        public System.Action<Transform> OnTargetAcquired;
        public System.Action<Transform> OnTargetLost;
        public System.Action<List<Transform>> OnPotentialTargetsUpdated;
        
        private void Awake()
        {
            unitController = GetComponent<RTS.UnitController>();
        }
        
        private void Start()
        {
            if (targetPriorities.Count == 0)
            {
                // Add default target priorities
                targetPriorities.Add(new TargetPriority { targetTag = "Unit", priority = 1f });
                targetPriorities.Add(new TargetPriority { targetTag = "Building", priority = 0.5f });
            }
        }
        
        private void Update()
        {
            if (autoTarget && Time.time >= lastTargetUpdate + targetUpdateInterval)
            {
                UpdateTargeting();
                lastTargetUpdate = Time.time;
            }
        }
        
        private void UpdateTargeting()
        {
            UpdatePotentialTargets();
            UpdateCurrentTarget();
        }
        
        private void UpdatePotentialTargets()
        {
            PotentialTargets.Clear();
            
            // Use OverlapSphereNonAlloc for better performance
            int numColliders = Physics.OverlapSphereNonAlloc(
                transform.position, 
                detectionRange, 
                detectionBuffer, 
                enemyLayers
            );
            
            for (int i = 0; i < numColliders; i++)
            {
                Transform target = detectionBuffer[i].transform;
                
                // Skip self
                if (target == transform) continue;
                
                // Check if target is valid
                if (IsValidTarget(target))
                {
                    PotentialTargets.Add(target);
                }
            }
            
            OnPotentialTargetsUpdated?.Invoke(PotentialTargets);
        }
        
        private void UpdateCurrentTarget()
        {
            Transform previousTarget = CurrentTarget;
            
            // Check if current target is still valid
            if (CurrentTarget != null && !IsTargetValid(CurrentTarget))
            {
                LoseTarget();
            }
            
            // Find best target if we don't have one
            if (CurrentTarget == null && PotentialTargets.Count > 0)
            {
                CurrentTarget = FindBestTarget();
                
                if (CurrentTarget != null)
                {
                    OnTargetAcquired?.Invoke(CurrentTarget);
                }
            }
            
            // Check for better targets
            else if (CurrentTarget != null && PotentialTargets.Count > 1)
            {
                Transform betterTarget = FindBestTarget();
                if (betterTarget != CurrentTarget && GetTargetScore(betterTarget) > GetTargetScore(CurrentTarget) * 1.2f)
                {
                    // Switch to better target (with 20% threshold to prevent constant switching)
                    LoseTarget();
                    CurrentTarget = betterTarget;
                    OnTargetAcquired?.Invoke(CurrentTarget);
                }
            }
        }
        
        private Transform FindBestTarget()
        {
            if (PotentialTargets.Count == 0) return null;
            
            Transform bestTarget = null;
            float bestScore = 0f;
            
            foreach (Transform target in PotentialTargets)
            {
                if (!IsTargetValid(target)) continue;
                
                float score = GetTargetScore(target);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = target;
                }
            }
            
            return bestTarget;
        }
        
        private float GetTargetScore(Transform target)
        {
            if (target == null) return 0f;
            
            float distance = Vector3.Distance(transform.position, target.position);
            float priority = GetTargetPriority(target);
            
            // Higher priority and closer targets get higher scores
            float distanceScore = Mathf.Max(0, detectionRange - distance) / detectionRange;
            float score = priority * distanceScore;
            
            // Bonus for damaged targets
            RTS.UnitController targetUnit = target.GetComponent<RTS.UnitController>();
            if (targetUnit != null)
            {
                float healthRatio = targetUnit.Health / targetUnit.MaxHealth;
                score *= (1.5f - healthRatio * 0.5f); // Prefer damaged targets
            }
            
            return score;
        }
        
        private float GetTargetPriority(Transform target)
        {
            foreach (var priority in targetPriorities)
            {
                if (target.CompareTag(priority.targetTag))
                {
                    return priority.priority;
                }
            }
            
            return 0.1f; // Default low priority
        }
        
        private bool IsValidTarget(Transform target)
        {
            if (target == null || target == transform) return false;
            
            // Check if target is alive
            RTS.UnitController targetUnit = target.GetComponent<RTS.UnitController>();
            if (targetUnit != null && !targetUnit.IsAlive)
            {
                return false;
            }
            
            // Check line of sight
            if (!HasLineOfSight(target))
            {
                return false;
            }
            
            // Check if within range (with bonus range for specific target types)
            float maxRange = detectionRange;
            foreach (var priority in targetPriorities)
            {
                if (target.CompareTag(priority.targetTag))
                {
                    maxRange += priority.bonusRange;
                    break;
                }
            }
            
            return Vector3.Distance(transform.position, target.position) <= maxRange;
        }
        
        private bool IsTargetValid(Transform target)
        {
            return PotentialTargets.Contains(target) && IsValidTarget(target);
        }
        
        private bool HasLineOfSight(Transform target)
        {
            Vector3 direction = target.position - transform.position;
            Ray ray = new Ray(transform.position + Vector3.up * 0.5f, direction.normalized);
            
            if (Physics.Raycast(ray, out RaycastHit hit, direction.magnitude))
            {
                return hit.transform == target;
            }
            
            return true; // No obstruction found
        }
        
        public void ForceTarget(Transform target)
        {
            if (target != null && IsValidTarget(target))
            {
                LoseTarget();
                CurrentTarget = target;
                OnTargetAcquired?.Invoke(CurrentTarget);
            }
        }
        
        public void ClearTarget()
        {
            LoseTarget();
        }
        
        private void LoseTarget()
        {
            if (CurrentTarget != null)
            {
                Transform lostTarget = CurrentTarget;
                CurrentTarget = null;
                OnTargetLost?.Invoke(lostTarget);
            }
        }
        
        public void SetDetectionRange(float range)
        {
            detectionRange = Mathf.Max(0, range);
        }
        
        public void SetAutoTargeting(bool enabled)
        {
            autoTarget = enabled;
            if (!enabled)
            {
                ClearTarget();
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // Draw current target
            if (CurrentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, CurrentTarget.position);
                Gizmos.DrawWireSphere(CurrentTarget.position, 1f);
            }
            
            // Draw potential targets
            Gizmos.color = Color.orange;
            foreach (Transform target in PotentialTargets)
            {
                if (target != CurrentTarget)
                {
                    Gizmos.DrawWireSphere(target.position, 0.5f);
                }
            }
        }
    }
}