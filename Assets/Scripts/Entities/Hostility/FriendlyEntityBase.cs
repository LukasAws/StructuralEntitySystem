using Entities.Hostility.Interfaces;

namespace Entities.Hostility
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
