using UnityEngine;

namespace Entities.Misc.Input
{
    public class InputValues : MonoBehaviour
    {
        public static InputValues Instance;

        [Tooltip("Mouse sensitivity for camera movement.")]
        [Range(0.1f, 20f)]
        [SerializeField] private float cameraSensitivity = 5f;
        [Tooltip("Sprint movement speed of the camera.")]
        [Range(10f, 30f)]
        [SerializeField] private float cameraSprintSpeed = 20f;
        [Tooltip("Normal movement speed of the camera.")]
        [Range(5f, 20f)]
        [SerializeField] private float cameraNormalSpeed = 7f;
        [Tooltip("Delay in seconds when switching between controlled entities.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float cameraSwitchEntityDelay = 0.5f;

        private void OnEnable()
        {
            Instance = this;
            LoadValues();
        }

        private void OnDisable()
        {
            SaveValues();
        }

        public void LoadValues()
        {
            cameraNormalSpeed = PlayerPrefs.GetFloat("CameraNormalSpeed", 7f);
            cameraSprintSpeed = PlayerPrefs.GetFloat("CameraSprintSpeed", 20f);
            cameraSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 5f);
            cameraSwitchEntityDelay = PlayerPrefs.GetFloat("CameraSwitchEntityDelay", 0.5f);
        }

        public void SaveValues()
        {
            PlayerPrefs.SetFloat("MouseSensitivity", cameraSensitivity);
            PlayerPrefs.SetFloat("CameraSprintSpeed", cameraSprintSpeed);
            PlayerPrefs.SetFloat("CameraNormalSpeed", cameraSprintSpeed);
            PlayerPrefs.SetFloat("CameraSwitchEntityDelay", cameraSwitchEntityDelay);
        }

        public void SetCameraSensitivity(float sensitivity) => cameraSensitivity = Mathf.Clamp(sensitivity, 0.1f, 20f);
        public float GetCameraSensitivity() => cameraSensitivity;
        public void SetCameraSprintSpeed(float sprintSpeed) => cameraSprintSpeed = Mathf.Clamp(sprintSpeed, 10f, 30f);
        public float GetCameraSprintSpeed() => cameraSprintSpeed;
        public void SetCameraNormalSpeed(float speed) => cameraSprintSpeed = Mathf.Clamp(speed, 5f, 20f);
        public float GetCameraNormalSpeed() => cameraNormalSpeed;
        public void SetCameraSwitchEntityDelay(float delay) => cameraSwitchEntityDelay = Mathf.Clamp(delay, 0.1f, 2f);
        public float GetCameraSwitchEntityDelay() => cameraSwitchEntityDelay;
    }
}

