using System;
using UnityEngine;

[RequireComponent(typeof(HorseStats))]
public abstract class HorseBase : EntityBase
{
    HorseStats HorseStats => GetComponent<HorseStats>();
    //protected override float RunToFrom() 
    //{
    //    if (ES.stamina <= 0f) return Wander();

    //    Vector3 movement = transform.forward
    //        * ES.runSpeed
    //        * Time.deltaTime
    //        * HorseStats.SpeedBoost;
    //    EntityRigidbody.MovePosition(EntityRigidbody.position + movement);

    //    return movement.magnitude;
    //}

    //protected override float Wander()
    //{
    //    Vector3 movement = transform.forward 
    //        * ES.speed 
    //        * Time.deltaTime
    //        * HorseStats.SpeedBoost;
    //    EntityRigidbody.MovePosition(EntityRigidbody.position + movement);

    //    return movement.magnitude;
    //}

    //protected override float LoseStamina(float amount)
    //{
    //    if (ES.stamina <= 0f) return 0f;

    //    float staminaLoss = amount 
    //        * Time.deltaTime 
    //        * HorseStats.StaminaLossReduction;
    //    ES.stamina -= staminaLoss;

    //    if (ES.stamina < 0f) ES.stamina = 0f;

    //    ES.staminaTimestamp = Time.time;

    //    return staminaLoss;
    //}

    //private void OnEnable()
    //{
    //    ES.maxStamina *= HorseStats.StaminaBoost;
    //    ES.stamina = ES.maxStamina;
    //}
}
