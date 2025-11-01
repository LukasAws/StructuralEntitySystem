namespace Entities.Interfaces.Hostility
{
    public interface INeutralEntity : IEntityHostility<EntityBase>
    {
        EntityStats entityStats { get; }
        void OnPursuitStart(EntityBase attacker);
        void OnPursuitEnd(EntityBase attacker);
        void OnEscapeStart(EntityBase attacker);
        void OnEscapeEnd(EntityBase attacker);

        void IEntityHostility<EntityBase>.HandleAttack(EntityBase attacker)
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

