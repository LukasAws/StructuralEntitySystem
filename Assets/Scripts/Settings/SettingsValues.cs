using Entities.Hostility;
using UnityEngine;

namespace Settings
{
    [HideInInspector]
    public class SettingsValues : MonoBehaviour
    {
        #region Singleton

        public static SettingsValues Instance;

        [Header("Simulation")]
        public float maxSimulationSpeed = 5.0f;
        public float simulationSpeed = 1.0f;
        public float minSimulationSpeed = 0.1f;
        public bool enableAutoSpawning = true;
        public bool enableMating = true;
        public int autoSpawnIntervalSeconds = 10; //nested under enableAutoSpawning

        //Spawning
        public int maxEntities = 100;
        public float defaultExperiencePerEntity = 3.0f;

        //Attack
        public HostileEntityBase.TypesToAttack hostile_typesToAttack;
        public bool neutralEntitiesWillAttackBack = false;

        #endregion

        #region Unity Methods

        private void OnEnable()
        {
            Instance = this;
            LoadValues();
        }



        private void OnDisable()
        {
            SaveValues();
        }

        #endregion

        #region Methods

        public void LoadValues() { }

        public void SaveValues() { }

        #endregion
    }
}


