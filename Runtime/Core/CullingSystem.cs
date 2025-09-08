using System;
using System.Collections.Generic;
using UnityEngine;

namespace UGF.WorldUI
{
    /// <summary>
    /// 剔除系统
    /// </summary>
    public class CullingSystem
    {
        #region Fields
        
        // 配置
        private readonly CullingConfig _config;
        
        // 剔除对象列表
        private readonly List<WorldSpaceUIComponent> _cullableObjects = new List<WorldSpaceUIComponent>();
        private readonly HashSet<WorldSpaceUIComponent> _culledObjects = new HashSet<WorldSpaceUIComponent>();
        
        // 更新控制
        private float _lastUpdateTime = 0f;
        private int _currentUpdateIndex = 0;
        
        // 统计信息
        private int _totalChecked = 0;
        private int _totalCulled = 0;
        private float _lastFrameTime = 0f;
        
        // 视锥体平面
        private Plane[] _frustumPlanes = new Plane[6];
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// 配置
        /// </summary>
        public CullingConfig Config => _config;
        
        /// <summary>
        /// 摄像机
        /// </summary>
        public Camera Camera => WorldSpaceUIManager.Instance?.UICamera;
        
        /// <summary>
        /// 可剔除对象数量
        /// </summary>
        public int CullableCount => _cullableObjects.Count;
        
        /// <summary>
        /// 已剔除对象数量
        /// </summary>
        public int CulledCount => _culledObjects.Count;
        
        /// <summary>
        /// 剔除率
        /// </summary>
        public float CullingRate => _cullableObjects.Count > 0 ? (float)_culledObjects.Count / _cullableObjects.Count : 0f;
        
        /// <summary>
        /// 统计信息
        /// </summary>
        public CullingStats Stats => new CullingStats
        {
            totalObjects = _cullableObjects.Count,
            culledObjects = _culledObjects.Count,
            cullingRate = CullingRate,
            totalChecked = _totalChecked,
            lastFrameTime = _lastFrameTime
        };
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="config">剔除配置</param>
        /// <param name="manager">WorldSpaceUIManager引用</param>
        public CullingSystem(CullingConfig config)
        {
            _config = config ?? CullingConfig.CreateDefault();
            
            Debug.Log($"[CullingSystem] 剔除系统初始化完成");
        }
        
        #endregion
        
        #region Object Management
        
        /// <summary>
        /// 添加可剔除对象
        /// </summary>
        /// <param name="component">UI组件</param>
        public void AddCullableObject(WorldSpaceUIComponent component)
        {
            if (component == null || _cullableObjects.Contains(component)) return;
            
            _cullableObjects.Add(component);
        }
        
        /// <summary>
        /// 移除可剔除对象
        /// </summary>
        /// <param name="component">UI组件</param>
        public void RemoveCullableObject(WorldSpaceUIComponent component)
        {
            if (component == null) return;
            
            _cullableObjects.Remove(component);
            _culledObjects.Remove(component);
        }
        
        /// <summary>
        /// 强制显示UI组件（用于UIGroup禁用剔除时）
        /// </summary>
        /// <param name="component">要强制显示的UI组件</param>
        public void ForceShow(WorldSpaceUIComponent component)
        {
            if (component == null || !component.IsCulled) return;
            
            // 从剔除列表中移除
            _culledObjects.Remove(component);
            
            // 显示组件
            component.SetCulled(false);
        }
        
        /// <summary>
        /// 清空所有对象
        /// </summary>
        public void ClearAllObjects()
        {
            _cullableObjects.Clear();
            _culledObjects.Clear();
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            ClearAllObjects();
            _frustumPlanes = null;
            Debug.Log("[CullingSystem] 已释放所有资源");
        }
        
        #endregion
        
        #region Culling Update
        
        /// <summary>
        /// 更新剔除
        /// </summary>
        public void UpdateCulling()
        {
            if (!_config.enableCulling || Camera == null || _cullableObjects.Count == 0)
                return;
            
            var startTime = Time.realtimeSinceStartup;
            
            // 检查更新间隔
            if (Time.time - _lastUpdateTime < _config.updateInterval)
                return;
            
            _lastUpdateTime = Time.time;
            
            // 更新视锥体平面
            if (_config.enableFrustumCulling)
            {
                GeometryUtility.CalculateFrustumPlanes(Camera, _frustumPlanes);
            }
            
            // 分批处理对象
            if (_config.enableBatchProcessing)
            {
                UpdateCullingBatched();
            }
            else
            {
                UpdateCullingAll();
            }
            
            _lastFrameTime = (Time.realtimeSinceStartup - startTime) * 1000f; // 转换为毫秒
        }
        
        /// <summary>
        /// 分批更新剔除
        /// </summary>
        private void UpdateCullingBatched()
        {
            int objectsToProcess = Mathf.Min(_config.maxObjectsPerFrame, _cullableObjects.Count);
            int processed = 0;
            
            while (processed < objectsToProcess && _currentUpdateIndex < _cullableObjects.Count)
            {
                var component = _cullableObjects[_currentUpdateIndex];
                if (component != null)
                {
                    UpdateObjectCulling(component);
                    processed++;
                }
                
                _currentUpdateIndex++;
            }
            
            // 重置索引
            if (_currentUpdateIndex >= _cullableObjects.Count)
            {
                _currentUpdateIndex = 0;
            }
        }
        
        /// <summary>
        /// 全量更新剔除
        /// </summary>
        private void UpdateCullingAll()
        {
            for (int i = _cullableObjects.Count - 1; i >= 0; i--)
            {
                var component = _cullableObjects[i];
                if (component == null)
                {
                    _cullableObjects.RemoveAt(i);
                    continue;
                }
                
                UpdateObjectCulling(component);
            }
        }
        
        /// <summary>
        /// 更新单个对象的剔除状态
        /// </summary>
        /// <param name="component">UI组件</param>
        private void UpdateObjectCulling(WorldSpaceUIComponent component)
        {
            _totalChecked++;
            
            bool shouldCull = ShouldCullObject(component);
            bool wasCulled = _culledObjects.Contains(component);
            
            if (shouldCull && !wasCulled)
            {
                // 开始剔除
                _culledObjects.Add(component);
                component.SetCulled(true);
                _totalCulled++;
            }
            else if (!shouldCull && wasCulled)
            {
                // 停止剔除
                _culledObjects.Remove(component);
                component.SetCulled(false);
            }
        }
        
        /// <summary>
        /// 判断对象是否应该被剔除
        /// </summary>
        /// <param name="component">UI组件</param>
        /// <returns>是否应该剔除</returns>
        private bool ShouldCullObject(WorldSpaceUIComponent component)
        {
            // 检查UIGroup是否启用剔除
            if (component.Group != null && !component.Group.IsCullingEnabled())
            {
                return false; // UIGroup禁用剔除，不剔除此组件
            }
            
            var position = component.transform.position;
            var cameraPosition = Camera.transform.position;
            
            // 距离剔除
            if (_config.enableDistanceCulling)
            {
                float distance;
                
                if (Camera.orthographic && _config.enableOrthographicCulling)
                {
                    var cameraSize = Camera.orthographicSize;
                    
                    // 根据剔除模式进行不同的处理
                    if (_config.orthographicCullingMode == OrthographicCullingMode.SizeThreshold)
                    {
                        // 基于Size阈值剔除：检查相机的orthographicSize是否在指定范围内
                        if (cameraSize < _config.minOrthographicSize || cameraSize > _config.maxOrthographicSize)
                        {
                            return true; // 相机Size超出范围，剔除所有UI
                        }
                        
                        // Size在范围内，还需要检查深度距离
                        var cameraForward = Camera.transform.forward;
                        var toPosition = position - cameraPosition;
                        var depthDistance = Mathf.Abs(Vector3.Dot(toPosition, cameraForward));
                        
                        if (depthDistance > _config.maxDistance)
                        {
                            return true;
                        }
                    }
                    else // ViewportBased
                    {
                        // 正交相机：基于相机size和位置进行剔除
                        var cameraForward = Camera.transform.forward;
                        var cameraRight = Camera.transform.right;
                        var cameraUp = Camera.transform.up;
                        var toPosition = position - cameraPosition;
                        
                        // 计算到相机平面的距离（深度）
                        var depthDistance = Mathf.Abs(Vector3.Dot(toPosition, cameraForward));
                        
                        // 计算在相机平面上的偏移
                        var rightOffset = Mathf.Abs(Vector3.Dot(toPosition, cameraRight));
                        var upOffset = Mathf.Abs(Vector3.Dot(toPosition, cameraUp));
                        
                        // 计算相机视野范围
                        var maxRightDistance = cameraSize * Camera.aspect * _config.orthographicCullingMultiplier;
                        var maxUpDistance = cameraSize * _config.orthographicCullingMultiplier;
                        
                        // 检查是否超出相机视野范围
                        if (rightOffset > maxRightDistance || upOffset > maxUpDistance || depthDistance > _config.maxDistance)
                        {
                            return true;
                        }
                    }
                }
                else if (Camera.orthographic)
                {
                    // 正交相机但未启用专用剔除，使用传统距离剔除
                    distance = Vector3.Distance(position, cameraPosition);
                    if (distance > _config.maxDistance)
                    {
                        return true;
                    }
                }
                else
                {
                    // 透视相机：使用传统距离剔除
                    distance = Vector3.Distance(position, cameraPosition);
                    if (distance > _config.maxDistance)
                    {
                        return true;
                    }
                }
            }
            
            // 视锥体剔除
            if (_config.enableFrustumCulling)
            {
                var bounds = component.GetBounds();
                if (!GeometryUtility.TestPlanesAABB(_frustumPlanes, bounds))
                {
                    return true;
                }
            }
            
            // 遮挡剔除
            if (_config.enableOcclusionCulling)
            {
                if (IsOccluded(component, cameraPosition))
                {
                    return true;
                }
            }
            
            // 角度剔除
            if (_config.enableAngleCulling)
            {
                var directionToCamera = (cameraPosition - position).normalized;
                var forward = component.transform.forward;
                float angle = Vector3.Angle(forward, directionToCamera);
                
                if (angle > _config.maxAngle)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 检查对象是否被遮挡
        /// </summary>
        /// <param name="component">UI组件</param>
        /// <param name="cameraPosition">摄像机位置</param>
        /// <returns>是否被遮挡</returns>
        private bool IsOccluded(WorldSpaceUIComponent component, Vector3 cameraPosition)
        {
            var position = component.transform.position;
            var direction = position - cameraPosition;
            var distance = direction.magnitude;
            
            // 射线检测
            if (Physics.Raycast(cameraPosition, direction.normalized, out RaycastHit hit, distance, _config.occlusionLayerMask))
            {
                // 检查击中的对象是否是UI组件本身
                var hitComponent = hit.collider.GetComponent<WorldSpaceUIComponent>();
                return hitComponent != component;
            }
            
            return false;
        }
        
        #endregion
        
        #region Force Operations
        
        /// <summary>
        /// 强制剔除对象
        /// </summary>
        /// <param name="component">UI组件</param>
        public void ForceCull(WorldSpaceUIComponent component)
        {
            if (component == null || _culledObjects.Contains(component)) return;
            
            _culledObjects.Add(component);
            component.SetCulled(true);
        }
        

        
        /// <summary>
        /// 强制更新所有对象
        /// </summary>
        public void ForceUpdateAll()
        {
            _lastUpdateTime = 0f;
            _currentUpdateIndex = 0;
            UpdateCulling();
        }
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// 绘制调试信息
        /// </summary>
        public void DrawDebugGizmos()
        {
            if (!_config.showDebugInfo || Camera == null) return;
            
            var cameraPosition = Camera.transform.position;
            
            // 绘制剔除距离
            if (_config.enableDistanceCulling)
            {
                if (Camera.orthographic && _config.enableOrthographicCulling)
            {
                var cameraTransform = Camera.transform;
                    
                    if (_config.orthographicCullingMode == OrthographicCullingMode.SizeThreshold)
                    {
                        // SizeThreshold模式：显示Size范围信息
                        var currentSize = Camera.orthographicSize;
                        var isInRange = currentSize >= _config.minOrthographicSize && currentSize <= _config.maxOrthographicSize;
                        
                        // 根据是否在范围内选择颜色
                        Gizmos.color = isInRange ? Color.green : Color.red;
                        
                        // 绘制当前相机Size的视野范围
                        var cameraForward = cameraTransform.forward;
                        var cameraRight = cameraTransform.right;
                        var cameraUp = cameraTransform.up;
                        
                        var rightDistance = currentSize * Camera.aspect;
                        var upDistance = currentSize;
                        
                        var corners = new Vector3[4];
                        corners[0] = cameraPosition + cameraRight * rightDistance + cameraUp * upDistance;
                        corners[1] = cameraPosition - cameraRight * rightDistance + cameraUp * upDistance;
                        corners[2] = cameraPosition - cameraRight * rightDistance - cameraUp * upDistance;
                        corners[3] = cameraPosition + cameraRight * rightDistance - cameraUp * upDistance;
                        
                        // 绘制当前Size的矩形
                        for (int i = 0; i < 4; i++)
                        {
                            Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
                        }
                        
                        // 绘制Size范围标识
                        Gizmos.color = Color.yellow;
                        var labelPos = cameraPosition + cameraUp * (currentSize + 1f);
                        Gizmos.DrawWireCube(labelPos, Vector3.one * 0.5f);
                    }
                    else // ViewportBased
                    {
                        // 正交相机：绘制矩形剔除范围
                        Gizmos.color = Color.yellow;
                        var cameraSize = Camera.orthographicSize;
                        var width = cameraSize * Camera.aspect * _config.orthographicCullingMultiplier * 2f;
                        var height = cameraSize * _config.orthographicCullingMultiplier * 2f;
                        var depth = _config.maxDistance * 2f;
                        
                        // 绘制剔除范围的立方体
                        var oldMatrix = Gizmos.matrix;
                        Gizmos.matrix = Matrix4x4.TRS(cameraPosition, Camera.transform.rotation, Vector3.one);
                        Gizmos.DrawWireCube(Vector3.zero, new Vector3(width, height, depth));
                        Gizmos.matrix = oldMatrix;
                    }
                }
                else
                {
                    // 透视相机或未启用正交剔除：绘制球形剔除范围
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(cameraPosition, _config.maxDistance);
                }
            }
            
            // 绘制被剔除的对象
            Gizmos.color = Color.red;
            foreach (var component in _culledObjects)
            {
                if (component != null)
                {
                    Gizmos.DrawWireCube(component.transform.position, Vector3.one * 0.5f);
                }
            }
            
            // 绘制可见对象
            Gizmos.color = Color.green;
            foreach (var component in _cullableObjects)
            {
                if (component != null && !_culledObjects.Contains(component))
                {
                    Gizmos.DrawWireCube(component.transform.position, Vector3.one * 0.3f);
                }
            }
        }
        
        /// <summary>
        /// 获取调试信息
        /// </summary>
        /// <returns>调试信息</returns>
        public string GetDebugInfo()
        {
            return $"剔除系统状态:\n" +
                   $"总对象: {_cullableObjects.Count}\n" +
                   $"已剔除: {_culledObjects.Count}\n" +
                   $"剔除率: {CullingRate:P2}\n" +
                   $"总检查: {_totalChecked}\n" +
                   $"上帧耗时: {_lastFrameTime:F2}ms";
        }
        
        #endregion
    }
    
    /// <summary>
    /// 剔除配置
    /// </summary>
    [Serializable]
    public class CullingConfig
    {
        [Header("基础设置")]
        [Tooltip("启用剔除")]
        public bool enableCulling = true;
        
        [Tooltip("更新间隔（秒）")]
        [Range(0.01f, 1f)]
        public float updateInterval = 0.1f;
        
        [Header("距离剔除")]
        [Tooltip("启用距离剔除")]
        public bool enableDistanceCulling = true;
        
        [Tooltip("最大距离")]
        [Min(1f)]
        public float maxDistance = 100f;
        
        [Header("正交相机设置")]
        [Tooltip("启用正交相机专用剔除")]
        public bool enableOrthographicCulling = true;
        
        [Tooltip("正交相机剔除模式")]
        public OrthographicCullingMode orthographicCullingMode = OrthographicCullingMode.SizeThreshold;
        
        [Tooltip("正交相机剔除倍数（相对于相机size的倍数）")]
        [Range(0.5f, 5f)]
        public float orthographicCullingMultiplier = 1.5f;
        
        [Tooltip("正交相机Size最小值（小于此值的相机会剔除UI）")]
        [Min(0.1f)]
        public float minOrthographicSize = 1f;
        
        [Tooltip("正交相机Size最大值（大于此值的相机会剔除UI）")]
        [Min(0.1f)]
        public float maxOrthographicSize = 80f;
        
        [Header("视锥体剔除")]
        [Tooltip("启用视锥体剔除")]
        public bool enableFrustumCulling = true;
        
        [Header("遮挡剔除")]
        [Tooltip("启用遮挡剔除")]
        public bool enableOcclusionCulling = false;
        
        [Tooltip("遮挡检测层级")]
        public LayerMask occlusionLayerMask = -1;
        
        [Header("角度剔除")]
        [Tooltip("启用角度剔除")]
        public bool enableAngleCulling = false;
        
        [Tooltip("最大角度")]
        [Range(0f, 180f)]
        public float maxAngle = 90f;
        
        [Header("性能优化")]
        [Tooltip("启用分批处理")]
        public bool enableBatchProcessing = true;
        
        [Tooltip("每帧最大处理对象数")]
        [Range(1, 100)]
        public int maxObjectsPerFrame = 20;
        
        [Header("调试")]
        [Tooltip("显示调试信息")]
        public bool showDebugInfo = false;
        
        /// <summary>
        /// 创建默认配置
        /// </summary>
        /// <returns>默认配置</returns>
        public static CullingConfig CreateDefault()
        {
            return new CullingConfig();
        }
        
        /// <summary>
        /// 创建高性能配置
        /// </summary>
        /// <returns>高性能配置</returns>
        public static CullingConfig CreateHighPerformance()
        {
            return new CullingConfig
            {
                enableCulling = true,
                updateInterval = 0.2f,
                enableDistanceCulling = true,
                maxDistance = 50f,
                enableFrustumCulling = true,
                enableOcclusionCulling = false,
                enableAngleCulling = true,
                maxAngle = 60f,
                enableBatchProcessing = true,
                maxObjectsPerFrame = 10
            };
        }
        
        /// <summary>
        /// 创建高质量配置
        /// </summary>
        /// <returns>高质量配置</returns>
        public static CullingConfig CreateHighQuality()
        {
            return new CullingConfig
            {
                enableCulling = true,
                updateInterval = 0.05f,
                enableDistanceCulling = true,
                maxDistance = 200f,
                enableFrustumCulling = true,
                enableOcclusionCulling = true,
                enableAngleCulling = false,
                enableBatchProcessing = true,
                maxObjectsPerFrame = 50
            };
        }
    }
    
    /// <summary>
    /// 剔除统计信息
    /// </summary>
    [Serializable]
    public struct CullingStats
    {
        public int totalObjects;
        public int culledObjects;
        public float cullingRate;
        public int totalChecked;
        public float lastFrameTime;
        
        public override string ToString()
        {
            return $"总对象: {totalObjects}, 已剔除: {culledObjects}, 剔除率: {cullingRate:P2}, 检查次数: {totalChecked}, 耗时: {lastFrameTime:F2}ms";
        }
    }
    
    /// <summary>
    /// 正交相机剔除模式
    /// </summary>
    [Serializable]
    public enum OrthographicCullingMode
    {
        /// <summary>
        /// 基于视口范围剔除（根据相机当前的orthographicSize和视野范围）
        /// </summary>
        ViewportBased,
        
        /// <summary>
        /// 基于Size阈值剔除（根据设置的orthographicSize最大最小值）
        /// </summary>
        SizeThreshold
    }
}