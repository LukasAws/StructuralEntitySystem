using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EntityStats))]
[RequireComponent(typeof(Rigidbody))]
public abstract class EntityBase : MonoBehaviour, IEntityHostility<EntityBase>
{
    public enum HostilityLevel : short
    {
        Friendly = 0, // will not attack
        Neutral = 1,  // will attack if attacked
        Hostile = 2,   // will always attack on sight
    }

    //public enum EntityType // not needed yet
    //{
    //    Human,
    //    Dog,
    //    Horse,
    //    Zombie,
    //    Chicken,
    //}

    public static readonly Dictionary<Guid, EntityBase> EntityRegistry = new Dictionary<Guid, EntityBase>();
    public EntityStats ES => GetComponent<EntityStats>();
    protected Rigidbody EntityRigidbody => GetComponent<Rigidbody>();

    // TODO: Let user choose the value in settings later -- may have to be static
    // This should be moved to the UI Manager or something
    //public float statChangeDisplayCooldown = 2f; // seconds between displaying stat changes

    protected EntityBase attackTarget;

    private bool inReaction = false;

    protected virtual void Awake()
    {
        if (!EntityRegistry.ContainsKey(ES.EntityID))
            EntityRegistry.Add(ES.EntityID, this);
    }

    [Header("Gizmos")]
    private Vector3 g_wanderEndPoint;
    private Vector3 g_escapeDirection;

    private void OnDrawGizmos()
    {
        if (ES.hostilityLevel == HostilityLevel.Hostile)
        {
            if (ES.isPursuing)
                Gizmos.color = Color.red;
            else
                Gizmos.color = Color.white;

            Gizmos.DrawWireSphere(transform.position, ES.visibilityDistance);
        }

        if(attackTarget)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, attackTarget.transform.position);
        }

        if (ES.isWandering && g_wanderEndPoint != Vector3.zero)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, g_wanderEndPoint);
        }

        if (ES.isBeingAttacked)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + g_escapeDirection * 5f);
        }
    }


    private void FixedUpdate() // the hierarchy of actions matters here
    {
        inReaction = TimeStampCalculator(ES.reactionTimestamp, ES.reactionCooldown);

        if(!ES.isWandering && !ES.isPursuing && !ES.isBeingAttacked)
            StartCoroutine(Wander());

        if(ES.isWandering && ES.hostilityLevel == HostilityLevel.Hostile)
        {
            GetEntitiesByProximity(this, ES.visibilityDistance, out List<EntityBase> entities);
            bool hasFound = FindEntityToAttack(
                entities,
                out attackTarget,
                typeof(HumanBase)
            );
            if (hasFound)
            {
                StopCoroutine(Wander());
                ES.isWandering = false;
                StartCoroutine(Pursue(attackTarget));
                Debug.Log($"{gameObject.name} is pursuing {attackTarget}");
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

    protected IEnumerator Wander() // TODO: Improve wandering behavior -- right now it doesn't consider obstacles -- think about implementing AI Nav
    {

        ES.isWandering = true;
        Debug.Log($"{gameObject.name} is now wandering");
        ES.isMoving = true;

        while (ES.isWandering)
        {
            float randomAngle = UnityEngine.Random.Range(0f, 360f);
            Vector3 randomDirection = new Vector3(Mathf.Cos(randomAngle), 0, Mathf.Sin(randomAngle)).normalized;

            float walkDuration = UnityEngine.Random.Range(2f, 5f);
            float walkEndTime = Time.time + walkDuration;

            g_wanderEndPoint = transform.position + randomDirection * ES.speed * walkDuration;

            while (Time.time < walkEndTime && ES.isWandering)
            {
                transform.LookAt(transform.position + randomDirection);
                Vector3 movement = transform.forward * ES.speed * Time.deltaTime;
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
        ES.isMoving = false;
        ES.isWandering = false;
    }

    public IEnumerator<EntityBase> Pursue(EntityBase entity)
    {
        ES.isBeingAttacked = false;
        ES.isPursuing = true;

        if (!entity.Equals(attackTarget))
        {
            Debug.LogWarning($"{gameObject.name} cannot pursue {entity.gameObject.name} as it is not the current attack target -- Something went wrong");
            ES.isPursuing = false;
            if (!ES.isWandering)
                StartCoroutine(Wander());
            yield break;
        }

        while (ES.isPursuing)
        {
            if (!entity)
            {
                Debug.Log($"{gameObject.name} lost its target {entity.gameObject.name} as it no longer exists (likely died)");
                attackTarget = null;
                ES.isPursuing = false;
                if (!ES.isWandering)
                    StartCoroutine(Wander());

                yield break;
            }

            if (!inReaction && ES.hostilityLevel == HostilityLevel.Neutral) // not interested
            {
                Debug.Log($"{gameObject.name} has stopped pursuing the target {entity.gameObject.name} as it is no longer interested");
                attackTarget = null;
                entity = null;
                ES.isPursuing = false;
                if (!ES.isWandering)
                    StartCoroutine(Wander());

                yield break;
            }

            float proximity = GetProximityToEntity(entity);

            if (proximity <= ES.visibilityDistance)
                RunToFrom(entity);
            else
            {
                Debug.Log($"{gameObject.name} has lost sight of the target {entity.gameObject.name}");
                attackTarget = null;
                entity = null;
                ES.isPursuing = false;
                if (!ES.isWandering)
                    StartCoroutine(Wander());

                yield break;
            }

            if ((inReaction || ES.hostilityLevel == HostilityLevel.Hostile) && (proximity <= ES.attackRange))
                Attack(entity);

            yield return entity;
        }
    }

    public virtual void Attack(EntityBase entity)
    {
        if (ES.attackTimestamp + ES.attackCooldown > Time.time) return;

        if (entity.GetProximityToEntity(this) > ES.attackRange) return;

        entity.TakeDamage(ES.attackDamage, this);
        ES.attackTimestamp = Time.time;
    }

    private void HandleAttack(EntityBase attackingEntity)
    {
        StopCoroutine(Wander());
        ES.isWandering = false;

        if (!attackingEntity)
        {
            StartCoroutine(Wander());
            return;
        }
        else switch (ES.hostilityLevel)
            {
                case HostilityLevel.Friendly:
                    StartCoroutine(Escape(attackingEntity));

                    break;
                case HostilityLevel.Neutral:
                    attackTarget = attackingEntity;

                    if (!ES.isPursuing)
                        StartCoroutine(Pursue(attackTarget));
                    else if (ES.isLowHealth)
                    {
                        StopCoroutine(Pursue(attackTarget));
                        ES.isPursuing = false;
                        StartCoroutine(Escape(attackingEntity));
                    }

                    break;
                case HostilityLevel.Hostile:
                    attackTarget = attackingEntity;

                    if (!ES.isPursuing)
                        StartCoroutine(Pursue(attackTarget));

                    break;
            }

        ES.reactionTimestamp = Time.time;
    }

    public virtual float TakeDamage(float damage, EntityBase receivedFromEntity = null)
    {
        if (receivedFromEntity && ES.hostilityLevel != HostilityLevel.Hostile) ES.isBeingAttacked = true;

        float damageTaken = damage * (1f - ES.armor / 100f * 0.66f);
        ES.health -= damageTaken;

        if (ES.health <= ES.lowHealthThreshold)
            ES.isLowHealth = true;
        else
            ES.isLowHealth = false;

        if (ES.health <= 0f)
        {
            ES.health = 0f;
            Die(receivedFromEntity);
        }
        if (ES.health > 0f) Knockback(receivedFromEntity);

        HandleAttack(receivedFromEntity);

        ES.reactionTimestamp = Time.time;
        ES.healthTimestamp = Time.time;

        return damageTaken;
    }

    public IEnumerator<EntityBase> Escape(EntityBase entity)
    {
        if (!entity)
        {
            Debug.LogWarning($"{gameObject.name} cannot escape {entity.gameObject.name} as it no longer exists (likely died)");
            ES.isBeingAttacked = false;
            yield break;
        }

        ES.isBeingAttacked = true;
        while (ES.isBeingAttacked)
        {
            if (!entity)
            {
                Debug.Log($"{gameObject.name} lost its target as it no longer exists (likely died)");
                ES.isBeingAttacked = false;

                yield break;
            }

            if (!inReaction) // forgot
            {
                Debug.Log($"{gameObject.name} has stopped escaping {entity.gameObject.name} as it forgot");
                entity = null;
                ES.isBeingAttacked = false;

                yield break;
            }

            float proximity = GetProximityToEntity(entity);

            if (proximity <= ES.visibilityDistance * 2)
            {
                RunToFrom(entity, false); //Run from

                yield return entity;
            }
            else
            {
                Debug.Log($"{gameObject.name} has lost sight of the attacker {entity.gameObject.name}");
                entity = null;
                ES.isBeingAttacked = false;

                yield break;
            }
        }
    }

    protected virtual void Die(EntityBase byEntity = null)
    {
        Camera.main.transform.parent = null; // detach camera
        Destroy(gameObject); // temporary
        //gameObject.SetActive(false);
        if (byEntity) byEntity.attackTarget = null;
        if (byEntity) return;
        // TODO: Handle kill crediting, experience gain, etc. // Means this entity was killed by non-entity source
        // TODO: Handle entity death (e.g., play animation, drop loot, remove from game world)
    } // TODO: Complete this

    protected virtual float RunToFrom(EntityBase entity, bool toEntity = true) // Runs either toward the entity or away from it 
    {
        ES.isMoving = true;

        if (ES.isOutOfStamina) return WalkToFrom(entity, toEntity);

        Vector3 direction;

        if (toEntity)
            direction = (entity.transform.position - transform.position).normalized;
        else
            direction = (transform.position - entity.transform.position).normalized;

        if(!toEntity)
            g_escapeDirection = direction;

        transform.LookAt(transform.position + direction);

        Vector3 movement = transform.forward
            * ES.runSpeed
            * Time.deltaTime;
        EntityRigidbody.MovePosition(EntityRigidbody.position + movement);

        LoseStamina(2f);

        return movement.magnitude;
    }

    protected virtual float WalkToFrom(EntityBase entity, bool toEntity = true)
    {
        ES.isMoving = true;

        Vector3 direction;
        if (toEntity)
            direction = (entity.transform.position - transform.position).normalized;
        else
            direction = (transform.position - entity.transform.position).normalized;

        transform.LookAt(transform.position + direction);

        Vector3 movement = transform.forward
            * ES.speed
            * Time.deltaTime;
        EntityRigidbody.MovePosition(EntityRigidbody.position + movement);

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

        float staminaRegain = 0f;
        if (ES.isMoving)
#if HARDMODE // still unsure if I want this
            staminaRegain = 0f;
#else
            staminaRegain = ES.staminaRegen * 0.5f * Time.deltaTime;
#endif
        else
            staminaRegain = ES.staminaRegen * Time.deltaTime;

        ES.stamina += staminaRegain;

        if (ES.stamina >= 50f) ES.isOutOfStamina = false;
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
        fromEntity.EntityRigidbody.AddForce(new Vector3(0, 1, 0).normalized * ES.knockbackForce*0.4f, ForceMode.Impulse);
    }

    public float GetEntitiesByProximity(float radius, out List<EntityBase> outEntities) // should be executed only once per second
    {
        outEntities = new();
        foreach (var entityPair in EntityRegistry)
        {
            EntityBase entity = entityPair.Value;
            if (entity == this) continue;

            float distance = Vector3.Distance(transform.position, entity.transform.position);
            if (distance <= radius) outEntities.Add(entity);
        }
        return outEntities.Count;
    }

    public static float GetEntitiesByProximity(EntityBase thisEntity, float radius, out List<EntityBase> outEntities) // should be executed only once per second
    {
        outEntities = new();
        foreach (var entityPair in EntityRegistry)
        {
            EntityBase entity = entityPair.Value;
            if (entity == thisEntity) continue;

            float distance = Vector3.Distance(thisEntity.transform.position, entity.transform.position);
            if (distance <= radius) outEntities.Add(entity);
        }
        return outEntities.Count;
    }

    public virtual bool FindEntityToAttack(List<EntityBase> entities, out EntityBase target, params Type[] types)
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

    public static bool TimeStampCalculator(float timestamp, float cooldown) => (timestamp + cooldown) > Time.time; // returns true if still in cooldown
    public Guid GetEntityGUID() => ES.EntityID;
    public static Guid GetEntityGUID(EntityBase entity) => entity ? entity.ES.EntityID : Guid.Empty;
    public static EntityBase GetEntityByGUID(Guid guid) => EntityRegistry.TryGetValue(guid, out var entity) ? entity : null;
    public float GetProximityToEntity(EntityBase entity) => Vector3.Distance(transform.position, entity.transform.position);
    public static float GetProximityToEntity(EntityBase entity1, EntityBase entity2) => Vector3.Distance(entity1.transform.position, entity2.transform.position);
    public float GetProximityToEntity(Guid guid) => Vector3.Distance(transform.position, GetEntityByGUID(guid).transform.position);
    public static float GetProximityToEntity(Guid guid1, Guid guid2) => Vector3.Distance(GetEntityByGUID(guid1).transform.position, GetEntityByGUID(guid2).transform.position);
    public static List<EntityBase> GetAllEntities() => new List<EntityBase>(EntityRegistry.Values);
}

