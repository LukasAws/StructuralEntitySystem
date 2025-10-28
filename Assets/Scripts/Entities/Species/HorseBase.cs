using Entities.Hostility;
using UnityEngine;
using Entities.SpeciesStats;

namespace Entities.Species
{
    [RequireComponent(typeof(HorseStats))]
    public abstract class HorseBase : NeutralEntityBase
    {
        protected HorseStats horseStats;

        private new void Awake()
        {
            horseStats = GetComponent<HorseStats>();
        }
        
        
    }
}
