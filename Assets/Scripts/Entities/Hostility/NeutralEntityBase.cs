using Entities.Interfaces.Hostility;

namespace Entities.Hostility
{
    public abstract class NeutralEntityBase : EntityBase, INeutralEntity
    {
        public void OnPursuitStart(EntityBase attacker)
        {
            onPursuitStart?.Invoke(attacker);       
        }

        public void OnPursuitEnd(EntityBase attacker)
        {
            onPursuitEnd?.Invoke(attacker);      
        }

        public void OnEscapeStart(EntityBase attacker)
        {
            onEscapeStart?.Invoke(attacker);      
        }

        public void OnEscapeEnd(EntityBase attacker)
        {
            onEscapeEnd?.Invoke(attacker);      
        }

        public override void HandleAttack(EntityBase attacker)
        {
            if (entityStats.isLowHealth)
            {
                OnPursuitEnd(attacker);
                OnEscapeStart(attacker);
            }
            else
            {
                OnEscapeEnd(attacker);
                OnPursuitStart(attacker);
            }
        }
    }
}
