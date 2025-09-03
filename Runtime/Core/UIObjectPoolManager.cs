using System;
using System.Collections.Generic;
using UnityEngine;

namespace UGF.WorldUI
{
    /// <summary>
    /// UI对象池管理器
    /// </summary>
    public class UIObjectPoolManager
    {
        #region Fields
        
        // 对象池字典
        private readonly Dictionary<string, UIPool> _pools = new Dictionary<string, UIPool>();
        
        // 池父对象
        private Transform _poolParent;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="poolParent">池父对象</param>
        public UIObjectPoolManager(Transform poolParent = null)
        {
            _poolParent = poolParent;
        }
        
        #endregion
        
        #region Pool Management
        
        /// <summary>
        /// 获取或创建对象池
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <param name="config">池配置</param>
        /// <returns>对象池</returns>
        public UIPool GetOrCreatePool(GameObject prefab, UIPoolConfig config = null)
        {
            if (prefab == null) return null;
            
            string poolKey = prefab.name;
            
            if (!_pools.TryGetValue(poolKey, out UIPool pool))
            {
                config = config ?? UIPoolConfig.CreateDefault();
                pool = new UIPool(prefab, config, _poolParent);
                _pools[poolKey] = pool;
                
                Debug.Log($"[UIObjectPoolManager] 创建对象池: {poolKey}");
            }
            
            return pool;
        }
        
        /// <summary>
        /// 获取对象池
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <returns>对象池</returns>
        public UIPool GetPool(GameObject prefab)
        {
            if (prefab == null) return null;
            
            _pools.TryGetValue(prefab.name, out UIPool pool);
            return pool;
        }
        
        /// <summary>
        /// 从池中获取UI组件
        /// </summary>
        /// <typeparam name="T">UI组件类型</typeparam>
        /// <param name="prefab">预制体</param>
        /// <param name="config">池配置</param>
        /// <returns>UI组件</returns>
        public T Get<T>(GameObject prefab, UIPoolConfig config = null) where T : WorldSpaceUIComponent
        {
            var pool = GetOrCreatePool(prefab, config);
            var component = pool?.Get();
            return component as T;
        }
        
        /// <summary>
        /// 将UI组件返回池中
        /// </summary>
        /// <param name="component">UI组件</param>
        public void Return(WorldSpaceUIComponent component)
        {
            if (component == null) return;
            
            // 查找对应的池
            foreach (var pool in _pools.Values)
            {
                if (pool.Contains(component))
                {
                    pool.Return(component);
                    return;
                }
            }
            
            Debug.LogWarning($"[UIObjectPoolManager] 无法找到组件对应的对象池: {component.name}");
        }
        
        /// <summary>
        /// 预热对象池
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <param name="count">预热数量</param>
        /// <param name="config">池配置</param>
        public void WarmPool(GameObject prefab, int count, UIPoolConfig config = null)
        {
            var pool = GetOrCreatePool(prefab, config);
            pool?.PreWarm(count);
        }
        
        /// <summary>
        /// 清理对象池
        /// </summary>
        /// <param name="prefab">预制体</param>
        public void ClearPool(GameObject prefab)
        {
            if (prefab == null) return;
            
            string poolKey = prefab.name;
            if (_pools.TryGetValue(poolKey, out UIPool pool))
            {
                pool.Clear();
                _pools.Remove(poolKey);
                Debug.Log($"[UIObjectPoolManager] 清理对象池: {poolKey}");
            }
        }
        
        /// <summary>
        /// 清理所有对象池
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
            }
            _pools.Clear();
            
            Debug.Log("[UIObjectPoolManager] 清理所有对象池");
        }
        
        /// <summary>
        /// 获取所有对象池
        /// </summary>
        /// <returns>对象池字典</returns>
        public Dictionary<string, UIPool> GetAllPools()
        {
            return new Dictionary<string, UIPool>(_pools);
        }
        
        /// <summary>
        /// 验证所有对象池
        /// </summary>
        public void ValidateAllPools()
        {
            var invalidPools = new List<string>();
            
            foreach (var kvp in _pools)
            {
                if (!kvp.Value.ValidatePool())
                {
                    invalidPools.Add(kvp.Key);
                }
            }
            
            foreach (var poolKey in invalidPools)
            {
                Debug.LogWarning($"[UIObjectPoolManager] 对象池验证失败，将被移除: {poolKey}");
                _pools.Remove(poolKey);
            }
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// 获取调试信息
        /// </summary>
        /// <returns>调试信息</returns>
        public string GetDebugInfo()
        {
            var info = $"UIObjectPoolManager - 总池数: {_pools.Count}\n";
            
            foreach (var kvp in _pools)
            {
                info += $"  {kvp.Key}: {kvp.Value.GetDebugInfo()}\n";
            }
            
            return info;
        }
        
        #endregion
    }
}