using System;
using System.Collections.Generic;
using UnityEngine;

namespace UGF.WorldUI
{
    /// <summary>
    /// UI对象池
    /// </summary>
    public class UIPool
    {
        #region Fields
        
        // 池配置
        private readonly UIPoolConfig _config;
        
        // 对象池
        private readonly Queue<WorldSpaceUIComponent> _pool = new Queue<WorldSpaceUIComponent>();
        private readonly HashSet<WorldSpaceUIComponent> _activeObjects = new HashSet<WorldSpaceUIComponent>();
        
        // 预制体引用
        private readonly GameObject _prefab;
        private readonly Transform _poolParent;
        
        // 统计信息
        private int _totalCreated = 0;
        private int _totalDestroyed = 0;
        private float _lastCleanupTime = 0f;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// 池中可用对象数量
        /// </summary>
        public int AvailableCount => _pool.Count;
        
        /// <summary>
        /// 活跃对象数量
        /// </summary>
        public int ActiveCount => _activeObjects.Count;
        
        /// <summary>
        /// 总创建数量
        /// </summary>
        public int TotalCreated => _totalCreated;
        
        /// <summary>
        /// 总销毁数量
        /// </summary>
        public int TotalDestroyed => _totalDestroyed;
        
        /// <summary>
        /// 池配置
        /// </summary>
        public UIPoolConfig Config => _config;
        
        /// <summary>
        /// 预制体
        /// </summary>
        public GameObject Prefab => _prefab;
        
        /// <summary>
        /// 池统计信息
        /// </summary>
        public UIPoolStats Stats => new UIPoolStats
        {
            availableCount = AvailableCount,
            activeCount = ActiveCount,
            totalCreated = _totalCreated,
            totalDestroyed = _totalDestroyed,
            hitRate = _totalCreated > 0 ? (float)(_totalCreated - _totalDestroyed) / _totalCreated : 0f
        };
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <param name="config">池配置</param>
        /// <param name="poolParent">池父对象</param>
        public UIPool(GameObject prefab, UIPoolConfig config, Transform poolParent = null)
        {
            _prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));
            _config = config ?? UIPoolConfig.CreateDefault();
            _poolParent = poolParent;
            
            // 验证预制体
            if (_prefab.GetComponent<WorldSpaceUIComponent>() == null)
            {
                Debug.LogError($"[UIPool] 预制体 {_prefab.name} 必须包含 WorldSpaceUIComponent 组件");
            }
            
            // 预热池
            if (_config.preWarmCount > 0)
            {
                PreWarm(_config.preWarmCount);
            }
            
            Debug.Log($"[UIPool] 对象池创建完成: {_prefab.name}, 预热数量: {_config.preWarmCount}");
        }
        
        #endregion
        
        #region Pool Operations
        
        /// <summary>
        /// 从池中获取对象
        /// </summary>
        /// <returns>UI组件</returns>
        public WorldSpaceUIComponent Get()
        {
            WorldSpaceUIComponent component;
            
            if (_pool.Count > 0)
            {
                // 从池中获取
                component = _pool.Dequeue();
                component.gameObject.SetActive(true);
            }
            else
            {
                // 创建新对象
                component = CreateNewObject();
            }
            
            // 添加到活跃列表
            _activeObjects.Add(component);
            
            return component;
        }
        
        /// <summary>
        /// 将对象返回池中
        /// </summary>
        /// <param name="component">UI组件</param>
        public void Return(WorldSpaceUIComponent component)
        {
            if (component == null) return;
            
            // 从活跃列表移除
            if (!_activeObjects.Remove(component))
            {
                Debug.LogWarning($"[UIPool] 尝试返回不属于此池的对象: {component.name}");
                return;
            }
            
            // 检查池容量
            if (_pool.Count >= _config.maxPoolSize)
            {
                // 池已满，直接销毁
                DestroyObject(component);
                return;
            }
            
            // 重置对象状态
            ResetObject(component);
            
            // 返回池中
            _pool.Enqueue(component);
            component.gameObject.SetActive(false);
            
            // 设置父对象
            if (_poolParent != null)
            {
                component.transform.SetParent(_poolParent);
            }
        }
        
        /// <summary>
        /// 预热池
        /// </summary>
        /// <param name="count">预热数量</param>
        public void PreWarm(int count)
        {
            count = Mathf.Clamp(count, 0, _config.maxPoolSize);
            
            for (int i = 0; i < count; i++)
            {
                var component = CreateNewObject();
                ResetObject(component);
                _pool.Enqueue(component);
                component.gameObject.SetActive(false);
                
                if (_poolParent != null)
                {
                    component.transform.SetParent(_poolParent);
                }
            }
            
            Debug.Log($"[UIPool] 预热完成: {_prefab.name}, 数量: {count}");
        }
        
        /// <summary>
        /// 清理池
        /// </summary>
        /// <param name="keepCount">保留数量</param>
        public void Cleanup(int keepCount = -1)
        {
            if (keepCount < 0)
            {
                keepCount = _config.minPoolSize;
            }
            
            int removeCount = Mathf.Max(0, _pool.Count - keepCount);
            
            for (int i = 0; i < removeCount; i++)
            {
                if (_pool.Count == 0) break;
                
                var component = _pool.Dequeue();
                DestroyObject(component);
            }
            
            _lastCleanupTime = Time.time;
            
            if (removeCount > 0)
            {
                Debug.Log($"[UIPool] 清理完成: {_prefab.name}, 移除数量: {removeCount}, 剩余数量: {_pool.Count}");
            }
        }
        
        /// <summary>
        /// 清空池
        /// </summary>
        public void Clear()
        {
            // 清理池中对象
            while (_pool.Count > 0)
            {
                var component = _pool.Dequeue();
                DestroyObject(component);
            }
            
            // 清理活跃对象
            var activeList = new List<WorldSpaceUIComponent>(_activeObjects);
            foreach (var component in activeList)
            {
                DestroyObject(component);
            }
            _activeObjects.Clear();
            
            Debug.Log($"[UIPool] 池已清空: {_prefab.name}");
        }
        
        /// <summary>
        /// 自动清理（由管理器定期调用）
        /// </summary>
        public void AutoCleanup()
        {
            if (!_config.enableAutoCleanup) return;
            
            if (Time.time - _lastCleanupTime >= _config.cleanupInterval)
            {
                Cleanup();
            }
        }
        
        #endregion
        
        #region Object Management
        
        /// <summary>
        /// 创建新对象
        /// </summary>
        /// <returns>UI组件</returns>
        private WorldSpaceUIComponent CreateNewObject()
        {
            var gameObject = UnityEngine.Object.Instantiate(_prefab);
            var component = gameObject.GetComponent<WorldSpaceUIComponent>();
            
            if (component == null)
            {
                Debug.LogError($"[UIPool] 预制体 {_prefab.name} 缺少 WorldSpaceUIComponent 组件");
                UnityEngine.Object.Destroy(gameObject);
                return null;
            }
            
            _totalCreated++;
            
            return component;
        }
        
        /// <summary>
        /// 重置对象状态
        /// </summary>
        /// <param name="component">UI组件</param>
        private void ResetObject(WorldSpaceUIComponent component)
        {
            if (component == null) return;
            
            // 重置变换
            component.transform.position = Vector3.zero;
            component.transform.rotation = Quaternion.identity;
            component.transform.localScale = Vector3.one;
            
            // 重置可见性
            component.SetVisible(true);
            component.SetCulled(false);
            
            // 重置跟随目标
            component.FollowTarget = null;
            
            // 如果有CanvasGroup，重置透明度
            var canvasGroup = component.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }
        
        /// <summary>
        /// 销毁对象
        /// </summary>
        /// <param name="component">UI组件</param>
        private void DestroyObject(WorldSpaceUIComponent component)
        {
            if (component == null) return;
            
            _activeObjects.Remove(component);
            _totalDestroyed++;
            
            UnityEngine.Object.Destroy(component.gameObject);
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// 验证对象是否属于此池
        /// </summary>
        /// <param name="component">UI组件</param>
        /// <returns>是否属于此池</returns>
        public bool Contains(WorldSpaceUIComponent component)
        {
            return _activeObjects.Contains(component) || _pool.Contains(component);
        }
        
        /// <summary>
        /// 验证池状态
        /// </summary>
        /// <returns>是否正常</returns>
        public bool ValidatePool()
        {
            // 检查池大小限制
            if (_pool.Count > _config.maxPoolSize)
            {
                Debug.LogWarning($"[UIPool] 池大小超出限制: {_pool.Count} > {_config.maxPoolSize}");
                return false;
            }
            
            // 检查活跃对象
            var invalidObjects = new List<WorldSpaceUIComponent>();
            foreach (var obj in _activeObjects)
            {
                if (obj == null)
                {
                    invalidObjects.Add(obj);
                }
            }
            
            // 移除无效对象
            foreach (var obj in invalidObjects)
            {
                _activeObjects.Remove(obj);
            }
            
            if (invalidObjects.Count > 0)
            {
                Debug.LogWarning($"[UIPool] 发现并移除了 {invalidObjects.Count} 个无效的活跃对象");
            }
            
            return true;
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// 获取调试信息
        /// </summary>
        /// <returns>调试信息</returns>
        public string GetDebugInfo()
        {
            return $"UIPool [{_prefab.name}]\n" +
                   $"可用: {AvailableCount}, 活跃: {ActiveCount}\n" +
                   $"总创建: {_totalCreated}, 总销毁: {_totalDestroyed}\n" +
                   $"命中率: {Stats.hitRate:P2}\n" +
                   $"配置: 最小={_config.minPoolSize}, 最大={_config.maxPoolSize}";
        }
        
        #endregion
    }
    
    /// <summary>
    /// UI对象池配置
    /// </summary>
    [Serializable]
    public class UIPoolConfig
    {
        [Header("池大小设置")]
        [Tooltip("最小池大小")]
        [Min(0)]
        public int minPoolSize = 0;
        
        [Tooltip("最大池大小")]
        [Min(1)]
        public int maxPoolSize = 50;
        
        [Tooltip("预热数量")]
        [Min(0)]
        public int preWarmCount = 5;
        
        [Header("自动清理设置")]
        [Tooltip("启用自动清理")]
        public bool enableAutoCleanup = true;
        
        [Tooltip("清理间隔（秒）")]
        [Min(1f)]
        public float cleanupInterval = 30f;
        
        /// <summary>
        /// 创建默认配置
        /// </summary>
        /// <returns>默认配置</returns>
        public static UIPoolConfig CreateDefault()
        {
            return new UIPoolConfig();
        }
        
        /// <summary>
        /// 创建高性能配置
        /// </summary>
        /// <returns>高性能配置</returns>
        public static UIPoolConfig CreateHighPerformance()
        {
            return new UIPoolConfig
            {
                minPoolSize = 10,
                maxPoolSize = 100,
                preWarmCount = 20,
                enableAutoCleanup = true,
                cleanupInterval = 60f
            };
        }
        
        /// <summary>
        /// 创建低内存配置
        /// </summary>
        /// <returns>低内存配置</returns>
        public static UIPoolConfig CreateLowMemory()
        {
            return new UIPoolConfig
            {
                minPoolSize = 0,
                maxPoolSize = 10,
                preWarmCount = 0,
                enableAutoCleanup = true,
                cleanupInterval = 10f
            };
        }
        
        /// <summary>
        /// 验证配置
        /// </summary>
        /// <returns>是否有效</returns>
        public bool Validate()
        {
            if (maxPoolSize <= 0)
            {
                Debug.LogError("[UIPoolConfig] 最大池大小必须大于0");
                return false;
            }
            
            if (minPoolSize > maxPoolSize)
            {
                Debug.LogError("[UIPoolConfig] 最小池大小不能大于最大池大小");
                return false;
            }
            
            if (preWarmCount > maxPoolSize)
            {
                Debug.LogWarning("[UIPoolConfig] 预热数量大于最大池大小，将被限制");
                preWarmCount = maxPoolSize;
            }
            
            if (cleanupInterval <= 0f)
            {
                Debug.LogError("[UIPoolConfig] 清理间隔必须大于0");
                return false;
            }
            
            return true;
        }
    }
    
    /// <summary>
    /// UI对象池统计信息
    /// </summary>
    [Serializable]
    public struct UIPoolStats
    {
        public int availableCount;
        public int activeCount;
        public int totalCreated;
        public int totalDestroyed;
        public float hitRate;
        
        public override string ToString()
        {
            return $"可用: {availableCount}, 活跃: {activeCount}, 创建: {totalCreated}, 销毁: {totalDestroyed}, 命中率: {hitRate:P2}";
        }
    }
}