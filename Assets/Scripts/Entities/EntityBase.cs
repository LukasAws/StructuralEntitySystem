using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Entities.Species;
using UnityEngine;

namespace Entities
{
    [RequireComponent(typeof(EntityStats))]
    [RequireComponent(typeof(Rigidbody))]
    public abstract class EntityBase : MonoBehaviour
    {
        //Events
        private delegate void PursuitEventHandler<T>(T entity);

        private PursuitEventHandler<EntityBase> _onPursuitStart;
        private PursuitEventHandler<EntityBase> _onPursuitEnd;

        private PursuitEventHandler<EntityBase> _onEscapeStart;
        private PursuitEventHandler<EntityBase> _onEscapeEnd;
        
        //Variables
        
        private static readonly Dictionary<Guid, EntityBase> EntityRegistry = new Dictionary<Guid, EntityBase>();
        private EntityStats ES => GetComponent<EntityStats>();
        private Rigidbody EntityRigidbody => GetComponent<Rigidbody>();

        public EntityBase attackTargetPublic;
        
        public enum HostilityLevel : short
        {
            Friendly = 0, // will not attack
            Neutral = 1,  // will attack if attacked
            Hostile = 2,   // will always attack on sight
        }

        // TODO: Let user choose the value in settings later -- may have to be static
        // This should be moved to the UI Manager or something
        //public float statChangeDisplayCooldown = 2f; // seconds between displaying stat changes

        private EntityBase _attackTarget;
        
        //Methods

        protected virtual void Awake()
        {
            if (!EntityRegistry.ContainsKey(ES.EntityID))
                EntityRegistry.Add(ES.EntityID, this);

            _onPursuitStart += (e) =>
            {
                StopAllCoroutines();
                _attackTarget = e;
                ES.isWandering = false;
                ES.isPursuing = true;
                StartCoroutine(Pursue());
                if(!e.ES.attackedBy.Contains(this))
                    e.ES.attackedBy.Add(this);
            };

            _onPursuitEnd += _ =>
            {
                StopAllCoroutines();
                ResetPursuitValues(); // set up for wandering
            };

            _onEscapeStart += _ =>
            {
                StopAllCoroutines();
                ES.isBeingAttacked = true;
                ES.isWandering = false;
                StartCoroutine(Escape(ES.attackedBy));
            };

            _onEscapeEnd += e =>
            {
                StopAllCoroutines();
                ES.isBeingAttacked = false;
                ES.attackedBy.Remove(e);
                ResetPursuitValues(); // set up for wandering
            };
        }

        [Header("Gizmos")]
        private Vector3 _gWanderEndPoint;
        private Vector3 _gEscapeDirection;

        private bool canWander = false;

        private void OnDrawGizmos()
        {
            if (ES.hostilityLevel == HostilityLevel.Hostile)
            {
                Gizmos.color = ES.isPursuing ? Color.red : Color.white;

                Gizmos.DrawWireSphere(transform.position, ES.visibilityDistance);
            }

            if(_attackTarget)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _attackTarget.transform.position);
            }

            if (ES.isWandering && _gWanderEndPoint != Vector3.zero && !ES.isPursuing)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, _gWanderEndPoint);
            }

            if (ES.isBeingAttacked && !ES.isPursuing)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, transform.position + _gEscapeDirection * 5f);
            }
        }

        private void FixedUpdate() // the hierarchy of actions matters here
        {
            if(transform.position.y <= -20)
            {
                Die();
            }

            attackTargetPublic = _attackTarget;

            canWander = !ES.isWandering && !ES.isPursuing && !ES.isBeingAttacked;
            
            if(canWander) StartCoroutine(Wander());

            if(ES.hostilityLevel == HostilityLevel.Hostile)
            {
                GetEntitiesByProximity(ES.visibilityDistance, out List<EntityBase> entities, true);
                bool hasFound = FindEntityToAttack(
                    entities,
                    out _attackTarget,
                    typeof(HumanBase)
                    );
                if (hasFound)
                {
                    _onPursuitStart?.Invoke(_attackTarget);
                }
            }

            RegainStamina();
            NaturalHeal();

            ES.isMoving = false; // has to be the last line in Update // TODO: This should probably be handled by Input and AI systems instead
        }

        protected virtual void OnDestroy()
        {
            EntityRegistry.Remove(ES.EntityID);
        }

        // if all pursuit values are 'false' the entities will wander
        protected virtual IEnumerator Wander() // TODO: Improve wandering behavior -- right now it doesn't consider obstacles -- think about implementing AI Nav
        {
            ES.isWandering = true;
            _attackTarget = null;
            ES.isMoving = true;

            while (ES.isWandering)
            {
                float randomAngle = UnityEngine.Random.Range(0f, 360f);
                Vector3 randomDirection = new Vector3(Mathf.Cos(randomAngle), 0, Mathf.Sin(randomAngle)).normalized;

                float walkDuration = UnityEngine.Random.Range(2f, 5f);
                float walkEndTime = Time.time + walkDuration;

                _gWanderEndPoint = transform.position + randomDirection * (ES.speed * walkDuration);

                while (Time.time < walkEndTime && ES.isWandering)
                {
                    transform.LookAt(transform.position + randomDirection);
                    Vector3 movement = transform.forward * (ES.speed * Time.deltaTime);
                    if(EntityRigidbody)
                    EntityRigidbody.MovePosition(EntityRigidbody.position + movement);
                    yield return null;
                }

                float pauseDuration = UnityEngine.Random.Range(1f, 3f);
                float pauseEndTime = Time.time + pauseDuration;
                while (Time.time < pauseEndTime && ES.isWandering)
                {
                    yield return null; 
                }
            }
            ES.isWandering = false;
        }

        private IEnumerator<EntityBase> Pursue()
        {
            if (!_attackTarget)
            {
                _onPursuitEnd?.Invoke(_attackTarget);
                yield break;
            }

            while (ES.isPursuing)
            {
                float proximity = _attackTarget ? GetProximityToEntity(_attackTarget) : float.MaxValue;

                if (proximity <= ES.visibilityDistance)
                    RunToFrom(_attackTarget);
                else
                {
                    _onPursuitEnd?.Invoke(_attackTarget);
                    _attackTarget = null;

                    yield break;
                }

                if (ES.hostilityLevel != HostilityLevel.Friendly && proximity <= ES.attackRange)
                {
                    EntityBase killedEntity = Attack(_attackTarget);
                    if (killedEntity)
                    {
                        _onPursuitEnd?.Invoke(killedEntity);
                        _onEscapeEnd?.Invoke(killedEntity);
                    }

                    yield return killedEntity;
                }

                yield return _attackTarget;
            }
        }

        protected virtual EntityBase Attack(EntityBase entity)
        {
            if (ES.attackTimestamp + ES.attackCooldown > Time.time) return null;

            ES.attackTimestamp = Time.time;
            return entity.TakeDamage(ES.attackDamage, this);
        }

        private void HandleAttack(EntityBase attackingEntity)
        {
            ES.reactionTimestamp = Time.time;
            
            switch (ES.hostilityLevel)
            {
                case HostilityLevel.Friendly:
                    _onEscapeStart?.Invoke(attackingEntity);

                    return;
                case HostilityLevel.Neutral:
                    if (ES.isLowHealth)
                    {
                        _onPursuitEnd?.Invoke(attackingEntity);
                        _attackTarget = null;
                        _onEscapeStart?.Invoke(attackingEntity);
                    }
                    else
                    {
                        _onEscapeEnd?.Invoke(attackingEntity);
                        _attackTarget = attackingEntity;
                        _onPursuitStart?.Invoke(attackingEntity);
                    }

                    break;
                case HostilityLevel.Hostile:
                    _onPursuitStart?.Invoke(attackingEntity);

                    break;
            }
            
            
            _attackTarget = attackingEntity;
        }

        protected virtual EntityBase TakeDamage(float damage, EntityBase receivedFromEntity = null)
        {
            if (!receivedFromEntity) ES.isBeingAttacked = false;

            float damageTaken = damage * (1f - ES.armor / 100f * 0.66f);
            ES.health -= damageTaken;

            ES.isLowHealth = ES.health <= ES.lowHealthThreshold;

            if (ES.health <= 0f)
            {
                ES.health = 0f;
                if(receivedFromEntity)
                    return Die(receivedFromEntity);
            }
            
            if (receivedFromEntity)
                Knockback(receivedFromEntity);

            if (receivedFromEntity)
                HandleAttack(receivedFromEntity);

            ES.reactionTimestamp = Time.time;
            ES.healthTimestamp = Time.time;

            return null;
        }

        private IEnumerator<EntityBase> Escape(List<EntityBase> entities)
        {
            // TODO: Handle escaping multiple entities
            if (entities.Count == 0)
            {
                ES.isBeingAttacked = false;
                yield break;
            }

            ES.isBeingAttacked = true;
            while (ES.isBeingAttacked)
            {
                bool temp = IsInsidePursuitRange(entities);
                if (entities.Count == 0 || !IsInsidePursuitRange(entities))
                {
                    _onEscapeEnd?.Invoke(entities[0]);
                    break;
                }

                Vector3 escapeDirection = Vector3.zero; //Add all pursuers' vectors together and normalize
                foreach (EntityBase entity in entities)
                    escapeDirection += entity.GetCurrentDirection();
                
                escapeDirection = escapeDirection.normalized;
                
                if (!escapeDirection.Equals(Vector3.zero))
                    RunInDirection(escapeDirection);
                else
                    _onEscapeEnd?.Invoke(entities[0]);
                
                yield return null;
            }
        }

        protected virtual EntityBase Die(EntityBase byEntity = null)
        {
            Destroy(gameObject);
            if (byEntity)
            {
                byEntity._attackTarget = null;
                return this;
            }

            return null;
            // TODO: Handle kill crediting, experience gain, etc. // Means this entity was killed by non-entity source
            // TODO: Handle entity death (e.g., play animation, drop loot, remove from game world)
        }   // TODO: Complete this

        protected virtual float RunToFrom(EntityBase entity, bool toEntity = true) // Runs either toward the entity or away from it 
        {
            // TODO: Handle running from multiple entities
            ES.isMoving = true;

            if (ES.isOutOfStamina) return WalkToFrom(entity, toEntity);

            var direction = toEntity ? 
                (entity.transform.position - transform.position).normalized : 
                (transform.position - entity.transform.position).normalized;

            if(!toEntity)
                _gEscapeDirection = direction;

            transform.LookAt(transform.position + direction);

            Vector3 movement = transform.forward * (ES.runSpeed * Time.deltaTime);
        
            EntityRigidbody.MovePosition(EntityRigidbody.position + movement);

            LoseStamina(2f);

            return movement.magnitude;
        }

        protected virtual float WalkToFrom(EntityBase entity, bool toEntity = true)
        {
            ES.isMoving = true;

            var direction = toEntity ? 
                (entity.transform.position - transform.position).normalized : 
                (transform.position - entity.transform.position).normalized;

            transform.LookAt(transform.position + direction);

            Vector3 movement = transform.forward * (ES.speed * Time.deltaTime);
            EntityRigidbody.MovePosition(EntityRigidbody.position + movement);

            ES.isMoving = false;

            return movement.magnitude;
        }
        
        protected virtual float RunInDirection(Vector3 direction) // Runs either toward the entity or away from it 
        {
            if (ES.isOutOfStamina) return WalkInDirection(direction);
            
            ES.isMoving = true;

            transform.LookAt(transform.position + direction);

            Vector3 movement = transform.forward * (ES.runSpeed * Time.deltaTime);
            EntityRigidbody.MovePosition(EntityRigidbody.position + movement);
            if(ES.isBeingAttacked) _gEscapeDirection = direction;

            LoseStamina(2f);

            return movement.magnitude;
        }

        protected virtual float WalkInDirection(Vector3 direction)
        {
            ES.isMoving = true;
            
            transform.LookAt(transform.position + direction);

            Vector3 movement = transform.forward * (ES.speed * Time.deltaTime);
            EntityRigidbody.MovePosition(EntityRigidbody.position + movement);
            if(ES.isBeingAttacked) _gEscapeDirection = direction;
            
            ES.isMoving = false;

            return movement.magnitude;
        } // TODO: test this

        protected virtual float LoseStamina(float amount)
        {
            if (ES.isOutOfStamina) return 0f;

            float staminaLoss = amount * Time.deltaTime;
            ES.stamina -= staminaLoss;
            if (ES.stamina <= 0f)
            {
                ES.isOutOfStamina = true;
                ES.stamina = 0f;
            }
            ES.staminaTimestamp = Time.time;

            return staminaLoss;
        }

        protected virtual float RegainStamina()
        {
            if (ES.staminaTimestamp + ES.staminaCooldown > Time.time) return 0f;
            if (ES.stamina >= 100f) return 0f;

            float staminaRegain;
            if (ES.isMoving)
#if HARDMODE // still unsure if I want this
            staminaRegain = 0f;
#else
                staminaRegain = ES.staminaRegen * 0.5f * Time.deltaTime;
#endif
            else
                staminaRegain = ES.staminaRegen * Time.deltaTime;

            ES.stamina += staminaRegain;

            if (ES.stamina >= ES.outOfStaminaUpperThreshold) ES.isOutOfStamina = false;
            if (ES.stamina > 100f) ES.stamina = 100f;

            return staminaRegain;
        }

        protected virtual float EatHeal(float amount) // each food item heals a different amount, hence the parameter
        {
            if (ES.health >= ES.maxHealth) return 0f;

            ES.health += amount;
            if (ES.health > ES.maxHealth)
                ES.health = ES.maxHealth;

            ES.healthTimestamp = Time.time;

            return amount;
        }

        protected virtual float NaturalHeal()
        {
            if (ES.healthTimestamp + (ES.isLowHealth ? ES.healthCooldown * 0.5 : ES.healthCooldown) > Time.time) return 0f; // Wait 3s/5s after last damage taken
            if (ES.health >= ES.maxHealth) return 0f;

            float healAmount = ES.healthRegen * Time.deltaTime;
            ES.health += healAmount;

            if (ES.health > ES.maxHealth) ES.health = ES.maxHealth;

            return healAmount;
        }

        protected virtual void Knockback(EntityBase fromEntity) // apply to fromEntity
        {
            if (!fromEntity) return;
            Vector3 knockbackDirection = (fromEntity.transform.position - transform.position).normalized;
            fromEntity.EntityRigidbody.AddForce(knockbackDirection * ES.knockbackForce, ForceMode.Impulse);
            fromEntity.EntityRigidbody.AddForce(new Vector3(0, 1, 0).normalized * (ES.knockbackForce * 0.4f), ForceMode.Impulse);
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
                    if (distance <= radius) entityDistanceDict.Add(entityPair.Value, distance);
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
                if (distance <= radius) outEntities.Add(entity);
            }
        }
        
        protected virtual bool FindEntityToAttack(List<EntityBase> entities, out EntityBase target, params Type[] types)
        {
            target = null;
            foreach (var entity in entities)
            {
                foreach (var type in types)
                    if (type.IsAssignableFrom(entity.GetType())) // this, I needed help from ChatGPT
                    {
                        target = entity;
                        return true;
                    }
            }
            return false;
        }

        //public void DisplayStatChange<T>(T arg)
        //{
        //    //if (GlobalSettings.ShowStatsChange.enabled) // TODO: Create a class to hold values like enabled, size, etc.
        //    if (Time.time < statChangeDisplayCooldown) return;

        //    // TODO: Push the stat change to a UI manager to handle display
        //    // This will be done using a UITK system with a floating text prefab

        //    // EntityUIManager.DisplayStat(transform, typeof(arg), arg);

        //    statChangeDisplayCooldown = Time.time + statChangeDisplayCooldown;
        //}

        private void ResetPursuitValues(bool wander = false, bool pursuit = false, bool attacked = false)
        {
            ES.isWandering = wander;
            ES.isPursuing = pursuit;
            ES.isBeingAttacked =  attacked;
        }

        public static bool IsInCooldown(float timestamp, float cooldown) => timestamp + cooldown > Time.time;

        private bool IsInsidePursuitRange(List<EntityBase> entities)
        {
            if(entities.Count == 0) return false;
            
            bool soFar = false;
            foreach (EntityBase entity in entities)
            {
                if (!entity) return false;
                if (GetProximityToEntity(entity) <= ES.visibilityDistance * 2)
                    soFar = true;
            }

            return soFar;
        }
        public Guid GetEntityGuid() => ES.EntityID;
        public static Guid GetEntityGuid(EntityBase entity) => entity ? entity.ES.EntityID : Guid.Empty;
        private static EntityBase GetEntityByGuid(Guid guid) => EntityRegistry.GetValueOrDefault(guid);
        private float GetProximityToEntity(EntityBase entity) => Vector3.Distance(transform.position, entity.transform.position);
        public static float GetProximityToEntity(EntityBase entity1, EntityBase entity2) => Vector3.Distance(entity1.transform.position, entity2.transform.position);
        public float GetProximityToEntity(Guid guid) => Vector3.Distance(transform.position, GetEntityByGuid(guid).transform.position);
        public static float GetProximityToEntity(Guid guid1, Guid guid2) => Vector3.Distance(GetEntityByGuid(guid1).transform.position, GetEntityByGuid(guid2).transform.position);
        public static List<EntityBase> GetAllEntities() => new List<EntityBase>(EntityRegistry.Values);
        public Vector3 GetCurrentDirection() => transform.forward;
    }
}

