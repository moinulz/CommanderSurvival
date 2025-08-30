using UnityEngine;

namespace RTSPrototype.RTS
{
    /// <summary>
    /// Handles projectile movement and collision
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField] private float speed = 20f;
        [SerializeField] private float damage = 10f;
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private bool isHoming = false;
        [SerializeField] private float homingStrength = 2f;
        [SerializeField] private LayerMask targetLayers = -1;
        
        [Header("Visual Effects")]
        [SerializeField] private GameObject hitEffect;
        [SerializeField] private GameObject trailEffect;
        [SerializeField] private bool destroyOnHit = true;
        
        public Transform Target { get; private set; }
        public UnitController Shooter { get; private set; }
        
        private Vector3 targetPosition;
        private Rigidbody projectileRigidbody;
        private float spawnTime;
        private bool hasHit = false;
        
        // Projectile events
        public System.Action<Projectile, Collider> OnHit;
        public System.Action<Projectile> OnDestroyed;
        
        private void Awake()
        {
            projectileRigidbody = GetComponent<Rigidbody>();
            spawnTime = Time.time;
        }
        
        private void Start()
        {
            if (trailEffect != null)
            {
                trailEffect.SetActive(true);
            }
        }
        
        private void Update()
        {
            UpdateMovement();
            CheckLifetime();
        }
        
        private void UpdateMovement()
        {
            if (hasHit) return;
            
            Vector3 direction;
            
            if (isHoming && Target != null)
            {
                // Homing projectile
                direction = (Target.position - transform.position).normalized;
                targetPosition = Target.position;
            }
            else
            {
                // Straight projectile
                direction = (targetPosition - transform.position).normalized;
            }
            
            // Move projectile
            if (projectileRigidbody != null)
            {
                projectileRigidbody.velocity = direction * speed;
            }
            else
            {
                transform.position += direction * speed * Time.deltaTime;
            }
            
            // Rotate to face movement direction
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
            
            // Check if we've reached close enough to target
            if (Vector3.Distance(transform.position, targetPosition) < 0.5f)
            {
                HitTarget();
            }
        }
        
        private void CheckLifetime()
        {
            if (Time.time - spawnTime >= lifetime)
            {
                DestroyProjectile();
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;
            
            // Check if we hit a valid target
            if (IsValidTarget(other))
            {
                ProcessHit(other);
            }
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            if (hasHit) return;
            
            // Check if we hit a valid target
            if (IsValidTarget(collision.collider))
            {
                ProcessHit(collision.collider);
            }
        }
        
        private bool IsValidTarget(Collider collider)
        {
            // Don't hit the shooter
            if (Shooter != null && collider.transform == Shooter.transform)
            {
                return false;
            }
            
            // Check layer mask
            if ((targetLayers.value & (1 << collider.gameObject.layer)) == 0)
            {
                return false;
            }
            
            return true;
        }
        
        private void ProcessHit(Collider hitCollider)
        {
            hasHit = true;
            
            // Apply damage if target has a UnitController
            UnitController targetUnit = hitCollider.GetComponent<UnitController>();
            if (targetUnit != null && targetUnit != Shooter)
            {
                targetUnit.TakeDamage(damage);
                Debug.Log($"Projectile hit {targetUnit.name} for {damage} damage");
            }
            
            // Trigger hit event
            OnHit?.Invoke(this, hitCollider);
            
            // Spawn hit effect
            if (hitEffect != null)
            {
                GameObject effect = Instantiate(hitEffect, transform.position, transform.rotation);
                Destroy(effect, 2f);
            }
            
            // Destroy projectile
            if (destroyOnHit)
            {
                DestroyProjectile();
            }
        }
        
        private void HitTarget()
        {
            if (Target != null)
            {
                ProcessHit(Target.GetComponent<Collider>());
            }
            else
            {
                DestroyProjectile();
            }
        }
        
        public void Initialize(Vector3 target, UnitController shooter, float projectileDamage = -1)
        {
            targetPosition = target;
            Shooter = shooter;
            
            if (projectileDamage >= 0)
            {
                damage = projectileDamage;
            }
            
            // Face initial direction
            Vector3 direction = (target - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
        
        public void Initialize(Transform target, UnitController shooter, float projectileDamage = -1)
        {
            Target = target;
            targetPosition = target.position;
            Shooter = shooter;
            
            if (projectileDamage >= 0)
            {
                damage = projectileDamage;
            }
            
            // Face initial direction
            Vector3 direction = (target.position - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
        
        public void SetHoming(bool homing, float strength = 2f)
        {
            isHoming = homing;
            homingStrength = strength;
        }
        
        public void SetSpeed(float newSpeed)
        {
            speed = newSpeed;
        }
        
        public void SetDamage(float newDamage)
        {
            damage = newDamage;
        }
        
        private void DestroyProjectile()
        {
            OnDestroyed?.Invoke(this);
            Destroy(gameObject);
        }
        
        private void OnDrawGizmos()
        {
            // Draw target line in editor
            if (Target != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, Target.position);
            }
            else
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, targetPosition);
            }
        }
    }
}