using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Entities.SpeciesStats
{
    [Serializable]
    public class PlayerStats : EntityStats
    {
        //[ContextMenu("Restore Defaults")] //-- doesn't work with override. Why? -- Might be a Unity limitation.
        public override void RestoreDefaults() // restore to defaults in editor
        {
            speedBoost = 1.25f;
            staminaBoost = 1.2f;
            staminaLossReduction = 0.8f;
        }

        [FormerlySerializedAs("SpeedBoost")] [Range(1.1f, 2f)]
        public float speedBoost = 1.25f;

        [FormerlySerializedAs("StaminaBoost")] [Range(1.1f, 2f)]
        public float staminaBoost = 1.2f;

        [FormerlySerializedAs("StaminaLossReduction")] [Range(0.5f, 1f)]
        public float staminaLossReduction = 0.6f;
    }
}
