using UnityEngine;

namespace UGF.WorldUI
{
    /// <summary>
    /// 聚合状态
    /// </summary>
    public enum AggregationStateType
    {
        /// <summary>正常显示</summary>
        Normal,

        /// <summary>已被聚合（自身隐藏）</summary>
        Aggregated,

        /// <summary>作为聚合展示UI（代表一个聚合组）</summary>
        Aggregator
    }


    /// <summary>
    /// 可聚合UI组件接口 - 实现此接口的WorldSpaceUIComponent可参与聚合
    /// </summary>
    public interface IAggregatable
    {
        /// <summary>聚合类型标识（同类型才能聚合，如 "HealthBar"、"DamageText"）</summary>
        string AggregationType { get; }

        /// <summary>
        /// 判断是否可以与另一个UI聚合。
        /// 由各UI自行实现判断逻辑，如：Boss血条返回false拒绝与小怪血条聚合。
        /// 默认应返回 AggregationType 相同即可。
        /// </summary>
        bool CanAggregateWith(IAggregatable other);

        /// <summary>在聚合中显示的优先级（越高越优先被选为代表）</summary>
        int AggregationPriority { get; }

        /// <summary>当前聚合状态</summary>
        AggregationStateType AggregationState { get; set; }

        /// <summary>世界位置（用于距离计算）</summary>
        Vector3 WorldPosition { get; }

        /// <summary>被聚合时调用（原始UI进入聚合状态）</summary>
        void OnAggregated(AggregationGroup group);

        /// <summary>从聚合中释放时调用（原始UI退出聚合状态）</summary>
        void OnDeaggregated(AggregationGroup group);

        /// <summary>设置聚合展示内容（传入聚合组，UI自行读取成员和配置）</summary>
        void SetAggregationDisplay(AggregationGroup group);
    }

    /// <summary>
    /// 可聚合UI基类（便捷基类，替代直接实现IAggregatable）
    /// </summary>
    public abstract class AggregatableUIComponent : WorldSpaceUIComponent, IAggregatable
    {
        [Header("聚合设置")]
        [SerializeField] protected string _aggregationType;
        [SerializeField] protected int _aggregationPriority = 0;

        private AggregationStateType _aggregationState = AggregationStateType.Normal;

        public string AggregationType => _aggregationType;
        public int AggregationPriority => _aggregationPriority;

        /// <summary>
        /// 设置聚合类型（内部使用，聚合展示UI创建时从组继承类型）
        /// </summary>
        internal void SetAggregationType(string type)
        {
            _aggregationType = type;
        }

        /// <summary>
        /// 默认实现：同 AggregationType 即可聚合。子类可重写以添加自定义判断。
        /// </summary>
        public virtual bool CanAggregateWith(IAggregatable other)
        {
            return other != null && other.AggregationType == AggregationType;
        }

        public AggregationStateType AggregationState
        {
            get => _aggregationState;
            set => _aggregationState = value;
        }

        public virtual void OnAggregated(AggregationGroup group)
        {
            _aggregationState = AggregationStateType.Aggregated;
            // 用 CanvasGroup 隐藏而非 SetActive(false)，保持 Update() 运行以跟踪位置
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }
        }

        public virtual void OnDeaggregated(AggregationGroup group)
        {
            _aggregationState = AggregationStateType.Normal;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = true;
            }
        }

        private AggregationGroup _currentGroup;

        public virtual void SetAggregationDisplay(AggregationGroup group)
        {
            _aggregationState = AggregationStateType.Aggregator;
            _currentGroup = group;
        }

        /// <summary>
        /// 聚合展示UI每帧跟随组中心，不依赖检测周期
        /// </summary>
        protected override void UpdatePosition()
        {
            if (_aggregationState == AggregationStateType.Aggregator && _currentGroup != null)
            {
                _worldPosition = _currentGroup.CenterPosition;
                transform.position = _worldPosition + _offset;
                return;
            }
            base.UpdatePosition();
        }

        /// <summary>
        /// 聚合状态下跳过 alpha 曲线更新，防止覆盖 OnAggregated 设置的 alpha=0
        /// </summary>
        protected override void UpdateAlpha()
        {
            if (_aggregationState == AggregationStateType.Aggregated) return;
            base.UpdateAlpha();
        }
    }
}
