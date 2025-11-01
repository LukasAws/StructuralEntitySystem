using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Entities;

namespace Entities.Interfaces
{
    public interface IEntityMating<TSpecies> where TSpecies : EntityBase
    {
        public IEnumerator Mate();

        public void FindEntityToMateWith(float distance);
    }
}