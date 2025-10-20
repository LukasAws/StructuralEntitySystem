using System;
using UnityEngine;

[Serializable]
public class HorseStats : EntityStats
{
    //[ContextMenu("Restore Defaults")] //-- doesn't work with override. Why? -- Might be a Unity limitation.
    public override void RestoreDefaults() // restore to defaults in editor
    {
        SpeedBoost = 1.25f;
        StaminaBoost = 1.2f;
        StaminaLossReduction = 0.8f;
    }

    [Range(1.1f, 2f)]
    public float SpeedBoost = 1.25f;
    [Range(1.1f, 2f)]
    public float StaminaBoost = 1.2f;
    [Range(0.5f, 1f)]
    public float StaminaLossReduction = 0.6f;
}