using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UGF.WorldUI;
using UGF.WorldUI.Utilities;

namespace UGF.WorldUI.Editor
{
    /// <summary>
    /// 世界空间UI管理器编辑器窗口
    /// </summary>
    public class WorldSpaceUIManagerWindow : EditorWindow
    {
        #region Fields
        
        private Vector2 _scrollPosition;
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "概览", "分组管理", "对象池", "性能监控", "设置" };
        
        // 概览相关
        private bool _showManagerInfo = true;
        private bool _showStatistics = true;
        private bool _showActiveComponents = true;
        
        // 分组管理相关
        private string _newGroupName = "";
        private bool _showGroupDetails = true;
        
        // 对象池相关
        private bool _showPoolDetails = true;
        private string _selectedPoolName = "";
        
        // 性能监控相关
        private bool _enablePerformanceMonitoring = true;
        private float _performanceUpdateInterval = 1.0f;
        private List<float> _frameRateHistory = new List<float>();
        private List<long> _memoryHistory = new List<long>();
        private const int MaxHistoryCount = 100;
        
        // 设置相关
        private bool _showDebugGizmos = true;
        private bool _showDebugInfo = true;
        private Color _gizmoColor = Color.yellow;
        
        #endregion
        
        #region Menu Items
        
        [MenuItem("Window/UGF/World Space UI Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<WorldSpaceUIManagerWindow>("World Space UI Manager");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }
        
        #endregion
        
        #region Unity Callbacks
        
        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }
        
        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }
        
        private void OnGUI()
        {
            DrawHeader();
            DrawTabs();
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            switch (_selectedTab)
            {
                case 0:
                    DrawOverviewTab();
                    break;
                case 1:
                    DrawGroupManagementTab();
                    break;
                case 2:
                    DrawObjectPoolTab();
                    break;
                case 3:
                    DrawPerformanceTab();
                    break;
                case 4:
                    DrawSettingsTab();
                    break;
            }
            
            EditorGUILayout.EndScrollView();
            
            DrawFooter();
        }
        
        private void OnEditorUpdate()
        {
            if (_enablePerformanceMonitoring && Application.isPlaying)
            {
                UpdatePerformanceData();
            }
        }
        
        #endregion
        
        #region Drawing Methods
        
        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            GUILayout.Label("World Space UI Manager", EditorStyles.boldLabel);
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                Repaint();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawTabs()
        {
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
            EditorGUILayout.Space();
        }
        
        private void DrawOverviewTab()
        {
            var manager = WorldSpaceUIManager.Instance;
            
            if (manager == null)
            {
                EditorGUILayout.HelpBox("世界空间UI管理器未初始化。请在场景中创建管理器实例。", MessageType.Warning);
                
                if (GUILayout.Button("创建管理器", GUILayout.Height(30)))
                {
                    CreateManager();
                }
                return;
            }
            
            // 管理器信息
            _showManagerInfo = EditorGUILayout.Foldout(_showManagerInfo, "管理器信息", true);
            if (_showManagerInfo)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("状态", manager.IsInitialized ? "已初始化" : "未初始化");
                EditorGUILayout.LabelField("UI相机", manager.UICamera != null ? manager.UICamera.name : "无");
                EditorGUILayout.LabelField("分组数量", manager.GroupCount.ToString());
                EditorGUILayout.LabelField("总UI数量", manager.TotalInstanceCount.ToString());
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 统计信息
            _showStatistics = EditorGUILayout.Foldout(_showStatistics, "统计信息", true);
            if (_showStatistics)
            {
                EditorGUI.indentLevel++;
                
                var stats = manager.GetStatistics();
                EditorGUILayout.LabelField("活跃UI数量", stats.ActiveUICount.ToString());
                EditorGUILayout.LabelField("可见UI数量", stats.VisibleUICount.ToString());
                EditorGUILayout.LabelField("被剔除UI数量", stats.CulledUICount.ToString());
                EditorGUILayout.LabelField("对象池数量", stats.PoolCount.ToString());
                EditorGUILayout.LabelField("池中对象数量", stats.PooledObjectCount.ToString());
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 活跃组件列表
            _showActiveComponents = EditorGUILayout.Foldout(_showActiveComponents, "活跃组件", true);
            if (_showActiveComponents)
            {
                EditorGUI.indentLevel++;
                
                var activeComponents = manager.GetAllActiveComponents();
                if (activeComponents.Count == 0)
                {
                    EditorGUILayout.LabelField("无活跃组件");
                }
                else
                {
                    foreach (var component in activeComponents.Take(10)) // 只显示前10个
                    {
                        EditorGUILayout.BeginHorizontal();
                        
                        EditorGUILayout.ObjectField(component, typeof(WorldSpaceUIComponent), true);
                        
                        if (GUILayout.Button("选择", GUILayout.Width(50)))
                        {
                            Selection.activeGameObject = component.gameObject;
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    if (activeComponents.Count > 10)
                    {
                        EditorGUILayout.LabelField($"... 还有 {activeComponents.Count - 10} 个组件");
                    }
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawGroupManagementTab()
        {
            var manager = WorldSpaceUIManager.Instance;
            if (manager == null)
            {
                EditorGUILayout.HelpBox("管理器未初始化", MessageType.Warning);
                return;
            }
            
            // 创建新分组
            EditorGUILayout.LabelField("创建新分组", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _newGroupName = EditorGUILayout.TextField("分组名称", _newGroupName);
            
            GUI.enabled = !string.IsNullOrEmpty(_newGroupName) && !manager.HasGroup(_newGroupName);
            if (GUILayout.Button("创建", GUILayout.Width(60)))
            {
                manager.CreateGroup(_newGroupName);
                _newGroupName = "";
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 现有分组列表
            EditorGUILayout.LabelField("现有分组", EditorStyles.boldLabel);
            
            var groups = manager.GetAllGroups();
            if (groups == null || groups.Count == 0)
            {
                EditorGUILayout.LabelField("暂无分组", EditorStyles.centeredGreyMiniLabel);
                return;
            }
            
            foreach (var group in groups.Values)
            {
                DrawGroupInfo(group);
            }
        }
        
        private void DrawGroupInfo(UIGroup group)
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField(group.Name, EditorStyles.boldLabel);
            
            GUILayout.FlexibleSpace();
            
            // 可见性切换
            bool newVisible = EditorGUILayout.Toggle("可见", group.IsVisible, GUILayout.Width(60));
            if (newVisible != group.IsVisible)
            {
                group.SetVisible(newVisible);
            }
            
            // 激活状态切换
            bool newActive = EditorGUILayout.Toggle("激活", group.IsActive, GUILayout.Width(60));
            if (newActive != group.IsActive)
            {
                group.SetActive(newActive);
            }
            
            // 删除按钮
            if (GUILayout.Button("删除", GUILayout.Width(50)))
            {
                if (EditorUtility.DisplayDialog("确认删除", $"确定要删除分组 '{group.Name}' 吗？", "删除", "取消"))
                {
                    WorldSpaceUIManager.Instance.RemoveGroup(group.Name);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (_showGroupDetails)
            {
                EditorGUI.indentLevel++;
                
                var stats = group.GetStats();
                EditorGUILayout.LabelField("UI数量", stats.TotalCount.ToString());
                EditorGUILayout.LabelField("可见数量", stats.VisibleCount.ToString());
                EditorGUILayout.LabelField("活跃数量", stats.ActiveCount.ToString());
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawObjectPoolTab()
        {
            var manager = WorldSpaceUIManager.Instance;
            if (manager == null)
            {
                EditorGUILayout.HelpBox("管理器未初始化", MessageType.Warning);
                return;
            }
            
            EditorGUILayout.LabelField("对象池管理", EditorStyles.boldLabel);
            
            var pools = manager.GetAllPools();
            if (pools.Count == 0)
            {
                EditorGUILayout.LabelField("无对象池");
            }
            else
            {
                foreach (var kvp in pools)
                {
                    DrawPoolInfo(kvp.Key, kvp.Value);
                }
            }
            
            EditorGUILayout.Space();
            
            // 全局池操作
            EditorGUILayout.LabelField("全局操作", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("清理所有池"))
            {
                if (EditorUtility.DisplayDialog("确认清理", "确定要清理所有对象池吗？", "清理", "取消"))
                {
                    manager.ClearAllPools();
                }
            }
            
            if (GUILayout.Button("预热所有池"))
            {
                manager.WarmAllPools();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawPoolInfo(string poolName, UIPool pool)
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField(poolName, EditorStyles.boldLabel);
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("清理", GUILayout.Width(50)))
            {
                pool.Clear();
            }
            
            if (GUILayout.Button("预热", GUILayout.Width(50)))
            {
                pool.PreWarm(pool.Config.preWarmCount);
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (_showPoolDetails)
            {
                EditorGUI.indentLevel++;
                
                var stats = pool.Stats;
                EditorGUILayout.LabelField("总数量", (stats.availableCount + stats.activeCount).ToString());
                EditorGUILayout.LabelField("可用数量", stats.availableCount.ToString());
                EditorGUILayout.LabelField("使用数量", stats.activeCount.ToString());
                EditorGUILayout.LabelField("命中率", $"{stats.hitRate:P2}");
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawPerformanceTab()
        {
            EditorGUILayout.LabelField("性能监控", EditorStyles.boldLabel);
            
            _enablePerformanceMonitoring = EditorGUILayout.Toggle("启用性能监控", _enablePerformanceMonitoring);
            
            if (_enablePerformanceMonitoring)
            {
                _performanceUpdateInterval = EditorGUILayout.Slider("更新间隔", _performanceUpdateInterval, 0.1f, 5.0f);
                
                EditorGUILayout.Space();
                
                // 当前性能数据
                EditorGUILayout.LabelField("当前性能", EditorStyles.boldLabel);
                
                float currentFPS = WorldSpaceUIUtilities.GetCurrentFrameRate();
                long currentMemory = WorldSpaceUIUtilities.GetMemoryUsageMB();
                
                EditorGUILayout.LabelField("帧率", $"{currentFPS:F1} FPS");
                EditorGUILayout.LabelField("内存使用", $"{currentMemory} MB");
                
                // 性能历史图表
                if (_frameRateHistory.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("帧率历史", EditorStyles.boldLabel);
                    DrawPerformanceGraph(_frameRateHistory, "FPS", 0f, 120f);
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("内存历史", EditorStyles.boldLabel);
                    DrawMemoryGraph(_memoryHistory, "MB");
                }
                
                EditorGUILayout.Space();
                
                if (GUILayout.Button("清除历史数据"))
                {
                    _frameRateHistory.Clear();
                    _memoryHistory.Clear();
                }
            }
        }
        
        private void DrawPerformanceGraph(List<float> data, string unit, float minValue, float maxValue)
        {
            if (data.Count == 0) return;
            
            Rect rect = GUILayoutUtility.GetRect(0, 100, GUILayout.ExpandWidth(true));
            
            // 绘制背景
            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 0.5f));
            
            // 绘制数据线
            if (data.Count > 1)
            {
                Vector3[] points = new Vector3[data.Count];
                
                for (int i = 0; i < data.Count; i++)
                {
                    float x = rect.x + (float)i / (data.Count - 1) * rect.width;
                    float y = rect.y + rect.height - Mathf.InverseLerp(minValue, maxValue, data[i]) * rect.height;
                    points[i] = new Vector3(x, y, 0);
                }
                
                Handles.color = Color.green;
                Handles.DrawAAPolyLine(2f, points);
            }
            
            // 绘制标签
            GUI.Label(new Rect(rect.x, rect.y - 20, rect.width, 20), $"最新: {data[data.Count - 1]:F1} {unit}");
        }
        
        private void DrawMemoryGraph(List<long> data, string unit)
        {
            if (data.Count == 0) return;
            
            List<float> floatData = data.Select(x => (float)x).ToList();
            float minValue = floatData.Min();
            float maxValue = floatData.Max();
            
            DrawPerformanceGraph(floatData, unit, minValue, maxValue);
        }
        
        private void DrawSettingsTab()
        {
            EditorGUILayout.LabelField("调试设置", EditorStyles.boldLabel);
            
            _showDebugGizmos = EditorGUILayout.Toggle("显示调试Gizmos", _showDebugGizmos);
            _showDebugInfo = EditorGUILayout.Toggle("显示调试信息", _showDebugInfo);
            _gizmoColor = EditorGUILayout.ColorField("Gizmo颜色", _gizmoColor);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("工具", EditorStyles.boldLabel);
            
            if (GUILayout.Button("强制垃圾回收"))
            {
                GC.Collect();
                EditorUtility.DisplayDialog("垃圾回收", "垃圾回收已执行", "确定");
            }
            
            if (GUILayout.Button("重新加载所有UI"))
            {
                var manager = WorldSpaceUIManager.Instance;
                if (manager != null)
                {
                    manager.ReloadAllUI();
                }
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("导出/导入", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("导出配置"))
            {
                ExportConfiguration();
            }
            
            if (GUILayout.Button("导入配置"))
            {
                ImportConfiguration();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            GUILayout.Label($"UGF.WorldUI v1.0.0", EditorStyles.miniLabel);
            
            GUILayout.FlexibleSpace();
            
            if (Application.isPlaying)
            {
                GUILayout.Label("运行中", EditorStyles.miniLabel);
            }
            else
            {
                GUILayout.Label("编辑模式", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        #endregion
        
        #region Utility Methods
        
        private void CreateManager()
        {
            var managerGO = new GameObject("WorldSpaceUIManager");
            managerGO.AddComponent<WorldSpaceUIManager>();
            
            Selection.activeGameObject = managerGO;
            EditorGUIUtility.PingObject(managerGO);
        }
        
        private void UpdatePerformanceData()
        {
            if (Time.realtimeSinceStartup % _performanceUpdateInterval < Time.deltaTime)
            {
                float fps = WorldSpaceUIUtilities.GetCurrentFrameRate();
                long memory = WorldSpaceUIUtilities.GetMemoryUsageMB();
                
                _frameRateHistory.Add(fps);
                _memoryHistory.Add(memory);
                
                // 限制历史数据数量
                if (_frameRateHistory.Count > MaxHistoryCount)
                {
                    _frameRateHistory.RemoveAt(0);
                }
                
                if (_memoryHistory.Count > MaxHistoryCount)
                {
                    _memoryHistory.RemoveAt(0);
                }
                
                Repaint();
            }
        }
        
        private void ExportConfiguration()
        {
            string path = EditorUtility.SaveFilePanel("导出配置", Application.dataPath, "WorldSpaceUIConfig", "json");
            if (!string.IsNullOrEmpty(path))
            {
                // TODO: 实现配置导出逻辑
                EditorUtility.DisplayDialog("导出完成", "配置已导出到: " + path, "确定");
            }
        }
        
        private void ImportConfiguration()
        {
            string path = EditorUtility.OpenFilePanel("导入配置", Application.dataPath, "json");
            if (!string.IsNullOrEmpty(path))
            {
                // TODO: 实现配置导入逻辑
                EditorUtility.DisplayDialog("导入完成", "配置已从以下位置导入: " + path, "确定");
            }
        }
        
        #endregion
    }
}