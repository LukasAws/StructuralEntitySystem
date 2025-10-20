using UnityEngine;

public abstract class ChickenBase : EntityBase
{
    public float EggLayingInterval = 300f; // Time in seconds between egg laying
    public float EggLayingAmount = 1f; // Number of eggs laid at a time

    protected virtual void LayEggs() 
    {
        // Logic to instantiate egg objects in the game world
        Debug.Log($"Chicken laid {EggLayingAmount} eggs!");
    }
}
