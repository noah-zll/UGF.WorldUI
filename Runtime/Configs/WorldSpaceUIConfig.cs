using System;
using UnityEngine;

namespace UGF.WorldUI
{
    /// <summary>
    /// 世界空间UI组件配置
    /// </summary>
    [CreateAssetMenu(fileName = "WorldSpaceUIConfig", menuName = "UGF/WorldUI/WorldSpaceUIConfig")]
    public class WorldSpaceUIConfig : ScriptableObject
    {
        #region Basic Settings
        
        [Header("基础设置")]
        [Tooltip("位置偏移")]
        public Vector3 offset = Vector3.zero;
        
        [Tooltip("是否跟随目标")]
        public bool followTarget = false;
        
        [Tooltip("是否面向摄像机")]
        public bool faceCamera = true;
        
        [Tooltip("生命周期时间（秒，0表示永久）")]
        [Min(0f)]
        public float lifeTime = 0f;
        
        #endregion
        
        #region Animation Settings
        
        [Header("动画设置")]
        [Tooltip("启用淡入淡出动画")]
        public bool enableFadeAnimation = true;
        
        [Tooltip("淡入淡出持续时间")]
        [Range(0.1f, 5f)]
        public float fadeDuration = 0.5f;
        
        [Tooltip("缩放动画曲线（基于生命周期）")]
        public AnimationCurve scaleCurve = AnimationCurve.Constant(0f, 1f, 1f);
        
        [Tooltip("最小缩放值")]
        [Range(0.01f, 1f)]
        public float minScale = 0.1f;
        
        [Tooltip("最大缩放值")]
        [Range(1f, 10f)]
        public float maxScale = 3f;
        
        [Tooltip("透明度动画曲线（基于生命周期）")]
        public AnimationCurve alphaCurve = AnimationCurve.Constant(0f, 1f, 1f);
        
        #endregion
        
        #region Performance Settings
        
        [Header("性能设置")]
        [Tooltip("启用距离剔除")]
        public bool enableDistanceCulling = true;
        
        [Tooltip("剔除距离")]
        [Min(1f)]
        public float cullingDistance = 100f;
        
        [Tooltip("更新频率（每秒更新次数，0表示每帧更新）")]
        [Min(0f)]
        public float updateRate = 0f;
        
        [Tooltip("LOD级别（0=最高质量，1=中等质量，2=最低质量）")]
        [Range(0, 2)]
        public int lodLevel = 0;
        
        #endregion
        
        #region Interaction Settings
        
        [Header("交互设置")]
        [Tooltip("启用交互")]
        public bool enableInteraction = true;
        
        [Tooltip("交互距离")]
        [Min(0.1f)]
        public float interactionDistance = 10f;
        
        [Tooltip("鼠标悬停时的缩放倍数")]
        [Range(0.5f, 2f)]
        public float hoverScale = 1.1f;
        
        #endregion
        

        
        #region Debug Settings
        
        [Header("调试设置")]
        [Tooltip("显示调试信息")]
        public bool showDebugInfo = false;
        
        [Tooltip("显示边界框")]
        public bool showBounds = false;
        
        [Tooltip("调试颜色")]
        public Color debugColor = Color.green;
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public WorldSpaceUIConfig()
        {
            // 使用默认值
        }
        
        /// <summary>
        /// 复制构造函数
        /// </summary>
        /// <param name="other">要复制的配置</param>
        public WorldSpaceUIConfig(WorldSpaceUIConfig other)
        {
            if (other == null) return;
            
            // 基础设置
            offset = other.offset;
            followTarget = other.followTarget;
            faceCamera = other.faceCamera;
            lifeTime = other.lifeTime;
            
            // 动画设置
            enableFadeAnimation = other.enableFadeAnimation;
            fadeDuration = other.fadeDuration;
            scaleCurve = new AnimationCurve(other.scaleCurve.keys);
            minScale = other.minScale;
            maxScale = other.maxScale;
            alphaCurve = new AnimationCurve(other.alphaCurve.keys);
            
            // 性能设置
            enableDistanceCulling = other.enableDistanceCulling;
            cullingDistance = other.cullingDistance;
            updateRate = other.updateRate;
            lodLevel = other.lodLevel;
            
            // 交互设置
            enableInteraction = other.enableInteraction;
            interactionDistance = other.interactionDistance;
            hoverScale = other.hoverScale;
            

            
            // 调试设置
            showDebugInfo = other.showDebugInfo;
            showBounds = other.showBounds;
            debugColor = other.debugColor;
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// 创建默认配置
        /// </summary>
        /// <returns>默认配置</returns>
        public static WorldSpaceUIConfig CreateDefault()
        {
            return new WorldSpaceUIConfig();
        }
        
        /// <summary>
        /// 创建高性能配置（适用于大量UI）
        /// </summary>
        /// <returns>高性能配置</returns>
        public static WorldSpaceUIConfig CreateHighPerformance()
        {
            var config = new WorldSpaceUIConfig
            {
                enableFadeAnimation = false,
                enableDistanceCulling = true,
                cullingDistance = 50f,
                updateRate = 10f, // 每秒10次更新
                lodLevel = 2, // 最低质量
                enableInteraction = false,
                scaleCurve = AnimationCurve.Constant(0f, 1f, 1f),
                minScale = 0.5f, // 高性能模式下限制最小缩放
                maxScale = 2f,   // 高性能模式下限制最大缩放
                alphaCurve = AnimationCurve.Constant(0f, 1f, 1f)
            };
            
            return config;
        }
        
        /// <summary>
        /// 创建高质量配置（适用于重要UI）
        /// </summary>
        /// <returns>高质量配置</returns>
        public static WorldSpaceUIConfig CreateHighQuality()
        {
            var config = new WorldSpaceUIConfig
            {
                enableFadeAnimation = true,
                fadeDuration = 0.8f,
                enableDistanceCulling = true,
                cullingDistance = 200f,
                updateRate = 0f, // 每帧更新
                lodLevel = 0, // 最高质量
                enableInteraction = true,
                interactionDistance = 20f,
                hoverScale = 1.2f,
                minScale = 0.1f, // 高质量模式下允许更小的缩放
                maxScale = 5f    // 高质量模式下允许更大的缩放
            };
            
            // 设置更丰富的动画曲线
            config.scaleCurve = new AnimationCurve(
                new Keyframe(0f, 0.8f),
                new Keyframe(0.2f, 1.1f),
                new Keyframe(1f, 1f)
            );
            
            config.alphaCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.3f, 1f),
                new Keyframe(0.9f, 1f),
                new Keyframe(1f, 0f)
            );
            
            return config;
        }
        
        /// <summary>
        /// 创建临时UI配置（短生命周期）
        /// </summary>
        /// <param name="lifeTime">生命周期时间</param>
        /// <returns>临时UI配置</returns>
        public static WorldSpaceUIConfig CreateTemporary(float lifeTime = 3f)
        {
            var config = new WorldSpaceUIConfig
            {
                lifeTime = lifeTime,
                enableFadeAnimation = true,
                fadeDuration = 0.3f,
                enableDistanceCulling = true,
                cullingDistance = 100f,
                updateRate = 30f, // 每秒30次更新
                lodLevel = 1 // 中等质量
            };
            
            // 设置淡出动画
            config.alphaCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.1f, 1f),
                new Keyframe(0.8f, 1f),
                new Keyframe(1f, 0f)
            );
            
            return config;
        }
        
        /// <summary>
        /// 创建跟随目标配置
        /// </summary>
        /// <param name="offset">偏移量</param>
        /// <returns>跟随目标配置</returns>
        public static WorldSpaceUIConfig CreateFollowTarget(Vector3 offset)
        {
            var config = new WorldSpaceUIConfig
            {
                followTarget = true,
                offset = offset,
                faceCamera = true,
                enableFadeAnimation = true,
                fadeDuration = 0.5f,
                enableDistanceCulling = true,
                cullingDistance = 150f,
                updateRate = 60f, // 每秒60次更新，保证跟随流畅
                lodLevel = 0, // 最高质量
                minScale = 0.2f, // 跟随目标模式下的最小缩放
                maxScale = 2.5f  // 跟随目标模式下的最大缩放
            };
            
            return config;
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// 验证配置有效性
        /// </summary>
        /// <returns>是否有效</returns>
        public bool Validate()
        {
            // 检查基本参数
            if (lifeTime < 0f)
            {
                Debug.LogWarning("[WorldSpaceUIConfig] 生命周期时间不能为负数");
                return false;
            }
            
            if (fadeDuration <= 0f)
            {
                Debug.LogWarning("[WorldSpaceUIConfig] 淡入淡出持续时间必须大于0");
                return false;
            }
            
            if (cullingDistance <= 0f)
            {
                Debug.LogWarning("[WorldSpaceUIConfig] 剔除距离必须大于0");
                return false;
            }
            
            if (updateRate < 0f)
            {
                Debug.LogWarning("[WorldSpaceUIConfig] 更新频率不能为负数");
                return false;
            }
            
            if (lodLevel < 0 || lodLevel > 2)
            {
                Debug.LogWarning("[WorldSpaceUIConfig] LOD级别必须在0-2之间");
                return false;
            }
            
            if (interactionDistance <= 0f)
            {
                Debug.LogWarning("[WorldSpaceUIConfig] 交互距离必须大于0");
                return false;
            }
            
            if (hoverScale <= 0f)
            {
                Debug.LogWarning("[WorldSpaceUIConfig] 悬停缩放倍数必须大于0");
                return false;
            }
            
            if (minScale <= 0f)
            {
                Debug.LogWarning("[WorldSpaceUIConfig] 最小缩放值必须大于0");
                return false;
            }
            
            if (maxScale <= 0f)
            {
                Debug.LogWarning("[WorldSpaceUIConfig] 最大缩放值必须大于0");
                return false;
            }
            
            if (minScale >= maxScale)
            {
                Debug.LogWarning("[WorldSpaceUIConfig] 最小缩放值必须小于最大缩放值");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 修复无效配置
        /// </summary>
        public void FixInvalidValues()
        {
            lifeTime = Mathf.Max(0f, lifeTime);
            fadeDuration = Mathf.Max(0.1f, fadeDuration);
            cullingDistance = Mathf.Max(1f, cullingDistance);
            updateRate = Mathf.Max(0f, updateRate);
            lodLevel = Mathf.Clamp(lodLevel, 0, 2);
            interactionDistance = Mathf.Max(0.1f, interactionDistance);
            hoverScale = Mathf.Max(0.1f, hoverScale);
            
            // 修复缩放值
            minScale = Mathf.Max(0.01f, minScale);
            maxScale = Mathf.Max(0.01f, maxScale);
            
            // 确保最小缩放值小于最大缩放值
            if (minScale >= maxScale)
            {
                maxScale = minScale + 0.1f;
            }
            
            // 确保动画曲线不为空
            if (scaleCurve == null || scaleCurve.length == 0)
            {
                scaleCurve = AnimationCurve.Constant(0f, 1f, 1f);
            }
            
            if (alphaCurve == null || alphaCurve.length == 0)
            {
                alphaCurve = AnimationCurve.Constant(0f, 1f, 1f);
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// 获取当前更新间隔
        /// </summary>
        /// <returns>更新间隔（秒）</returns>
        public float GetUpdateInterval()
        {
            return updateRate > 0f ? 1f / updateRate : 0f;
        }
        
        /// <summary>
        /// 是否需要每帧更新
        /// </summary>
        /// <returns>是否每帧更新</returns>
        public bool ShouldUpdateEveryFrame()
        {
            return updateRate <= 0f;
        }
        
        /// <summary>
        /// 获取LOD质量描述
        /// </summary>
        /// <returns>质量描述</returns>
        public string GetLODQualityDescription()
        {
            return lodLevel switch
            {
                0 => "高质量",
                1 => "中等质量",
                2 => "低质量",
                _ => "未知"
            };
        }
        
        /// <summary>
        /// 转换为字符串
        /// </summary>
        /// <returns>配置描述</returns>
        public override string ToString()
        {
            return $"WorldSpaceUIConfig [生命周期: {(lifeTime > 0 ? lifeTime + "s" : "永久")}, " +
                   $"LOD: {GetLODQualityDescription()}, " +
                   $"更新频率: {(updateRate > 0 ? updateRate + "Hz" : "每帧")}, " +
                   $"剔除距离: {cullingDistance}m]";
        }
        
        #endregion
    }
}