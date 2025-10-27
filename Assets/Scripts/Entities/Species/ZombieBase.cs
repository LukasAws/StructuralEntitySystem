using System.Collections.Generic;
using UnityEngine;
using Entities.SpeciesStats;

namespace Entities.Species
{
    [RequireComponent(typeof(ZombieStats))]
    public abstract class ZombieBase : HostileEntityBase
    {
        void FixedUpdate()
        {
            attackTargetPublic = AttackTarget;

            if (!AttackTarget)
            {
                GetEntitiesByProximity(entityStats.visibilityDistance, out List<EntityBase> entities, true);
                FindEntityToAttack(
                    entities,
                    out EntityBase a,
                    typeof(NeutralEntityBase),
                    typeof(FriendlyEntityBase)
                );
                AttackTarget = a;
                if (AttackTarget)
                    onPursuitStart?.Invoke(AttackTarget);
            }
            
            
            if(transform.position.y <= -20)
                Die();

            RegainStamina();
            NaturalHeal();

            entityStats.isMoving = false; // has to be the last line in Update
        }

    }
}
