using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HudController : MonoBehaviour
{
    [SerializeField]private Button pauseButton;
    [SerializeField] private CentralHub hub;
    [SerializeField] private ItemDef watchItem;
    [SerializeField] private Text label;

    void Awake()
    {
        pauseButton.onClick.AddListener(OnPauseButtonClicked);
        
        if (hub) hub.OnDelivered += (_, __) => Refresh();
        Refresh();
    }

    private void OnDestroy()
    {
        if (hub) hub.OnDelivered -= (_, __) => Refresh();
    }

    private void Refresh()
    {
        int n = hub ? hub.GetDelivered(watchItem) : 0;
        label.text = $"{watchItem.displayName}: {n}";
    }

    private void OnPauseButtonClicked()
    {
        UIManager.Instance.OpenPanel("PausePanel");
    }
}
