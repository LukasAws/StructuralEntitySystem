using System.Collections;
using System.Collections.Generic;
using Entities.Hostility;
using Entities.Interfaces;
using UnityEngine;
using Entities.SpeciesStats;
using Settings;

namespace Entities.Species
{
    [RequireComponent(typeof(HumanStats))]
    public abstract class HumanBase : NeutralEntityBase, IEntityMating<HumanBase>
    {
        public List<HumanBase> entitiesToMateWith = new();
        public IEnumerator Mate()
        {
            if (entitiesToMateWith.Count == 0) yield break;
            
            while (true)
            {
                if (GetProximityToEntity(entitiesToMateWith[0]) <= entityStats.attackRange && entityStats.matingTimestamp + entityStats.matingCooldown < Time.time)
                {
                    entityStats.matingTimestamp = Time.time;
                    while (entityStats.matingTimestamp + entityStats.matingTime >= Time.time && entityStats.attackedBy.Count == 0 && !AttackTarget)
                    {
                        yield return new WaitForFixedUpdate();
                    }

                    if (entityStats.matingTimestamp + entityStats.matingTime < Time.time && entityStats.attackedBy.Count == 0 && !AttackTarget)
                    {
                        entityStats.matingTimestamp = Time.time;
                        Instantiate(gameObject);
                        entitiesToMateWith.RemoveAt(0);
                    }
                    else
                    {
                        entityStats.matingTimestamp = Time.time;
                    }
                    yield break;
                }
                
                RunToFrom(entitiesToMateWith[0]);
                yield return new WaitForFixedUpdate();
            }
        }
        
        public void FindEntityToMateWith(float distance)
        {
            GetEntitiesByProximity(distance, out List<EntityBase> es, true);
            foreach (var e in es)
            {
                if (typeof(HumanBase).IsAssignableFrom(e.GetType()))
                {
                    entitiesToMateWith.Add(e as HumanBase);
                    return;
                }
            }
        }

        private void FixedUpdate()
        {
            if (entitiesToMateWith.Count == 0 
                && entityStats.attackedBy.Count == 0 
                && !AttackTarget 
                && SettingsValues.Instance.enableMating 
                && entityStats.matingCooldown + entityStats.matingTimestamp < Time.time)
            {
                FindEntityToMateWith(entityStats.visibilityDistance);
                if (entitiesToMateWith.Count == 0) return;
                StopCoroutine(Wander());
                StartCoroutine(Mate());
            }
        }
    }
}
