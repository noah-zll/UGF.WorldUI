using System;
using UnityEngine;

namespace UGF.WorldUI
{
    /// <summary>
    /// 聚合触发模式
    /// </summary>
    public enum AggregationTriggerMode
    {
        /// <summary>基于世界空间距离（固定距离，不受镜头缩放影响）</summary>
        WorldDistance,

        /// <summary>基于屏幕空间距离（投影后距离，受镜头缩放影响）</summary>
        ScreenDistance,

        /// <summary>基于屏幕空间UI矩形重叠（受镜头缩放影响）</summary>
        ScreenOverlap,

        /// <summary>WorldDistance + ScreenDistance 同时满足</summary>
        Both,

        /// <summary>WorldDistance 或 ScreenDistance 任一满足</summary>
        Either
    }

    /// <summary>
    /// 聚合展示模式
    /// </summary>
    public enum AggregationDisplayMode
    {
        /// <summary>显示数量（x5）</summary>
        Count,

        /// <summary>求和（伤害总计）</summary>
        Sum,

        /// <summary>显示最大/最重要的一个，其余隐藏</summary>
        Max,

        /// <summary>显示首个，附带"+N"标记</summary>
        FirstWithCount,

        /// <summary>自定义（由子类实现）</summary>
        Custom
    }

    /// <summary>
    /// 聚合锚点模式
    /// </summary>
    public enum AggregationAnchorMode
    {
        /// <summary>聚合组成员的世界空间中心</summary>
        CenterOfGroup,

        /// <summary>第一个成员的位置</summary>
        FirstMember,

        /// <summary>最重要的成员的位置</summary>
        MostImportantMember,

        /// <summary>聚合UI的屏幕空间中心</summary>
        ScreenCenter
    }

    /// <summary>
    /// UI聚合配置（分组级，每个 UIGroup 可独立设置）
    /// </summary>
    [Serializable]
    public class UIAggregationConfig
    {
        [Header("检测设置")]
        [Tooltip("聚合触发模式")]
        public AggregationTriggerMode triggerMode = AggregationTriggerMode.ScreenDistance;

        [Tooltip("世界空间聚合距离（单位：米，不受镜头缩放影响）")]
        [Range(0.1f, 50f)]
        public float worldDistance = 5f;

        [Tooltip("屏幕空间聚合距离阈值（0~1，占屏幕高度的比例，受镜头缩放影响）")]
        [Range(0f, 1f)]
        public float screenDistanceThreshold = 0.08f;

        [Tooltip("屏幕空间重叠阈值（0~1，屏幕高度占比）")]
        [Range(0f, 1f)]
        public float screenOverlapThreshold = 0.05f;

        [Header("聚合组设置")]
        [Tooltip("最小聚合成员数（小于此值不解散已有聚合组）")]
        [Range(2, 100)]
        public int minGroupSize = 2;

        [Tooltip("最大聚合成员数")]
        [Range(2, 500)]
        public int maxGroupSize = 50;

        [Tooltip("聚合组解散距离（超过此距离的成员会离开聚合组）")]
        [Range(0.1f, 100f)]
        public float disbandDistance = 8f;

        [Tooltip("聚合组生命周期（秒，0=永久）")]
        [Min(0f)]
        public float groupLifetime = 0f;

        [Header("显示设置")]
        [Tooltip("聚合展示模式")]
        public AggregationDisplayMode displayMode = AggregationDisplayMode.FirstWithCount;

        [Tooltip("聚合锚点模式")]
        public AggregationAnchorMode anchorMode = AggregationAnchorMode.CenterOfGroup;

        [Tooltip("聚合展示UI预制体（为null时使用系统级 RegisterAggregationPrefab 注册的预制体）")]
        public GameObject aggregationPrefab;

        /// <summary>
        /// 创建默认配置
        /// </summary>
        public static UIAggregationConfig CreateDefault()
        {
            return new UIAggregationConfig();
        }

        /// <summary>
        /// 验证配置有效性
        /// </summary>
        public bool Validate()
        {
            if (worldDistance <= 0)
            {
                Debug.LogWarning("[UIAggregationConfig] worldDistance 必须大于0");
                return false;
            }

            if (screenDistanceThreshold < 0)
            {
                Debug.LogWarning("[UIAggregationConfig] screenDistanceThreshold 不能为负数");
                return false;
            }

            if (minGroupSize < 2)
            {
                Debug.LogWarning("[UIAggregationConfig] minGroupSize 至少为2");
                return false;
            }

            return true;
        }
    }
}
