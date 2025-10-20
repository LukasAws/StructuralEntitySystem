using System;
using UnityEngine;

[RequireComponent(typeof(HorseStats))]
public abstract class HorseBase : EntityBase
{
    HorseStats HorseStats => GetComponent<HorseStats>();
    protected override float Run() 
    {
        if (m_EntityStats.stamina <= 0f) return Walk();

        Vector3 movement = transform.forward
            * m_EntityStats.runSpeed
            * Time.deltaTime
            * HorseStats.SpeedBoost;
        EntityRigidbody.MovePosition(EntityRigidbody.position + movement);

        return movement.magnitude;
    }

    protected override float Walk()
    {
        Vector3 movement = transform.forward 
            * m_EntityStats.speed 
            * Time.deltaTime
            * HorseStats.SpeedBoost;
        EntityRigidbody.MovePosition(EntityRigidbody.position + movement);

        return movement.magnitude;
    }

    protected override float LoseStamina(float amount)
    {
        if (m_EntityStats.stamina <= 0f) return 0f;

        float staminaLoss = amount 
            * Time.deltaTime 
            * HorseStats.StaminaLossReduction;
        m_EntityStats.stamina -= staminaLoss;

        if (m_EntityStats.stamina < 0f) m_EntityStats.stamina = 0f;

        staminaChangedTimestamp = Time.time;

        return staminaLoss;
    }

    private void OnEnable()
    {
        m_EntityStats.stamina *= HorseStats.StaminaBoost;
    }
}
