using System;
using Entities;
using UnityEngine;
using UnityEngine.UIElements;

public class ScreenStatsDisplay : MonoBehaviour
{
    private UIDocument uid;
    [SerializeField] private VisualTreeAsset root;
    private EntityStats stats;

    private void OnTransformParentChanged()
    {
        uid = GetComponent<UIDocument>();
        var copy = root.CloneTree();
        stats = GetComponentInParent<EntityStats>();
        copy.Q<Label>("Health").text = "Health: ";
        copy.dataSource = stats;
        uid.rootVisualElement.Clear();
        uid.rootVisualElement.Add(copy);
    }
}
    