namespace Entities.Interfaces
{
    public interface IFriendlyEntity : IEntityHostility<EntityBase>
    {
        void OnEscapeStart(EntityBase attacker);
        
        void IEntityHostility<EntityBase>.HandleAttack(EntityBase attacker)
        {
            OnEscapeStart(attacker);
        }
    }
}
