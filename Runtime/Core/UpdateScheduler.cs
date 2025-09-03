using System;
using System.Collections.Generic;
using UnityEngine;

namespace UGF.WorldUI
{
    /// <summary>
    /// 更新调度器
    /// </summary>
    public class UpdateScheduler
    {
        #region Fields
        
        // 配置
        private readonly UpdateSchedulerConfig _config;
        
        // 更新队列
        private readonly List<WorldSpaceUIComponent> _updateQueue = new List<WorldSpaceUIComponent>();
        private readonly Dictionary<WorldSpaceUIComponent, float> _lastUpdateTimes = new Dictionary<WorldSpaceUIComponent, float>();
        private readonly Dictionary<WorldSpaceUIComponent, float> _updateIntervals = new Dictionary<WorldSpaceUIComponent, float>();
        
        // UIGroup更新队列
        private readonly List<UIGroup> _groupUpdateQueue = new List<UIGroup>();
        private readonly Dictionary<UIGroup, float> _groupLastUpdateTimes = new Dictionary<UIGroup, float>();
        private readonly Dictionary<UIGroup, float> _groupUpdateIntervals = new Dictionary<UIGroup, float>();
        
        // 分批更新
        private int _currentBatchIndex = 0;
        private float _lastBatchTime = 0f;
        
        // 统计信息
        private int _totalUpdates = 0;
        private float _totalUpdateTime = 0f;
        private float _lastFrameTime = 0f;
        private int _updatesThisFrame = 0;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// 配置
        /// </summary>
        public UpdateSchedulerConfig Config => _config;
        
        /// <summary>
        /// 更新队列中的对象数量
        /// </summary>
        public int QueueCount => _updateQueue.Count;
        
        /// <summary>
        /// UIGroup更新队列中的对象数量
        /// </summary>
        public int GroupQueueCount => _groupUpdateQueue.Count;
        
        /// <summary>
        /// 平均更新时间
        /// </summary>
        public float AverageUpdateTime => _totalUpdates > 0 ? _totalUpdateTime / _totalUpdates : 0f;
        
        /// <summary>
        /// 统计信息
        /// </summary>
        public UpdateSchedulerStats Stats => new UpdateSchedulerStats
        {
            queueCount = _updateQueue.Count,
            totalUpdates = _totalUpdates,
            averageUpdateTime = AverageUpdateTime,
            lastFrameTime = _lastFrameTime,
            updatesThisFrame = _updatesThisFrame
        };
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="config">调度器配置</param>
        public UpdateScheduler(UpdateSchedulerConfig config = null)
        {
            _config = config ?? UpdateSchedulerConfig.CreateDefault();
            
            Debug.Log($"[UpdateScheduler] 更新调度器初始化完成");
        }
        
        #endregion
        
        #region Object Management
        
        /// <summary>
        /// 添加更新对象
        /// </summary>
        /// <param name="component">UI组件</param>
        /// <param name="updateInterval">更新间隔（0表示每帧更新）</param>
        public void AddUpdateObject(WorldSpaceUIComponent component, float updateInterval = 0f)
        {
            if (component == null || _updateQueue.Contains(component)) return;
            
            _updateQueue.Add(component);
            _lastUpdateTimes[component] = Time.time;
            _updateIntervals[component] = updateInterval;
            
            // 根据配置自动调整更新间隔
            if (_config.enableAdaptiveUpdate)
            {
                AdjustUpdateInterval(component);
            }
        }
        
        /// <summary>
        /// 移除更新对象
        /// </summary>
        /// <param name="component">UI组件</param>
        public void RemoveUpdateObject(WorldSpaceUIComponent component)
        {
            if (component == null) return;
            
            _updateQueue.Remove(component);
            _lastUpdateTimes.Remove(component);
            _updateIntervals.Remove(component);
        }
        
        /// <summary>
        /// 清空所有对象
        /// </summary>
        public void ClearAllObjects()
        {
            _updateQueue.Clear();
            _lastUpdateTimes.Clear();
            _updateIntervals.Clear();
            
            _groupUpdateQueue.Clear();
            _groupLastUpdateTimes.Clear();
            _groupUpdateIntervals.Clear();
        }
        
        /// <summary>
        /// 设置对象更新间隔
        /// </summary>
        /// <param name="component">UI组件</param>
        /// <param name="interval">更新间隔</param>
        public void SetUpdateInterval(WorldSpaceUIComponent component, float interval)
        {
            if (component == null || !_updateQueue.Contains(component)) return;
            
            _updateIntervals[component] = interval;
        }
        
        /// <summary>
        /// 添加UIGroup到更新队列
        /// </summary>
        /// <param name="group">UI分组</param>
        /// <param name="updateInterval">更新间隔</param>
        public void AddUpdateGroup(UIGroup group, float updateInterval = 0f)
        {
            if (group == null || _groupUpdateQueue.Contains(group)) return;
            
            _groupUpdateQueue.Add(group);
            _groupUpdateIntervals[group] = updateInterval;
            _groupLastUpdateTimes[group] = Time.time;
        }
        
        /// <summary>
        /// 移除UIGroup
        /// </summary>
        /// <param name="group">UI分组</param>
        public void RemoveUpdateGroup(UIGroup group)
        {
            if (group == null) return;
            
            _groupUpdateQueue.Remove(group);
            _groupLastUpdateTimes.Remove(group);
            _groupUpdateIntervals.Remove(group);
        }
        
        /// <summary>
        /// 设置UIGroup更新间隔
        /// </summary>
        /// <param name="group">UI分组</param>
        /// <param name="interval">更新间隔</param>
        public void SetGroupUpdateInterval(UIGroup group, float interval)
        {
            if (group == null || !_groupUpdateQueue.Contains(group)) return;
            
            _groupUpdateIntervals[group] = interval;
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            ClearAllObjects();
            Debug.Log("[UpdateScheduler] 已释放所有资源");
        }
        
        #endregion
        
        #region Update Processing
        
        /// <summary>
        /// 更新调度
        /// </summary>
        public void UpdateSchedule()
        {
            if (_updateQueue.Count == 0 && _groupUpdateQueue.Count == 0) return;
            
            var startTime = Time.realtimeSinceStartup;
            _updatesThisFrame = 0;
            
            // 首先更新UIGroup
            UpdateGroups();
            
            // 然后更新UI组件
            if (_updateQueue.Count > 0)
            {
                if (_config.enableBatchUpdate)
                {
                    UpdateBatched();
                }
                else
                {
                    UpdateAll();
                }
            }
            
            _lastFrameTime = (Time.realtimeSinceStartup - startTime) * 1000f; // 转换为毫秒
        }
        
        /// <summary>
        /// 更新UIGroup
        /// </summary>
        private void UpdateGroups()
        {
            for (int i = _groupUpdateQueue.Count - 1; i >= 0; i--)
            {
                var group = _groupUpdateQueue[i];
                
                if (group == null)
                {
                    // 移除无效对象
                    _groupUpdateQueue.RemoveAt(i);
                    _groupLastUpdateTimes.Remove(group);
                    _groupUpdateIntervals.Remove(group);
                    continue;
                }
                
                if (ShouldUpdateGroup(group))
                {
                    UpdateGroup(group);
                }
            }
        }
        
        /// <summary>
        /// 分批更新
        /// </summary>
        private void UpdateBatched()
        {
            int maxUpdatesThisFrame = _config.maxUpdatesPerFrame;
            int updatesProcessed = 0;
            
            // 检查是否需要开始新的批次
            if (Time.time - _lastBatchTime >= _config.batchInterval)
            {
                _currentBatchIndex = 0;
                _lastBatchTime = Time.time;
            }
            
            // 处理当前批次
            while (updatesProcessed < maxUpdatesThisFrame && _currentBatchIndex < _updateQueue.Count)
            {
                var component = _updateQueue[_currentBatchIndex];
                
                if (component == null)
                {
                    // 移除无效对象
                    _updateQueue.RemoveAt(_currentBatchIndex);
                    _lastUpdateTimes.Remove(component);
                    _updateIntervals.Remove(component);
                    continue;
                }
                
                if (ShouldUpdateObject(component))
                {
                    UpdateObject(component);
                    updatesProcessed++;
                }
                
                _currentBatchIndex++;
            }
            
            // 重置批次索引
            if (_currentBatchIndex >= _updateQueue.Count)
            {
                _currentBatchIndex = 0;
            }
        }
        
        /// <summary>
        /// 全量更新
        /// </summary>
        private void UpdateAll()
        {
            for (int i = _updateQueue.Count - 1; i >= 0; i--)
            {
                var component = _updateQueue[i];
                
                if (component == null)
                {
                    // 移除无效对象
                    _updateQueue.RemoveAt(i);
                    _lastUpdateTimes.Remove(component);
                    _updateIntervals.Remove(component);
                    continue;
                }
                
                if (ShouldUpdateObject(component))
                {
                    UpdateObject(component);
                }
            }
        }
        
        /// <summary>
        /// 判断对象是否需要更新
        /// </summary>
        /// <param name="component">UI组件</param>
        /// <returns>是否需要更新</returns>
        private bool ShouldUpdateObject(WorldSpaceUIComponent component)
        {
            // 检查对象是否被剔除
            if (component.IsCulled && !_config.updateCulledObjects)
            {
                return false;
            }
            
            // 检查更新间隔
            if (_updateIntervals.TryGetValue(component, out float interval) && interval > 0f)
            {
                if (_lastUpdateTimes.TryGetValue(component, out float lastTime))
                {
                    return Time.time - lastTime >= interval;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 更新单个对象
        /// </summary>
        /// <param name="component">UI组件</param>
        private void UpdateObject(WorldSpaceUIComponent component)
        {
            var startTime = Time.realtimeSinceStartup;
            
            try
            {
                component.UpdateUI();
                _lastUpdateTimes[component] = Time.time;
                
                _totalUpdates++;
                _updatesThisFrame++;
                
                var updateTime = (Time.realtimeSinceStartup - startTime) * 1000f;
                _totalUpdateTime += updateTime;
                
                // 自适应更新间隔调整
                if (_config.enableAdaptiveUpdate)
                {
                    AdjustUpdateIntervalBasedOnPerformance(component, updateTime);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[UpdateScheduler] 更新对象时发生错误: {component.name}, 错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 判断UIGroup是否需要更新
        /// </summary>
        /// <param name="group">UI分组</param>
        /// <returns>是否需要更新</returns>
        private bool ShouldUpdateGroup(UIGroup group)
        {
            // 检查分组是否激活
            if (!group.IsActive)
            {
                return false;
            }
            
            // 检查更新间隔
            if (_groupUpdateIntervals.TryGetValue(group, out float interval) && interval > 0f)
            {
                if (_groupLastUpdateTimes.TryGetValue(group, out float lastTime))
                {
                    return Time.time - lastTime >= interval;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 更新单个UIGroup
        /// </summary>
        /// <param name="group">UI分组</param>
        private void UpdateGroup(UIGroup group)
        {
            var startTime = Time.realtimeSinceStartup;
            
            try
            {
                group.Update();
                _groupLastUpdateTimes[group] = Time.time;
                
                _totalUpdates++;
                _updatesThisFrame++;
                
                var updateTime = (Time.realtimeSinceStartup - startTime) * 1000f;
                _totalUpdateTime += updateTime;
            }
            catch (Exception e)
            {
                Debug.LogError($"[UpdateScheduler] 更新UIGroup时发生错误: {group.Name}, 错误: {e.Message}");
            }
        }
        
        #endregion
        
        #region Adaptive Update
        
        /// <summary>
        /// 调整更新间隔
        /// </summary>
        /// <param name="component">UI组件</param>
        private void AdjustUpdateInterval(WorldSpaceUIComponent component)
        {
            if (component.Manager?.UICamera == null) return;
            
            var camera = component.Manager.UICamera;
            var distance = Vector3.Distance(component.transform.position, camera.transform.position);
            
            // 基于距离调整更新频率
            float interval = 0f;
            
            if (distance > _config.farDistance)
            {
                interval = _config.farUpdateInterval;
            }
            else if (distance > _config.mediumDistance)
            {
                interval = _config.mediumUpdateInterval;
            }
            else
            {
                interval = _config.nearUpdateInterval;
            }
            
            _updateIntervals[component] = interval;
        }
        
        /// <summary>
        /// 基于性能调整更新间隔
        /// </summary>
        /// <param name="component">UI组件</param>
        /// <param name="updateTime">更新耗时</param>
        private void AdjustUpdateIntervalBasedOnPerformance(WorldSpaceUIComponent component, float updateTime)
        {
            if (!_updateIntervals.TryGetValue(component, out float currentInterval))
                return;
            
            // 如果更新耗时过长，增加更新间隔
            if (updateTime > _config.maxUpdateTimeThreshold)
            {
                float newInterval = Mathf.Min(currentInterval * 1.5f, _config.maxUpdateInterval);
                _updateIntervals[component] = newInterval;
            }
            // 如果更新耗时很短，减少更新间隔
            else if (updateTime < _config.minUpdateTimeThreshold && currentInterval > 0f)
            {
                float newInterval = Mathf.Max(currentInterval * 0.8f, _config.minUpdateInterval);
                _updateIntervals[component] = newInterval;
            }
        }
        
        #endregion
        
        #region Priority Management
        
        /// <summary>
        /// 设置对象优先级
        /// </summary>
        /// <param name="component">UI组件</param>
        /// <param name="priority">优先级</param>
        public void SetObjectPriority(WorldSpaceUIComponent component, UpdatePriority priority)
        {
            if (component == null || !_updateQueue.Contains(component)) return;
            
            // 移除并重新插入以调整位置
            _updateQueue.Remove(component);
            
            int insertIndex = GetInsertIndexByPriority(priority);
            _updateQueue.Insert(insertIndex, component);
        }
        
        /// <summary>
        /// 根据优先级获取插入位置
        /// </summary>
        /// <param name="priority">优先级</param>
        /// <returns>插入位置</returns>
        private int GetInsertIndexByPriority(UpdatePriority priority)
        {
            // 高优先级对象放在队列前面
            switch (priority)
            {
                case UpdatePriority.High:
                    return 0;
                case UpdatePriority.Medium:
                    // 找到第一个非高优先级对象的位置
                    for (int i = 0; i < _updateQueue.Count; i++)
                    {
                        // 这里可以扩展为存储优先级信息
                        // 暂时简单处理
                    }
                    return _updateQueue.Count / 2;
                case UpdatePriority.Low:
                default:
                    return _updateQueue.Count;
            }
        }
        
        #endregion
        
        #region Force Operations
        
        /// <summary>
        /// 强制更新对象
        /// </summary>
        /// <param name="component">UI组件</param>
        public void ForceUpdate(WorldSpaceUIComponent component)
        {
            if (component == null) return;
            
            UpdateObject(component);
        }
        
        /// <summary>
        /// 强制更新所有对象
        /// </summary>
        public void ForceUpdateAll()
        {
            foreach (var component in _updateQueue)
            {
                if (component != null)
                {
                    UpdateObject(component);
                }
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
            return $"更新调度器状态:\n" +
                   $"队列对象: {_updateQueue.Count}\n" +
                   $"总更新次数: {_totalUpdates}\n" +
                   $"平均更新时间: {AverageUpdateTime:F2}ms\n" +
                   $"上帧更新时间: {_lastFrameTime:F2}ms\n" +
                   $"本帧更新数: {_updatesThisFrame}";
        }
        
        #endregion
    }
    
    /// <summary>
    /// 更新调度器配置
    /// </summary>
    [Serializable]
    public class UpdateSchedulerConfig
    {
        [Header("基础设置")]
        [Tooltip("启用分批更新")]
        public bool enableBatchUpdate = true;
        
        [Tooltip("每帧最大更新数")]
        [Range(1, 100)]
        public int maxUpdatesPerFrame = 20;
        
        [Tooltip("批次间隔（秒）")]
        [Range(0.01f, 1f)]
        public float batchInterval = 0.1f;
        
        [Tooltip("更新被剔除的对象")]
        public bool updateCulledObjects = false;
        
        [Header("自适应更新")]
        [Tooltip("启用自适应更新")]
        public bool enableAdaptiveUpdate = false;
        
        [Tooltip("近距离")]
        [Min(0f)]
        public float nearDistance = 10f;
        
        [Tooltip("中距离")]
        [Min(0f)]
        public float mediumDistance = 50f;
        
        [Tooltip("远距离")]
        [Min(0f)]
        public float farDistance = 100f;
        
        [Tooltip("近距离更新间隔")]
        [Min(0f)]
        public float nearUpdateInterval = 0f;
        
        [Tooltip("中距离更新间隔")]
        [Min(0f)]
        public float mediumUpdateInterval = 0.1f;
        
        [Tooltip("远距离更新间隔")]
        [Min(0f)]
        public float farUpdateInterval = 0.5f;
        
        [Header("性能调整")]
        [Tooltip("最大更新时间阈值（毫秒）")]
        [Min(0f)]
        public float maxUpdateTimeThreshold = 5f;
        
        [Tooltip("最小更新时间阈值（毫秒）")]
        [Min(0f)]
        public float minUpdateTimeThreshold = 0.5f;
        
        [Tooltip("最大更新间隔")]
        [Min(0f)]
        public float maxUpdateInterval = 2f;
        
        [Tooltip("最小更新间隔")]
        [Min(0f)]
        public float minUpdateInterval = 0f;
        
        /// <summary>
        /// 创建默认配置
        /// </summary>
        /// <returns>默认配置</returns>
        public static UpdateSchedulerConfig CreateDefault()
        {
            return new UpdateSchedulerConfig();
        }
        
        /// <summary>
        /// 创建高性能配置
        /// </summary>
        /// <returns>高性能配置</returns>
        public static UpdateSchedulerConfig CreateHighPerformance()
        {
            return new UpdateSchedulerConfig
            {
                enableBatchUpdate = true,
                maxUpdatesPerFrame = 10,
                batchInterval = 0.2f,
                updateCulledObjects = false,
                enableAdaptiveUpdate = true,
                nearDistance = 5f,
                mediumDistance = 25f,
                farDistance = 50f,
                nearUpdateInterval = 0.05f,
                mediumUpdateInterval = 0.2f,
                farUpdateInterval = 1f,
                maxUpdateTimeThreshold = 3f,
                minUpdateTimeThreshold = 0.3f,
                maxUpdateInterval = 3f,
                minUpdateInterval = 0.05f
            };
        }
        
        /// <summary>
        /// 创建高质量配置
        /// </summary>
        /// <returns>高质量配置</returns>
        public static UpdateSchedulerConfig CreateHighQuality()
        {
            return new UpdateSchedulerConfig
            {
                enableBatchUpdate = true,
                maxUpdatesPerFrame = 50,
                batchInterval = 0.05f,
                updateCulledObjects = true,
                enableAdaptiveUpdate = true,
                nearDistance = 20f,
                mediumDistance = 100f,
                farDistance = 200f,
                nearUpdateInterval = 0f,
                mediumUpdateInterval = 0.05f,
                farUpdateInterval = 0.2f,
                maxUpdateTimeThreshold = 10f,
                minUpdateTimeThreshold = 1f,
                maxUpdateInterval = 1f,
                minUpdateInterval = 0f
            };
        }
    }
    
    /// <summary>
    /// 更新优先级
    /// </summary>
    public enum UpdatePriority
    {
        Low = 0,
        Medium = 1,
        High = 2
    }
    
    /// <summary>
    /// 更新调度器统计信息
    /// </summary>
    [Serializable]
    public struct UpdateSchedulerStats
    {
        public int queueCount;
        public int totalUpdates;
        public float averageUpdateTime;
        public float lastFrameTime;
        public int updatesThisFrame;
        
        public override string ToString()
        {
            return $"队列: {queueCount}, 总更新: {totalUpdates}, 平均耗时: {averageUpdateTime:F2}ms, 上帧耗时: {lastFrameTime:F2}ms, 本帧更新: {updatesThisFrame}";
        }
    }
}