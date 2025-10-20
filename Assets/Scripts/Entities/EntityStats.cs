using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[Serializable]
public class EntityStats : MonoBehaviour
{
    [ContextMenu("Restore Defaults")]
    public virtual void RestoreDefaults() // restore to defaults in editor
    {
        health = 100f;
        maxHealth = 100f;
        armor = 0f;
        healthRegen = 2f;
        speed = 3f;
        runSpeed = 6f;
        stamina = 100f;
        staminaRegen = 15f;
        experience = 0f;
    }

    [Header("Health")]
    [Min(0f)]
    public float health = 100; // current health
    [Range(0f, 100f)]
    public float maxHealth = 100f; // max 100
    [Range(0f, 100f)]
    public float armor = 0f; // max 100
    [Range(0f, 5f)]
    public float healthRegen = 2f; // max 5 per second

    [Header("Movement")]
    [Range(0f, 10f)]
    public float speed = 3f; // max 10
    [Range(0f, 15f)]
    public float runSpeed = 6f; // max 15
    [Min(0f)]
    public float stamina = 100f; // current stamina
    [Min(0f)]
    public float maxStamina = 100f; // max stamina
    [Range(0f, 30f)]
    public float staminaRegen = 15f; // max 30 per second

    [Header("Attack")]
    [Min(0f)]
    public float attackDamage = 10f; // no max
    [Range(0f, 10f)]
    public float attackRange = 5f; // max 10
    [Min(0f)]
    public float attackCooldown = 0.6f; // seconds between attacks
    [Range(0f, 75f)]
    public float pursuitRange = 25f; // max 75


    [Header("Misc")]
    public Guid EntityID { get; private set; } = Guid.NewGuid();

    [Min(0f)]
    public float experience = 0f; // no max

    public EntityBase.HostilityLevel hostilityLevel = EntityBase.HostilityLevel.Neutral;

    [Header("Intermediate Variables")]
    public bool isPursuing = false;
    public bool isOutOfStamina = false;
    public bool isLowHealth = false;
    public bool isMoving = false;
    public float reactionCooldown = 15f; // seconds to react
    public float staminaCooldown = 5f; // seconds to recover after running out of stamina
    public float healthCooldown = 5f; // seconds to recover after low health
    public float reactionTimestamp = -Mathf.Infinity; // last reaction time
    public float attackTimestamp = -Mathf.Infinity; // last attack time
    public float staminaTimestamp = -Mathf.Infinity; // last stamina change time
    public float healthTimestamp = -Mathf.Infinity; // last health change time

    public float knockbackForce = 7f; // force applied when knocked back -- temporary -- will be handled by weapons later

}