using UnityEngine;
using UnityEngine.InputSystem;

namespace Entities.Misc
{
    [RequireComponent(typeof(InputValues))]
    public class InputController : MonoBehaviour
    {
        public static InputController Instance { get; private set; }
    
        SimInput _inputActions;

        [SerializeField]
        private Camera topDownCamera;
        [SerializeField]
        private Camera thirdPersonCamera;
        [SerializeField]
        private Camera freeRoamCamera;

        private Vector2 _mousePos;
        private EntityBase.HostilityLevel _hostilityLevel = EntityBase.HostilityLevel.Friendly;
        private enum CameraType
        {
            TopDown = 0, 
            ThirdPerson = 1, 
            FreeRoam = 2 
        }
        
        private CameraType _cameraType = CameraType.TopDown;

        [SerializeField]
        private EntityBase friendlyEntity;
        [SerializeField]
        private EntityBase neutralEntity;
        [SerializeField]
        private EntityBase hostileEnemy;

        [SerializeField]
        private Transform entitiesParent;

        [SerializeField]
        private GameObject previewGo;

        private GameObject _previewObject;

        private bool _previewing = false;
        private bool _previewingCancelled = false;

        private static uint _entityIndex = 0;
        

        private bool sprinting = false;

        private void OnEnable()
        {
            Instance = this;
            _inputActions.TopDownCamera.Enable();
        }

        private void OnDisable()
        {
            _inputActions.Disable();
        }

        private void HandlePreviewing()
        {
            if (!_previewing) return;
            
            if (_previewObject == null)
            {
                _previewObject = Instantiate(previewGo, null, true);
                var renderer = _previewObject.GetComponent<MeshRenderer>();

                switch (_hostilityLevel)
                {
                    case EntityBase.HostilityLevel.Friendly:
                        renderer.material = friendlyEntity.GetComponent<MeshRenderer>().sharedMaterial;
                        break;
                    case EntityBase.HostilityLevel.Neutral:
                        renderer.material = neutralEntity.GetComponent<MeshRenderer>().sharedMaterial;
                        break;
                    case EntityBase.HostilityLevel.Hostile:
                        renderer.material = hostileEnemy.GetComponent<MeshRenderer>().sharedMaterial;
                        break;
                }
            }

            Ray ray = topDownCamera.ScreenPointToRay(new Vector3(_mousePos.x, _mousePos.y, 0));
            if (new Plane(topDownCamera.transform.forward, 1).Raycast(ray, out float distance))
            {
                Vector3 temp = ray.GetPoint(distance);
                _previewObject.transform.position = temp;
            }
        }

        private void OnGUI()
        {
            GUIStyle style = new GUIStyle()
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                normal = {textColor = Color.white}
            };
            if(thirdPersonCamera.isActiveAndEnabled)
                GUILayout.Label($"Entity Health: {thirdPersonCamera.transform.parent.GetComponent<EntityStats>().health}", style);
        }

        private void Awake()
        {
            _inputActions = new SimInput();

            _inputActions.TopDownCamera.SwitchCamera.performed += _ => SwitchCamera(CameraType.ThirdPerson);
            _inputActions.TopDownCamera.ToFriendly.performed += SwitchToFriendly;
            _inputActions.TopDownCamera.ToNeutral.performed += SwitchToNeutral;
            _inputActions.TopDownCamera.ToHostile.performed += SwitchToHostile;
            _inputActions.TopDownCamera.ToggleSimulation.performed += ToggleSimulation;
            _inputActions.TopDownCamera.Instantiate.performed += InstantiateEntity;
            _inputActions.TopDownCamera.Instantiate.canceled += InstantiateEntity;
            _inputActions.TopDownCamera.CancelInstaniation.performed += CancelInstantiation;
            _inputActions.TopDownCamera.MousePos.performed += ctx => _mousePos = ctx.ReadValue<Vector2>();

            _inputActions.ThirdPerson.SwitchCamera.performed += _ => SwitchCamera(CameraType.FreeRoam);
            _inputActions.ThirdPerson.ToggleSimulation.performed += ToggleSimulation;
            _inputActions.ThirdPerson.SwitchEntityInc.performed += SwitchEntity_Increment;
            _inputActions.ThirdPerson.SwitchEntityDec.performed += SwitchEntity_Decrement;
            
            _inputActions.FreeRoam.SwitchCamera.performed += _ => SwitchCamera(CameraType.TopDown);
            _inputActions.FreeRoam.ToggleSimulation.performed += ToggleSimulation;
            _inputActions.FreeRoam.SwitchToEntity.performed += SwitchToEntity;
            _inputActions.FreeRoam.Forward.performed += ctx => _forwardMomentum = ctx.ReadValue<float>();
            _inputActions.FreeRoam.Right.performed += ctx => _rightMomentum = ctx.ReadValue<float>();
            _inputActions.FreeRoam.Up.performed += ctx => _upMomentum = ctx.ReadValue<float>();
            _inputActions.FreeRoam.Rotate.performed += ctx => _rotationMomentum = ctx.ReadValue<Vector2>();
            _inputActions.FreeRoam.Sprint.performed += _ => sprinting = true;
            _inputActions.FreeRoam.SprintSpeed.performed += ctx => SprintSpeedBoostChange(ctx.ReadValue<float>());
            
            _inputActions.FreeRoam.Forward.canceled += _ => _forwardMomentum = 0;
            _inputActions.FreeRoam.Right.canceled += _ => _rightMomentum = 0;
            _inputActions.FreeRoam.Up.canceled += _ => _upMomentum = 0;
            _inputActions.FreeRoam.Rotate.canceled += _ => _rotationMomentum = Vector2.zero;
            _inputActions.FreeRoam.Sprint.canceled += _ => sprinting = false;
        }

        private void SprintSpeedBoostChange(float delta)
        {
            float newSpeed = InputValues.Instance.GetCameraSprintSpeedMultiplier() + delta * 0.1f;
            InputValues.Instance.SetCameraSprintSpeedMultiplier(newSpeed);
        }

        private float _forwardMomentum;
        private float _rightMomentum;
        private float _upMomentum;
        private Vector2 _rotationMomentum;

        private void Update()
        {
            if(_cameraType == CameraType.TopDown)
                HandlePreviewing();
            
            float speed = (sprinting 
                ? InputValues.Instance.GetCameraSprintSpeed() * InputValues.Instance.GetCameraSprintSpeedMultiplier()
                : InputValues.Instance.GetCameraNormalSpeed()) 
                * Time.unscaledDeltaTime;
            
            if(_forwardMomentum != 0)
                freeRoamCamera.transform.position += freeRoamCamera.transform.forward * (_forwardMomentum * speed);
            if(_rightMomentum != 0)
                freeRoamCamera.transform.position += freeRoamCamera.transform.right * (_rightMomentum * speed);
            if(_upMomentum != 0)
                freeRoamCamera.transform.position += freeRoamCamera.transform.up * (_upMomentum * speed);
            if(_rotationMomentum != Vector2.zero)
                RotateCamera();
        }

        private void RotateCamera()
        {
            freeRoamCamera.transform.Rotate(Vector3.up, _rotationMomentum.x * Time.unscaledDeltaTime * InputValues.Instance.GetCameraSensitivity(), Space.World);
            freeRoamCamera.transform.Rotate(Vector3.right, -_rotationMomentum.y * Time.unscaledDeltaTime * InputValues.Instance.GetCameraSensitivity(), Space.Self);
        }

        private void SwitchToEntity(InputAction.CallbackContext callbackContext)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = freeRoamCamera.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit hit, 100, LayerMask.GetMask("Entities")))
            {
                EntityBase entity = hit.collider.GetComponent<EntityBase>();
                if (entity != null)
                {
                    thirdPersonCamera.transform.parent = entity.transform;
                    thirdPersonCamera.transform.localPosition = new Vector3(0, 2, -4);
                    thirdPersonCamera.transform.localRotation = Quaternion.identity;
                    
                    thirdCameraDelay = Time.realtimeSinceStartup;
                    SwitchCamera(CameraType.ThirdPerson);
                }
            }
        }

        private void CancelInstantiation(InputAction.CallbackContext obj)
        {
            if (_previewing)
            {
                _previewing = false;
                if (_previewObject != null)
                {
                    Destroy(_previewObject);
                    _previewObject = null;
                }

                _previewingCancelled = true;
            }
        }

        private void SwitchEntity_Increment(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            if (thirdCameraDelay + InputValues.Instance.GetCameraSwitchEntityDelay() > Time.realtimeSinceStartup) return;

            ++_entityIndex;
            SwitchEntity();
        }
        private void SwitchEntity_Decrement(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            if (thirdCameraDelay + InputValues.Instance.GetCameraSwitchEntityDelay() > Time.realtimeSinceStartup) return;

            --_entityIndex;
            SwitchEntity();
        }

        private void SwitchEntity()
        {
            uint entityCount = (uint)entitiesParent.childCount;

            if (entityCount == 0)
                return;

            _entityIndex %= entityCount;

            Transform child = entitiesParent.GetChild((int)_entityIndex);
            if (child)
            {
                thirdPersonCamera.transform.parent = child;
                thirdPersonCamera.transform.localPosition = new Vector3(0, 2, -4);
                thirdPersonCamera.transform.localRotation = Quaternion.identity;
            }
        }

        private void InstantiateEntity(InputAction.CallbackContext obj)
        {
            if (obj.phase == InputActionPhase.Performed)
            {
                _previewing = true;
            }
            else if (obj.phase == InputActionPhase.Canceled && !_previewingCancelled)
            {
                _previewing = false;

                if (_previewObject != null)
                {
                    Destroy(_previewObject);
                    _previewObject = null;
                }

                EntityBase entity = _hostilityLevel switch
                {
                    EntityBase.HostilityLevel.Friendly => Instantiate(friendlyEntity, entitiesParent, true),
                    EntityBase.HostilityLevel.Neutral => Instantiate(neutralEntity, entitiesParent, true),
                    EntityBase.HostilityLevel.Hostile => Instantiate(hostileEnemy, entitiesParent, true),
                    _ => null
                };

                Ray ray = topDownCamera.ScreenPointToRay(new Vector3(_mousePos.x, _mousePos.y, 0));
                if (new Plane(topDownCamera.transform.forward, 1).Raycast(ray, out float distance))
                {
                    Vector3 temp = ray.GetPoint(distance);
                    if (entity != null)
                        entity.transform.position = temp;
                }
            }

            _previewingCancelled = false;
        }

        private void ToggleSimulation(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            if (Time.timeScale != 0)
                Time.timeScale = 0;
            else
                Time.timeScale = 1;
        }

        private void SwitchToHostile(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            _hostilityLevel = EntityBase.HostilityLevel.Hostile;
        }

        private void SwitchToNeutral(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            _hostilityLevel = EntityBase.HostilityLevel.Neutral;
        }

        private void SwitchToFriendly(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            _hostilityLevel = EntityBase.HostilityLevel.Friendly;
        }

        private float thirdCameraDelay;

        private void SwitchCamera(CameraType cameraType)
        {
            _cameraType = cameraType;
            
            switch (cameraType)
            {
                case CameraType.TopDown:
                    
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    
                    _inputActions.TopDownCamera.Enable();
                    _inputActions.ThirdPerson.Disable();
                    _inputActions.FreeRoam.Disable();
                    break;
                case CameraType.ThirdPerson:
                    
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    
                    if(!thirdPersonCamera.transform.parent && entitiesParent.childCount > 0)
                    {
                        Transform child = entitiesParent.GetChild(0);
                        thirdPersonCamera.transform.parent = child;
                        thirdPersonCamera.transform.localPosition = new Vector3(0, 2, -4);
                        thirdPersonCamera.transform.localRotation = Quaternion.identity;
                    }
                    
                    _inputActions.TopDownCamera.Disable();
                    _inputActions.ThirdPerson.Enable();
                    _inputActions.FreeRoam.Disable();
                    break;
                case CameraType.FreeRoam:
                    
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    
                    freeRoamCamera.transform.position = thirdPersonCamera.transform.position;
                    freeRoamCamera.transform.rotation = thirdPersonCamera.transform.rotation;
                    
                    _inputActions.TopDownCamera.Disable();
                    _inputActions.ThirdPerson.Disable();
                    _inputActions.FreeRoam.Enable();
                    break;
            }
            
            topDownCamera.gameObject.SetActive(_cameraType == CameraType.TopDown);
            thirdPersonCamera.gameObject.SetActive(_cameraType == CameraType.ThirdPerson);
            freeRoamCamera.gameObject.SetActive(_cameraType == CameraType.FreeRoam);
        }

        public void SetNextEntity()
        {
            _entityIndex++;
            SwitchEntity();
        }
    
        public void SetPreviousEntity()
        {
            _entityIndex--;
            SwitchEntity();   
        }
    }
}
