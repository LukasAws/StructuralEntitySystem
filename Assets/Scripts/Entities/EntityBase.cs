using System;
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

    public static readonly Dictionary<Guid, EntityBase> entityRegistry = new Dictionary<Guid, EntityBase>();
    protected EntityStats m_EntityStats => GetComponent<EntityStats>();
    protected Rigidbody EntityRigidbody => GetComponent<Rigidbody>();

    // TODO: Let user choose the value in settings later -- may have to be static
    // This should be moved to the UI Manager or something
    public float statChangeDisplayCooldown = 2f; // seconds between displaying stat changes

    protected EntityBase attackTarget;

    public float healthChangedTimestamp { get; protected set; } = -Mathf.Infinity;
    public float staminaChangedTimestamp { get; protected set; } = -Mathf.Infinity;

    protected virtual void Awake()
    {
        if (!entityRegistry.ContainsKey(m_EntityStats.EntityID))
            entityRegistry.Add(m_EntityStats.EntityID, this);

        if((int)m_EntityStats.hostilityLevel == 2)
            m_EntityStats.isAggro = true;

        // TODO: Aggro mechanic is not fully implemented yet !!!!!!
    }

    private void Update()
    {
        if (attackTarget) Pursue(attackTarget);
        // TOOD: else Roam();
    }

    protected virtual void OnDestroy()
    {
        entityRegistry.Remove(m_EntityStats.EntityID);
    }

    public virtual void Attack(EntityBase entity)
    {
        if (entity.GetProximityToEntity(this) > m_EntityStats.attackRange) return;

        entity.TakeDamage(m_EntityStats.attackDamage, this);
    }

    public virtual void Pursue(EntityBase entity)
    {
        // TOOD: Implement invoking of persuation and attacking of an entity
        //Run(entity);
        //Attack(entity);
    }


    protected virtual float Walk()
    {
        Vector3 movement = transform.forward * m_EntityStats.speed * Time.deltaTime;
        EntityRigidbody.MovePosition(EntityRigidbody.position + movement);

        return movement.magnitude;
    }

    //protected virtual float Walk(EntityBase entity)
    //{
    //    // TODO: Implement movement towards entity with AI Navigation package
    //    Vector3 movement = transform.forward * m_EntityStats.speed * Time.deltaTime;
    //    EntityRigidbody.MovePosition(EntityRigidbody.position + movement);

    //    return movement.magnitude;
    //}

    protected virtual float Run()
    {
        if (m_EntityStats.stamina <= 0f) return Walk();

        Vector3 movement = transform.forward * 
            (m_EntityStats.stamina > 0 ? m_EntityStats.runSpeed : m_EntityStats.speed)
            * Time.deltaTime;
        EntityRigidbody.MovePosition(EntityRigidbody.position + movement);

        return movement.magnitude;
    }

    //protected virtual float Run(EntityBase entity)
    //{
    //    TODO: Implement movement towards entity with AI Navigation package

    //    if (m_EntityStats.stamina <= 0f) return Walk();

    //    Vector3 movement = transform.forward *
    //        (m_EntityStats.stamina > 0 ? m_EntityStats.runSpeed : m_EntityStats.speed)
    //        * Time.deltaTime;
    //    EntityRigidbody.MovePosition(EntityRigidbody.position + movement);

    //    return movement.magnitude;
    //}

    protected virtual float LoseStamina(float amount)
    {
        float staminaLoss = amount * Time.deltaTime;
        m_EntityStats.stamina -= staminaLoss;
        if (m_EntityStats.stamina < 0f) m_EntityStats.stamina = 0f;

        staminaChangedTimestamp = Time.time;

        return staminaLoss;
    }

    protected virtual float RegainStamina(float amount)
    {
        // TODO: if walking, regain stamina slower? or not at all?
        if (staminaChangedTimestamp + 5f > Time.time) return 0f;
        if (m_EntityStats.stamina >= 100f) return 0f;

        float staminaGain = amount * Time.deltaTime;
        m_EntityStats.stamina += staminaGain;
        if (m_EntityStats.stamina > 100f) m_EntityStats.stamina = 100f;

        staminaChangedTimestamp = Time.time;

        return staminaGain;
    }

    protected virtual float TakeDamage(float damage, EntityBase receivedFromEntity = null)
    {
        float damageTaken = damage * (1f - m_EntityStats.armor / 100f * 0.66f);
        m_EntityStats.health -= damageTaken;

        if (m_EntityStats.health <= 0f) Die();

        healthChangedTimestamp = Time.time;

        if(receivedFromEntity && m_EntityStats.hostilityLevel != HostilityLevel.Friendly)
            attackTarget = receivedFromEntity;

        return damageTaken;
    }

    protected virtual float EatHeal(float amount)
    {
        if (m_EntityStats.health >= m_EntityStats.maxHealth) return 0f;

        m_EntityStats.health += amount;
        if (m_EntityStats.health > m_EntityStats.maxHealth) m_EntityStats.health = m_EntityStats.maxHealth;

        healthChangedTimestamp = Time.time;

        return amount;
    }

    protected virtual float NaturalHeal()
    {
        if (healthChangedTimestamp + 5f > Time.time) return 0f; // Wait 5 seconds after last damage taken
        if (m_EntityStats.health >= m_EntityStats.maxHealth) return 0f;

        float healAmount = m_EntityStats.healthRegen * Time.deltaTime;
        m_EntityStats.health += healAmount;

        if (m_EntityStats.health > m_EntityStats.maxHealth) m_EntityStats.health = m_EntityStats.maxHealth;
        healthChangedTimestamp = Time.time;

        return healAmount;
    }

    public void Die() 
    {
        // TODO: Handle entity death (e.g., play animation, drop loot, remove from game world)
    }

    public Guid GetEntityGUID()
    {
        return m_EntityStats.EntityID;
    }

    public static EntityBase GetEntityByGUID(Guid guid)
    {
        entityRegistry.TryGetValue(guid, out var entity);
        return entity;
    }

    public float GetProximityToEntity(EntityBase entity)
    {
        return Vector3.Distance(this.transform.position, entity.transform.position);
    }

    public float GetProximityToEntity(Guid guid)
    {
        EntityBase entity = GetEntityByGUID(guid);
        return Vector3.Distance(this.transform.position, entity.transform.position);
    }

    // execute only if enabled
    public virtual void DisplayStatChange<T>(T arg)
    {
        //if (GlobalSettings.ShowStatsChange.enabled) // TODO: Create a class to hold values like enabled, size, etc.
        if (Time.time < statChangeDisplayCooldown) return;

        // TODO: Push the stat change to a UI manager to handle display
        // This will be done using a UITK system with a floating text prefab

        statChangeDisplayCooldown = Time.time + statChangeDisplayCooldown;
    }
}

