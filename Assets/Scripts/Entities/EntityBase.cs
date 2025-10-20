using System;
using System.Collections.Generic;
using System.Net;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
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
    protected EntityStats m_EntityStats => GetComponent<EntityStats>();
    protected Rigidbody EntityRigidbody => GetComponent<Rigidbody>();

    // TODO: Let user choose the value in settings later -- may have to be static
    // This should be moved to the UI Manager or something
    public float statChangeDisplayCooldown = 2f; // seconds between displaying stat changes

    protected EntityBase attackTarget;

    public EntityBase target;

    protected virtual void Awake()
    {
        if (!EntityRegistry.ContainsKey(m_EntityStats.EntityID))
            EntityRegistry.Add(m_EntityStats.EntityID, this);

        // TODO: Aggro mechanic is not tested out
    }

    private void FixedUpdate() // the hierarchy of actions matters here
    {
        target = attackTarget;

        if (!m_EntityStats.isPursuing && m_EntityStats.hostilityLevel == HostilityLevel.Hostile)
        {
            if (!attackTarget)
            {
                List<EntityBase> nearbyEntities;
                GetEntitiesByProximity(m_EntityStats.pursuitRange, out nearbyEntities);
                if (nearbyEntities.Count > 0)
                {
                    Debug.Log($"found a human? {FindEntityToAttack(nearbyEntities, out attackTarget, typeof(Male), typeof(HumanBase))}");
                    HandleAttacker(attackTarget);
                }
                else
                    Roam();
            }
        }
        if (m_EntityStats.hostilityLevel == HostilityLevel.Friendly || m_EntityStats.hostilityLevel == HostilityLevel.Neutral && !m_EntityStats.isPursuing)
            Roam();

        RegainStamina();
        NaturalHeal();

        m_EntityStats.isMoving = false; // has to be the last line in Update // TODO: This should probably be handled by Input and AI systems instead
    }

    private void OnDrawGizmos()
    {
        if(m_EntityStats.hostilityLevel == HostilityLevel.Hostile)
        Gizmos.DrawWireSphere(transform.position, m_EntityStats.pursuitRange);
    }

    protected virtual void OnDestroy()
    {
        EntityRegistry.Remove(m_EntityStats.EntityID);
    }

    public virtual void Attack(EntityBase entity)
    {
        if (m_EntityStats.attackTimestamp + m_EntityStats.attackCooldown > Time.time) return;

        if (entity.GetProximityToEntity(this) > m_EntityStats.attackRange) return;

        entity.TakeDamage(m_EntityStats.attackDamage, this);
        m_EntityStats.attackTimestamp = Time.time;
    }

    public IEnumerator<float> Pursue(EntityBase entity)
    {
        m_EntityStats.isPursuing = true;

        while (m_EntityStats.isPursuing)
        {
            if (!entity)
            {
                attackTarget = null;
                m_EntityStats.isPursuing = false;
                break;
            }
            float proximity = GetProximityToEntity(entity);

            if (proximity > m_EntityStats.pursuitRange)
            {
                attackTarget = null;
                m_EntityStats.isPursuing = false;
                break;
            }

            if (proximity <= m_EntityStats.attackRange) Attack(entity);

            if (proximity <= m_EntityStats.pursuitRange)
            {
                Run(entity);
                if (m_EntityStats.hostilityLevel == HostilityLevel.Hostile) 
                    m_EntityStats.isPursuing = true;
                else 
                    if (m_EntityStats.reactionTimestamp + m_EntityStats.reactionCooldown < Time.time)
                    {
                        m_EntityStats.isPursuing = false;
                        attackTarget = null;
                    }
            }
            else
            {
                m_EntityStats.isPursuing = false;
                attackTarget = null;
            }
            yield return 0f;
        }
    }

    protected virtual float Walk()
    {
        m_EntityStats.isMoving = true;

        Vector3 movement = transform.forward * m_EntityStats.speed * Time.deltaTime;
        EntityRigidbody.MovePosition(EntityRigidbody.position + movement);

        return movement.magnitude;
    }

    protected virtual float Walk(EntityBase entity)
    {
        m_EntityStats.isMoving = true;

        transform.LookAt(entity.transform);
        Vector3 movement = transform.forward * m_EntityStats.speed * Time.deltaTime;
        EntityRigidbody.MovePosition(EntityRigidbody.position + movement);

        return movement.magnitude;
    }

    protected virtual void Roam()
    {
        //TODO: Implement roaming behavior(e.g., random wandering within a certain area)-- needs AI Navigation package
    }

    protected virtual float Run()
    {
        m_EntityStats.isMoving = true;

        if (m_EntityStats.isOutOfStamina) return Walk();

        Vector3 movement = transform.forward 
            * m_EntityStats.runSpeed
            * Time.deltaTime;
        EntityRigidbody.MovePosition(EntityRigidbody.position + movement);

        LoseStamina(2f);

        return movement.magnitude;
    }

    protected virtual float Run(EntityBase entity)
    {
        //TODO: Implement movement towards entity with AI Navigation package
        m_EntityStats.isMoving = true;

        if (m_EntityStats.isOutOfStamina) return Walk(entity);

        transform.LookAt(entity.transform);
        Vector3 movement = transform.forward
            * m_EntityStats.runSpeed
            * Time.deltaTime;
        EntityRigidbody.MovePosition(EntityRigidbody.position + movement);

        LoseStamina(2f);

        return movement.magnitude;
    }

    protected virtual float LoseStamina(float amount)
    {
        if (m_EntityStats.isOutOfStamina) return 0f;

        float staminaLoss = amount * Time.deltaTime;
        m_EntityStats.stamina -= staminaLoss;
        if (m_EntityStats.stamina <= 0f)
        {
            m_EntityStats.isOutOfStamina = true;
            m_EntityStats.stamina = 0f;
        }
        m_EntityStats.staminaTimestamp = Time.time;

        return staminaLoss;
    }

    protected virtual float RegainStamina()
    {
        if (m_EntityStats.staminaTimestamp + m_EntityStats.staminaCooldown > Time.time) return 0f;
        if (m_EntityStats.stamina >= 100f) return 0f;

        float staminaRegain = 0f;
        if (m_EntityStats.isMoving)
#if HARDMODE // still unsure if I want this
            staminaRegain = 0f;
#else
            staminaRegain = m_EntityStats.staminaRegen * 0.5f * Time.deltaTime;
#endif
        else
            staminaRegain = m_EntityStats.staminaRegen * Time.deltaTime;

        m_EntityStats.stamina += staminaRegain;

        if (m_EntityStats.stamina >= 50f) m_EntityStats.isOutOfStamina = false;
        if (m_EntityStats.stamina > 100f) m_EntityStats.stamina = 100f;

        return staminaRegain;
    }

    public virtual float TakeDamage(float damage, EntityBase receivedFromEntity = null)
    {
        float damageTaken = damage * (1f - m_EntityStats.armor / 100f * 0.66f);
        m_EntityStats.health -= damageTaken;

        if (m_EntityStats.health <= 0f)
        {
            m_EntityStats.health = 0f;
            Die(receivedFromEntity);
        }
        if (m_EntityStats.health > 0f) Knockback(receivedFromEntity);

        HandleAttacker(receivedFromEntity);

        m_EntityStats.healthTimestamp = Time.time;

        return damageTaken;
    }

    public virtual float RunAway(EntityBase fromEntity) // not working because it's not coroutine // TODO: fix this
    {
        m_EntityStats.isMoving = true;
        if (m_EntityStats.isOutOfStamina) return WalkAway();
        Vector3 directionAway = (transform.position - fromEntity.transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(directionAway);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        Vector3 movement = transform.forward
            * m_EntityStats.runSpeed
            * Time.deltaTime;
        EntityRigidbody.MovePosition(EntityRigidbody.position + movement);
        LoseStamina(2f);
        return movement.magnitude;
    }

    public virtual float WalkAway()
    {
        m_EntityStats.isMoving = true;
        Vector3 directionAway = (transform.position - attackTarget.transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(directionAway);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        Vector3 movement = transform.forward
            * m_EntityStats.speed
            * Time.deltaTime;
        EntityRigidbody.MovePosition(EntityRigidbody.position + movement);
        return movement.magnitude;
    } // not working because it's not coroutine // TODO: fix this

    protected virtual float EatHeal(float amount) // each food item heals a different amount, hence the parameter
    {
        if (m_EntityStats.health >= m_EntityStats.maxHealth) return 0f;

        m_EntityStats.health += amount;
        if (m_EntityStats.health > m_EntityStats.maxHealth) 
            m_EntityStats.health = m_EntityStats.maxHealth;

        m_EntityStats.healthTimestamp = Time.time;

        return amount;
    }

    protected virtual float NaturalHeal()
    {
        if (m_EntityStats.healthTimestamp + (m_EntityStats.isLowHealth ? m_EntityStats.healthCooldown * 0.5 : m_EntityStats.healthCooldown) > Time.time) return 0f; // Wait 3s/5s after last damage taken
        if (m_EntityStats.health >= m_EntityStats.maxHealth) return 0f;

        float healAmount = m_EntityStats.healthRegen * Time.deltaTime;
        m_EntityStats.health += healAmount;

        if (m_EntityStats.health > m_EntityStats.maxHealth) m_EntityStats.health = m_EntityStats.maxHealth;

        return healAmount;
    }

    public void HandleAttacker(EntityBase receivedFromEntity) //hardcoded for now
    {
        switch (m_EntityStats.hostilityLevel)
        {
            case HostilityLevel.Friendly:
                if (receivedFromEntity)
                    RunAway(receivedFromEntity); // TODO: Use AI navigation package to run away from attacker
                break;
            case HostilityLevel.Neutral:
                if (receivedFromEntity)
                {
                    attackTarget = receivedFromEntity;
                    target = attackTarget;
                    if (!m_EntityStats.isPursuing)
                        StartCoroutine(Pursue(attackTarget));
                }
                break;
            case HostilityLevel.Hostile:
                attackTarget = receivedFromEntity;
                if (!m_EntityStats.isPursuing)
                    StartCoroutine(Pursue(attackTarget));
                break;
        }

        m_EntityStats.reactionTimestamp = Time.time;
    }

    protected virtual void Die(EntityBase byEntity = null) 
    {
        Destroy(gameObject); // temporary
        //gameObject.SetActive(false);
        if (byEntity) byEntity.attackTarget = null;
        if (byEntity) return; 
        // TODO: Handle kill crediting, experience gain, etc. // Means this entity was killed by non-entity source
        // TODO: Handle entity death (e.g., play animation, drop loot, remove from game world)
    }

    protected virtual void Knockback(EntityBase fromEntity) // apply to fromEntity
    {
        if (!fromEntity) return;
        Vector3 knockbackDirection = (fromEntity.transform.position - transform.position).normalized;
        fromEntity.EntityRigidbody.AddForce(knockbackDirection * m_EntityStats.knockbackForce, ForceMode.Impulse);
        fromEntity.EntityRigidbody.AddForce(new Vector3(0, 1, 0).normalized * m_EntityStats.knockbackForce*0.4f, ForceMode.Impulse);
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
                if (entity.GetType() == type)
                {
                    target = entity;
                    return true;
                }
        }
        return false;
    }

    // execute only if enabled
    public void DisplayStatChange<T>(T arg)
    {
        //if (GlobalSettings.ShowStatsChange.enabled) // TODO: Create a class to hold values like enabled, size, etc.
        if (Time.time < statChangeDisplayCooldown) return;

        // TODO: Push the stat change to a UI manager to handle display
        // This will be done using a UITK system with a floating text prefab

        // EntityUIManager.DisplayStat(transform, typeof(arg), arg);

        statChangeDisplayCooldown = Time.time + statChangeDisplayCooldown;
    }

    public Guid GetEntityGUID() => m_EntityStats.EntityID;
    public static Guid GetEntityGUID(EntityBase entity) => entity ? entity.m_EntityStats.EntityID : Guid.Empty;
    public static EntityBase GetEntityByGUID(Guid guid) => EntityRegistry.TryGetValue(guid, out var entity) ? entity : null;
    public float GetProximityToEntity(EntityBase entity) => Vector3.Distance(transform.position, entity.transform.position);
    //public static float GetProximityToEntity(EntityBase entity1, EntityBase entity2) => Vector3.Distance(entity1.transform.position, entity2.transform.position);
    public float GetProximityToEntity(Guid guid) => Vector3.Distance(transform.position, GetEntityByGUID(guid).transform.position);
    //public static float GetProximityToEntity(Guid guid1, Guid guid2) => Vector3.Distance(GetEntityByGUID(guid1).transform.position, GetEntityByGUID(guid2).transform.position);
}

