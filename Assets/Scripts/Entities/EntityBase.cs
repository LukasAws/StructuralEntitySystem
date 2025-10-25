using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Entities.Species;
using UnityEngine;
using UnityEngine.UIElements;

namespace Entities
{
    [RequireComponent(typeof(EntityStats))]
    [RequireComponent(typeof(Rigidbody))]
    public abstract class EntityBase : MonoBehaviour
    {
        //Events
        private delegate void PursuitEventHandler<in TEntityBase>(TEntityBase entity);
        private delegate EntityBase AttackEventHandler();

        private AttackEventHandler _onAttack;

        private PursuitEventHandler<EntityBase> _onPursuitStart;
        private PursuitEventHandler<EntityBase> _onPursuitEnd;
        
        private PursuitEventHandler<EntityBase> _onEscapeStart;
        private PursuitEventHandler<EntityBase> _onEscapeEnd;

        private Vector3 _escapeDirection;
        
        //Variables
        
        private static readonly Dictionary<Guid, EntityBase> EntityRegistry = new Dictionary<Guid, EntityBase>();


        public EntityBase attackTargetPublic;
        
        public enum HostilityLevel : short
        {
            Friendly = 0, // will not attack
            Neutral = 1,  // will attack if attacked
            Hostile = 2,   // will always attack on sight
        }

        private EntityBase _attackTarget = null;

        private EntityStats _entityStats;
        private Rigidbody _rigidbody;

        //Methods

        protected virtual void Awake()
        {
             _entityStats = GetComponent<EntityStats>();
             _rigidbody = GetComponent<Rigidbody>();

            if (!EntityRegistry.ContainsKey(_entityStats.EntityID))
                EntityRegistry.Add(_entityStats.EntityID, this);

            _onAttack = () =>
            {
                if (_entityStats.attackTimestamp + _entityStats.attackCooldown > Time.time) return null;
                
                if (!_attackTarget._entityStats.attackedBy.Contains(this))
                    _attackTarget._entityStats.attackedBy.Add(this);
                
                _entityStats.attackTimestamp = Time.time;
                return _attackTarget.TakeDamage(_entityStats.attackDamage, this);
            };

            _onPursuitStart += (e) =>
            {
                StopAllCoroutines();
                _attackTarget = e;
                StartCoroutine(Pursue());
            };

            _onPursuitEnd += e =>
            {
                StopAllCoroutines();
                _attackTarget = null;
                StartCoroutine(Wander());
            };

            _onEscapeStart += e =>
            {
                StopAllCoroutines();
                StartCoroutine(Escape(_entityStats.attackedBy));
            };

            _onEscapeEnd += e =>
            {
                StopAllCoroutines();
                if (!_entityStats.attackedBy.Contains(this))
                    _entityStats.attackedBy.Remove(e);
                StartCoroutine(Wander());
            };

        }

        [Header("Gizmos")]
        private Vector3 _gWanderEndPoint;
        private Vector3 _gEscapeDirection;

        private void OnDrawGizmos()
        {
            if (_entityStats.hostilityLevel == HostilityLevel.Hostile)
            {
                Gizmos.color = _attackTarget ? Color.red : Color.white;

                if (_attackTarget)
                    Gizmos.DrawWireSphere(transform.position, _entityStats.visibilityDistance * 1.5f);
                else
                    Gizmos.DrawWireSphere(transform.position, _entityStats.visibilityDistance);
            }

            if (_attackTarget)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _attackTarget.transform.position);
            }

            if (!_attackTarget && _entityStats.attackedBy.Count <= 0 && _gWanderEndPoint != Vector3.zero)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, _gWanderEndPoint);
            }

            if (_entityStats.attackedBy.Count > 0 && !_attackTarget)
            {
                Gizmos.color = Color.orange;
                Gizmos.DrawLine(transform.position, transform.position + _gEscapeDirection * 5f);
            }

            if (_entityStats.attackedBy.Count == 0)
                _gEscapeDirection = Vector3.zero;
            
            if(_attackTarget || _entityStats.attackedBy.Count > 0)
                _gWanderEndPoint = Vector3.zero;
        }

        private void Start()
        {
            StartCoroutine(Wander());
        }


        private void FixedUpdate() // the hierarchy of actions matters here
        {
            attackTargetPublic = _attackTarget;
            
            if(transform.position.y <= -20)
                Die();

            if(_entityStats.hostilityLevel == HostilityLevel.Hostile && !_attackTarget)
            {
                GetEntitiesByProximity(_entityStats.visibilityDistance, out List<EntityBase> entities, true);
                FindEntityToAttack(
                    entities,
                    out _attackTarget,
                    typeof(HumanBase)
                    );
                if (_attackTarget)
                    _onPursuitStart?.Invoke(_attackTarget);
            }

            RegainStamina();
            NaturalHeal();

            _entityStats.isMoving = false; // has to be the last line in Update
        }

        protected virtual void OnDestroy()
        {
            EntityRegistry.Remove(_entityStats.EntityID);
        }

        // if all pursuit values are 'false' the entities will wander
        protected virtual IEnumerator Wander() // TODO: Improve wandering behavior -- right now it doesn't consider obstacles -- think about implementing AI Nav
        {
            _entityStats.isMoving = true;

            float pauseEndTime = 0;
            
            while (true)
            {
                if (Time.time >= pauseEndTime)
                {
                    var pauseDuration = UnityEngine.Random.Range(1f, 3f);
                    pauseEndTime = Time.time + pauseDuration;
                }
                
                while (Time.time < pauseEndTime)
                {
                    yield return null; 
                }
                
                float randomAngle = UnityEngine.Random.Range(0f, 360f);
                Vector3 randomDirection = new Vector3(Mathf.Cos(randomAngle), 0, Mathf.Sin(randomAngle)).normalized;

                float walkDuration = UnityEngine.Random.Range(2f, 5f);
                float walkEndTime = Time.time + walkDuration;

                Vector3 walkPosition = transform.position + randomDirection * (_entityStats.speed * walkDuration);
                _gWanderEndPoint = walkPosition;

                if (Physics.Raycast(transform.position, randomDirection, walkDuration*_entityStats.speed, LayerMask.GetMask("Environment")))
                    continue;
                
                while (Time.time < walkEndTime)
                {
                    RotateTowards(transform.position + randomDirection);

                    Vector3 movement = transform.forward * (_entityStats.speed * Time.deltaTime);
                    if (_rigidbody)
                        _rigidbody.MovePosition(_rigidbody.position + movement);
                    yield return null;
                }
            }
        }

        private IEnumerator<EntityBase> Pursue()
        {
            if (!_attackTarget)
            {
                Debug.LogAssertion("Pursued _attackTarget is null");
                yield break;
            }

            while (true)
            {
                float proximity = _attackTarget ? GetProximityToEntity(_attackTarget) : float.MaxValue;

                if (Physics.Raycast(transform.position, _attackTarget.transform.position - transform.position , out RaycastHit hit, GetProximityToEntity(_attackTarget), LayerMask.GetMask("Environment")))
                {
                    Debug.DrawLine(transform.position, hit.point);
                    _onPursuitEnd(_attackTarget);
                    yield break;
                }
                
                if (proximity <= _entityStats.visibilityDistance*1.5f)
                    RunToFrom(_attackTarget);
                else
                {
                    _onPursuitEnd(_attackTarget);
                    yield break;
                }

                if (_entityStats.hostilityLevel != HostilityLevel.Friendly && proximity <= _entityStats.attackRange)
                {
                    EntityBase killedEntity = _onAttack?.Invoke();
                    if (killedEntity)
                    {
                        _entityStats.experience = killedEntity._entityStats.experience;
                        _entityStats.entitiesKilledCount++;
                        _onPursuitEnd?.Invoke(killedEntity);
                        _onEscapeEnd?.Invoke(killedEntity);
                    }

                    yield return killedEntity;
                }

                yield return _attackTarget;
            }
        }

        private void HandleAttack(EntityBase attackingEntity)
        {
            
            switch (_entityStats.hostilityLevel)
            {
                case HostilityLevel.Friendly:
                    _onEscapeStart?.Invoke(attackingEntity);

                    return;
                case HostilityLevel.Neutral:
                    if (_entityStats.isLowHealth)
                    {
                        _onPursuitEnd?.Invoke(attackingEntity);
                        _onEscapeStart?.Invoke(attackingEntity);
                    }
                    else
                    {
                        _onEscapeEnd?.Invoke(attackingEntity);
                        _onPursuitStart?.Invoke(attackingEntity);
                    }

                    break;
                case HostilityLevel.Hostile:
                    _onPursuitStart?.Invoke(attackingEntity);

                    break;
            }
        }

        protected virtual EntityBase TakeDamage(float damage, EntityBase receivedFromEntity = null)
        {
            float damageTaken = damage * (1f - _entityStats.armor / 100f * 0.66f);
            _entityStats.health -= damageTaken;

            _entityStats.isLowHealth = _entityStats.health <= _entityStats.lowHealthThreshold;

            if (_entityStats.health <= 0f)
            { 
                return Die(receivedFromEntity);
                _entityStats.health = 0f;
            }
            
            if (receivedFromEntity)
                Knockback(receivedFromEntity);

            if (receivedFromEntity)
                HandleAttack(receivedFromEntity);

            _entityStats.healthTimestamp = Time.time;

            return null;
        }

        private IEnumerator<EntityBase> Escape(List<EntityBase> entities)
        {
            if (entities.Count == 0)
                _onEscapeEnd(_attackTarget);

            while (true)
            {
                if (entities.Count == 0 || !IsInsidePursuitRange(entities))
                {
                    if(_entityStats.attackedBy.Count > 0)
                        _onEscapeEnd?.Invoke(_entityStats.attackedBy[0]);
                    else
                        _onEscapeEnd?.Invoke(null);
                    break;
                }

                _escapeDirection = Vector3.zero;
                foreach (EntityBase entity in entities)
                    _escapeDirection += (transform.position - entity.transform.position).normalized;
                ObstacleDetection();
                
                if (!_escapeDirection.Equals(Vector3.zero))
                    RunInDirection(_escapeDirection);
                else
                    _onEscapeEnd?.Invoke(entities[0]);
                
                yield return null;
            }
        }

        protected virtual EntityBase Die(EntityBase byEntity = null)
        {
            if (transform.childCount > 0) // if there is a camera{
            {
                transform.GetChild(0).parent = InputController.Instance.GetNextEntity(); // not sure if the pos and rot will work
                transform.GetChild(0).parent.position = new Vector3(0, 2, -4);
                transform.GetChild(0).parent.rotation = Quaternion.identity;
            }

            Destroy(gameObject);
            
            if (byEntity)
            {
                byEntity._attackTarget = null;
                return this;
            }
            return null;
        }

        protected virtual float RunToFrom(EntityBase entity, bool toEntity = true) // Runs either toward the entity or away from it 
        {
            _entityStats.isMoving = true;

            if (_entityStats.isOutOfStamina) return WalkToFrom(entity, toEntity);

            Vector3 direction = toEntity ? 
                (entity.transform.position - transform.position).normalized : 
                (transform.position - entity.transform.position).normalized;
            if(!toEntity)
                _gEscapeDirection = direction;

            RotateTowards(transform.position + direction);
            Vector3 movement = transform.forward * (_entityStats.runSpeed * Time.deltaTime);
            _rigidbody.MovePosition(_rigidbody.position + movement);

            LoseStamina(2f);

            return movement.magnitude;
        }

        private IEnumerator LookAt(Vector3 vector3)
        {
            Quaternion targetRotation = Quaternion.LookRotation(vector3 - transform.position);
            while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5);
                yield return null;
            }
        }

        protected virtual float WalkToFrom(EntityBase entity, bool toEntity = true)
        {
            _entityStats.isMoving = true;

            var direction = toEntity ? 
                (entity.transform.position - transform.position).normalized : 
                (transform.position - entity.transform.position).normalized;

            RotateTowards(transform.position + direction);

            Vector3 movement = transform.forward * (_entityStats.speed * Time.deltaTime);
            _rigidbody.MovePosition(_rigidbody.position + movement);

            return movement.magnitude;
        }
        
        protected virtual float RunInDirection(Vector3 direction) // Runs either toward the entity or away from it 
        {
            if (_entityStats.isOutOfStamina) return WalkInDirection(direction);
            
            _entityStats.isMoving = true;

            RotateTowards(transform.position + direction);

            Vector3 movement = transform.forward * (_entityStats.runSpeed * Time.deltaTime);
            _rigidbody.MovePosition(_rigidbody.position + movement);
            if(_entityStats.attackedBy.Count > 0) _gEscapeDirection = direction;

            LoseStamina(2f);

            return movement.magnitude;
        }

        protected virtual float WalkInDirection(Vector3 direction)
        {
            _entityStats.isMoving = true;
            
            RotateTowards(transform.position + direction);

            Vector3 movement = transform.forward * (_entityStats.speed * Time.deltaTime);
            _rigidbody.MovePosition(_rigidbody.position + movement);
            _gEscapeDirection = direction;

            return movement.magnitude;
        }

        protected virtual float LoseStamina(float amount)
        {
            if (_entityStats.isOutOfStamina) return 0f;

            float staminaLoss = amount * Time.deltaTime;
            _entityStats.stamina -= staminaLoss;
            if (_entityStats.stamina <= 0f)
            {
                _entityStats.isOutOfStamina = true;
                _entityStats.stamina = 0f;
            }
            _entityStats.staminaTimestamp = Time.time;

            return staminaLoss;
        }

        protected virtual float RegainStamina()
        {
            if (_entityStats.staminaTimestamp + _entityStats.staminaCooldown > Time.time) return 0f;
            if (_entityStats.stamina >= 100f) return 0f;

            float staminaRegain;
            if (_entityStats.isMoving)
#if HARDMODE
            staminaRegain = 0f;
#else
                staminaRegain = _entityStats.staminaRegen * 0.5f * Time.deltaTime;
#endif
            else
                staminaRegain = _entityStats.staminaRegen * Time.deltaTime;

            _entityStats.stamina += staminaRegain;

            if (_entityStats.stamina >= _entityStats.outOfStaminaUpperThreshold) _entityStats.isOutOfStamina = false;
            if (_entityStats.stamina > 100f) _entityStats.stamina = 100f;

            return staminaRegain;
        }

        protected virtual float EatHeal(float amount) // each food item heals a different amount, hence the parameter
        {
            if (_entityStats.health >= _entityStats.maxHealth) return 0f;

            _entityStats.health += amount;
            if (_entityStats.health > _entityStats.maxHealth)
                _entityStats.health = _entityStats.maxHealth;

            _entityStats.healthTimestamp = Time.time;

            return amount;
        }

        protected virtual float NaturalHeal()
        {
            if (_entityStats.healthTimestamp + (_entityStats.isLowHealth ? _entityStats.healthCooldown * 0.5 : _entityStats.healthCooldown) > Time.time) return 0f; // Wait 3s/5s after last damage taken
            if (_entityStats.health >= _entityStats.maxHealth) return 0f;

            float healAmount = _entityStats.healthRegen * Time.deltaTime;
            _entityStats.health += healAmount;

            if (_entityStats.health > _entityStats.maxHealth) _entityStats.health = _entityStats.maxHealth;

            return healAmount;
        }

        protected virtual void Knockback(EntityBase fromEntity) // apply to fromEntity
        {
            if (!fromEntity) return;
            Vector3 knockbackDirection = (fromEntity.transform.position - transform.position).normalized;
            fromEntity._rigidbody.AddForce(knockbackDirection * _entityStats.knockbackForce, ForceMode.Impulse);
        }

        private void GetEntitiesByProximity(float radius, out List<EntityBase> outEntities, bool byProximity) // should be executed only once per second
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
                        && !Physics.Raycast(transform.position, entity.transform.position - transform.position , out RaycastHit hit, GetProximityToEntity(entity), LayerMask.GetMask("Environment"))) // this, I needed help from ChatGPT
                    {
                        Debug.DrawLine(transform.position, hit.point, Color.cyan);
                        target = entity;
                        return true;
                    }
            }
            return false;
        }

        public static bool IsInCooldown(float timestamp, float cooldown) => timestamp + cooldown > Time.time;

        private bool IsInsidePursuitRange(List<EntityBase> entities)
        {
            if(entities.Count == 0) return false;
            
            bool soFar = false;
            foreach (EntityBase entity in entities)
            {
                if (!entity) return false;
                if (GetProximityToEntity(entity) <= _entityStats.visibilityDistance * 2)
                    soFar = true;
            }

            return soFar;
        }
        public Guid GetEntityGuid() => _entityStats.EntityID;
        public static Guid GetEntityGuid(EntityBase entity) => entity ? entity._entityStats.EntityID : Guid.Empty;
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
                if (_attackTarget)
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * (0.75f * (_entityStats.visibilityDistance - GetProximityToEntity(_attackTarget))) + 0.5f);
                else
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
        }

        private bool ObstacleDetection()
        {
            //this is the more accurate way of doing it, but it's slower and more complex
            Collider[] obstacles = new Collider[7];
            Physics.OverlapSphereNonAlloc(transform.position, _entityStats.obstacleDetectionDistance, obstacles, LayerMask.GetMask("Environment", "Obstacles"));
            
            if (obstacles.Length == 0) return false;
            
            if(_entityStats.attackedBy.Count > 0)
                foreach (Collider obstacle in obstacles)
                {
                    if (!obstacle) continue;
                    Vector3 closestPoint = obstacle.ClosestPoint(transform.position);
                    Vector3 direction = transform.position - closestPoint; // get the direction from the closest point to the entity
                    _escapeDirection += direction;
                }
            
            //this is much simpler and faster, but it's less accurate
            // bool hit = Physics.Raycast(transform.position, _escapeDirection, out RaycastHit hitInfo, _entityStats.visibilityDistance, LayerMask.GetMask("Environment", "Obstacles"));
            //
            // if (!hit) return false;
            //
            // _escapeDirection += (transform.position - hitInfo.point).normalized;

            return true;
        }
    }
}

