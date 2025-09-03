using UnityEngine;
using UnityEditor;
using UGF.WorldUI;

namespace UGF.WorldUI.Editor
{
    /// <summary>
    /// WorldSpaceUIComponent的自定义编辑器
    /// </summary>
    [CustomEditor(typeof(WorldSpaceUIComponent), true)]
    [CanEditMultipleObjects]
    public class WorldSpaceUIComponentEditor : UnityEditor.Editor
    {
        #region SerializedProperties
        
        private SerializedProperty _configProperty;
        private SerializedProperty _followTargetProperty;
        private SerializedProperty _groupNameProperty;
        
        #endregion
        
        #region Foldout States
        
        private bool _showBasicSettings = true;
        private bool _showAnimationSettings = false;
        private bool _showPerformanceSettings = false;
        private bool _showInteractionSettings = false;
        private bool _showRenderingSettings = false;
        private bool _showDebugSettings = false;
        private bool _showRuntimeInfo = false;
        
        #endregion
        
        #region Unity Callbacks
        
        private void OnEnable()
        {
            _configProperty = serializedObject.FindProperty("_config");
            _followTargetProperty = serializedObject.FindProperty("_followTarget");
            _groupNameProperty = serializedObject.FindProperty("_groupName"); // 可能为null，需要检查
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // 如果目标类型不是WorldSpaceUIComponent基类，则显示默认Inspector
            if (target.GetType() != typeof(WorldSpaceUIComponent))
            {
                // 显示继承类的所有字段
                DrawPropertiesExcluding(serializedObject, "m_Script");
                
                EditorGUILayout.Space();
                
                if (Application.isPlaying)
                {
                    DrawRuntimeInfo();
                }
            }
            else
            {
                // 对于基类，显示完整的自定义界面
                DrawHeader();
                
                EditorGUILayout.Space();
                
                DrawBasicSettings();
                DrawAnimationSettings();
                DrawPerformanceSettings();
                DrawInteractionSettings();
                DrawRenderingSettings();
                DrawDebugSettings();
                
                if (Application.isPlaying)
                {
                    DrawRuntimeInfo();
                }
                
                DrawFooter();
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        #endregion
        
        #region Drawing Methods
        
        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("World Space UI Component", EditorStyles.boldLabel);
            
            var component = target as WorldSpaceUIComponent;
            if (component != null)
            {
                EditorGUILayout.LabelField("状态", component.IsInitialized ? "已初始化" : "未初始化");
                
                if (Application.isPlaying)
                {
                    EditorGUILayout.LabelField("可见性", component.IsVisible ? "可见" : "隐藏");
                    EditorGUILayout.LabelField("剔除状态", component.IsCulled ? "被剔除" : "未剔除");
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawBasicSettings()
        {
            _showBasicSettings = EditorGUILayout.Foldout(_showBasicSettings, "基础设置", true);
            
            if (_showBasicSettings)
            {
                EditorGUI.indentLevel++;
                
                // 分组名称
                if (_groupNameProperty != null)
                {
                    EditorGUILayout.PropertyField(_groupNameProperty, new GUIContent("分组名称"));
                }
                
                // 跟随目标
                if (_followTargetProperty != null)
                {
                    EditorGUILayout.PropertyField(_followTargetProperty, new GUIContent("跟随目标"));
                }
                
                // 配置
                if (_configProperty != null && _configProperty.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("需要配置WorldSpaceUIConfig", MessageType.Warning);
                    
                    if (GUILayout.Button("创建默认配置"))
                    {
                        CreateDefaultConfig();
                    }
                }
                else if (_configProperty != null)
                {
                    EditorGUILayout.PropertyField(_configProperty, new GUIContent("配置"));
                    
                    if (GUILayout.Button("编辑配置"))
                    {
                        Selection.activeObject = _configProperty.objectReferenceValue;
                    }
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawAnimationSettings()
        {
            if (_configProperty.objectReferenceValue == null) return;
            
            _showAnimationSettings = EditorGUILayout.Foldout(_showAnimationSettings, "动画设置");
            
            if (_showAnimationSettings)
            {
                EditorGUI.indentLevel++;
                
                var config = _configProperty.objectReferenceValue as WorldSpaceUIConfig;
                if (config != null)
                {
                    EditorGUILayout.LabelField("淡入淡出", config.enableFadeAnimation ? "启用" : "禁用");
                    
                    if (config.enableFadeAnimation)
                    {
                        EditorGUILayout.LabelField("淡入淡出时间", config.fadeDuration.ToString("F2") + "s");
                    }
                    
                    EditorGUILayout.LabelField("缩放曲线", config.scaleCurve != null ? "已设置" : "未设置");
                    EditorGUILayout.LabelField("最小缩放", config.minScale.ToString("F2"));
                    EditorGUILayout.LabelField("最大缩放", config.maxScale.ToString("F2"));
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawPerformanceSettings()
        {
            if (_configProperty.objectReferenceValue == null) return;
            
            _showPerformanceSettings = EditorGUILayout.Foldout(_showPerformanceSettings, "性能设置");
            
            if (_showPerformanceSettings)
            {
                EditorGUI.indentLevel++;
                
                var config = _configProperty.objectReferenceValue as WorldSpaceUIConfig;
                if (config != null)
                {
                    EditorGUILayout.LabelField("剔除", config.enableDistanceCulling ? "启用" : "禁用");
                    
                    if (config.enableDistanceCulling)
                    {
                        EditorGUILayout.LabelField("剔除距离", config.cullingDistance.ToString("F1") + "m");
                    }
                    
                    EditorGUILayout.LabelField("更新频率", config.updateRate.ToString());
                    EditorGUILayout.LabelField("LOD等级", config.lodLevel.ToString());
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawInteractionSettings()
        {
            if (_configProperty.objectReferenceValue == null) return;
            
            _showInteractionSettings = EditorGUILayout.Foldout(_showInteractionSettings, "交互设置");
            
            if (_showInteractionSettings)
            {
                EditorGUI.indentLevel++;
                
                var config = _configProperty.objectReferenceValue as WorldSpaceUIConfig;
                if (config != null)
                {
                    EditorGUILayout.LabelField("交互", config.enableInteraction ? "启用" : "禁用");
                    
                    if (config.enableInteraction)
                    {
                        EditorGUILayout.LabelField("交互距离", config.interactionDistance.ToString("F1") + "m");
                        EditorGUILayout.LabelField("悬停缩放", config.hoverScale.ToString("F2"));
                    }
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawRenderingSettings()
        {
            if (_configProperty.objectReferenceValue == null) return;
            
            _showRenderingSettings = EditorGUILayout.Foldout(_showRenderingSettings, "渲染设置");
            
            if (_showRenderingSettings)
            {
                EditorGUI.indentLevel++;
                
                var config = _configProperty.objectReferenceValue as WorldSpaceUIConfig;
                if (config != null)
                {
                    EditorGUILayout.LabelField("面向相机", config.faceCamera ? "是" : "否");
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawDebugSettings()
        {
            if (_configProperty.objectReferenceValue == null) return;
            
            _showDebugSettings = EditorGUILayout.Foldout(_showDebugSettings, "调试设置");
            
            if (_showDebugSettings)
            {
                EditorGUI.indentLevel++;
                
                var config = _configProperty.objectReferenceValue as WorldSpaceUIConfig;
                if (config != null)
                {
                    EditorGUILayout.LabelField("显示边界", config.showBounds ? "是" : "否");
                    EditorGUILayout.LabelField("显示信息", config.showDebugInfo ? "是" : "否");
                    EditorGUILayout.LabelField("调试颜色", config.debugColor.ToString());
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawRuntimeInfo()
        {
            var component = target as WorldSpaceUIComponent;
            if (component == null || !component.IsInitialized) return;
            
            _showRuntimeInfo = EditorGUILayout.Foldout(_showRuntimeInfo, "运行时信息");
            
            if (_showRuntimeInfo)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.LabelField("管理器", component.Manager != null ? "已连接" : "未连接");
                EditorGUILayout.LabelField("分组", component.Group != null ? component.Group.Name : "无");
                
                if (component.FollowTarget != null)
                {
                    float distance = Vector3.Distance(component.transform.position, component.FollowTarget.position);
                    EditorGUILayout.LabelField("跟随距离", distance.ToString("F2") + "m");
                }
                
                var uiCamera = component.Manager?.UICamera;
                if (uiCamera != null)
                {
                    float cameraDistance = Vector3.Distance(component.transform.position, uiCamera.transform.position);
                    EditorGUILayout.LabelField("相机距离", cameraDistance.ToString("F2") + "m");
                }
                
                EditorGUILayout.LabelField("生存时间", component.LifeTime.ToString("F2") + "s");
                
                if (component.Config != null && component.Config.lifeTime > 0)
                {
                    float remainingTime = component.Config.lifeTime - component.LifeTime;
                    EditorGUILayout.LabelField("剩余时间", Mathf.Max(0, remainingTime).ToString("F2") + "s");
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawFooter()
        {
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("重置配置"))
            {
                if (EditorUtility.DisplayDialog("确认重置", "确定要重置配置吗？这将创建一个新的默认配置。", "重置", "取消"))
                {
                    CreateDefaultConfig();
                }
            }
            
            if (Application.isPlaying)
            {
                var component = target as WorldSpaceUIComponent;
                if (component != null)
                {
                    if (GUILayout.Button(component.IsVisible ? "隐藏" : "显示"))
                    {
                        component.SetVisible(!component.IsVisible);
                    }
                    
                    if (GUILayout.Button("销毁"))
                    {
                        component.DestroyUI();
                    }
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        #endregion
        
        #region Utility Methods
        
        private void CreateDefaultConfig()
        {
            var config = WorldSpaceUIConfig.CreateDefault();
            
            string path = EditorUtility.SaveFilePanelInProject(
                "保存配置",
                "WorldSpaceUIConfig",
                "asset",
                "选择保存位置");
            
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
                
                _configProperty.objectReferenceValue = config;
                serializedObject.ApplyModifiedProperties();
                
                Selection.activeObject = config;
            }
        }
        
        #endregion
        
        #region Scene GUI
        
        private void OnSceneGUI()
        {
            var component = target as WorldSpaceUIComponent;
            if (component == null || !component.IsInitialized) return;
            
            var config = component.Config;
            if (config == null || !config.showBounds) return;
            
            // 绘制剔除距离
            if (config.enableDistanceCulling && config.cullingDistance > 0)
            {
                Handles.color = Color.yellow;
                Handles.DrawWireDisc(component.transform.position, Vector3.up, config.cullingDistance);
            }
            
            // 绘制交互距离
            if (config.enableInteraction && config.interactionDistance > 0)
            {
                Handles.color = Color.green;
                Handles.DrawWireDisc(component.transform.position, Vector3.up, config.interactionDistance);
            }
            
            // 绘制跟随连线
            if (component.FollowTarget != null)
            {
                Handles.color = Color.blue;
                Handles.DrawLine(component.transform.position, component.FollowTarget.position);
            }
            
            // 绘制边界框
            if (config.showBounds)
            {
                var bounds = component.GetBounds();
                Handles.color = Color.red;
                Handles.DrawWireCube(bounds.center, bounds.size);
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// WorldSpaceUIConfig的自定义编辑器
    /// </summary>
    [CustomEditor(typeof(WorldSpaceUIConfig))]
    public class WorldSpaceUIConfigEditor : UnityEditor.Editor
    {
        private bool _showBasicSettings = true;
        private bool _showAnimationSettings = true;
        private bool _showPerformanceSettings = true;
        private bool _showInteractionSettings = true;
        private bool _showRenderingSettings = true;
        private bool _showDebugSettings = true;
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.LabelField("World Space UI Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            DrawBasicSettings();
            DrawAnimationSettings();
            DrawPerformanceSettings();
            DrawInteractionSettings();
            DrawDebugSettings();
            
            EditorGUILayout.Space();
            
            DrawPresets();
            DrawValidation();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawBasicSettings()
        {
            _showBasicSettings = EditorGUILayout.Foldout(_showBasicSettings, "基础设置", true);
            
            if (_showBasicSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("offset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("followTarget"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("faceCamera"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("lifeTime"));
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawAnimationSettings()
        {
            _showAnimationSettings = EditorGUILayout.Foldout(_showAnimationSettings, "动画设置");
            
            if (_showAnimationSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("enableFadeAnimation"));
                
                if (serializedObject.FindProperty("enableFadeAnimation").boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("fadeDuration"));
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("scaleCurve"));
                
                // 缩放限制设置
                EditorGUILayout.LabelField("缩放限制", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minScale"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxScale"));
                EditorGUI.indentLevel--;
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("alphaCurve"));
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawPerformanceSettings()
        {
            _showPerformanceSettings = EditorGUILayout.Foldout(_showPerformanceSettings, "性能设置");
            
            if (_showPerformanceSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("enableDistanceCulling"));
                
                if (serializedObject.FindProperty("enableDistanceCulling").boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("cullingDistance"));
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("updateRate"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("lodLevel"));
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawInteractionSettings()
        {
            _showInteractionSettings = EditorGUILayout.Foldout(_showInteractionSettings, "交互设置");
            
            if (_showInteractionSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("enableInteraction"));
                
                if (serializedObject.FindProperty("enableInteraction").boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("interactionDistance"));
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hoverScale"));
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawDebugSettings()
        {
            _showDebugSettings = EditorGUILayout.Foldout(_showDebugSettings, "调试设置");
            
            if (_showDebugSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("showBounds"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("showDebugInfo"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("debugColor"));
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawPresets()
        {
            EditorGUILayout.LabelField("预设", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("默认"))
            {
                ApplyPreset(WorldSpaceUIConfig.CreateDefault());
            }
            
            if (GUILayout.Button("高性能"))
            {
                ApplyPreset(WorldSpaceUIConfig.CreateHighPerformance());
            }
            
            if (GUILayout.Button("高质量"))
            {
                ApplyPreset(WorldSpaceUIConfig.CreateHighQuality());
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("临时UI"))
            {
                ApplyPreset(WorldSpaceUIConfig.CreateTemporary());
            }
            
            if (GUILayout.Button("跟随目标"))
            {
                ApplyPreset(WorldSpaceUIConfig.CreateFollowTarget(Vector3.zero));
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawValidation()
        {
            var config = target as WorldSpaceUIConfig;
            if (config != null)
            {
                if (!config.Validate())
                {
                    EditorGUILayout.HelpBox("配置包含无效值，将在运行时自动修复。", MessageType.Warning);
                    
                    if (GUILayout.Button("修复配置"))
                    {
                        config.FixInvalidValues();
                        EditorUtility.SetDirty(config);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("配置有效", MessageType.Info);
                }
            }
        }
        
        private void ApplyPreset(WorldSpaceUIConfig preset)
        {
            if (EditorUtility.DisplayDialog("应用预设", "确定要应用此预设吗？当前设置将被覆盖。", "应用", "取消"))
            {
                var config = target as WorldSpaceUIConfig;
                if (config != null)
                {
                    EditorUtility.CopySerialized(preset, config);
                    EditorUtility.SetDirty(config);
                }
            }
        }
    }
}