using UnityEngine;
using UnityEngine.UI;

namespace UGF.WorldUI.Examples
{
    /// <summary>
    /// UpdateScheduler整合示例
    /// 演示UIGroup.Update方法如何被整合到UpdateScheduler系统中
    /// </summary>
    public class UpdateSchedulerIntegrationExample : MonoBehaviour
    {
        [Header("示例配置")]
        [SerializeField] private GameObject uiPrefab;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private int uiCount = 5;
        [SerializeField] private float spawnRadius = 10f;
        
        [Header("分组配置")]
        [SerializeField] private float fastUpdateInterval = 0f;     // 每帧更新
        [SerializeField] private float mediumUpdateInterval = 0.1f; // 每0.1秒更新
        [SerializeField] private float slowUpdateInterval = 0.5f;   // 每0.5秒更新
        
        private WorldSpaceUIManager _manager;
        
        void Start()
        {
            InitializeManager();
            CreateExampleGroups();
            SpawnExampleUIs();
        }
        
        private void InitializeManager()
        {
            // 获取或创建WorldSpaceUIManager
            _manager = FindObjectOfType<WorldSpaceUIManager>();
            if (_manager == null)
            {
                var managerGO = new GameObject("WorldSpaceUIManager");
                _manager = managerGO.AddComponent<WorldSpaceUIManager>();
            }
            
            // 设置相机
            if (targetCamera != null)
            {
                _manager.UICamera = targetCamera;
            }
            
            // 初始化管理器
            if (!_manager.IsInitialized)
            {
                _manager.Initialize();
            }
            
            Debug.Log("[UpdateSchedulerIntegrationExample] WorldSpaceUIManager已初始化");
        }
        
        private void CreateExampleGroups()
        {
            // 创建快速更新分组
            var fastConfig = new UIGroupConfig
            {
                updateInterval = fastUpdateInterval,
                maxInstances = 20,
                enablePooling = true,
                sortingOrder = 1
            };
            _manager.CreateGroup("FastUpdate", fastConfig);
            
            // 创建中等更新分组
            var mediumConfig = new UIGroupConfig
            {
                updateInterval = mediumUpdateInterval,
                maxInstances = 15,
                enablePooling = true,
                sortingOrder = 2
            };
            _manager.CreateGroup("MediumUpdate", mediumConfig);
            
            // 创建慢速更新分组
            var slowConfig = new UIGroupConfig
            {
                updateInterval = slowUpdateInterval,
                maxInstances = 10,
                enablePooling = true,
                sortingOrder = 3
            };
            _manager.CreateGroup("SlowUpdate", slowConfig);
            
            Debug.Log("[UpdateSchedulerIntegrationExample] 已创建示例分组");
        }
        
        private void SpawnExampleUIs()
        {
            if (uiPrefab == null)
            {
                Debug.LogWarning("[UpdateSchedulerIntegrationExample] UI预制体未设置，创建默认UI");
                CreateDefaultUIPrefab();
            }
            
            var groups = new[] { "FastUpdate", "MediumUpdate", "SlowUpdate" };
            
            for (int i = 0; i < uiCount; i++)
            {
                // 随机选择分组
                var groupName = groups[i % groups.Length];
                
                // 随机位置
                var position = Random.insideUnitSphere * spawnRadius;
                position.y = Mathf.Abs(position.y); // 确保在地面上方
                
                // 创建UI
                var ui = _manager.CreateUI<ExampleWorldSpaceUI>(uiPrefab, position, groupName);
                if (ui != null)
                {
                    ui.SetDisplayText($"UI {i + 1}\nGroup: {groupName}");
                    Debug.Log($"[UpdateSchedulerIntegrationExample] 创建UI {i + 1} 在分组 {groupName}");
                }
            }
        }
        
        private void CreateDefaultUIPrefab()
        {
            // 创建默认UI预制体
            var prefabGO = new GameObject("DefaultUI");
            
            // 添加Canvas
            var canvas = prefabGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            // 添加CanvasScaler
            var scaler = prefabGO.AddComponent<CanvasScaler>();
            scaler.scaleFactor = 0.01f;
            
            // 添加GraphicRaycaster
            prefabGO.AddComponent<GraphicRaycaster>();
            
            // 添加Text组件
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(prefabGO.transform, false);
            
            var text = textGO.AddComponent<Text>();
            text.text = "Example UI";
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            
            var rectTransform = textGO.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 100);
            
            // 添加背景
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(prefabGO.transform, false);
            bgGO.transform.SetAsFirstSibling();
            
            var image = bgGO.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.8f);
            
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(220, 120);
            
            // 添加示例组件
            prefabGO.AddComponent<ExampleWorldSpaceUI>();
            
            uiPrefab = prefabGO;
        }
        
        void OnGUI()
        {
            if (_manager == null) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("UpdateScheduler整合示例", GUI.skin.box);
            
            var allGroups = _manager.GetAllGroups();
            GUILayout.Label($"总分组数: {allGroups.Count}");
            GUILayout.Label($"总UI实例数: {_manager.TotalInstanceCount}");
            
            if (GUILayout.Button("重新加载所有UI"))
            {
                _manager.ReloadAllUI();
                SpawnExampleUIs();
            }
            
            if (GUILayout.Button("清理所有UI"))
            {
                _manager.ReloadAllUI();
            }
            
            GUILayout.EndArea();
        }
    }
    
    /// <summary>
    /// 示例WorldSpaceUI组件
    /// </summary>
    public class ExampleWorldSpaceUI : WorldSpaceUIComponent
    {
        private Text _displayText;
        private float _updateCounter;
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            _displayText = GetComponentInChildren<Text>();
        }
        
        public override void UpdateUI()
        {
            base.UpdateUI();
            
            _updateCounter += Time.deltaTime;
            
            if (_displayText != null)
            {
                var baseText = _displayText.text.Split('\n')[0] + "\n" + _displayText.text.Split('\n')[1];
                _displayText.text = $"{baseText}\nUpdates: {_updateCounter:F1}s";
            }
        }
        
        public void SetDisplayText(string text)
        {
            if (_displayText != null)
            {
                _displayText.text = text;
            }
        }
    }
}