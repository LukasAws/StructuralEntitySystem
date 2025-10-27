using Entities.SpeciesStats;
using UnityEngine;

namespace Entities.Species
{
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerBase : EntityBase
    {
        public override void HandleAttack(EntityBase attackingEntity)
        {
            
        }
    }
}
