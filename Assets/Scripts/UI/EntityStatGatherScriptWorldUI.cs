using System;
using Entities;
using UnityEngine;
using UnityEngine.UIElements;

public class EntityStatGatherScriptWorldUI : MonoBehaviour
{
    private UIDocument uid;
    [SerializeField] private VisualTreeAsset root;
    private EntityStats stats;

    private void OnTransformChildrenChanged()
    {
        uid.enabled = transform.childCount > 0 ? true : false; // might break if any children related functionality is added later
        if (!uid.enabled) return;

        uid = GetComponent<UIDocument>();
        var copy = root.CloneTree();
        stats = GetComponentInParent<EntityStats>();
        copy.dataSource = stats;
        uid.rootVisualElement.Clear();
        uid.rootVisualElement.Add(copy);
    }
}
