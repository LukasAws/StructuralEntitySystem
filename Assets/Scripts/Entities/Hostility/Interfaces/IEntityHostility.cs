namespace Entities.Hostility.Interfaces
{
    public interface IEntityHostility<in TEntityBase> where TEntityBase : EntityBase
    {
        public abstract void HandleAttack(TEntityBase attacker);
    }
}
