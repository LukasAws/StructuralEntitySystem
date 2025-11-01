namespace Entities.Interfaces.Hostility
{
    public interface IHostileEntity : IEntityHostility<EntityBase>
    {
        void OnPursuitStart(EntityBase target);
        
        void IEntityHostility<EntityBase>.HandleAttack(EntityBase attacker)
        {
            OnPursuitStart(attacker);
        }
    }
}
