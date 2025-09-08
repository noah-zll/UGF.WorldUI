using UnityEngine;
using UGF.WorldUI;
using WorldUISample.Components;
using System.Collections.Generic;

namespace WorldUISample.Controllers
{
    /// <summary>
    /// 示例控制器 - 演示WorldUI系统的基本使用方法
    /// </summary>
    public class SampleController : MonoBehaviour
    {
        [Header("UI预制体")]
        [SerializeField] private GameObject healthBarPrefab;
        [SerializeField] private GameObject damageTextPrefab;
        [SerializeField] private GameObject interactionPrefab;
        
        [Header("测试角色")]
        [SerializeField] private Transform[] testCharacters;
        [SerializeField] private Transform[] interactionPoints;
        
        [Header("测试设置")]
        [SerializeField] private KeyCode damageKey = KeyCode.Space;
        [SerializeField] private KeyCode healKey = KeyCode.H;
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private float damageAmount = 20f;
        [SerializeField] private float healAmount = 15f;
        [SerializeField] private float criticalChance = 0.2f;
        
        // 私有字段
        private List<HealthBarUI> _healthBars = new List<HealthBarUI>();
        private List<InteractionUI> _interactionUIs = new List<InteractionUI>();
        private Camera _mainCamera;
        
        #region Unity Callbacks
        
        private void Start()
        {
            _mainCamera = Camera.main;
            
            // 初始化WorldUI管理器
            InitializeWorldUIManager();
            
            // 创建血条UI
            CreateHealthBars();
            
            // 创建交互UI
            CreateInteractionUIs();
            
            Debug.Log("[SampleController] 示例控制器初始化完成");
        }
        
        private void Update()
        {
            HandleInput();
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// 初始化WorldUI管理器
        /// </summary>
        private void InitializeWorldUIManager()
        {
            var manager = WorldSpaceUIManager.Instance;
            
            // 设置UI相机
            if (_mainCamera != null)
            {
                manager.UICamera = _mainCamera;
            }
            
            // 配置全局设置
            var globalConfig = new WorldSpaceUIManagerConfig
            {
                maxTotalInstances = 500,
                enableAutoCleanup = true,
                cleanupInterval = 5f,

                globalCullingDistance = 100f,
                maxUpdatePerFrame = 50,
                enablePerformanceMonitoring = true,
                showDebugInfo = true
            };
            manager.SetGlobalConfig(globalConfig);
            
            // 创建UI分组
            CreateUIGroups();
            
            // 订阅事件
            SubscribeToEvents();
            
            Debug.Log("[SampleController] WorldUI管理器初始化完成");
        }
        
        /// <summary>
        /// 创建UI分组
        /// </summary>
        private void CreateUIGroups()
        {
            var manager = WorldSpaceUIManager.Instance;
            
            // 血条分组
            var healthBarConfig = new UIGroupConfig
            {
                sortingOrder = 10,
                maxInstances = 50,
                enableAutoRemoveOldest = true,

                cullingDistance = 50f,
                enablePooling = true,
                poolSize = 20,
                enableFadeAnimation = true
            };
            manager.CreateGroup("HealthBar", healthBarConfig);
            
            // 伤害文字分组
            var damageTextConfig = new UIGroupConfig
            {
                sortingOrder = 20,
                maxInstances = 100,
                enableAutoRemoveOldest = true,

                cullingDistance = 30f,
                enablePooling = true,
                poolSize = 50,
                enableFadeAnimation = false // 伤害文字有自己的动画
            };
            manager.CreateGroup("DamageText", damageTextConfig);
            
            // 交互提示分组
            var interactionConfig = new UIGroupConfig
            {
                sortingOrder = 15,
                maxInstances = 20,
                enableAutoRemoveOldest = false,

                cullingDistance = 20f,
                enablePooling = true,
                poolSize = 10,
                enableFadeAnimation = true
            };
            manager.CreateGroup("Interaction", interactionConfig);
        }
        
        /// <summary>
        /// 订阅事件
        /// </summary>
        private void SubscribeToEvents()
        {
            var manager = WorldSpaceUIManager.Instance;
            
            manager.OnUICreated += OnUICreated;
            manager.OnUIDestroyed += OnUIDestroyed;
            manager.OnGroupCreated += OnGroupCreated;
            manager.OnInstanceCountChanged += OnInstanceCountChanged;
        }
        
        #endregion
        
        #region UI Creation
        
        /// <summary>
        /// 创建血条UI
        /// </summary>
        private void CreateHealthBars()
        {
            if (healthBarPrefab == null || testCharacters == null) return;
            
            var manager = WorldSpaceUIManager.Instance;
            
            foreach (var character in testCharacters)
            {
                if (character == null) continue;
                
                // 计算血条位置（角色头顶上方）
                Vector3 healthBarPosition = character.position + Vector3.up * 2.5f;
                
                // 创建血条UI
                var healthBar = manager.CreateUI<HealthBarUI>(healthBarPrefab, healthBarPosition, "HealthBar");
                if (healthBar != null)
                {
                    // 设置血量
                    healthBar.SetHealth(100f, 100f);
                    
                    _healthBars.Add(healthBar);
                    
                    Debug.Log($"[SampleController] 为角色 {character.name} 创建血条UI");
                }
            }
        }
        
        /// <summary>
        /// 创建交互UI
        /// </summary>
        private void CreateInteractionUIs()
        {
            if (interactionPrefab == null || interactionPoints == null) return;
            
            var manager = WorldSpaceUIManager.Instance;
            
            for (int i = 0; i < interactionPoints.Length; i++)
            {
                var point = interactionPoints[i];
                if (point == null) continue;
                
                // 计算交互UI位置
                Vector3 interactionPosition = point.position + Vector3.up * 1.5f;
                
                // 创建交互UI
                var interactionUI = manager.CreateUI<InteractionUI>(interactionPrefab, interactionPosition, "Interaction");
                if (interactionUI != null)
                {
                    // 设置交互信息
                    string interactionText = $"交互点 {i + 1}";
                    int pointIndex = i; // 捕获循环变量
                    interactionUI.SetInteraction(interactionText, null, () => OnInteractionTriggered(pointIndex));
                    
                    _interactionUIs.Add(interactionUI);
                    
                    Debug.Log($"[SampleController] 创建交互UI: {interactionText}");
                }
            }
        }
        
        #endregion
        
        #region Input Handling
        
        /// <summary>
        /// 处理输入
        /// </summary>
        private void HandleInput()
        {
            // 造成伤害
            if (Input.GetKeyDown(damageKey))
            {
                DealDamageToRandomCharacter();
            }
            
            // 治疗
            if (Input.GetKeyDown(healKey))
            {
                HealRandomCharacter();
            }
            
            // 交互（简单实现：触发最近的交互UI）
            if (Input.GetKeyDown(interactKey))
            {
                TriggerNearestInteraction();
            }
        }
        
        #endregion
        
        #region Game Logic
        
        /// <summary>
        /// 对随机角色造成伤害
        /// </summary>
        private void DealDamageToRandomCharacter()
        {
            if (_healthBars.Count == 0) return;
            
            var randomHealthBar = _healthBars[Random.Range(0, _healthBars.Count)];
            if (randomHealthBar == null) return;
            
            // 判断是否暴击
            bool isCritical = Random.value < criticalChance;
            float actualDamage = isCritical ? damageAmount * 1.5f : damageAmount;
            
            // 造成伤害
            randomHealthBar.TakeDamage(actualDamage);
            
            // 显示伤害数字
            ShowDamageText(randomHealthBar.transform.position, actualDamage, isCritical);
            
            Debug.Log($"[SampleController] 造成伤害: {actualDamage} (暴击: {isCritical})");
        }
        
        /// <summary>
        /// 治疗随机角色
        /// </summary>
        private void HealRandomCharacter()
        {
            if (_healthBars.Count == 0) return;
            
            var randomHealthBar = _healthBars[Random.Range(0, _healthBars.Count)];
            if (randomHealthBar == null) return;
            
            // 治疗
            randomHealthBar.Heal(healAmount);
            
            // 显示治疗数字
            ShowHealText(randomHealthBar.transform.position, healAmount);
            
            Debug.Log($"[SampleController] 治疗: {healAmount}");
        }
        
        /// <summary>
        /// 显示伤害数字
        /// </summary>
        private void ShowDamageText(Vector3 position, float damage, bool isCritical)
        {
            if (damageTextPrefab == null) return;
            
            var manager = WorldSpaceUIManager.Instance;
            Vector3 textPosition = position + Vector3.up * 1f + Random.insideUnitSphere * 0.5f;
            
            var damageText = manager.CreateUI<DamageTextUI>(damageTextPrefab, textPosition, "DamageText");
            if (damageText != null)
            {
                damageText.SetDamage(damage, isCritical);
            }
        }
        
        /// <summary>
        /// 显示治疗数字
        /// </summary>
        private void ShowHealText(Vector3 position, float heal)
        {
            if (damageTextPrefab == null) return;
            
            var manager = WorldSpaceUIManager.Instance;
            Vector3 textPosition = position + Vector3.up * 1f + Random.insideUnitSphere * 0.5f;
            
            var damageText = manager.CreateUI<DamageTextUI>(damageTextPrefab, textPosition, "DamageText");
            if (damageText != null)
            {
                damageText.SetHeal(heal);
            }
        }
        
        /// <summary>
        /// 触发最近的交互
        /// </summary>
        private void TriggerNearestInteraction()
        {
            if (_interactionUIs.Count == 0 || _mainCamera == null) return;
            
            Vector3 cameraPosition = _mainCamera.transform.position;
            InteractionUI nearestInteraction = null;
            float nearestDistance = float.MaxValue;
            
            foreach (var interaction in _interactionUIs)
            {
                if (interaction == null) continue;
                
                float distance = Vector3.Distance(cameraPosition, interaction.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestInteraction = interaction;
                }
            }
            
            if (nearestInteraction != null && nearestDistance <= 5f)
            {
                nearestInteraction.TriggerInteraction();
            }
        }
        
        /// <summary>
        /// 交互触发回调
        /// </summary>
        private void OnInteractionTriggered(int pointIndex)
        {
            Debug.Log($"[SampleController] 交互点 {pointIndex + 1} 被触发!");
            
            // 这里可以添加具体的交互逻辑
            // 例如：打开宝箱、触发对话、激活机关等
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// UI创建事件
        /// </summary>
        private void OnUICreated(WorldSpaceUIComponent ui)
        {
            Debug.Log($"[SampleController] UI创建: {ui.GetType().Name}");
        }
        
        /// <summary>
        /// UI销毁事件
        /// </summary>
        private void OnUIDestroyed(WorldSpaceUIComponent ui)
        {
            Debug.Log($"[SampleController] UI销毁: {ui.GetType().Name}");
            
            // 从列表中移除
            if (ui is HealthBarUI healthBar)
            {
                _healthBars.Remove(healthBar);
            }
            else if (ui is InteractionUI interaction)
            {
                _interactionUIs.Remove(interaction);
            }
        }
        
        /// <summary>
        /// 分组创建事件
        /// </summary>
        private void OnGroupCreated(string groupName, UIGroup group)
        {
            Debug.Log($"[SampleController] UI分组创建: {groupName}");
        }
        
        /// <summary>
        /// 实例数量变化事件
        /// </summary>
        private void OnInstanceCountChanged(int count)
        {
            Debug.Log($"[SampleController] UI实例数量: {count}");
        }
        
        #endregion
        
        #region Cleanup
        
        private void OnDestroy()
        {
            // 取消订阅事件
            var manager = WorldSpaceUIManager.Instance;
            if (manager != null)
            {
                manager.OnUICreated -= OnUICreated;
                manager.OnUIDestroyed -= OnUIDestroyed;
                manager.OnGroupCreated -= OnGroupCreated;
                manager.OnInstanceCountChanged -= OnInstanceCountChanged;
            }
        }
        
        #endregion
        
        #region GUI
        
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("WorldUI 示例控制", GUI.skin.box);
            GUILayout.Label($"按 {damageKey} 键造成伤害");
            GUILayout.Label($"按 {healKey} 键进行治疗");
            GUILayout.Label($"按 {interactKey} 键触发交互");
            GUILayout.Label($"血条数量: {_healthBars.Count}");
            GUILayout.Label($"交互UI数量: {_interactionUIs.Count}");
            
            var manager = WorldSpaceUIManager.Instance;
            if (manager != null)
            {
                GUILayout.Label($"总UI数量: {manager.TotalInstanceCount}");
                GUILayout.Label($"分组数量: {manager.GroupCount}");
            }
            
            GUILayout.EndArea();
        }
        
        #endregion
    }
}