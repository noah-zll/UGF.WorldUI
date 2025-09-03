using System;
using UnityEngine;
using UnityEngine.Events;

namespace UGF.WorldUI
{
    /// <summary>
    /// 世界空间UI事件系统
    /// </summary>
    public static class WorldSpaceUIEvents
    {
        #region Manager Events
        
        /// <summary>
        /// 管理器初始化事件
        /// </summary>
        public static event Action<WorldSpaceUIManager> OnManagerInitialized;
        
        /// <summary>
        /// 管理器销毁事件
        /// </summary>
        public static event Action<WorldSpaceUIManager> OnManagerDestroyed;
        
        /// <summary>
        /// 管理器配置改变事件
        /// </summary>
        public static event Action<WorldSpaceUIManager, WorldSpaceUIManagerConfig> OnManagerConfigChanged;
        
        #endregion
        
        #region UI Component Events
        
        /// <summary>
        /// UI组件创建事件
        /// </summary>
        public static event Action<WorldSpaceUIComponent> OnUIComponentCreated;
        
        /// <summary>
        /// UI组件初始化事件
        /// </summary>
        public static event Action<WorldSpaceUIComponent> OnUIComponentInitialized;
        
        /// <summary>
        /// UI组件销毁事件
        /// </summary>
        public static event Action<WorldSpaceUIComponent> OnUIComponentDestroyed;
        
        /// <summary>
        /// UI组件可见性改变事件
        /// </summary>
        public static event Action<WorldSpaceUIComponent, bool> OnUIComponentVisibilityChanged;
        
        /// <summary>
        /// UI组件剔除状态改变事件
        /// </summary>
        public static event Action<WorldSpaceUIComponent, bool> OnUIComponentCullingChanged;
        
        /// <summary>
        /// UI组件过期事件
        /// </summary>
        public static event Action<WorldSpaceUIComponent> OnUIComponentExpired;
        
        #endregion
        
        #region Group Events
        
        /// <summary>
        /// UI分组创建事件
        /// </summary>
        public static event Action<UIGroup> OnGroupCreated;
        
        /// <summary>
        /// UI分组销毁事件
        /// </summary>
        public static event Action<UIGroup> OnGroupDestroyed;
        
        /// <summary>
        /// UI分组可见性改变事件
        /// </summary>
        public static event Action<UIGroup, bool> OnGroupVisibilityChanged;
        
        /// <summary>
        /// UI分组激活状态改变事件
        /// </summary>
        public static event Action<UIGroup, bool> OnGroupActiveChanged;
        
        /// <summary>
        /// UI分组配置改变事件
        /// </summary>
        public static event Action<UIGroup, UIGroupConfig> OnGroupConfigChanged;
        
        #endregion
        
        #region Pool Events
        
        /// <summary>
        /// 对象池创建事件
        /// </summary>
        public static event Action<string, UIPool> OnPoolCreated;
        
        /// <summary>
        /// 对象池销毁事件
        /// </summary>
        public static event Action<string, UIPool> OnPoolDestroyed;
        
        /// <summary>
        /// 对象从池中获取事件
        /// </summary>
        public static event Action<WorldSpaceUIComponent, UIPool> OnObjectGetFromPool;
        
        /// <summary>
        /// 对象返回池中事件
        /// </summary>
        public static event Action<WorldSpaceUIComponent, UIPool> OnObjectReturnToPool;
        
        /// <summary>
        /// 对象池清理事件
        /// </summary>
        public static event Action<UIPool, int> OnPoolCleaned;
        
        #endregion
        
        #region Performance Events
        
        /// <summary>
        /// 性能警告事件
        /// </summary>
        public static event Action<string, float> OnPerformanceWarning;
        
        /// <summary>
        /// 内存使用警告事件
        /// </summary>
        public static event Action<long> OnMemoryWarning;
        
        /// <summary>
        /// 帧率下降事件
        /// </summary>
        public static event Action<float> OnFrameRateDropped;
        
        #endregion
        
        #region Culling Events
        
        /// <summary>
        /// 剔除系统更新事件
        /// </summary>
        public static event Action<CullingStats> OnCullingUpdated;
        
        /// <summary>
        /// 对象被剔除事件
        /// </summary>
        public static event Action<WorldSpaceUIComponent> OnObjectCulled;
        
        /// <summary>
        /// 对象取消剔除事件
        /// </summary>
        public static event Action<WorldSpaceUIComponent> OnObjectUnculled;
        
        #endregion
        
        #region Update Events
        
        /// <summary>
        /// 更新调度器统计事件
        /// </summary>
        public static event Action<UpdateSchedulerStats> OnUpdateSchedulerStats;
        
        /// <summary>
        /// 对象更新间隔改变事件
        /// </summary>
        public static event Action<WorldSpaceUIComponent, float> OnObjectUpdateIntervalChanged;
        
        #endregion
        
        #region Event Triggers
        
        /// <summary>
        /// 触发管理器初始化事件
        /// </summary>
        /// <param name="manager">管理器</param>
        internal static void TriggerManagerInitialized(WorldSpaceUIManager manager)
        {
            try
            {
                OnManagerInitialized?.Invoke(manager);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSpaceUIEvents] 管理器初始化事件处理错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 触发管理器销毁事件
        /// </summary>
        /// <param name="manager">管理器</param>
        internal static void TriggerManagerDestroyed(WorldSpaceUIManager manager)
        {
            try
            {
                OnManagerDestroyed?.Invoke(manager);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSpaceUIEvents] 管理器销毁事件处理错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 触发管理器配置改变事件
        /// </summary>
        /// <param name="manager">管理器</param>
        /// <param name="config">新配置</param>
        internal static void TriggerManagerConfigChanged(WorldSpaceUIManager manager, WorldSpaceUIManagerConfig config)
        {
            try
            {
                OnManagerConfigChanged?.Invoke(manager, config);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSpaceUIEvents] 管理器配置改变事件处理错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 触发UI组件创建事件
        /// </summary>
        /// <param name="component">UI组件</param>
        internal static void TriggerUIComponentCreated(WorldSpaceUIComponent component)
        {
            try
            {
                OnUIComponentCreated?.Invoke(component);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSpaceUIEvents] UI组件创建事件处理错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 触发UI组件初始化事件
        /// </summary>
        /// <param name="component">UI组件</param>
        internal static void TriggerUIComponentInitialized(WorldSpaceUIComponent component)
        {
            try
            {
                OnUIComponentInitialized?.Invoke(component);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSpaceUIEvents] UI组件初始化事件处理错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 触发UI组件销毁事件
        /// </summary>
        /// <param name="component">UI组件</param>
        internal static void TriggerUIComponentDestroyed(WorldSpaceUIComponent component)
        {
            try
            {
                OnUIComponentDestroyed?.Invoke(component);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSpaceUIEvents] UI组件销毁事件处理错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 触发UI组件可见性改变事件
        /// </summary>
        /// <param name="component">UI组件</param>
        /// <param name="visible">是否可见</param>
        internal static void TriggerUIComponentVisibilityChanged(WorldSpaceUIComponent component, bool visible)
        {
            try
            {
                OnUIComponentVisibilityChanged?.Invoke(component, visible);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSpaceUIEvents] UI组件可见性改变事件处理错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 触发UI组件剔除状态改变事件
        /// </summary>
        /// <param name="component">UI组件</param>
        /// <param name="culled">是否被剔除</param>
        internal static void TriggerUIComponentCullingChanged(WorldSpaceUIComponent component, bool culled)
        {
            try
            {
                OnUIComponentCullingChanged?.Invoke(component, culled);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSpaceUIEvents] UI组件剔除状态改变事件处理错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 触发UI组件过期事件
        /// </summary>
        /// <param name="component">UI组件</param>
        internal static void TriggerUIComponentExpired(WorldSpaceUIComponent component)
        {
            try
            {
                OnUIComponentExpired?.Invoke(component);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSpaceUIEvents] UI组件过期事件处理错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 触发UI分组创建事件
        /// </summary>
        /// <param name="group">UI分组</param>
        internal static void TriggerGroupCreated(UIGroup group)
        {
            try
            {
                OnGroupCreated?.Invoke(group);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSpaceUIEvents] UI分组创建事件处理错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 触发UI分组销毁事件
        /// </summary>
        /// <param name="group">UI分组</param>
        internal static void TriggerGroupDestroyed(UIGroup group)
        {
            try
            {
                OnGroupDestroyed?.Invoke(group);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSpaceUIEvents] UI分组销毁事件处理错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 触发UI分组可见性改变事件
        /// </summary>
        /// <param name="group">UI分组</param>
        /// <param name="visible">是否可见</param>
        internal static void TriggerGroupVisibilityChanged(UIGroup group, bool visible)
        {
            try
            {
                OnGroupVisibilityChanged?.Invoke(group, visible);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSpaceUIEvents] UI分组可见性改变事件处理错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 触发UI分组激活状态改变事件
        /// </summary>
        /// <param name="group">UI分组</param>
        /// <param name="active">是否激活</param>
        internal static void TriggerGroupActiveChanged(UIGroup group, bool active)
        {
            try
            {
                OnGroupActiveChanged?.Invoke(group, active);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSpaceUIEvents] UI分组激活状态改变事件处理错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 触发UI分组配置改变事件
        /// </summary>
        /// <param name="group">UI分组</param>
        /// <param name="config">新配置</param>
        internal static void TriggerGroupConfigChanged(UIGroup group, UIGroupConfig config)
        {
            try
            {
                OnGroupConfigChanged?.Invoke(group, config);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSpaceUIEvents] UI分组配置改变事件处理错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 触发对象池创建事件
        /// </summary>
        /// <param name="poolName">池名称</param>
        /// <param name="pool">对象池</param>
        internal static void TriggerPoolCreated(string poolName, UIPool pool)
        {
            try
            {
                OnPoolCreated?.Invoke(poolName, pool);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSpaceUIEvents] 对象池创建事件处理错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 触发对象池销毁事件
        /// </summary>
        /// <param name="poolName">池名称</param>
        /// <param name="pool">对象池</param>
        internal static void TriggerPoolDestroyed(string poolName, UIPool pool)
        {
            try
            {
                OnPoolDestroyed?.Invoke(poolName, pool);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSpaceUIEvents] 对象池销毁事件处理错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 触发对象从池中获取事件
        /// </summary>
        /// <param name="component">UI组件</param>
        /// <param name="pool">对象池</param>
        internal static void TriggerObjectGetFromPool(WorldSpaceUIComponent component, UIPool pool)
        {
            try
            {
                OnObjectGetFromPool?.Invoke(component, pool);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSpaceUIEvents] 对象从池中获取事件处理错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 触发对象返回池中事件
        /// </summary>
        /// <param name="component">UI组件</param>
        /// <param name="pool">对象池</param>
        internal static void TriggerObjectReturnToPool(WorldSpaceUIComponent component, UIPool pool)
        {
            try
            {
                OnObjectReturnToPool?.Invoke(component, pool);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSpaceUIEvents] 对象返回池中事件处理错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 触发对象池清理事件
        /// </summary>
        /// <param name="pool">对象池</param>
        /// <param name="cleanedCount">清理数量</param>
        internal static void TriggerPoolCleaned(UIPool pool, int cleanedCount)
        {
            try
            {
                OnPoolCleaned?.Invoke(pool, cleanedCount);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSpaceUIEvents] 对象池清理事件处理错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 触发性能警告事件
        /// </summary>
        /// <param name="message">警告信息</param>
        /// <param name="value">相关数值</param>
        internal static void TriggerPerformanceWarning(string message, float value)
        {
            try
            {
                OnPerformanceWarning?.Invoke(message, value);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSpaceUIEvents] 性能警告事件处理错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 触发内存使用警告事件
        /// </summary>
        /// <param name="memoryUsage">内存使用量</param>
        internal static void TriggerMemoryWarning(long memoryUsage)
        {
            try
            {
                OnMemoryWarning?.Invoke(memoryUsage);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSpaceUIEvents] 内存使用警告事件处理错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 触发帧率下降事件
        /// </summary>
        /// <param name="frameRate">当前帧率</param>
        internal static void TriggerFrameRateDropped(float frameRate)
        {
            try
            {
                OnFrameRateDropped?.Invoke(frameRate);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSpaceUIEvents] 帧率下降事件处理错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 触发剔除系统更新事件
        /// </summary>
        /// <param name="stats">剔除统计</param>
        internal static void TriggerCullingUpdated(CullingStats stats)
        {
            try
            {
                OnCullingUpdated?.Invoke(stats);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSpaceUIEvents] 剔除系统更新事件处理错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 触发对象被剔除事件
        /// </summary>
        /// <param name="component">UI组件</param>
        internal static void TriggerObjectCulled(WorldSpaceUIComponent component)
        {
            try
            {
                OnObjectCulled?.Invoke(component);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSpaceUIEvents] 对象被剔除事件处理错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 触发对象取消剔除事件
        /// </summary>
        /// <param name="component">UI组件</param>
        internal static void TriggerObjectUnculled(WorldSpaceUIComponent component)
        {
            try
            {
                OnObjectUnculled?.Invoke(component);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSpaceUIEvents] 对象取消剔除事件处理错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 触发更新调度器统计事件
        /// </summary>
        /// <param name="stats">统计信息</param>
        internal static void TriggerUpdateSchedulerStats(UpdateSchedulerStats stats)
        {
            try
            {
                OnUpdateSchedulerStats?.Invoke(stats);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSpaceUIEvents] 更新调度器统计事件处理错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 触发对象更新间隔改变事件
        /// </summary>
        /// <param name="component">UI组件</param>
        /// <param name="interval">新的更新间隔</param>
        internal static void TriggerObjectUpdateIntervalChanged(WorldSpaceUIComponent component, float interval)
        {
            try
            {
                OnObjectUpdateIntervalChanged?.Invoke(component, interval);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSpaceUIEvents] 对象更新间隔改变事件处理错误: {e.Message}");
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// 清空所有事件监听器
        /// </summary>
        public static void ClearAllListeners()
        {
            OnManagerInitialized = null;
            OnManagerDestroyed = null;
            OnManagerConfigChanged = null;
            
            OnUIComponentCreated = null;
            OnUIComponentInitialized = null;
            OnUIComponentDestroyed = null;
            OnUIComponentVisibilityChanged = null;
            OnUIComponentCullingChanged = null;
            OnUIComponentExpired = null;
            
            OnGroupCreated = null;
            OnGroupDestroyed = null;
            OnGroupVisibilityChanged = null;
            OnGroupActiveChanged = null;
            OnGroupConfigChanged = null;
            
            OnPoolCreated = null;
            OnPoolDestroyed = null;
            OnObjectGetFromPool = null;
            OnObjectReturnToPool = null;
            OnPoolCleaned = null;
            
            OnPerformanceWarning = null;
            OnMemoryWarning = null;
            OnFrameRateDropped = null;
            
            OnCullingUpdated = null;
            OnObjectCulled = null;
            OnObjectUnculled = null;
            
            OnUpdateSchedulerStats = null;
            OnObjectUpdateIntervalChanged = null;
            
            Debug.Log("[WorldSpaceUIEvents] 所有事件监听器已清空");
        }
        
        #endregion
    }
    
    /// <summary>
    /// UI组件事件参数
    /// </summary>
    [Serializable]
    public class UIComponentEventArgs : EventArgs
    {
        public WorldSpaceUIComponent Component { get; set; }
        public string EventType { get; set; }
        public object Data { get; set; }
        
        public UIComponentEventArgs(WorldSpaceUIComponent component, string eventType, object data = null)
        {
            Component = component;
            EventType = eventType;
            Data = data;
        }
    }
    
    /// <summary>
    /// UI分组事件参数
    /// </summary>
    [Serializable]
    public class UIGroupEventArgs : EventArgs
    {
        public UIGroup Group { get; set; }
        public string EventType { get; set; }
        public object Data { get; set; }
        
        public UIGroupEventArgs(UIGroup group, string eventType, object data = null)
        {
            Group = group;
            EventType = eventType;
            Data = data;
        }
    }
}