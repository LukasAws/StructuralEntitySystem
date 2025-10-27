
using Entities.Interfaces;
using UnityEngine;

namespace Entities
{
    public abstract class HostileEntityBase : EntityBase, IHostileEntity
    {
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

