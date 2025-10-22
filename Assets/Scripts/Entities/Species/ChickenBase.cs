using UnityEngine;
using UnityEngine.Serialization;

namespace Entities.Species
{
    public abstract class ChickenBase : EntityBase
    {
        [FormerlySerializedAs("EggLayingInterval")] public float eggLayingInterval = 300f; // Time in seconds between egg laying
        [FormerlySerializedAs("EggLayingAmount")] public float eggLayingAmount = 1f; // Number of eggs laid at a time

        protected virtual void LayEggs() 
        {
            // Logic to instantiate egg objects in the game world
            Debug.Log($"Chicken laid {eggLayingAmount} eggs!");
        }
    }
}
