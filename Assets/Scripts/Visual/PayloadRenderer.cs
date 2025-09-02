using UnityEngine;


[RequireComponent(typeof(Conveyor))]
public class PayloadRenderer : MonoBehaviour
{
    [SerializeField] private float iconScale = 0.6f; // 图标缩放
    [SerializeField] private float zOffset = -0.1f; // 让图标略微浮在上层

    private Conveyor _conv;
    private SpriteRenderer _sr;
    
    void Awake()
    {
        _conv = GetComponent<Conveyor>();
        var go = new GameObject("ItemIcon");
        go.transform.SetParent(transform);
        _sr = go.AddComponent<SpriteRenderer>();
        _sr.sortingOrder = 10; // 在建筑之上
        _sr.enabled = false;
    }


    void LateUpdate()
    {
        if (_conv.GetVisualState(out var sprite, out var pos))
        {
            _sr.sprite = sprite;
            _sr.transform.position = new Vector3(pos.x, pos.y, zOffset);
            _sr.transform.localScale = Vector3.one * iconScale;
            _sr.enabled = true;
        }
        else
        {
            _sr.enabled = false;
        }
    }
}