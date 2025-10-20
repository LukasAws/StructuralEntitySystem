using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class ZombieBase : MonoBehaviour
{
    public virtual List<Guid> nearbyEntities { get; protected set; }

    public virtual void ScanForNearbyEntities(float scanRadius)
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, scanRadius);
        foreach (Collider collider in hitColliders)
        {
            EntityBase entity = collider.GetComponent<EntityBase>();
            if (entity != null && entity != this)
            {
                nearbyEntities.Add(entity.GetEntityGUID());
            }
        }
    }

    private void FixedUpdate()
    {
        ScanForNearbyEntities(25f);
    }
}
