using System;
using Entities;
using UnityEngine;
using UnityEngine.UIElements;

public class EntityStatGatherScript : MonoBehaviour
{
    private UIDocument uid;
    [SerializeField] private VisualTreeAsset root;
    private EntityStats stats;

    private void OnEnable()
    {
        uid = GetComponent<UIDocument>();
        var copy = root.CloneTree();
        stats = GetComponentInParent<EntityStats>();
        copy.dataSource = stats;
        
        uid.rootVisualElement.Clear();
        uid.rootVisualElement.Add(copy);
    }

    private void OnTransformParentChanged()
    {
        uid = GetComponent<UIDocument>();
        var copy = root.CloneTree();
        stats = GetComponentInParent<EntityStats>();
        copy.dataSource = stats;
        uid.rootVisualElement.Clear();
        uid.rootVisualElement.Add(copy);
    }
}
    