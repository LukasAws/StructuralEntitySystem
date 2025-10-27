using Entities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Misc
{
    public class InputController : MonoBehaviour
    {
        public static InputController Instance { get; private set; }
    
        SimInput _inputActions;

        [SerializeField]
        private Camera topDownCamera;
        [SerializeField]
        private Camera thirdPersonCamera;

        private Vector2 _mousePos;
        private EntityBase.HostilityLevel _hostilityLevel = EntityBase.HostilityLevel.Friendly;
        [SerializeField]
        private EntityBase friendlyEntity;
        [SerializeField]
        private EntityBase neutralEntity;
        [SerializeField]
        private EntityBase hostileEnemy;

        [SerializeField]
        private Transform entitiesParent;

        [FormerlySerializedAs("previewGO")] [SerializeField]
        private GameObject previewGo;

        private GameObject _previewObject;

        private bool _previewing = false;
        private bool _previewingCancelled = false;

        private static uint _entityIndex = 0;

        private void OnEnable()
        {
            Instance = this;
            _inputActions.TopDownCamera.Enable();
        }

        private void OnDisable()
        {
            _inputActions.Disable();
        }

        private void Update()
        {
            if (_previewing)
            {
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


            _inputActions.TopDownCamera.SwitchCamera.performed += SwitchCamera;
            _inputActions.TopDownCamera.ToFriendly.performed += SwitchToFriendly;
            _inputActions.TopDownCamera.ToNeutral.performed += SwitchToNeutral;
            _inputActions.TopDownCamera.ToHostile.performed += SwitchToHostile;
            _inputActions.TopDownCamera.ToggleSimulation.performed += ToggleSimulation;
            _inputActions.TopDownCamera.Instantiate.performed += InstantiateEntity;
            _inputActions.TopDownCamera.Instantiate.canceled += InstantiateEntity;
            _inputActions.TopDownCamera.CancelInstaniation.performed += CancelInstantiation;
            _inputActions.TopDownCamera.MousePos.performed += ctx => _mousePos = ctx.ReadValue<Vector2>();

            _inputActions.ThirdPerson.SwitchCamera.performed += SwitchCamera;
            _inputActions.ThirdPerson.ToggleSimulation.performed += ToggleSimulation;
            _inputActions.ThirdPerson.SwitchEntityInc.performed += SwitchEntity_Increment;
            _inputActions.ThirdPerson.SwitchEntityDec.performed += SwitchEntity_Decrement;
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
            ++_entityIndex;
            SwitchEntity();
        }
        private void SwitchEntity_Decrement(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
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
            if (child != null)
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

        private void SwitchCamera(UnityEngine.InputSystem.InputAction.CallbackContext obj)
        {
            if (topDownCamera.gameObject.activeSelf)
            {
                if(!thirdPersonCamera.transform.parent && entitiesParent.childCount > 0)
                {
                    Transform child = entitiesParent.GetChild(0);
                    thirdPersonCamera.transform.parent = child;
                    thirdPersonCamera.transform.localPosition = new Vector3(0, 2, -4);
                    thirdPersonCamera.transform.localRotation = Quaternion.identity;
                }

                topDownCamera.gameObject.SetActive(false);
                thirdPersonCamera.gameObject.SetActive(true);

                _inputActions.TopDownCamera.Disable();
                _inputActions.ThirdPerson.Enable();
            }
            else
            {
                topDownCamera.gameObject.SetActive(true);
                thirdPersonCamera.gameObject.SetActive(false);

                _inputActions.ThirdPerson.Disable();
                _inputActions.TopDownCamera.Enable();
            }
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
