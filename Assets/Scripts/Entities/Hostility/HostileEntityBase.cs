using System;
using System.Collections.Generic;
using Entities.Hostility.Interfaces;
using UnityEngine;

namespace Entities.Hostility
{
    public abstract class HostileEntityBase : EntityBase, IHostileEntity
    {
        [System.Flags]
        public enum TypesToAttack : byte
        {
            None = 0, 
            Friendly = 1,
            Neutral = 2,
            Hostile = 4,
            Explosive = 8,
        }

        public TypesToAttack typesToAttack;

        private readonly List<Type> _attackedTypes = new();

        private void OnEnable()
        {
            // ChatGPT created the further code
            _attackedTypes.Clear();

            // Loop through all flags defined in the enum
            foreach (TypesToAttack flag in Enum.GetValues(typeof(TypesToAttack)))
            {
                if (flag == TypesToAttack.None)
                    continue;

                // Bitwise check if the flag is set
                if ((typesToAttack & flag) == flag)
                {
                    switch (flag)
                    {
                        case TypesToAttack.Friendly:
                            _attackedTypes.Add(typeof(FriendlyEntityBase));
                            break;

                        case TypesToAttack.Neutral:
                            _attackedTypes.Add(typeof(NeutralEntityBase));
                            break;

                        case TypesToAttack.Hostile:
                            _attackedTypes.Add(typeof(HostileEntityBase));
                            break;

                        case TypesToAttack.Explosive:
                            Debug.Log("Explosive entities are not yet implemented");
                            // _attackedTypes.Add(typeof(ExplosiveEntityBase));
                            break;
                    }
                }
            }
        }

        protected void FixedUpdate()
        {
            if (!AttackTarget)
            {
                GetEntitiesByProximity(entityStats.visibilityDistance, out List<EntityBase> entities, true);
                FindEntityToAttack(
                    entities,
                    out EntityBase a,
                    _attackedTypes.ToArray()
                );
                AttackTarget = a;
            }
            if (AttackTarget)
                OnPursuitStart(AttackTarget);
        }
        
        public override void HandleAttack(EntityBase attackingEntity)
        {
            OnPursuitStart(attackingEntity);      
        }

        public void OnPursuitStart(EntityBase target)
        {
            onPursuitStart?.Invoke(target);      
        }
    }
}

