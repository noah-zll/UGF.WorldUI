using UnityEngine;
using UGF.WorldUI;
using WorldUISample.Components;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace WorldUISample.Tests
{
    /// <summary>
    /// 性能测试脚本 - 测试WorldUI系统在大量UI元素下的性能表现
    /// </summary>
    public class PerformanceTest : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private GameObject healthBarPrefab;
        [SerializeField] private GameObject damageTextPrefab;
        [SerializeField] private int maxUICount = 1000;
        [SerializeField] private float spawnRadius = 50f;
        [SerializeField] private float testDuration = 60f;
        [SerializeField] private bool autoStartTest = false;
        
        [Header("批量测试")]
        [SerializeField] private int batchSize = 50;
        [SerializeField] private float batchInterval = 0.1f;
        
        [Header("压力测试")]
        [SerializeField] private bool enableStressTest = false;
        [SerializeField] private float damageInterval = 0.05f;
        [SerializeField] private int damageTextPerSecond = 100;
        
        // 测试数据
        private List<HealthBarUI> _testHealthBars = new List<HealthBarUI>();
        private List<DamageTextUI> _testDamageTexts = new List<DamageTextUI>();
        private Stopwatch _testStopwatch = new Stopwatch();
        private bool _isTestRunning = false;
        private float _testStartTime;
        
        // 性能统计
        private struct PerformanceStats
        {
            public float averageFPS;
            public float minFPS;
            public float maxFPS;
            public long memoryUsage;
            public int totalUICreated;
            public int totalUIDestroyed;
            public float averageCreationTime;
            public float averageDestructionTime;
        }
        
        private PerformanceStats _currentStats;
        private List<float> _fpsHistory = new List<float>();
        private List<float> _creationTimes = new List<float>();
        private List<float> _destructionTimes = new List<float>();
        
        #region Unity Callbacks
        
        private void Start()
        {
            InitializeTest();
            
            if (autoStartTest)
            {
                StartCoroutine(AutoStartTestSequence());
            }
        }
        
        private void Update()
        {
            if (_isTestRunning)
            {
                UpdatePerformanceStats();
                
                if (enableStressTest)
                {
                    HandleStressTest();
                }
                
                // 检查测试时间
                if (Time.time - _testStartTime >= testDuration)
                {
                    StopTest();
                }
            }
        }
        
        #endregion
        
        #region Test Initialization
        
        /// <summary>
        /// 初始化测试
        /// </summary>
        private void InitializeTest()
        {
            var manager = WorldSpaceUIManager.Instance;
            
            // 配置高性能设置
            var config = new WorldSpaceUIManagerConfig
            {
                maxTotalInstances = maxUICount * 2,
                enableAutoCleanup = true,
                cleanupInterval = 1f,
                enableGlobalCulling = true,
                globalCullingDistance = spawnRadius * 2f,
                maxUpdatePerFrame = 100,
                enablePerformanceMonitoring = true,
                showDebugInfo = false // 关闭调试信息以提高性能
            };
            manager.SetGlobalConfig(config);
            
            // 创建测试分组
            CreateTestGroups();
            
            Debug.Log("[PerformanceTest] 性能测试初始化完成");
        }
        
        /// <summary>
        /// 创建测试分组
        /// </summary>
        private void CreateTestGroups()
        {
            var manager = WorldSpaceUIManager.Instance;
            
            // 高性能血条分组
            var healthBarConfig = new UIGroupConfig
            {
                sortingOrder = 10,
                maxInstances = maxUICount,
                enableAutoRemoveOldest = true,
                enableCulling = true,
                cullingDistance = spawnRadius,
                enablePooling = true,
                poolSize = Mathf.Min(100, maxUICount / 10),
                enableFadeAnimation = false, // 关闭动画以提高性能
                enableOrthographicOptimization = true,
            };
            manager.CreateGroup("TestHealthBar", healthBarConfig);
            
            // 高性能伤害文字分组
            var damageTextConfig = new UIGroupConfig
            {
                sortingOrder = 20,
                maxInstances = damageTextPerSecond * 5, // 5秒的缓冲
                enableAutoRemoveOldest = true,
                enableCulling = true,
                cullingDistance = spawnRadius / 2f,
                enablePooling = true,
                poolSize = damageTextPerSecond * 2,
                enableFadeAnimation = false,
                enableOrthographicOptimization = true,
            };
            manager.CreateGroup("TestDamageText", damageTextConfig);
        }
        
        #endregion
        
        #region Test Control
        
        /// <summary>
        /// 自动开始测试序列
        /// </summary>
        private IEnumerator AutoStartTestSequence()
        {
            yield return new WaitForSeconds(1f);
            
            Debug.Log("[PerformanceTest] 开始自动测试序列");
            
            // 批量创建测试
            yield return StartCoroutine(BatchCreationTest());
            
            yield return new WaitForSeconds(2f);
            
            // 开始性能测试
            StartTest();
        }
        
        /// <summary>
        /// 开始测试
        /// </summary>
        [ContextMenu("开始测试")]
        public void StartTest()
        {
            if (_isTestRunning) return;
            
            _isTestRunning = true;
            _testStartTime = Time.time;
            _testStopwatch.Restart();
            
            // 重置统计数据
            ResetStats();
            
            Debug.Log($"[PerformanceTest] 开始性能测试 - 持续时间: {testDuration}秒");
        }
        
        /// <summary>
        /// 停止测试
        /// </summary>
        [ContextMenu("停止测试")]
        public void StopTest()
        {
            if (!_isTestRunning) return;
            
            _isTestRunning = false;
            _testStopwatch.Stop();
            
            // 计算最终统计
            CalculateFinalStats();
            
            // 输出测试结果
            LogTestResults();
            
            Debug.Log("[PerformanceTest] 性能测试完成");
        }
        
        /// <summary>
        /// 批量创建测试
        /// </summary>
        private IEnumerator BatchCreationTest()
        {
            Debug.Log($"[PerformanceTest] 开始批量创建测试 - 目标数量: {maxUICount}");
            
            int created = 0;
            while (created < maxUICount)
            {
                int batchCount = Mathf.Min(batchSize, maxUICount - created);
                
                var stopwatch = Stopwatch.StartNew();
                
                for (int i = 0; i < batchCount; i++)
                {
                    CreateRandomHealthBar();
                    created++;
                }
                
                stopwatch.Stop();
                _creationTimes.Add(stopwatch.ElapsedMilliseconds);
                
                yield return new WaitForSeconds(batchInterval);
            }
            
            Debug.Log($"[PerformanceTest] 批量创建完成 - 总数: {created}");
        }
        
        #endregion
        
        #region UI Creation
        
        /// <summary>
        /// 创建随机位置的血条
        /// </summary>
        private void CreateRandomHealthBar()
        {
            if (healthBarPrefab == null) return;
            
            var manager = WorldSpaceUIManager.Instance;
            
            // 随机位置
            Vector3 randomPosition = transform.position + Random.insideUnitSphere * spawnRadius;
            randomPosition.y = Mathf.Abs(randomPosition.y) + 1f; // 确保在地面上方
            
            var healthBar = manager.CreateUI<HealthBarUI>(healthBarPrefab, randomPosition, "TestHealthBar");
            if (healthBar != null)
            {
                // 设置随机血量
                float maxHealth = Random.Range(50f, 200f);
                float currentHealth = Random.Range(10f, maxHealth);
                healthBar.SetHealth(currentHealth, maxHealth);
                
                _testHealthBars.Add(healthBar);
                _currentStats.totalUICreated++;
            }
        }
        
        /// <summary>
        /// 创建随机伤害文字
        /// </summary>
        private void CreateRandomDamageText()
        {
            if (damageTextPrefab == null || _testHealthBars.Count == 0) return;
            
            var manager = WorldSpaceUIManager.Instance;
            
            // 选择随机血条位置
            var randomHealthBar = _testHealthBars[Random.Range(0, _testHealthBars.Count)];
            if (randomHealthBar == null) return;
            
            Vector3 textPosition = randomHealthBar.transform.position + Vector3.up * 1f + Random.insideUnitSphere * 0.5f;
            
            var damageText = manager.CreateUI<DamageTextUI>(damageTextPrefab, textPosition, "TestDamageText");
            if (damageText != null)
            {
                float damage = Random.Range(10f, 100f);
                bool isCritical = Random.value < 0.3f;
                
                if (Random.value < 0.2f)
                {
                    damageText.SetHeal(damage);
                }
                else
                {
                    damageText.SetDamage(damage, isCritical);
                }
                
                _testDamageTexts.Add(damageText);
                _currentStats.totalUICreated++;
            }
        }
        
        #endregion
        
        #region Stress Test
        
        private float _lastDamageTime;
        
        /// <summary>
        /// 处理压力测试
        /// </summary>
        private void HandleStressTest()
        {
            if (Time.time - _lastDamageTime >= damageInterval)
            {
                // 创建多个伤害文字
                int count = Mathf.RoundToInt(damageTextPerSecond * damageInterval);
                for (int i = 0; i < count; i++)
                {
                    CreateRandomDamageText();
                }
                
                // 随机对血条造成伤害
                if (_testHealthBars.Count > 0)
                {
                    var randomHealthBar = _testHealthBars[Random.Range(0, _testHealthBars.Count)];
                    if (randomHealthBar != null)
                    {
                        randomHealthBar.TakeDamage(Random.Range(5f, 25f));
                    }
                }
                
                _lastDamageTime = Time.time;
            }
        }
        
        #endregion
        
        #region Performance Monitoring
        
        /// <summary>
        /// 更新性能统计
        /// </summary>
        private void UpdatePerformanceStats()
        {
            float currentFPS = 1f / Time.unscaledDeltaTime;
            _fpsHistory.Add(currentFPS);
            
            // 限制历史记录长度
            if (_fpsHistory.Count > 300) // 保留5秒的数据（60fps）
            {
                _fpsHistory.RemoveAt(0);
            }
        }
        
        /// <summary>
        /// 重置统计数据
        /// </summary>
        private void ResetStats()
        {
            _currentStats = new PerformanceStats();
            _fpsHistory.Clear();
            _creationTimes.Clear();
            _destructionTimes.Clear();
        }
        
        /// <summary>
        /// 计算最终统计
        /// </summary>
        private void CalculateFinalStats()
        {
            if (_fpsHistory.Count > 0)
            {
                float totalFPS = 0f;
                _currentStats.minFPS = float.MaxValue;
                _currentStats.maxFPS = float.MinValue;
                
                foreach (float fps in _fpsHistory)
                {
                    totalFPS += fps;
                    if (fps < _currentStats.minFPS) _currentStats.minFPS = fps;
                    if (fps > _currentStats.maxFPS) _currentStats.maxFPS = fps;
                }
                
                _currentStats.averageFPS = totalFPS / _fpsHistory.Count;
            }
            
            // 计算平均创建时间
            if (_creationTimes.Count > 0)
            {
                float totalCreationTime = 0f;
                foreach (float time in _creationTimes)
                {
                    totalCreationTime += time;
                }
                _currentStats.averageCreationTime = totalCreationTime / _creationTimes.Count;
            }
            
            // 获取内存使用情况
            _currentStats.memoryUsage = System.GC.GetTotalMemory(false);
        }
        
        /// <summary>
        /// 输出测试结果
        /// </summary>
        private void LogTestResults()
        {
            Debug.Log("=== 性能测试结果 ===");
            Debug.Log($"测试持续时间: {_testStopwatch.ElapsedMilliseconds / 1000f:F2} 秒");
            Debug.Log($"平均FPS: {_currentStats.averageFPS:F2}");
            Debug.Log($"最低FPS: {_currentStats.minFPS:F2}");
            Debug.Log($"最高FPS: {_currentStats.maxFPS:F2}");
            Debug.Log($"总创建UI数量: {_currentStats.totalUICreated}");
            Debug.Log($"总销毁UI数量: {_currentStats.totalUIDestroyed}");
            Debug.Log($"平均创建时间: {_currentStats.averageCreationTime:F2} ms");
            Debug.Log($"内存使用: {_currentStats.memoryUsage / 1024f / 1024f:F2} MB");
            
            var manager = WorldSpaceUIManager.Instance;
            if (manager != null)
            {
                Debug.Log($"当前UI总数: {manager.TotalInstanceCount}");
                Debug.Log($"分组数量: {manager.GroupCount}");
            }
            
            Debug.Log("===================");
        }
        
        #endregion
        
        #region Cleanup
        
        /// <summary>
        /// 清理所有测试UI
        /// </summary>
        [ContextMenu("清理测试UI")]
        public void CleanupTestUI()
        {
            var manager = WorldSpaceUIManager.Instance;
            
            // 销毁所有测试UI
            foreach (var healthBar in _testHealthBars)
            {
                if (healthBar != null)
                {
                    manager.DestroyUI(healthBar);
                }
            }
            
            foreach (var damageText in _testDamageTexts)
            {
                if (damageText != null)
                {
                    manager.DestroyUI(damageText);
                }
            }
            
            _testHealthBars.Clear();
            _testDamageTexts.Clear();
            
            Debug.Log("[PerformanceTest] 清理完成");
        }
        
        private void OnDestroy()
        {
            if (_isTestRunning)
            {
                StopTest();
            }
            
            CleanupTestUI();
        }
        
        #endregion
        
        #region GUI
        
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(Screen.width - 320, 10, 310, 400));
            GUILayout.Label("性能测试控制面板", GUI.skin.box);
            
            GUILayout.Label($"测试状态: {(_isTestRunning ? "运行中" : "停止")}");
            
            if (_isTestRunning)
            {
                float elapsed = Time.time - _testStartTime;
                GUILayout.Label($"已运行: {elapsed:F1}s / {testDuration:F1}s");
                
                if (_fpsHistory.Count > 0)
                {
                    float currentFPS = _fpsHistory[_fpsHistory.Count - 1];
                    GUILayout.Label($"当前FPS: {currentFPS:F1}");
                }
            }
            
            GUILayout.Label($"血条数量: {_testHealthBars.Count}");
            GUILayout.Label($"伤害文字数量: {_testDamageTexts.Count}");
            
            var manager = WorldSpaceUIManager.Instance;
            if (manager != null)
            {
                GUILayout.Label($"总UI数量: {manager.TotalInstanceCount}");
            }
            
            GUILayout.Space(10);
            
            if (!_isTestRunning)
            {
                if (GUILayout.Button("开始测试"))
                {
                    StartTest();
                }
                
                if (GUILayout.Button("批量创建UI"))
                {
                    StartCoroutine(BatchCreationTest());
                }
            }
            else
            {
                if (GUILayout.Button("停止测试"))
                {
                    StopTest();
                }
            }
            
            if (GUILayout.Button("清理测试UI"))
            {
                CleanupTestUI();
            }
            
            GUILayout.Space(10);
            
            enableStressTest = GUILayout.Toggle(enableStressTest, "启用压力测试");
            
            GUILayout.Label("配置:");
            GUILayout.Label($"最大UI数量: {maxUICount}");
            GUILayout.Label($"生成半径: {spawnRadius}m");
            GUILayout.Label($"测试时长: {testDuration}s");
            
            if (enableStressTest)
            {
                GUILayout.Label($"伤害文字/秒: {damageTextPerSecond}");
            }
            
            GUILayout.EndArea();
        }
        
        #endregion
    }
}