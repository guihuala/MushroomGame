#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class TechTreeEditor : EditorWindow
{
    private TechTreeConfig config;
    private Vector2 scrollPosition;
    private BuildingData selectedBuilding;
    private Vector2 panOffset = Vector2.zero;
    private float zoom = 1f;
    private const float nodeWidth = 120f;
    private const float nodeHeight = 80f;

    [MenuItem("Tools/Tech Tree Editor")]
    public static void ShowWindow()
    {
        GetWindow<TechTreeEditor>("科技树编辑器");
    }

    private void OnGUI()
    {
        DrawToolbar();
        
        if (config == null)
        {
            EditorGUILayout.HelpBox("请先选择一个TechTreeConfig文件", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        {
            // 左侧属性面板
            DrawPropertyPanel();
            
            // 右侧节点图
            DrawNodeGraph();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        {
            config = (TechTreeConfig)EditorGUILayout.ObjectField("配置文件", config, typeof(TechTreeConfig), false);
            
            if (GUILayout.Button("新建节点", EditorStyles.toolbarButton))
            {
                AddNewNode();
            }
            
            if (GUILayout.Button("保存", EditorStyles.toolbarButton))
            {
                SaveConfig();
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawPropertyPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(300));
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // 初始解锁建筑列表
            EditorGUILayout.LabelField("初始解锁建筑", EditorStyles.boldLabel);
            for (int i = 0; i < config.initialUnlocks.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                config.initialUnlocks[i] = (BuildingData)EditorGUILayout.ObjectField(
                    config.initialUnlocks[i], typeof(BuildingData), false);
                
                if (GUILayout.Button("×", GUILayout.Width(20)))
                {
                    config.initialUnlocks.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            
            if (GUILayout.Button("+ 添加初始建筑"))
            {
                config.initialUnlocks.Add(null);
            }

            EditorGUILayout.Space(20);
            
            // 节点列表
            EditorGUILayout.LabelField("科技节点", EditorStyles.boldLabel);
            foreach (var node in config.nodes)
            {
                if (DrawNodeInList(node))
                {
                    selectedBuilding = node.building;
                }
            }

            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();
    }

    private bool DrawNodeInList(TechNodeConfig node)
    {
        EditorGUILayout.BeginVertical("box");
        
        bool isSelected = selectedBuilding == node.building;
        if (isSelected) GUI.backgroundColor = Color.blue;
        
        node.building = (BuildingData)EditorGUILayout.ObjectField("建筑", node.building, typeof(BuildingData), false);
        
        if (node.building == null)
        {
            EditorGUILayout.HelpBox("请先选择一个建筑", MessageType.Warning);
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();
            return isSelected;
        }

        // 解锁成本
        EditorGUILayout.LabelField("解锁成本");
        for (int i = 0; i < node.unlockCost.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            
            // 创建临时变量来修改ItemStack
            ItemDef currentItem = node.unlockCost[i].item;
            int currentAmount = node.unlockCost[i].amount;
            
            // 显示对象字段
            ItemDef newItem = (ItemDef)EditorGUILayout.ObjectField(
                currentItem, typeof(ItemDef), false);
            
            int newAmount = EditorGUILayout.IntField(currentAmount);
            
            // 如果值有变化，创建新的ItemStack并替换
            if (newItem != currentItem || newAmount != currentAmount)
            {
                node.unlockCost[i] = new ItemStack { item = newItem, amount = newAmount };
            }
            
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                node.unlockCost.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }
        
        if (GUILayout.Button("+ 添加资源"))
        {
            node.unlockCost.Add(new ItemStack());
        }

        // 前置条件
        EditorGUILayout.LabelField("前置建筑");
        for (int i = 0; i < node.prerequisites.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            node.prerequisites[i] = (BuildingData)EditorGUILayout.ObjectField(
                node.prerequisites[i], typeof(BuildingData), false);
            
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                node.prerequisites.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }
        
        if (GUILayout.Button("+ 添加前置"))
        {
            node.prerequisites.Add(null);
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndVertical();
        
        return isSelected;
    }

    private void DrawNodeGraph()
    {
        // 节点图绘制逻辑
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        EditorGUILayout.LabelField("节点图预览", EditorStyles.centeredGreyMiniLabel);
        
        // 简单的节点预览
        foreach (var node in config.nodes)
        {
            if (node.building != null)
            {
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(node.building.buildingName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"前置: {node.prerequisites.Count}");
                EditorGUILayout.LabelField($"成本: {node.unlockCost.Count}种资源");
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
        }
        
        EditorGUILayout.EndVertical();
    }

    private void AddNewNode()
    {
        var newNode = new TechNodeConfig();
        config.nodes.Add(newNode);
        selectedBuilding = newNode.building;
        EditorUtility.SetDirty(config);
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

    private void OnEnable()
    {
        // 自动加载选中的配置文件
        if (Selection.activeObject is TechTreeConfig selectedConfig)
        {
            config = selectedConfig;
        }
    }

    // 重绘方法，确保编辑器实时更新
    private void OnInspectorUpdate()
    {
        Repaint();
    }
}
#endif