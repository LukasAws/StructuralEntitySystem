using UnityEngine;
using System;

namespace Entities.Misc
{
    public class TypesToAttack
    {
        [System.Flags]
        public enum TypesToAttackTemp : byte // change the class name later
        {
            None = 0,
            Friendly = 1,
            Neutral = 2,
            Hostile = 4,
            Explosive = 8,
        }
        public TypesToAttackTemp typesToAttack;
    }
}

