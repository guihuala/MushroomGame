#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TechTreeEditor : EditorWindow
{
    // ====== 数据 ======
    private TechTreeConfig config;
    private BuildingData selectedBuilding;

    // ====== 视图态 ======
    private Vector2 leftScroll;                   // 左侧列表滚动
    private Vector2 graphScrollDummy;             // 未用（保留）
    private Vector2 panOffset = Vector2.zero;     // 画布平移
    private float zoom = 1f;                      // 画布缩放

    // ====== 常量（可按需微调） ======
    private const float NodeWidth = 140f;
    private const float NodeHeight = 84f;
    private const float LevelGapX = 260f;         // 层级水平间距
    private const float RowGapY = 120f;           // 同层垂直间距
    private readonly Color GridMinor = new Color(1f,1f,1f,0.06f);
    private readonly Color GridMajor = new Color(1f,1f,1f,0.12f);
    private readonly Color LinkColor = new Color(0.2f, 0.9f, 1f, 0.85f);
    private readonly Color NodeColor = new Color(0.12f, 0.16f, 0.2f, 0.9f);
    private readonly Color NodeSelected = new Color(0.18f, 0.3f, 0.45f, 0.95f);
    private readonly Color NodeHeader = new Color(1f,1f,1f,0.92f);

    [MenuItem("Tools/Tech Tree Editor")]
    public static void ShowWindow()
    {
        GetWindow<TechTreeEditor>("科技树编辑器");
    }

    private void OnEnable()
    {
        // 自动加载选中的配置文件
        if (Selection.activeObject is TechTreeConfig selectedConfig)
        {
            config = selectedConfig;
        }
    }

    private void OnInspectorUpdate()
    {
        Repaint();
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (config == null)
        {
            EditorGUILayout.HelpBox("请先在工具栏选择一个 TechTreeConfig 资源。", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        {
            DrawPropertyPanel();   // 左侧
            DrawNodeGraph();       // 右侧预览
        }
        EditorGUILayout.EndHorizontal();
    }

    // ======================== 顶部工具栏 ========================
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        {
            var newCfg = (TechTreeConfig)EditorGUILayout.ObjectField(
                new GUIContent("配置文件"), config, typeof(TechTreeConfig), false, GUILayout.MinWidth(250));
            if (newCfg != config)
            {
                config = newCfg;
                selectedBuilding = null;
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("新建节点", EditorStyles.toolbarButton))
            {
                AddNewNode();
            }

            if (GUILayout.Button("按依赖自动填充层级", EditorStyles.toolbarButton))
            {
                AutoFillLevels();
            }

            if (GUILayout.Button("保存", EditorStyles.toolbarButton))
            {
                SaveConfig();
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    // ======================== 左侧属性面板 ========================
    private void DrawPropertyPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(340));
        {
            leftScroll = EditorGUILayout.BeginScrollView(leftScroll);

            // 初始解锁
            EditorGUILayout.LabelField("初始解锁建筑", EditorStyles.boldLabel);
            for (int i = 0; i < config.initialUnlocks.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                config.initialUnlocks[i] = (BuildingData)EditorGUILayout.ObjectField(
                    config.initialUnlocks[i], typeof(BuildingData), false);
                if (GUILayout.Button("×", GUILayout.Width(22)))
                {
                    config.initialUnlocks.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("+ 添加初始建筑")) config.initialUnlocks.Add(null);

            EditorGUILayout.Space(16);

            // 节点列表
            EditorGUILayout.LabelField("科技节点", EditorStyles.boldLabel);
            foreach (var node in config.nodes)
            {
                if (node == null) continue;
                DrawNodeInList(node);
            }

            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawNodeInList(TechNodeConfig node)
    {
        bool isSelected = (selectedBuilding == node.building);
        var bg = GUI.backgroundColor;
        GUI.backgroundColor = isSelected ? new Color(0.3f,0.55f,0.9f,0.55f) : Color.white;

        EditorGUILayout.BeginVertical("box");
        GUI.backgroundColor = bg;

        // 头部：建筑
        EditorGUILayout.BeginHorizontal();
        node.building = (BuildingData)EditorGUILayout.ObjectField("建筑", node.building, typeof(BuildingData), false);
        if (GUILayout.Button("选中", GUILayout.Width(48)))
        {
            selectedBuilding = node.building;
        }
        EditorGUILayout.EndHorizontal();

        if (node.building == null)
        {
            EditorGUILayout.HelpBox("请选择一个 BuildingData", MessageType.Warning);
        }

        // 解锁成本
        EditorGUILayout.LabelField("解锁成本");
        for (int i = 0; i < node.unlockCost.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            var current = node.unlockCost[i];
            var newItem = (ItemDef)EditorGUILayout.ObjectField(current.item, typeof(ItemDef), false);
            var newAmount = EditorGUILayout.IntField(current.amount);
            if (newItem != current.item || newAmount != current.amount)
                node.unlockCost[i] = new ItemStack { item = newItem, amount = newAmount };
            if (GUILayout.Button("×", GUILayout.Width(22)))
            {
                node.unlockCost.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("+ 添加资源")) node.unlockCost.Add(new ItemStack());

        // 前置
        EditorGUILayout.LabelField("前置建筑");
        for (int i = 0; i < node.prerequisites.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            node.prerequisites[i] = (BuildingData)EditorGUILayout.ObjectField(
                node.prerequisites[i], typeof(BuildingData), false);
            if (GUILayout.Button("×", GUILayout.Width(22)))
            {
                node.prerequisites.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("+ 添加前置")) node.prerequisites.Add(null);

        // ====== 新增：UI 布局配置 ======
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("UI 布局", EditorStyles.boldLabel);
        node.overrideLevel = EditorGUILayout.IntField(
            new GUIContent("层级(override)", "手动指定该节点所处层（0 开始）。-1 表示自动按依赖深度计算。"),
            node.overrideLevel);
        node.orderInLevel = EditorGUILayout.IntField(
            new GUIContent("同层顺序", "同层内显示顺序，值越小越靠上/靠左。"),
            node.orderInLevel);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(6);
    }

    // ======================== 右侧图预览 ========================
    private void DrawNodeGraph()
    {
        // 计算画布区域
        Rect rect = GUILayoutUtility.GetRect(10, 10, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        GUI.Box(rect, GUIContent.none); // 外框

        // 交互：缩放/平移
        HandleGraphEvents(rect);

        // 创建绘制范围（缩放 & 平移）
        Matrix4x4 old = GUI.matrix;
        var pivot = rect.center;
        GUI.BeginClip(rect);
        {
            var m = Matrix4x4.TRS(panOffset, Quaternion.identity, Vector3.one * zoom);
            GUI.matrix = m;

            DrawGrid(rect.size / zoom, panOffset / zoom);

            // 计算布局 & 画节点、连线
            BuildLayoutAndDraw();
        }
        GUI.matrix = old;
        GUI.EndClip();
    }

    private void HandleGraphEvents(Rect rect)
    {
        var e = Event.current;
        if (!rect.Contains(e.mousePosition)) return;

        // 缩放：滚轮
        if (e.type == EventType.ScrollWheel)
        {
            float delta = -e.delta.y * 0.05f;
            float oldZoom = zoom;
            zoom = Mathf.Clamp(zoom + delta, 0.3f, 2.5f);

            // 以鼠标为中心缩放的“视觉修正”（把鼠标点固定在内容坐标）
            Vector2 mouseInContent = (e.mousePosition - panOffset) / oldZoom;
            panOffset = e.mousePosition - mouseInContent * zoom;

            e.Use();
        }

        // 平移：中键 或 右键拖动
        if (e.type == EventType.MouseDrag && (e.button == 2 || e.button == 1))
        {
            panOffset += e.delta;
            e.Use();
        }
    }

    private void DrawGrid(Vector2 viewSize, Vector2 offset)
    {
        // 画两层网格：细 + 粗
        DrawGridLayer(viewSize, offset, 16f, GridMinor);
        DrawGridLayer(viewSize, offset, 64f, GridMajor);
    }

    private void DrawGridLayer(Vector2 viewSize, Vector2 offset, float spacing, Color col)
    {
        Handles.BeginGUI();
        Handles.color = col;

        float x0 = -offset.x % spacing;
        for (float x = x0; x < viewSize.x; x += spacing)
            Handles.DrawLine(new Vector3(x, 0, 0), new Vector3(x, viewSize.y, 0));

        float y0 = -offset.y % spacing;
        for (float y = y0; y < viewSize.y; y += spacing)
            Handles.DrawLine(new Vector3(0, y, 0), new Vector3(viewSize.x, y, 0));

        Handles.EndGUI();
    }

    // ====== 布局与绘制 ======
    private class PreviewNode
    {
        public TechNodeConfig cfg;
        public int level;
        public int order;
        public Rect rect;
        public string name;
    }

    private void BuildLayoutAndDraw()
    {
        if (config.nodes == null) return;

        // 1) 预处理：映射
        var nodes = config.nodes.Where(n => n != null && n.building != null).ToList();
        var byBuilding = nodes.ToDictionary(n => n.building, n => n);

        // 2) 计算层级：优先 overrideLevel，否则依赖深度
        Dictionary<BuildingData, int> depthCache = new Dictionary<BuildingData, int>();
        int GetDepth(BuildingData b)
        {
            if (b == null) return 0;
            if (depthCache.TryGetValue(b, out var d)) return d;
            var cfg = byBuilding.TryGetValue(b, out var c) ? c : null;
            if (cfg == null || cfg.prerequisites == null || cfg.prerequisites.Count == 0)
            {
                depthCache[b] = 0;
                return 0;
            }
            int maxP = 0;
            foreach (var pre in cfg.prerequisites)
            {
                if (pre == null) continue;
                maxP = Mathf.Max(maxP, GetDepth(pre) + 1);
            }
            depthCache[b] = maxP;
            return maxP;
        }

        // level -> list
        Dictionary<int, List<PreviewNode>> levels = new Dictionary<int, List<PreviewNode>>();
        foreach (var cfg in nodes)
        {
            int lv = (cfg.overrideLevel >= 0) ? cfg.overrideLevel : GetDepth(cfg.building);
            if (!levels.ContainsKey(lv)) levels[lv] = new List<PreviewNode>();
            levels[lv].Add(new PreviewNode
            {
                cfg = cfg,
                level = lv,
                order = cfg.orderInLevel,
                name = cfg.building != null ? cfg.building.buildingName : "(null)"
            });
        }

        // 3) 同层排序（稳定）：orderInLevel -> name
        foreach (var kv in levels)
        {
            kv.Value.Sort((a, b) =>
            {
                int c = a.order.CompareTo(b.order);
                if (c != 0) return c;
                return string.Compare(a.name, b.name, System.StringComparison.Ordinal);
            });
        }

        // 4) 定位（X=level，Y=同层索引）
        foreach (var kv in levels)
        {
            var list = kv.Value;
            float x = kv.Key * LevelGapX;

            // 让同层以 0 为中心对称分布
            float total = (list.Count - 1) * RowGapY;
            float startY = -total * 0.5f;

            for (int i = 0; i < list.Count; i++)
            {
                float y = startY + i * RowGapY;
                list[i].rect = new Rect(
                    x - NodeWidth * 0.5f,
                    y - NodeHeight * 0.5f,
                    NodeWidth,
                    NodeHeight
                );
            }
        }

        // 5) 画连线（先画线再画节点，线在下层）
        Handles.BeginGUI();
        Handles.color = LinkColor;
        foreach (var kv in levels)
        {
            foreach (var pn in kv.Value)
            {
                if (pn.cfg.prerequisites == null) continue;
                var fromRect = pn.rect;
                Vector2 start = new Vector2(fromRect.xMin, fromRect.center.y);

                foreach (var pre in pn.cfg.prerequisites)
                {
                    if (pre == null) continue;
                    if (!byBuilding.TryGetValue(pre, out var preCfg)) continue;

                    // 找到前置节点的预览数据
                    int preLv = (preCfg.overrideLevel >= 0) ? preCfg.overrideLevel : GetDepth(pre);
                    var preList = levels[preLv];
                    var prePn = preList.FirstOrDefault(x => x.cfg == preCfg);
                    if (prePn == null) continue;

                    var toRect = prePn.rect;
                    Vector2 end = new Vector2(toRect.xMax, toRect.center.y);

                    float dx = Mathf.Abs(end.x - start.x) * 0.5f + 40f;
                    Vector2 c1 = start + Vector2.left * 16f + Vector2.right * dx;
                    Vector2 c2 = end + Vector2.right * 16f - Vector2.right * dx;

                    Handles.DrawBezier(start, end, c1, c2, LinkColor, null, 3f);
                }
            }
        }
        Handles.EndGUI();

        // 6) 画节点 & 处理点击
        var e = Event.current;
        foreach (var kv in levels)
        {
            foreach (var pn in kv.Value)
            {
                bool isSel = (pn.cfg.building == selectedBuilding && pn.cfg.building != null);
                DrawNodeBox(pn.rect, pn.name, isSel);

                // 点击检测（转换到当前 GUI.matrix 下的坐标系，已在 BeginClip/缩放中）
                if (e.type == EventType.MouseDown && e.button == 0 && pn.rect.Contains(e.mousePosition))
                {
                    selectedBuilding = pn.cfg.building;
                    Repaint();
                }
            }
        }
    }

    private void DrawNodeBox(Rect r, string title, bool selected)
    {
        // 背景
        EditorGUI.DrawRect(r, selected ? NodeSelected : NodeColor);

        // 标题条
        var head = new Rect(r.x, r.y, r.width, 22f);
        EditorGUI.DrawRect(head, new Color(0f, 0f, 0f, 0.25f));
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = NodeHeader }
        };
        GUI.Label(head, string.IsNullOrEmpty(title) ? "(Unnamed)" : title, style);
    }

    // ======================== 数据操作 ========================
    private void AddNewNode()
    {
        var newNode = new TechNodeConfig
        {
            overrideLevel = -1,
            orderInLevel = 0
        };
        config.nodes.Add(newNode);
        selectedBuilding = newNode.building;
        EditorUtility.SetDirty(config);
    }

    private void AutoFillLevels()
    {
        // 将所有 overrideLevel 尚未设置（=-1）的节点，按依赖深度写入（方便批量初始化）
        var nodes = config.nodes.Where(n => n != null && n.building != null).ToList();
        var byBuilding = nodes.ToDictionary(n => n.building, n => n);
        Dictionary<BuildingData, int> depthCache = new Dictionary<BuildingData, int>();

        int GetDepth(BuildingData b)
        {
            if (b == null) return 0;
            if (depthCache.TryGetValue(b, out var d)) return d;
            var cfg = byBuilding.TryGetValue(b, out var c) ? c : null;
            if (cfg == null || cfg.prerequisites == null || cfg.prerequisites.Count == 0)
            {
                depthCache[b] = 0;
                return 0;
            }
            int maxP = 0;
            foreach (var pre in cfg.prerequisites)
            {
                if (pre == null) continue;
                maxP = Mathf.Max(maxP, GetDepth(pre) + 1);
            }
            depthCache[b] = maxP;
            return maxP;
        }

        foreach (var n in nodes)
        {
            if (n.overrideLevel < 0)
                n.overrideLevel = GetDepth(n.building);
        }

        EditorUtility.SetDirty(config);
        Debug.Log("已按依赖自动填充值到 overrideLevel（仅填 -1 的项）。");
    }

    private void SaveConfig()
    {
        if (config != null)
        {
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("科技树配置已保存");
        }
    }
}
#endif
