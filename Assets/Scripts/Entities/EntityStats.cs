using System;
using System.Collections.Generic;
using UnityEngine;

namespace Entities
{
    
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
            experience = 3f;
        }

        public EntityBase.HostilityLevel hostilityLevel = EntityBase.HostilityLevel.Neutral;

        [Header("Health")]
        [Min(0f)]
        public float health = 100; // current health
        [Range(0f, 100f)]
        public float maxHealth = 100f; // max 100
        [Range(0f, 100f)]
        public float armor = 0f; // max 100
        [Range(0f, 5f)]
        public float healthRegen = 2f; // max 5 per second
        [Range(1f, 20f)]
        public float healthCooldown = 5f; // seconds to recover after low health
        [Range(10f, 50f)]
        public float lowHealthThreshold = 25f; // threshold for low health

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
        [Range(1f, 15f)]
        public float staminaCooldown = 5f; // seconds to recover after running out of stamina
        [Range(10f, 50f)]
        public float outOfStaminaUpperThreshold = 50f;

        [Header("Attack")]
        [Min(0f)]
        public float attackDamage = 5f; // no max
        [Range(0f, 10f)]
        public float attackRange = 1f; // max 10
        [Min(0f)]
        public float attackCooldown = 0.6f; // seconds between attacks
        [Range(0f, 45f)]
        public float visibilityDistance = 10f; // max 45


        [Header("Misc")]
        public float obstacleDetectionDistance = 2f;
        public Guid EntityID { get; private set; } = Guid.NewGuid();

        public int entitiesKilledCount = 0;

        [Min(0f)]
        public float experience = 3f; // no max

        [Header("Intermediate Variables")]
        public bool isOutOfStamina = false;
        public bool isLowHealth = false;
        public bool isMoving = false;

        public float attackTimestamp = -Mathf.Infinity; // last attack time
        public float staminaTimestamp = -Mathf.Infinity; // last stamina change time
        public float healthTimestamp = -Mathf.Infinity; // last health change time

        public float knockbackForce = 7f; // force applied when knocked back -- temporary -- will be handled by weapons later
        
        public List<EntityBase> attackedBy = new List<EntityBase>();
    }
}