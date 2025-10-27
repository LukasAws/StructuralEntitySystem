using Entities.Interfaces;
using UnityEngine;

namespace Entities
{
    public abstract class FriendlyEntityBase : EntityBase, IFriendlyEntity
    {
        public void OnEscapeStart(EntityBase attacker)
        {
            onEscapeStart?.Invoke(attacker);      
        }

        public override void HandleAttack(EntityBase attackingEntity)
        {
            OnEscapeStart(attackingEntity);
        }
    }
}
