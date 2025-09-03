using UnityEngine;
using UnityEngine.UI;
using UGF.WorldUI;
using System.Collections;

namespace WorldUISample.Components
{
    /// <summary>
    /// 交互提示UI组件 - 显示可交互物体的提示信息
    /// </summary>
    public class InteractionUI : WorldSpaceUIComponent
    {
        [Header("交互设置")]
        [SerializeField] private Text interactionText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Button interactionButton;
        
        [Header("显示设置")]
        [SerializeField] private string defaultText = "按 E 交互";
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = Color.yellow;
        [SerializeField] private float highlightDistance = 3f;
        
        [Header("动画设置")]
        [SerializeField] private bool enablePulseAnimation = true;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseIntensity = 0.2f;
        [SerializeField] private bool enableFloatAnimation = true;
        [SerializeField] private float floatSpeed = 1f;
        [SerializeField] private float floatHeight = 0.3f;
        
        // 私有字段
        private string _interactionText;
        private Sprite _interactionIcon;
        private System.Action _onInteract;
        private Transform _playerTransform;
        private Vector3 _originalPosition;
        private Vector3 _originalScale;
        private bool _isHighlighted;
        private Coroutine _animationCoroutine;
        
        #region Properties
        
        /// <summary>
        /// 交互文本
        /// </summary>
        public string InteractionText
        {
            get => _interactionText;
            set
            {
                _interactionText = value;
                UpdateDisplay();
            }
        }
        
        /// <summary>
        /// 交互图标
        /// </summary>
        public Sprite InteractionIcon
        {
            get => _interactionIcon;
            set
            {
                _interactionIcon = value;
                UpdateDisplay();
            }
        }
        
        /// <summary>
        /// 交互回调
        /// </summary>
        public System.Action OnInteract
        {
            get => _onInteract;
            set => _onInteract = value;
        }
        
        /// <summary>
        /// 是否高亮显示
        /// </summary>
        public bool IsHighlighted
        {
            get => _isHighlighted;
            set
            {
                if (_isHighlighted != value)
                {
                    _isHighlighted = value;
                    UpdateHighlight();
                }
            }
        }
        
        #endregion
        
        #region Unity Callbacks
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            // 缓存初始值
            _originalPosition = transform.localPosition;
            _originalScale = transform.localScale;
            
            // 查找玩家
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
            }
            
            // 设置默认文本
            if (string.IsNullOrEmpty(_interactionText))
            {
                _interactionText = defaultText;
            }
            
            // 初始化显示
            UpdateDisplay();
            
            // 设置按钮事件
            if (interactionButton != null)
            {
                interactionButton.onClick.AddListener(OnInteractionButtonClicked);
            }
            
            // 开始动画
            if (enablePulseAnimation || enableFloatAnimation)
            {
                StartAnimations();
            }
            
            Debug.Log($"[InteractionUI] 交互提示UI初始化完成 - Text: {_interactionText}");
        }
        
        protected override void OnUpdate()
        {
            base.OnUpdate();
            
            // 检查与玩家的距离
            if (_playerTransform != null)
            {
                float distance = Vector3.Distance(transform.position, _playerTransform.position);
                IsHighlighted = distance <= highlightDistance;
            }
        }
        
        protected override void OnDestroyed()
        {
            // 清理按钮事件
            if (interactionButton != null)
            {
                interactionButton.onClick.RemoveListener(OnInteractionButtonClicked);
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 设置交互信息
        /// </summary>
        /// <param name="text">交互文本</param>
        /// <param name="icon">交互图标</param>
        /// <param name="onInteract">交互回调</param>
        public void SetInteraction(string text, Sprite icon = null, System.Action onInteract = null)
        {
            _interactionText = text;
            _interactionIcon = icon;
            _onInteract = onInteract;
            UpdateDisplay();
        }
        
        /// <summary>
        /// 触发交互
        /// </summary>
        public void TriggerInteraction()
        {
            _onInteract?.Invoke();
            
            // 播放交互效果
            PlayInteractionEffect();
        }
        
        /// <summary>
        /// 设置玩家引用
        /// </summary>
        /// <param name="playerTransform">玩家Transform</param>
        public void SetPlayer(Transform playerTransform)
        {
            _playerTransform = playerTransform;
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// 更新显示
        /// </summary>
        private void UpdateDisplay()
        {
            if (interactionText != null)
            {
                interactionText.text = _interactionText ?? defaultText;
            }
            
            if (iconImage != null)
            {
                iconImage.sprite = _interactionIcon;
                iconImage.gameObject.SetActive(_interactionIcon != null);
            }
        }
        
        /// <summary>
        /// 更新高亮状态
        /// </summary>
        private void UpdateHighlight()
        {
            Color targetColor = _isHighlighted ? highlightColor : normalColor;
            
            if (interactionText != null)
            {
                interactionText.color = targetColor;
            }
            
            if (backgroundImage != null)
            {
                var bgColor = backgroundImage.color;
                bgColor.a = _isHighlighted ? 0.8f : 0.5f;
                backgroundImage.color = bgColor;
            }
        }
        
        /// <summary>
        /// 开始动画
        /// </summary>
        private void StartAnimations()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }
            _animationCoroutine = StartCoroutine(AnimationCoroutine());
        }
        
        /// <summary>
        /// 动画协程
        /// </summary>
        private IEnumerator AnimationCoroutine()
        {
            float time = 0f;
            
            while (true)
            {
                // 脉冲动画
                if (enablePulseAnimation)
                {
                    float pulse = 1f + Mathf.Sin(time * pulseSpeed) * pulseIntensity;
                    transform.localScale = _originalScale * pulse;
                }
                
                // 浮动动画
                if (enableFloatAnimation)
                {
                    float floatOffset = Mathf.Sin(time * floatSpeed) * floatHeight;
                    Vector3 newPosition = _originalPosition;
                    newPosition.y += floatOffset;
                    transform.localPosition = newPosition;
                }
                
                time += Time.deltaTime;
                yield return null;
            }
        }
        
        /// <summary>
        /// 播放交互效果
        /// </summary>
        private void PlayInteractionEffect()
        {
            // 简单的缩放效果
            StartCoroutine(InteractionEffectCoroutine());
        }
        
        /// <summary>
        /// 交互效果协程
        /// </summary>
        private IEnumerator InteractionEffectCoroutine()
        {
            Vector3 targetScale = _originalScale * 1.2f;
            float duration = 0.2f;
            float elapsed = 0f;
            
            // 放大
            while (elapsed < duration)
            {
                float progress = elapsed / duration;
                transform.localScale = Vector3.Lerp(_originalScale, targetScale, progress);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 缩小
            elapsed = 0f;
            while (elapsed < duration)
            {
                float progress = elapsed / duration;
                transform.localScale = Vector3.Lerp(targetScale, _originalScale, progress);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            transform.localScale = _originalScale;
        }
        
        /// <summary>
        /// 按钮点击事件
        /// </summary>
        private void OnInteractionButtonClicked()
        {
            TriggerInteraction();
        }
        
        #endregion
        
        #region Validation
        
        private void OnValidate()
        {
            if (highlightDistance <= 0f)
                highlightDistance = 3f;
                
            if (pulseSpeed <= 0f)
                pulseSpeed = 2f;
                
            if (floatSpeed <= 0f)
                floatSpeed = 1f;
        }
        
        #endregion
    }
}