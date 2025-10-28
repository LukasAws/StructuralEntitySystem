using System.Collections.Generic;
using Entities.Hostility.Interfaces;

namespace Entities.Hostility
{
    public abstract class HostileEntityBase : EntityBase, IHostileEntity
    {
        protected new void FixedUpdate()
        {
            base.FixedUpdate();
            
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

            entityStats.isMoving = false; // has to be the last line in Update
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

