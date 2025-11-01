using System;
using System.Collections.Generic;
using UnityEngine;

namespace Entities
{
    
    [Serializable]
    public class EntityStats : MonoBehaviour
    {
        public Guid EntityID { get; private set; } = Guid.NewGuid();

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
        public float recoveryTimeLowHealth = 5f; // seconds to recover after low health
        [Range(10f, 50f)]
        public float lowHealthLowerThreshold = 25f; // threshold for low health
        [Range(10f, 50f)]
        public float lowHealthUpperThreshold = 50f; // threshold for low health

        [Header("Movement")]
        [Range(0f, 10f)]
        public float speed = 3f; // max 10
        [Range(0f, 15f)]
        public float runSpeed = 6f; // max 15
        [Min(0f)]
        public float stamina = 100f; // current stamina
        [Min(0f)]
        public float maxStamina = 100f; // max stamina
        [Range(0f, 10f)]
        public float staminaRegen = 5f; // max 30 per second
        [Range(0f, 5f)]
        public float staminaLoss = 3f; // max 30 per second
        [Range(1f, 15f)]
        public float staminaCooldown = 5f; // seconds to recover after running out of stamina
        [Range(0f, 30f)]
        public float outOfStaminaLowerThreshold = 15f;
        [Range(30, 100f)]
        public float outOfStaminaUpperThreshold = 65f;

        [Header("Attack")]
        [Min(0f)]
        public float attackDamage = 5f; // no max
        [Range(0f, 10f)]
        public float attackRange = 1f; // max 10
        [Min(0f)]
        public float attackCooldown = 0.6f; // seconds between attacks
        [Range(0f, 45f)]
        public float visibilityDistance = 10f; // max 45
        [Range(1f, 15f)]
        public float knockbackForce = 7f; // force applied when knocked back -- temporary -- will be handled by weapons later


        [Header("Misc")]
        public float matingTime = 5f;
        public float matingCooldown = 20f;
        public float obstacleDetectionDistance = 2f;
        public float experience = 3f; // no max
        public List<EntityBase> attackedBy = new List<EntityBase>();
        public ushort entitiesKilledCount = 0;

        [Header("Intermediate Variables")]
        public bool isOutOfStamina = false;
        public bool isLowHealth = false;
        public bool isMoving = false;

        public float attackTimestamp = -Mathf.Infinity; // last attack time
        public float staminaTimestamp = -Mathf.Infinity; // last stamina change time
        public float healthTimestamp = -Mathf.Infinity; // last health change time
        public float matingTimestamp = -Mathf.Infinity;
        
        //-----------------------------------------------------------------------------------
        
        #region Methods
        
        private void Update()
        {
            NaturalHeal();
            RegainStamina();
            
            if (health < lowHealthLowerThreshold) isLowHealth = true;
            else if (health > lowHealthUpperThreshold) isLowHealth = false;
            
            if (stamina < outOfStaminaLowerThreshold) isOutOfStamina = true;
            else if (stamina > outOfStaminaUpperThreshold) isOutOfStamina = false;
        }
        
        protected virtual float RegainStamina()
        {
            if (staminaTimestamp + staminaCooldown > Time.time) return 0f;
            if (stamina >= maxStamina) return 0f;

            float staminaRegain;
            if (isMoving)
#if HARDMODE
                staminaRegain = 0f;
#else
                staminaRegain = staminaRegen * 0.5f * Time.deltaTime;
#endif
            else
                staminaRegain = staminaRegen * Time.deltaTime;
            
            stamina = Mathf.Clamp(stamina + staminaRegain, 0f, maxStamina);

            return staminaRegain;
        }
        
        public virtual float LoseStamina()
        {
            if (isOutOfStamina) return 0f;

            stamina = Mathf.Clamp(stamina - staminaLoss * Time.deltaTime, 0f, maxStamina);
            
            staminaTimestamp = Time.time;

            return staminaLoss;
        }
        
        protected virtual float NaturalHeal()
        {
            if (healthTimestamp + (isLowHealth ? recoveryTimeLowHealth * 0.5 : recoveryTimeLowHealth) > Time.time) return 0f;
            if (health >= maxHealth) return 0f;

            float healAmount = healthRegen * Time.deltaTime;

            health = Mathf.Clamp(health + healAmount, 0f, maxHealth);

            return healAmount;
        }
        
        protected virtual float EatHeal(float amount) // each food item heals a different amount, hence the parameter
        {
            if (health >= maxHealth) return 0f;

            health = Mathf.Clamp(health + amount, 0f, maxHealth);

            return amount;
        }
        
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
        
        #endregion
        
        
    }
    
    
}