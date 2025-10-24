using Entities;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputController : MonoBehaviour
{
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
    private GameObject preview;

    private GameObject previewObject;

    private bool previewing = false;

    private void Update()
    {
        if (previewing)
        {
            if (previewObject == null)
            {
                previewObject = Instantiate(preview, null, true);
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


    private void Awake()
    {
        inputActions = new SimInput();

        inputActions.TopDownCamera.Enable();

        inputActions.TopDownCamera.SwitchCamera.performed += SwitchCamera;
        inputActions.TopDownCamera.ToFriendly.performed += SwitchToFriendly;
        inputActions.TopDownCamera.ToNeutral.performed += SwitchToNeutral;
        inputActions.TopDownCamera.ToHostile.performed += SwitchToHostile;
        inputActions.TopDownCamera.ToggleSimulation.performed += ToggleSimulation;
        inputActions.TopDownCamera.Instantiate.performed += InstantiateEntity;
        inputActions.TopDownCamera.Instantiate.canceled += InstantiateEntity;
        inputActions.TopDownCamera.MousePos.performed += ctx => mousePos = ctx.ReadValue<Vector2>();

        inputActions.ThirdPerson.SwitchCamera.performed += SwitchCamera;
        inputActions.ThirdPerson.ToggleSimulation.performed += ToggleSimulation;
        inputActions.ThirdPerson.SwitchEntity.performed += SwitchEntity;
    }

    private void SwitchEntity(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        throw new System.NotImplementedException();
    }

    private void InstantiateEntity(InputAction.CallbackContext obj)
    {
        if (obj.phase == InputActionPhase.Performed)
        {
            previewing = true;
        }
        else if (obj.phase == InputActionPhase.Canceled)
        {
            previewing = false;

            if (previewObject != null)
            {
                Destroy(previewObject);
                previewObject = null;
            }

            EntityBase Entity = hostilityLevel switch
            {
                EntityBase.HostilityLevel.Friendly => Instantiate(friendlyEntity, null, true),
                EntityBase.HostilityLevel.Neutral => Instantiate(neutralEntity, null, true),
                EntityBase.HostilityLevel.Hostile => Instantiate(hostileEnemy, null, true),
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

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void SwitchCamera(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        if (topDownCamera.gameObject.activeSelf)
        {
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


}
