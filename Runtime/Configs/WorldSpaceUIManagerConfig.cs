using System;
using UnityEngine;

namespace UGF.WorldUI
{
    /// <summary>
    /// 世界空间UI管理器全局配置
    /// </summary>
    [Serializable]
    public class WorldSpaceUIManagerConfig
    {
        [Header("实例管理")]
        [Tooltip("最大总实例数量，0表示无限制")]
        public int maxTotalInstances = 1000;
        
        [Header("自动清理")]
        [Tooltip("启用自动清理过期UI")]
        public bool enableAutoCleanup = true;
        
        [Tooltip("清理间隔时间（秒）")]
        [Range(1f, 60f)]
        public float cleanupInterval = 5f;
        
        [Header("性能优化")]
        [Tooltip("启用全局视锥剔除")]
        public bool enableGlobalCulling = true;
        
        [Tooltip("全局剔除距离")]
        [Range(10f, 1000f)]
        public float globalCullingDistance = 100f;
        
        [Tooltip("每帧最大更新UI数量")]
        [Range(10, 1000)]
        public int maxUpdatePerFrame = 100;
        
        [Header("调试")]
        [Tooltip("启用性能监控")]
        public bool enablePerformanceMonitoring = true;
        
        [Tooltip("显示调试信息")]
        public bool showDebugInfo = false;
        
        [Tooltip("在Scene视图中显示Gizmos")]
        public bool showGizmos = true;
        
        /// <summary>
        /// 创建默认配置
        /// </summary>
        /// <returns>默认配置实例</returns>
        public static WorldSpaceUIManagerConfig CreateDefault()
        {
            return new WorldSpaceUIManagerConfig
            {
                maxTotalInstances = 1000,
                enableAutoCleanup = true,
                cleanupInterval = 5f,
                enableGlobalCulling = true,
                globalCullingDistance = 100f,
                maxUpdatePerFrame = 100,
                enablePerformanceMonitoring = true,
                showDebugInfo = false,
                showGizmos = true
            };
        }
        
        /// <summary>
        /// 验证配置有效性
        /// </summary>
        /// <returns>是否有效</returns>
        public bool Validate()
        {
            if (maxTotalInstances < 0)
            {
                Debug.LogWarning("[WorldSpaceUIManagerConfig] maxTotalInstances 不能为负数");
                return false;
            }
            
            if (cleanupInterval <= 0)
            {
                Debug.LogWarning("[WorldSpaceUIManagerConfig] cleanupInterval 必须大于0");
                return false;
            }
            
            if (globalCullingDistance <= 0)
            {
                Debug.LogWarning("[WorldSpaceUIManagerConfig] globalCullingDistance 必须大于0");
                return false;
            }
            
            if (maxUpdatePerFrame <= 0)
            {
                Debug.LogWarning("[WorldSpaceUIManagerConfig] maxUpdatePerFrame 必须大于0");
                return false;
            }
            
            return true;
        }
    }
}