using System.Collections;
using UnityEngine;

public class FloatingLootIcon : MonoBehaviour
{
    public float duration = 0.9f;
    public float height = 1.0f;
    public float startScale = 0.85f;
    public float endScale = 1.1f;

    public Vector2 labelOffset = new Vector2(0.22f, -0.18f); // 角标相对图标的偏移
    public int labelSortingOffset = 1;                       // 角标绘制在图标之上

    SpriteRenderer _sr;
    TextMesh _tm;
    Color _startColor;
    
    public static void Spawn(Sprite icon, Vector3 worldPos, float height = 1.0f, float duration = 0.9f, string label = null)
    {
        var go = new GameObject("FloatingLootIcon");
        var comp = go.AddComponent<FloatingLootIcon>();
        comp.duration = Mathf.Max(0.1f, duration);
        comp.height = height;

        go.transform.position = worldPos;

        comp._sr = go.AddComponent<SpriteRenderer>();
        comp._sr.sprite = icon;
        comp._sr.sortingOrder = 10000;
        comp._sr.sortingLayerID = SortingLayer.NameToID("Default");
        comp._startColor = Color.white;

        if (!string.IsNullOrEmpty(label))
        {
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, worldPositionStays: false);
            labelGO.transform.localPosition = (Vector3)comp.labelOffset;

            comp._tm = labelGO.AddComponent<TextMesh>();
            comp._tm.text = label;
            comp._tm.characterSize = 0.18f;  // 根据你项目的像素密度可微调
            comp._tm.fontSize = 64;
            comp._tm.anchor = TextAnchor.LowerRight;
            comp._tm.color = Color.white;

            var lsr = labelGO.AddComponent<MeshRenderer>();
            lsr.sortingOrder = comp._sr.sortingOrder + comp.labelSortingOffset;
            lsr.sortingLayerID = comp._sr.sortingLayerID;
        }

        comp.StartCoroutine(comp.Anim());
    }

    IEnumerator Anim()
    {
        float t = 0f;
        Vector3 start = transform.position;
        Vector3 end = start + Vector3.up * height;

        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            float eu = EaseOutCubic(u);

            // 位置
            transform.position = Vector3.LerpUnclamped(start, end, eu);

            // 透明度
            var c = _startColor;
            c.a = 1f - u;
            _sr.color = c;
            if (_tm) _tm.color = c;

            // 缩放
            float s = Mathf.Lerp(startScale, endScale, eu);
            transform.localScale = new Vector3(s, s, 1f);

            yield return null;
        }

        Destroy(gameObject);
    }

    static float EaseOutCubic(float x)
    {
        float inv = 1f - x;
        return 1f - inv * inv * inv;
    }
}