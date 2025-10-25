using System;
using Entities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputController : MonoBehaviour
{
    public static InputController Instance { get; private set; }
    
    SimInput inputActions;

    [SerializeField]
    private Camera topDownCamera;
    [SerializeField]
    private Camera thirdPersonCamera;

    private Vector2 mousePos;
    private EntityBase.HostilityLevel hostilityLevel;
    [SerializeField]
    private EntityBase friendlyEntity;
    [SerializeField]
    private EntityBase neutralEntity;
    [SerializeField]
    private EntityBase hostileEnemy;

    [SerializeField]
    private Transform entitiesParent;

    [SerializeField]
    private GameObject previewGO;

    private GameObject previewObject;

    private bool previewing = false;
    private bool previewingCancelled = false;

    private static uint entityIndex = 0;

    private void OnEnable()
    {
        Instance = this;
        inputActions.TopDownCamera.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Update()
    {
        if (previewing)
        {
            if (previewObject == null)
            {
                previewObject = Instantiate(previewGO, null, true);
                var renderer = previewObject.GetComponent<MeshRenderer>();

                switch (hostilityLevel)
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

            Ray ray = topDownCamera.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0));
            if (new Plane(topDownCamera.transform.forward, 1).Raycast(ray, out float distance))
            {
                Vector3 temp = ray.GetPoint(distance);
                previewObject.transform.position = temp;
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
        inputActions = new SimInput();


        inputActions.TopDownCamera.SwitchCamera.performed += SwitchCamera;
        inputActions.TopDownCamera.ToFriendly.performed += SwitchToFriendly;
        inputActions.TopDownCamera.ToNeutral.performed += SwitchToNeutral;
        inputActions.TopDownCamera.ToHostile.performed += SwitchToHostile;
        inputActions.TopDownCamera.ToggleSimulation.performed += ToggleSimulation;
        inputActions.TopDownCamera.Instantiate.performed += InstantiateEntity;
        inputActions.TopDownCamera.Instantiate.canceled += InstantiateEntity;
        inputActions.TopDownCamera.CancelInstaniation.performed += CancelInstantiation;
        inputActions.TopDownCamera.MousePos.performed += ctx => mousePos = ctx.ReadValue<Vector2>();

        inputActions.ThirdPerson.SwitchCamera.performed += SwitchCamera;
        inputActions.ThirdPerson.ToggleSimulation.performed += ToggleSimulation;
        inputActions.ThirdPerson.SwitchEntityInc.performed += SwitchEntity_Increment;
        inputActions.ThirdPerson.SwitchEntityDec.performed += SwitchEntity_Decrement;
    }

    private void CancelInstantiation(InputAction.CallbackContext obj)
    {
        if (previewing)
        {
            previewing = false;
            if (previewObject != null)
            {
                Destroy(previewObject);
                previewObject = null;
            }

            previewingCancelled = true;
        }
    }

    private void SwitchEntity_Increment(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        ++entityIndex;
        SwitchEntity();
    }
    private void SwitchEntity_Decrement(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        --entityIndex;
        SwitchEntity();
    }

    private void SwitchEntity()
    {
        uint entityCount = (uint)entitiesParent.childCount;

        if (entityCount == 0)
            return;

        entityIndex %= entityCount;
        if (entityIndex < 0)
            entityIndex += entityCount;

        Transform child = entitiesParent.GetChild((int)entityIndex);
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
            previewing = true;
        }
        else if (obj.phase == InputActionPhase.Canceled && !previewingCancelled)
        {
            previewing = false;

            if (previewObject != null)
            {
                Destroy(previewObject);
                previewObject = null;
            }

            EntityBase Entity = hostilityLevel switch
            {
                EntityBase.HostilityLevel.Friendly => Instantiate(friendlyEntity, entitiesParent, true),
                EntityBase.HostilityLevel.Neutral => Instantiate(neutralEntity, entitiesParent, true),
                EntityBase.HostilityLevel.Hostile => Instantiate(hostileEnemy, entitiesParent, true),
                _ => null
            };

            Ray ray = topDownCamera.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0));
            if (new Plane(topDownCamera.transform.forward, 1).Raycast(ray, out float distance))
            {
                Vector3 temp = ray.GetPoint(distance);
                if (Entity != null)
                    Entity.transform.position = temp;
            }
        }

        previewingCancelled = false;
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
        hostilityLevel = EntityBase.HostilityLevel.Hostile;
    }

    private void SwitchToNeutral(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        hostilityLevel = EntityBase.HostilityLevel.Neutral;
    }

    private void SwitchToFriendly(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        hostilityLevel = EntityBase.HostilityLevel.Friendly;
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

            inputActions.TopDownCamera.Disable();
            inputActions.ThirdPerson.Enable();
        }
        else
        {
            topDownCamera.gameObject.SetActive(true);
            thirdPersonCamera.gameObject.SetActive(false);

            inputActions.ThirdPerson.Disable();
            inputActions.TopDownCamera.Enable();
        }
    }

    public Transform GetNextEntity()
    { 
        return entitiesParent.GetChild((int)++entityIndex);
    }
    
    
}
