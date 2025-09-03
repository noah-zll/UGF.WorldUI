using System;
using UnityEngine;

namespace UGF.WorldUI
{
    /// <summary>
    /// 正交相机距离计算模式
    /// </summary>
    public enum OrthographicDistanceMode
    {
        /// <summary>
        /// 使用Z轴深度
        /// </summary>
        ZDepth,
        
        /// <summary>
        /// 使用固定距离
        /// </summary>
        FixedDistance,
        
        /// <summary>
        /// 使用相机Size计算
        /// </summary>
        CameraSize
    }
    
    /// <summary>
    /// UI分组配置
    /// </summary>
    [Serializable]
    public class UIGroupConfig
    {
        [Header("渲染设置")]
        [Tooltip("排序层级")]
        public int sortingOrder = 0;
        
        [Tooltip("渲染层级")]
        public string sortingLayerName = "Default";
        
        [Header("实例管理")]
        [Tooltip("最大实例数量，0表示无限制")]
        public int maxInstances = 100;
        
        [Tooltip("当达到最大数量时自动移除最旧的UI")]
        public bool enableAutoRemoveOldest = true;
        
        [Header("性能优化")]
        [Tooltip("启用视锥剔除")]
        public bool enableCulling = true;
        
        [Tooltip("剔除距离")]
        [Range(1f, 500f)]
        public float cullingDistance = 50f;
        
        [Tooltip("更新间隔（秒），0表示每帧更新")]
        [Range(0f, 10f)]
        public float updateInterval = 0.016f;
        
        [Header("对象池")]
        [Tooltip("启用对象池")]
        public bool enablePooling = true;
        
        [Tooltip("对象池大小")]
        [Range(1, 100)]
        public int poolSize = 20;
        
        [Header("LOD设置")]
        [Tooltip("启用LOD系统")]
        public bool enableLOD = false;
        
        [Tooltip("LOD距离阈值")]
        public float[] lodDistances = { 20f, 50f, 100f };
        
        [Header("动画设置")]
        [Tooltip("启用淡入淡出动画")]
        public bool enableFadeAnimation = true;
        
        [Tooltip("淡入淡出持续时间")]
        [Range(0.1f, 2f)]
        public float fadeDuration = 0.3f;
        
        [Header("Canvas设置")]
        [Tooltip("启用CanvasScaler World模式")]
        public bool enableWorldSpaceScaler = true;
        
        [Tooltip("世界空间缩放因子")]
        [Range(0.001f, 1f)]
        public float worldSpaceScaleFactor = 0.01f;
        
        [Tooltip("动态像素密度")]
        [Range(1f, 10f)]
        public float dynamicPixelsPerUnit = 1f;
        
        [Header("正交相机设置")]
        [Tooltip("启用正交相机优化")]
        public bool enableOrthographicOptimization = true;
        
        [Tooltip("正交相机缩放因子")]
        [Range(0.001f, 1f)]
        public float orthographicScaleFactor = 0.005f;
        
        [Tooltip("正交相机距离计算模式")]
        public OrthographicDistanceMode orthographicDistanceMode = OrthographicDistanceMode.CameraSize;
        
        [Tooltip("正交相机固定距离值")]
        [Range(1f, 100f)]
        public float orthographicFixedDistance = 10f;
        
        [Header("调试")]
        [Tooltip("显示调试信息")]
        public bool showDebugInfo = false;
        
        [Tooltip("调试颜色")]
        public Color debugColor = Color.white;
        
        /// <summary>
        /// 创建默认配置
        /// </summary>
        /// <returns>默认配置实例</returns>
        public static UIGroupConfig CreateDefault()
        {
            return new UIGroupConfig
            {
                sortingOrder = 0,
                sortingLayerName = "Default",
                maxInstances = 100,
                enableAutoRemoveOldest = true,
                enableCulling = true,
                cullingDistance = 50f,
                updateInterval = 0.016f,
                enablePooling = true,
                poolSize = 20,
                enableLOD = false,
                lodDistances = new float[] { 20f, 50f, 100f },
                enableFadeAnimation = true,
                fadeDuration = 0.3f,
                enableWorldSpaceScaler = true,
                worldSpaceScaleFactor = 0.01f,
                dynamicPixelsPerUnit = 1f,
                enableOrthographicOptimization = true,
                orthographicScaleFactor = 0.005f,
                orthographicDistanceMode = OrthographicDistanceMode.ZDepth,
                orthographicFixedDistance = 10f,
                showDebugInfo = false,
                debugColor = Color.white
            };
        }
        
        /// <summary>
        /// 创建高性能配置（适用于大量UI元素）
        /// </summary>
        /// <returns>高性能配置实例</returns>
        public static UIGroupConfig CreateHighPerformance()
        {
            return new UIGroupConfig
            {
                sortingOrder = 0,
                sortingLayerName = "Default",
                maxInstances = 200,
                enableAutoRemoveOldest = true,
                enableCulling = true,
                cullingDistance = 30f,
                updateInterval = 0.033f, // 30fps
                enablePooling = true,
                poolSize = 50,
                enableLOD = true,
                lodDistances = new float[] { 15f, 30f, 60f },
                enableFadeAnimation = false,
                fadeDuration = 0.1f,
                enableWorldSpaceScaler = true,
                worldSpaceScaleFactor = 0.005f, // 更小的缩放因子提高性能
                dynamicPixelsPerUnit = 0.5f,
                enableOrthographicOptimization = true,
                orthographicScaleFactor = 0.003f,
                orthographicDistanceMode = OrthographicDistanceMode.FixedDistance,
                orthographicFixedDistance = 15f,
                showDebugInfo = false,
                debugColor = Color.green
            };
        }
        
        /// <summary>
        /// 创建高质量配置（适用于重要UI元素）
        /// </summary>
        /// <returns>高质量配置实例</returns>
        public static UIGroupConfig CreateHighQuality()
        {
            return new UIGroupConfig
            {
                sortingOrder = 0,
                sortingLayerName = "Default",
                maxInstances = 50,
                enableAutoRemoveOldest = false,
                enableCulling = true,
                cullingDistance = 100f,
                updateInterval = 0f, // 每帧更新
                enablePooling = true,
                poolSize = 10,
                enableLOD = false,
                lodDistances = new float[] { 50f, 100f, 200f },
                enableFadeAnimation = true,
                fadeDuration = 0.5f,
                enableWorldSpaceScaler = true,
                worldSpaceScaleFactor = 0.02f, // 更大的缩放因子提高质量
                dynamicPixelsPerUnit = 2f,
                enableOrthographicOptimization = true,
                orthographicScaleFactor = 0.01f,
                orthographicDistanceMode = OrthographicDistanceMode.CameraSize,
                orthographicFixedDistance = 20f,
                showDebugInfo = false,
                debugColor = Color.blue
            };
        }
        
        /// <summary>
        /// 验证配置有效性
        /// </summary>
        /// <returns>是否有效</returns>
        public bool Validate()
        {
            if (maxInstances < 0)
            {
                Debug.LogWarning("[UIGroupConfig] maxInstances 不能为负数");
                return false;
            }
            
            if (cullingDistance <= 0)
            {
                Debug.LogWarning("[UIGroupConfig] cullingDistance 必须大于0");
                return false;
            }
            
            if (updateInterval < 0)
            {
                Debug.LogWarning("[UIGroupConfig] updateInterval 不能为负数");
                return false;
            }
            
            if (poolSize <= 0)
            {
                Debug.LogWarning("[UIGroupConfig] poolSize 必须大于0");
                return false;
            }
            
            if (fadeDuration <= 0)
            {
                Debug.LogWarning("[UIGroupConfig] fadeDuration 必须大于0");
                return false;
            }
            
            if (enableLOD && (lodDistances == null || lodDistances.Length == 0))
            {
                Debug.LogWarning("[UIGroupConfig] 启用LOD时必须设置lodDistances");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 复制配置
        /// </summary>
        /// <returns>配置副本</returns>
        public UIGroupConfig Clone()
        {
            var clone = new UIGroupConfig
            {
                sortingOrder = this.sortingOrder,
                sortingLayerName = this.sortingLayerName,
                maxInstances = this.maxInstances,
                enableAutoRemoveOldest = this.enableAutoRemoveOldest,
                enableCulling = this.enableCulling,
                cullingDistance = this.cullingDistance,
                updateInterval = this.updateInterval,
                enablePooling = this.enablePooling,
                poolSize = this.poolSize,
                enableLOD = this.enableLOD,
                enableFadeAnimation = this.enableFadeAnimation,
                fadeDuration = this.fadeDuration,
                enableWorldSpaceScaler = this.enableWorldSpaceScaler,
                worldSpaceScaleFactor = this.worldSpaceScaleFactor,
                dynamicPixelsPerUnit = this.dynamicPixelsPerUnit,
                enableOrthographicOptimization = this.enableOrthographicOptimization,
                orthographicScaleFactor = this.orthographicScaleFactor,
                orthographicDistanceMode = this.orthographicDistanceMode,
                orthographicFixedDistance = this.orthographicFixedDistance,
                showDebugInfo = this.showDebugInfo,
                debugColor = this.debugColor
            };
            
            if (lodDistances != null)
            {
                clone.lodDistances = new float[lodDistances.Length];
                Array.Copy(lodDistances, clone.lodDistances, lodDistances.Length);
            }
            
            return clone;
        }
    }
}