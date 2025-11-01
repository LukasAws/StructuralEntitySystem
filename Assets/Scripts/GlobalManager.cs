using System;
using UnityEngine;

public class GlobalManager : MonoBehaviour
{
    public static GlobalManager Instance;
    
    [SerializeField] private GameObject settingsGO;
    public GameObject escapeUI;
    
    private void OnEnable()
    {
        Instance = this;
        settingsGO.SetActive(true);
        DontDestroyOnLoad(settingsGO);
        DontDestroyOnLoad(this);
    }
}
