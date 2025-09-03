using UnityEngine;
using System.Collections.Generic;

namespace UGF.WorldUI
{
    /// <summary>
    /// UpdateScheduler整合测试
    /// </summary>
    public class UpdateSchedulerIntegrationTest : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private bool enableDebugLog = true;
        [SerializeField] private float testDuration = 10f;
        
        private WorldSpaceUIManager _manager;
        private float _testStartTime;
        private Dictionary<string, int> _updateCounts = new Dictionary<string, int>();
        
        void Start()
        {
            _testStartTime = Time.time;
            _manager = FindObjectOfType<WorldSpaceUIManager>();
            
            if (_manager == null)
            {
                Debug.LogError("[UpdateSchedulerIntegrationTest] 未找到WorldSpaceUIManager");
                return;
            }
            
            // 创建测试分组
            CreateTestGroups();
            
            if (enableDebugLog)
            {
                Debug.Log("[UpdateSchedulerIntegrationTest] 开始测试UIGroup整合到UpdateScheduler");
            }
        }
        
        void Update()
        {
            if (_manager == null) return;
            
            // 测试时间结束
            if (Time.time - _testStartTime > testDuration)
            {
                PrintTestResults();
                enabled = false;
                return;
            }
            
            // 每秒输出一次统计信息
            if (enableDebugLog && Time.time - _testStartTime > 0 && (int)(Time.time - _testStartTime) % 1 == 0)
            {
                LogUpdateStatistics();
            }
        }
        
        private void CreateTestGroups()
        {
            // 创建不同更新间隔的测试分组
            var configs = new[]
            {
                new { name = "FastUpdate", interval = 0f },      // 每帧更新
                new { name = "MediumUpdate", interval = 0.1f },   // 每0.1秒更新
                new { name = "SlowUpdate", interval = 0.5f }      // 每0.5秒更新
            };
            
            foreach (var config in configs)
            {
                var groupConfig = new UIGroupConfig
                {
                    updateInterval = config.interval,
                    maxInstances = 10
                };
                
                var group = _manager.CreateGroup(config.name, groupConfig);
                if (group != null)
                {
                    _updateCounts[config.name] = 0;
                    
                    // 订阅分组更新事件（如果有的话）
                    SubscribeToGroupEvents(group);
                    
                    if (enableDebugLog)
                    {
                        Debug.Log($"[UpdateSchedulerIntegrationTest] 创建测试分组: {config.name}, 更新间隔: {config.interval}s");
                    }
                }
            }
        }
        
        private void SubscribeToGroupEvents(UIGroup group)
        {
            // 这里可以添加对UIGroup事件的订阅
            // 由于UIGroup.Update现在由UpdateScheduler调用，我们可以通过其他方式验证
        }
        
        private void LogUpdateStatistics()
        {
            var updateScheduler = GetUpdateScheduler();
            if (updateScheduler != null)
            {
                Debug.Log($"[UpdateSchedulerIntegrationTest] 统计信息 - " +
                         $"UI组件队列: {updateScheduler.QueueCount}, " +
                         $"UIGroup队列: {updateScheduler.GroupQueueCount}, " +
                         $"平均更新时间: {updateScheduler.AverageUpdateTime:F2}ms");
            }
        }
        
        private void PrintTestResults()
        {
            Debug.Log("[UpdateSchedulerIntegrationTest] 测试完成");
            
            var updateScheduler = GetUpdateScheduler();
            if (updateScheduler != null)
            {
                Debug.Log($"[UpdateSchedulerIntegrationTest] 最终统计 - " +
                         $"UI组件队列: {updateScheduler.QueueCount}, " +
                         $"UIGroup队列: {updateScheduler.GroupQueueCount}, " +
                         $"平均更新时间: {updateScheduler.AverageUpdateTime:F2}ms");
            }
            
            // 验证UIGroup是否被正确添加到UpdateScheduler
            var allGroups = _manager.GetAllGroups();
            Debug.Log($"[UpdateSchedulerIntegrationTest] 总分组数: {allGroups.Count}");
            
            foreach (var kvp in allGroups)
            {
                Debug.Log($"[UpdateSchedulerIntegrationTest] 分组: {kvp.Key}, 激活状态: {kvp.Value.IsActive}");
            }
        }
        
        private UpdateScheduler GetUpdateScheduler()
        {
            // 通过反射获取UpdateScheduler实例（仅用于测试）
            var field = typeof(WorldSpaceUIManager).GetField("_updateScheduler", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(_manager) as UpdateScheduler;
        }
    }
}