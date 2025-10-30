using UnityEngine;

namespace Entities.Settings
{
    public class SettingsManager : MonoBehaviour // script where all settings are managed through UI
    {
        public static SettingsManager Instance;

        private void OnEnable()
        {
            Instance = this;
        }
    }
}
