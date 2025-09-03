using System;
using UnityEngine;
using UnityEngine.UI;

public class HudController : MonoBehaviour
{
    [SerializeField] private Button pauseButton;
    [SerializeField] private Hub hub;
    [SerializeField] private ItemDef watchItem;
    [SerializeField] private Text label;

    void Awake()
    {
        pauseButton.onClick.AddListener(OnPauseButtonClicked);
    }

    void OnEnable()
    {
        if (hub != null)
        {
            hub.OnItemReceived += HandleItemReceived;
        }
    }

    void OnDisable()
    {
        if (hub != null)
        {
            hub.OnItemReceived -= HandleItemReceived;
        }
    }

    private void OnPauseButtonClicked()
    {
        UIManager.Instance.OpenPanel("PausePanel");
    }

    private void HandleItemReceived(ItemPayload payload)
    {
        if (watchItem != null && payload.item == watchItem)
        {
            int count = hub.GetItemCount(watchItem);
            label.text = $"{watchItem.name}: {count}";
        }
    }
}