using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Entities.Interfaces;
using Entities.Misc.Input;
using UnityEngine;

namespace Entities
{
    [RequireComponent(typeof(EntityStats))]
    [RequireComponent(typeof(Rigidbody))]
    public abstract class EntityBase : MonoBehaviour, IEntityHostility<EntityBase>
    {
        //Events
        protected delegate void PursuitEventHandler<in TEntityBase>(TEntityBase entity);
        protected delegate EntityBase AttackEventHandler();

        protected AttackEventHandler onAttack;

        protected PursuitEventHandler<EntityBase> onPursuitStart;
        protected PursuitEventHandler<EntityBase> onPursuitEnd;
        
        protected PursuitEventHandler<EntityBase> onEscapeStart;
        protected PursuitEventHandler<EntityBase> onEscapeEnd;

        private Vector3 _escapeDirection;
        
        //Variables
        
        public static readonly Dictionary<Guid, EntityBase> EntityRegistry = new Dictionary<Guid, EntityBase>();


        public EntityBase attackTargetPublic;
        
        public enum HostilityLevel : short
        {
            Friendly = 0, // will not attack
            Neutral = 1,  // will attack if attacked
            Hostile = 2,   // will always attack on sight
        }

        public EntityBase AttackTarget { get; set; } = null;

        public EntityStats entityStats { get; private set; }
        private Rigidbody _rigidbody;

        //Methods

        protected virtual void Awake()
        {
             entityStats = GetComponent<EntityStats>();
             _rigidbody = GetComponent<Rigidbody>();

            if (!EntityRegistry.ContainsKey(entityStats.EntityID))
                EntityRegistry.Add(entityStats.EntityID, this);

            onAttack = () =>
            {
                if (entityStats.attackTimestamp + entityStats.attackCooldown > Time.time) return null;
                
                if (!AttackTarget.entityStats.attackedBy.Contains(this))
                    AttackTarget.entityStats.attackedBy.Add(this);
                
                entityStats.attackTimestamp = Time.time;
                return AttackTarget.TakeDamage(entityStats.attackDamage, this);
            };

            onPursuitStart += (e) =>
            {
                StopAllCoroutines();
                AttackTarget = e;
                StartCoroutine(Pursue());
            };

            onPursuitEnd += e =>
            {
                StopAllCoroutines();
                AttackTarget = null;
                StartCoroutine(Wander());
            };

            onEscapeStart += e =>
            {
                StopAllCoroutines();
                StartCoroutine(Escape(entityStats.attackedBy));
            };

            onEscapeEnd += e =>
            {
                StopAllCoroutines();
                if (!entityStats.attackedBy.Contains(this))
                    entityStats.attackedBy.Remove(e);
                StartCoroutine(Wander());
            };

        }

        [Header("Gizmos")]
        private Vector3 _gWanderEndPoint;
        private Vector3 _gEscapeDirection;

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            if (entityStats.hostilityLevel == HostilityLevel.Hostile)
            {
                Gizmos.color = AttackTarget ? Color.red : Color.white;

                if (AttackTarget)
                    Gizmos.DrawWireSphere(transform.position, entityStats.visibilityDistance * 1.5f);
                else
                    Gizmos.DrawWireSphere(transform.position, entityStats.visibilityDistance);
            }

            if (AttackTarget)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, AttackTarget.transform.position);
            }

            if (!AttackTarget && entityStats.attackedBy.Count <= 0 && _gWanderEndPoint != Vector3.zero)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, _gWanderEndPoint);
            }

            if (entityStats.attackedBy.Count > 0 && !AttackTarget)
            {
                Gizmos.color = Color.orange;
                Gizmos.DrawLine(transform.position, transform.position + _gEscapeDirection * 5f);
            }

            if (entityStats.attackedBy.Count == 0)
                _gEscapeDirection = Vector3.zero;
            
            if(AttackTarget || entityStats.attackedBy.Count > 0)
                _gWanderEndPoint = Vector3.zero;
        }

        private void Start()
        {
            StartCoroutine(Wander());
        }


        private void FixedUpdate() // the hierarchy of actions matters here
        {
            attackTargetPublic = AttackTarget;
            
            if(transform.position.y <= -20)
                Die();

            RegainStamina();
            NaturalHeal();

            entityStats.isMoving = false; // has to be the last line in Update
        }

        protected virtual void OnDestroy()
        {
            EntityRegistry.Remove(entityStats.EntityID);
        }

        // if all pursuit values are 'false' the entities will wander
        protected virtual IEnumerator Wander()
        {
            entityStats.isMoving = true;

            float pauseEndTime = 0;
            
            while (true)
            {
                if (UnityEngine.Random.Range(0, 2) == 1)
                    if (Time.time >= pauseEndTime)
                    {
                        var pauseDuration = UnityEngine.Random.Range(0.5f, 2f);
                        pauseEndTime = Time.time + pauseDuration;
                    }
                
                while (Time.time < pauseEndTime)
                {
                    yield return null; 
                }
                
                
                float walkDuration = UnityEngine.Random.Range(2f, 5f);
                float walkEndTime = Time.time + walkDuration;
                
                float randomAngle = UnityEngine.Random.Range(0f, 360f);
                Vector3 randomDirection = new Vector3(Mathf.Cos(randomAngle), 0, Mathf.Sin(randomAngle)).normalized;

                Vector3 walkPosition = transform.position + randomDirection * (entityStats.speed * walkDuration);
                _gWanderEndPoint = walkPosition;

                if (Physics.Raycast(transform.position, randomDirection, walkDuration*entityStats.speed, LayerMask.GetMask("Environment", "Obstacles")))
                    continue;
                
                while (Time.time < walkEndTime)
                {
                    RotateTowards(transform.position + randomDirection);

                    Vector3 movement = transform.forward * (entityStats.speed * Time.deltaTime);
                    if (_rigidbody)
                        _rigidbody.MovePosition(_rigidbody.position + movement);
                    yield return null;
                }
            }
        }

        private IEnumerator Pursue()
        {
            if (!AttackTarget)
            {
                Debug.LogAssertion("Pursued _attackTarget is null");
                yield break;
            }

            while (true)
            {
                float proximity = AttackTarget ? GetProximityToEntity(AttackTarget) : float.MaxValue;

                if(AttackTarget)
                    if (Physics.Raycast(transform.position, AttackTarget.transform.position - transform.position , out RaycastHit hit, GetProximityToEntity(AttackTarget), LayerMask.GetMask("Environment", "Obstacles")))
                    // if the environment is in the way, stop pursuit
                    {
                        Debug.DrawLine(transform.position, hit.point);
                        onPursuitEnd(AttackTarget);
                        yield break;
                    }
                
                if (proximity <= entityStats.visibilityDistance*1.5f)
                    RunToFrom(AttackTarget);
                else
                {
                    onPursuitEnd(AttackTarget);
                    yield break;
                }

                if (entityStats.hostilityLevel != HostilityLevel.Friendly && proximity <= entityStats.attackRange)
                {
                    EntityBase killedEntity = onAttack?.Invoke();
                    if (killedEntity)
                    {
                        entityStats.experience = killedEntity.entityStats.experience;
                        entityStats.entitiesKilledCount++;
                        onPursuitEnd?.Invoke(killedEntity);
                        onEscapeEnd?.Invoke(killedEntity);
                    }

                    yield return killedEntity;
                }

                yield return new WaitForFixedUpdate();
            }
        }

        public abstract void HandleAttack(EntityBase attackingEntity);

        protected virtual EntityBase TakeDamage(float damage, EntityBase receivedFromEntity = null)
        {
            float damageTaken = damage * (1f - entityStats.armor / 100f * 0.66f);
            entityStats.health -= damageTaken;

            entityStats.isLowHealth = entityStats.health <= entityStats.lowHealthThreshold;

            if (entityStats.health <= 0f)
                return Die(receivedFromEntity);
            
            if (receivedFromEntity)
                Knockback(receivedFromEntity);

            if (receivedFromEntity)
                HandleAttack(receivedFromEntity);

            entityStats.healthTimestamp = Time.time;

            return null;
        }

        private IEnumerator Escape(List<EntityBase> entities)
        {
            if (entities.Count == 0)
                onEscapeEnd(AttackTarget);

            while (true)
            {
                if (entities.Count == 0)
                    break;

                EntityBase entityEscaped = AreEntitiesInRange(entities);
                if (entityEscaped)
                    onEscapeEnd?.Invoke(entityEscaped);

                _escapeDirection = Vector3.zero;
                foreach (EntityBase entity in entities)
                {
                    Debug.DrawLine(transform.position, entity.transform.position, Color.darkRed);
                    _escapeDirection += (transform.position - entity.transform.position).normalized;
                }
                ObstacleDetection();
                
                
                Debug.DrawLine(transform.position, transform.position + _escapeDirection.normalized*5, Color.darkRed);

                if (!_escapeDirection.Equals(Vector3.zero)) // if there is an escape direction
                    RunInDirection(_escapeDirection);
                
                yield return new WaitForFixedUpdate();
            }
        }

        protected virtual EntityBase Die(EntityBase byEntity = null)
        {
            if (transform.childCount > 0) // if there is a camera{
            {
                InputController.Instance.SetNextEntity(); // not sure if the pos and rot will work
            }

            Destroy(gameObject);
            
            if (byEntity)
            {
                byEntity.AttackTarget = null;
                return this;
            }
            return null;
        }

        protected virtual float RunToFrom(EntityBase entity, bool toEntity = true) // Runs either toward the entity or away from it 
        {
            entityStats.isMoving = true;

            if (entityStats.isOutOfStamina) return WalkToFrom(entity, toEntity);

            Vector3 direction = toEntity ? 
                (entity.transform.position - transform.position).normalized : 
                (transform.position - entity.transform.position).normalized;
            if(!toEntity)
                _gEscapeDirection = direction;

            RotateTowards(transform.position + direction);
            Vector3 movement = transform.forward * (entityStats.runSpeed * Time.deltaTime);
            _rigidbody.MovePosition(_rigidbody.position + movement); 
            
            LoseStamina(2f);

            return movement.magnitude;
        }

        protected virtual float WalkToFrom(EntityBase entity, bool toEntity = true)
        {
            entityStats.isMoving = true;

            var direction = toEntity ? 
                (entity.transform.position - transform.position).normalized : 
                (transform.position - entity.transform.position).normalized;

            RotateTowards(transform.position + direction);

            Vector3 movement = transform.forward * (entityStats.speed * Time.deltaTime);
            _rigidbody.MovePosition(_rigidbody.position + movement);

            return movement.magnitude;
        }
        
        protected virtual float RunInDirection(Vector3 direction) // Runs either toward the entity or away from it 
        {
            if (entityStats.isOutOfStamina) return WalkInDirection(direction);
            
            entityStats.isMoving = true;

            RotateTowards(transform.position + direction); 

            Vector3 movement = transform.forward * (entityStats.runSpeed * Time.deltaTime);
            _rigidbody.MovePosition(_rigidbody.position + movement); // TODO: FIX ENTITIES GOING INTO WALLS
            if(entityStats.attackedBy.Count > 0) _gEscapeDirection = direction;

            LoseStamina(2f);

            return movement.magnitude;
        }

        protected virtual float WalkInDirection(Vector3 direction)
        {
            entityStats.isMoving = true;
            
            RotateTowards(transform.position + direction);

            Vector3 movement = transform.forward * (entityStats.speed * Time.deltaTime);
            _rigidbody.MovePosition(_rigidbody.position + movement); // TODO: FIX ENTITIES GOING INTO WALLS
            _gEscapeDirection = direction;

            return movement.magnitude;
        }

        protected virtual float LoseStamina(float amount)
        {
            if (entityStats.isOutOfStamina) return 0f;

            float staminaLoss = amount * Time.deltaTime;
            entityStats.stamina -= staminaLoss;
            if (entityStats.stamina <= 0f)
            {
                entityStats.isOutOfStamina = true;
                entityStats.stamina = 0f;
            }
            entityStats.staminaTimestamp = Time.time;

            return staminaLoss;
        }

        protected virtual float RegainStamina()
        {
            if (entityStats.staminaTimestamp + entityStats.staminaCooldown > Time.time) return 0f;
            if (entityStats.stamina >= 100f) return 0f;

            float staminaRegain;
            if (entityStats.isMoving)
#if HARDMODE
            staminaRegain = 0f;
#else
                staminaRegain = entityStats.staminaRegen * 0.5f * Time.deltaTime;
#endif
            else
                staminaRegain = entityStats.staminaRegen * Time.deltaTime;

            entityStats.stamina += staminaRegain;

            if (entityStats.stamina >= entityStats.outOfStaminaUpperThreshold) entityStats.isOutOfStamina = false;
            if (entityStats.stamina > 100f) entityStats.stamina = 100f;

            return staminaRegain;
        }

        protected virtual float EatHeal(float amount) // each food item heals a different amount, hence the parameter
        {
            if (entityStats.health >= entityStats.maxHealth) return 0f;

            entityStats.health += amount;
            if (entityStats.health > entityStats.maxHealth)
                entityStats.health = entityStats.maxHealth;

            entityStats.healthTimestamp = Time.time;

            return amount;
        }

        protected virtual float NaturalHeal()
        {
            if (entityStats.healthTimestamp + (entityStats.isLowHealth ? entityStats.healthCooldown * 0.5 : entityStats.healthCooldown) > Time.time) return 0f; // Wait 3s/5s after last damage taken
            if (entityStats.health >= entityStats.maxHealth) return 0f;

            float healAmount = entityStats.healthRegen * Time.deltaTime;
            entityStats.health += healAmount;

            if (entityStats.health > entityStats.maxHealth) entityStats.health = entityStats.maxHealth;

            return healAmount;
        }

        protected virtual void Knockback(EntityBase fromEntity) // apply from an entity
        {
            if (!fromEntity) return;
            Vector3 knockbackDirection = (fromEntity.transform.position - transform.position).normalized;
            fromEntity._rigidbody.AddForce(knockbackDirection * entityStats.knockbackForce, ForceMode.Impulse);
        }

        public void GetEntitiesByProximity(float radius, out List<EntityBase> outEntities, bool byProximity) // should be executed only once per second
        {
            outEntities = new();
            Dictionary<EntityBase, float> entityDistanceDict = new();
            
            if (byProximity)
            {
                foreach (var entityPair in EntityRegistry)
                {
                    EntityBase entity = entityPair.Value;
                    if (entity == this) continue;
                    float distance = GetProximityToEntity(entity);
                    if (distance <= radius)
                        entityDistanceDict.Add(entityPair.Value, distance);
                }
            
                var sortedDict = from entry in entityDistanceDict orderby entry.Value select entry;
                if(entityDistanceDict.Count != 0) outEntities.Add(sortedDict.First().Key);
                return;
            }
            
            foreach (var entityPair in EntityRegistry)
            {
                EntityBase entity = entityPair.Value;
                if (entity == this) continue;
            
                float distance = Vector3.Distance(transform.position, entity.transform.position);
                if (distance <= radius) 
                    outEntities.Add(entity);
            }
        }
        
        protected virtual bool FindEntityToAttack(List<EntityBase> entities, out EntityBase target, params Type[] types)
        {
            target = null;
            foreach (var entity in entities)
            {
                foreach (var type in types)
                    if (type.IsAssignableFrom(entity.GetType()) 
                        && !Physics.Raycast(transform.position, entity.transform.position - transform.position , out RaycastHit hit, GetProximityToEntity(entity), LayerMask.GetMask("Environment", "Obstacles"))) // this, I needed help from ChatGPT
                    {
                        Debug.DrawLine(transform.position, transform.position + hit.point, Color.cyan);
                        target = entity;
                        return true;
                    }
            }
            return false;
        }

        public static bool IsInCooldown(float timestamp, float cooldown) => timestamp + cooldown > Time.time;

        private EntityBase AreEntitiesInRange(List<EntityBase> entities)
        {
            if (entities.Count == 0)
            {
                entityStats.attackedBy = new List<EntityBase>();
                return null;
            }
            
            foreach (EntityBase entity in entities)
            {
                if (GetProximityToEntity(entity) > entityStats.visibilityDistance*2f)
                    return entity;
            }

            return null;
        }
        public Guid GetEntityGuid() => entityStats.EntityID;
        public static Guid GetEntityGuid(EntityBase entity) => entity ? entity.entityStats.EntityID : Guid.Empty;
        private static EntityBase GetEntityByGuid(Guid guid) => EntityRegistry.GetValueOrDefault(guid);
        private float GetProximityToEntity(EntityBase entity) => Vector3.Distance(transform.position, entity.transform.position);
        public static float GetProximityToEntity(EntityBase entity1, EntityBase entity2) => Vector3.Distance(entity1.transform.position, entity2.transform.position);
        public float GetProximityToEntity(Guid guid) => Vector3.Distance(transform.position, GetEntityByGuid(guid).transform.position);
        public static float GetProximityToEntity(Guid guid1, Guid guid2) => Vector3.Distance(GetEntityByGuid(guid1).transform.position, GetEntityByGuid(guid2).transform.position);
        public static List<EntityBase> GetAllEntities() => new List<EntityBase>(EntityRegistry.Values);
        public Vector3 GetCurrentDirection() => transform.forward;
    
        private void RotateTowards(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - transform.position;
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
        }

        private bool ObstacleDetection()
        {
            Collider[] obstacles = new Collider[7];
            Physics.OverlapSphereNonAlloc(transform.position, entityStats.obstacleDetectionDistance, obstacles, LayerMask.GetMask("Environment", "Obstacles"));
            
            if (obstacles.Length == 0) return false;
            
            if(entityStats.attackedBy.Count > 0)
                foreach (Collider obstacle in obstacles)
                {
                    if (!obstacle) continue;
                    Vector3 closestPoint = obstacle.ClosestPoint(transform.position);
                    Debug.DrawLine(transform.position, closestPoint, Color.darkRed);
                    Vector3 direction = transform.position - closestPoint;
                    _escapeDirection += direction;
                }

            return true;
        }
    }
}