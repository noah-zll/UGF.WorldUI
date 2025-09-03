using System;
using System.Collections.Generic;
using UnityEngine;

namespace UGF.WorldUI
{
    /// <summary>
    /// 世界空间UI管理器 - 统一管理所有世界空间UI元素
    /// </summary>
    public class WorldSpaceUIManager : MonoBehaviour
    {
        #region Singleton
        
        private static WorldSpaceUIManager _instance;
        
        /// <summary>
        /// 单例实例
        /// </summary>
        public static WorldSpaceUIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<WorldSpaceUIManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("[WorldSpaceUIManager]");
                        _instance = go.AddComponent<WorldSpaceUIManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Fields
        
        [Header("全局配置")]
        [SerializeField] private WorldSpaceUIManagerConfig _globalConfig = new WorldSpaceUIManagerConfig();
        
        [Header("相机设置")]
        [SerializeField] private Camera _uiCamera;
        
        // 分组管理
        private readonly Dictionary<string, UIGroup> _groups = new Dictionary<string, UIGroup>();
        
        // 对象池管理
        private UIObjectPoolManager _objectPoolManager;
        
        // 更新调度器
        private UpdateScheduler _updateScheduler;
        
        // 剔除系统
        private CullingSystem _cullingSystem;
        
        // 相机投影模式检测
        private bool _lastOrthographicState;
        
        // 事件
        public event Action<WorldSpaceUIComponent> OnUICreated;
        public event Action<WorldSpaceUIComponent> OnUIDestroyed;
        public event Action<string, UIGroup> OnGroupCreated;
        public event Action<string> OnGroupRemoved;
        public event Action<int> OnInstanceCountChanged;
        public event Action<float> OnUpdateTimeChanged;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// 全局配置
        /// </summary>
        public WorldSpaceUIManagerConfig GlobalConfig => _globalConfig;
        
        /// <summary>
        /// UI相机
        /// </summary>
        public Camera UICamera
        {
            get => _uiCamera ?? Camera.main;
            set => _uiCamera = value;
        }
        
        /// <summary>
        /// 当前UI相机是否为正交投影
        /// </summary>
        public bool IsOrthographicCamera => UICamera != null && UICamera.orthographic;
        
        /// <summary>
        /// 获取当前相机的投影模式描述
        /// </summary>
        public string CameraProjectionMode => IsOrthographicCamera ? "正交投影" : "透视投影";
        
        /// <summary>
        /// 检测相机投影模式变化事件
        /// </summary>
        public event System.Action<bool> OnCameraProjectionModeChanged;
        
        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized { get; private set; }
        
        /// <summary>
        /// 分组数量
        /// </summary>
        public int GroupCount => _groups.Count;
        
        /// <summary>
        /// 当前UI实例总数
        /// </summary>
        public int TotalInstanceCount { get; private set; }
        
        /// <summary>
        /// 所有分组
        /// </summary>
        public IReadOnlyDictionary<string, UIGroup> Groups => _groups;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Update()
        {
            var startTime = Time.realtimeSinceStartup;
            
            // 检测相机投影模式变化
            CheckCameraProjectionModeChange();
            
            // 更新调度器
            _updateScheduler?.UpdateSchedule();
            
            // 剔除系统
            _cullingSystem?.UpdateCulling();
            
            // 自动清理
            if (_globalConfig.enableAutoCleanup)
            {
                AutoCleanup();
            }
            
            var updateTime = Time.realtimeSinceStartup - startTime;
            OnUpdateTimeChanged?.Invoke(updateTime);
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                // 触发销毁事件
                WorldSpaceUIEvents.TriggerManagerDestroyed(this);
                
                // 清理所有UI分组
                foreach (var group in _groups.Values)
                {
                    group?.Dispose();
                }
                _groups.Clear();
                
                // 清理对象池
                _objectPoolManager?.ClearAllPools();
                _objectPoolManager = null;
                
                // 清理剔除系统
                _cullingSystem?.Dispose();
                _cullingSystem = null;
                
                // 清理更新调度器
                _updateScheduler?.Dispose();
                _updateScheduler = null;
                
                // 清理事件监听器
                WorldSpaceUIEvents.ClearAllListeners();
                
                _instance = null;
                
                Debug.Log("[WorldSpaceUIManager] 已完全清理所有资源");
            }
        }
        
        #endregion
        
        #region Initialization
        
        public void Initialize()
        {
            // 初始化对象池管理器
            _objectPoolManager = new UIObjectPoolManager(transform);
            
            // 初始化更新调度器
            _updateScheduler = new UpdateScheduler();
            
            // 初始化剔除系统
            var cullingConfig = CullingConfig.CreateDefault();
            _cullingSystem = new CullingSystem(cullingConfig);
            
            // 创建默认分组
            CreateGroup("Default", new UIGroupConfig());
            
            IsInitialized = true;
            
            Debug.Log("[WorldSpaceUIManager] 初始化完成");
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// 创建世界空间UI
        /// </summary>
        /// <typeparam name="T">UI组件类型</typeparam>
        /// <param name="prefab">UI预制体</param>
        /// <param name="worldPosition">世界坐标位置</param>
        /// <param name="groupName">分组名称</param>
        /// <returns>创建的UI组件实例</returns>
        public T CreateUI<T>(GameObject prefab, Vector3 worldPosition, string groupName = "Default") where T : WorldSpaceUIComponent
        {
            if (prefab == null)
            {
                Debug.LogError("[WorldSpaceUIManager] 预制体不能为空");
                return null;
            }
            
            // 检查实例数量限制
            if (_globalConfig.maxTotalInstances > 0 && TotalInstanceCount >= _globalConfig.maxTotalInstances)
            {
                Debug.LogWarning("[WorldSpaceUIManager] 已达到最大实例数量限制");
                return null;
            }
            
            // 获取或创建分组
            var group = GetOrCreateGroup(groupName);
            if (group == null)
            {
                Debug.LogError($"[WorldSpaceUIManager] 无法获取或创建分组: {groupName}");
                return null;
            }
            
            // 从对象池获取或创建实例
            T uiComponent;
            if (group.Config.enablePooling)
            {
                uiComponent = _objectPoolManager.Get<T>(prefab);
                // 设置父级Transform
                uiComponent?.transform.SetParent(group.Transform, false);
            }
            else
            {
                var instance = Instantiate(prefab, group.Transform);
                uiComponent = instance.GetComponent<T>();
                if (uiComponent == null)
                {
                    uiComponent = instance.AddComponent<T>();
                }
            }
            
            if (uiComponent == null)
            {
                Debug.LogError($"[WorldSpaceUIManager] 无法获取UI组件: {typeof(T).Name}");
                return null;
            }
            
            // 初始化UI组件
            uiComponent.Initialize(this, group, worldPosition);
            
            // 添加到分组
            var addResult = group.AddUI(uiComponent);
            
            // 添加到更新调度器
            _updateScheduler?.AddUpdateObject(uiComponent, group.Config.updateInterval);
            
            // 添加到剔除系统
            _cullingSystem?.AddCullableObject(uiComponent);
            
            // 更新计数
            TotalInstanceCount++;
            OnInstanceCountChanged?.Invoke(TotalInstanceCount);
            
            // 触发事件
            OnUICreated?.Invoke(uiComponent);
            
            return uiComponent;
        }
        
        /// <summary>
        /// 销毁世界空间UI
        /// </summary>
        /// <param name="uiComponent">要销毁的UI组件</param>
        public void DestroyUI(WorldSpaceUIComponent uiComponent)
        {
            if (uiComponent == null) return;
            
            var group = uiComponent.Group;
            if (group != null)
            {
                group.RemoveUI(uiComponent);
            }
            
            // 从更新调度器移除
            _updateScheduler?.RemoveUpdateObject(uiComponent);
            
            // 从剔除系统移除
            _cullingSystem?.RemoveCullableObject(uiComponent);
            
            // 返回对象池或销毁
            if (group?.Config.enablePooling == true)
            {
                _objectPoolManager.Return(uiComponent);
            }
            else
            {
                if (uiComponent.gameObject != null)
                {
                    Destroy(uiComponent.gameObject);
                }
            }
            
            // 更新计数
            TotalInstanceCount--;
            OnInstanceCountChanged?.Invoke(TotalInstanceCount);
            
            // 触发事件
            OnUIDestroyed?.Invoke(uiComponent);
        }
        
        #endregion
        
        #region Statistics and Information
        
        /// <summary>
        /// 获取统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        public UIManagerStats GetStatistics()
        {
            int activeUICount = 0;
            int visibleUICount = 0;
            int culledUICount = 0;
            
            foreach (var group in _groups.Values)
            {
                var components = group.UIComponents;
                foreach (var component in components)
                {
                    if (component != null)
                    {
                        activeUICount++;
                        if (component.IsVisible)
                            visibleUICount++;
                        if (component.IsCulled)
                            culledUICount++;
                    }
                }
            }
            
            return new UIManagerStats
            {
                ActiveUICount = activeUICount,
                VisibleUICount = visibleUICount,
                CulledUICount = culledUICount,
                PoolCount = _objectPoolManager?.GetAllPools()?.Count ?? 0,
                PooledObjectCount = GetTotalPooledObjectCount()
            };
        }
        
        /// <summary>
        /// 获取所有活跃的UI组件
        /// </summary>
        /// <returns>活跃的UI组件列表</returns>
        public List<WorldSpaceUIComponent> GetAllActiveComponents()
        {
            var activeComponents = new List<WorldSpaceUIComponent>();
            
            foreach (var group in _groups.Values)
            {
                activeComponents.AddRange(group.UIComponents);
            }
            
            return activeComponents;
        }
        
        /// <summary>
        /// 检查是否存在指定分组
        /// </summary>
        /// <param name="groupName">分组名称</param>
        /// <returns>是否存在</returns>
        public bool HasGroup(string groupName)
        {
            return _groups.ContainsKey(groupName);
        }
        
        /// <summary>
        /// 获取所有分组
        /// </summary>
        /// <returns>所有分组的字典</returns>
        public Dictionary<string, UIGroup> GetAllGroups()
        {
            return new Dictionary<string, UIGroup>(_groups);
        }
        
        /// <summary>
        /// 重新加载所有UI
        /// </summary>
        public void ReloadAllUI()
        {
            foreach (var group in _groups.Values)
            {
                // 清除所有UI
                group.ClearUI();
            }
            
            // 清理对象池
            _objectPoolManager?.ClearAllPools();
            
            Debug.Log("[WorldSpaceUIManager] 已重新加载所有UI");
        }
        
        /// <summary>
        /// 获取总的池化对象数量
        /// </summary>
        /// <returns>池化对象数量</returns>
        private int GetTotalPooledObjectCount()
        {
            if (_objectPoolManager == null) return 0;
            
            int count = 0;
            var pools = _objectPoolManager.GetAllPools();
            if (pools != null)
            {
                foreach (var pool in pools.Values)
                {
                    count += pool.AvailableCount;
                }
            }
            return count;
        }
        
        #endregion
        
        #region Group Management
        
        /// <summary>
        /// 创建UI分组
        /// </summary>
        /// <param name="groupName">分组名称</param>
        /// <param name="config">分组配置</param>
        /// <returns>创建的分组</returns>
        public UIGroup CreateGroup(string groupName, UIGroupConfig config = null)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                Debug.LogError("[WorldSpaceUIManager] 分组名称不能为空");
                return null;
            }
            
            if (_groups.ContainsKey(groupName))
            {
                Debug.LogWarning($"[WorldSpaceUIManager] 分组已存在: {groupName}");
                return _groups[groupName];
            }
            
            config ??= new UIGroupConfig();
            var group = new UIGroup(groupName, config, transform);
            _groups[groupName] = group;
            
            // 将UIGroup添加到更新调度器
            _updateScheduler?.AddUpdateGroup(group, config.updateInterval);
            
            OnGroupCreated?.Invoke(groupName, group);
            
            return group;
        }
        
        /// <summary>
        /// 获取UI分组
        /// </summary>
        /// <param name="groupName">分组名称</param>
        /// <returns>分组实例</returns>
        public UIGroup GetGroup(string groupName)
        {
            return _groups.TryGetValue(groupName, out var group) ? group : null;
        }
        
        /// <summary>
        /// 删除UI分组
        /// </summary>
        /// <param name="groupName">分组名称</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveGroup(string groupName)
        {
            if (groupName == "Default")
            {
                Debug.LogWarning("[WorldSpaceUIManager] 不能删除默认分组");
                return false;
            }
            
            if (_groups.TryGetValue(groupName, out var group))
            {
                // 从更新调度器中移除
                _updateScheduler?.RemoveUpdateGroup(group);
                
                group.Dispose();
                _groups.Remove(groupName);
                OnGroupRemoved?.Invoke(groupName);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 设置分组可见性
        /// </summary>
        /// <param name="groupName">分组名称</param>
        /// <param name="visible">是否可见</param>
        public void SetGroupVisible(string groupName, bool visible)
        {
            var group = GetGroup(groupName);
            group?.SetVisible(visible);
        }
        
        /// <summary>
        /// 设置分组激活状态
        /// </summary>
        /// <param name="groupName">分组名称</param>
        /// <param name="active">是否激活</param>
        public void SetGroupActive(string groupName, bool active)
        {
            var group = GetGroup(groupName);
            group?.SetActive(active);
        }
        
        private UIGroup GetOrCreateGroup(string groupName)
        {
            var group = GetGroup(groupName);
            if (group == null)
            {
                group = CreateGroup(groupName);
            }
            return group;
        }
        

        
        #endregion
        
        #region Object Pool
        
        /// <summary>
        /// 预热对象池
        /// </summary>
        /// <typeparam name="T">UI组件类型</typeparam>
        /// <param name="prefab">预制体</param>
        /// <param name="count">预热数量</param>
        public void WarmupPool<T>(GameObject prefab, int count) where T : WorldSpaceUIComponent
        {
            _objectPoolManager?.WarmPool(prefab, count);
        }
        
        /// <summary>
        /// 清理对象池
        /// </summary>
        public void ClearPool()
        {
            _objectPoolManager?.ClearAllPools();
        }
        
        /// <summary>
        /// 获取所有对象池
        /// </summary>
        /// <returns>对象池字典</returns>
        public Dictionary<string, UIPool> GetAllPools()
        {
            return _objectPoolManager?.GetAllPools() ?? new Dictionary<string, UIPool>();
        }
        
        /// <summary>
        /// 清理所有对象池
        /// </summary>
        public void ClearAllPools()
        {
            _objectPoolManager?.ClearAllPools();
        }
        
        /// <summary>
        /// 预热所有对象池
        /// </summary>
        public void WarmAllPools()
        {
            var pools = GetAllPools();
            foreach (var pool in pools.Values)
            {
                pool.PreWarm(pool.Config.preWarmCount);
            }
        }
        
        #endregion
        
        #region Configuration
        
        /// <summary>
        /// 设置全局配置
        /// </summary>
        /// <param name="config">全局配置</param>
        public void SetGlobalConfig(WorldSpaceUIManagerConfig config)
        {
            _globalConfig = config ?? new WorldSpaceUIManagerConfig();
        }
        
        #endregion
        
        #region Auto Cleanup
        
        private float _lastCleanupTime;
        
        private void AutoCleanup()
        {
            if (Time.time - _lastCleanupTime < _globalConfig.cleanupInterval)
                return;
            
            _lastCleanupTime = Time.time;
            
            // 清理过期的UI
            foreach (var group in _groups.Values)
            {
                group.CleanupExpiredUIs();
            }
            
            // 清理对象池
            _objectPoolManager?.ValidateAllPools();
        }
        
        /// <summary>
        /// 检测相机投影模式变化
        /// </summary>
        private void CheckCameraProjectionModeChange()
        {
            var currentOrthographicState = IsOrthographicCamera;
            if (currentOrthographicState != _lastOrthographicState)
            {
                _lastOrthographicState = currentOrthographicState;
                OnCameraProjectionModeChanged?.Invoke(currentOrthographicState);
                
                Debug.Log($"[WorldSpaceUIManager] 相机投影模式已变更为: {CameraProjectionMode}");
                
                // 通知所有分组更新缩放设置
                foreach (var group in _groups.Values)
                {
                    group.RefreshCanvasScaler();
                }
            }
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// 绘制调试Gizmos
        /// </summary>
        private void OnDrawGizmos()
        {
            if (_cullingSystem != null)
            {
                _cullingSystem.DrawDebugGizmos();
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// UI管理器统计信息
    /// </summary>
    [System.Serializable]
    public struct UIManagerStats
    {
        /// <summary>
        /// 活跃UI数量
        /// </summary>
        public int ActiveUICount;
        
        /// <summary>
        /// 可见UI数量
        /// </summary>
        public int VisibleUICount;
        
        /// <summary>
        /// 被剔除UI数量
        /// </summary>
        public int CulledUICount;
        
        /// <summary>
        /// 对象池数量
        /// </summary>
        public int PoolCount;
        
        /// <summary>
        /// 池化对象数量
        /// </summary>
        public int PooledObjectCount;
    }
}