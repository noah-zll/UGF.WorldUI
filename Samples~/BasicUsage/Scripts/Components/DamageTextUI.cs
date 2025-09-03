using UnityEngine;
using UnityEngine.UI;
using UGF.WorldUI;
using System.Collections;

namespace WorldUISample.Components
{
    /// <summary>
    /// 伤害数字UI组件 - 显示伤害数值的飘字效果
    /// </summary>
    public class DamageTextUI : WorldSpaceUIComponent
    {
        [Header("文本设置")]
        [SerializeField] private Text damageText;
        [SerializeField] private Color normalDamageColor = Color.white;
        [SerializeField] private Color criticalDamageColor = Color.red;
        [SerializeField] private Color healColor = Color.green;
        
        [Header("动画设置")]
        [SerializeField] private float floatHeight = 2f;
        [SerializeField] private float floatDuration = 1.5f;
        [SerializeField] private AnimationCurve floatCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.Linear(0f, 1.2f, 1f, 0.8f);
        [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        
        [Header("暴击效果")]
        [SerializeField] private float criticalScaleMultiplier = 1.5f;
        [SerializeField] private bool enableCriticalShake = true;
        [SerializeField] private float shakeIntensity = 0.2f;
        
        // 私有字段
        private float _damageValue;
        private bool _isCritical;
        private bool _isHeal;
        private Vector3 _startPosition;
        private Vector3 _originalScale;
        private Coroutine _animationCoroutine;
        
        #region Properties
        
        /// <summary>
        /// 伤害值
        /// </summary>
        public float DamageValue
        {
            get => _damageValue;
            set
            {
                _damageValue = value;
                _isHeal = value < 0;
                UpdateDisplay();
            }
        }
        
        /// <summary>
        /// 是否暴击
        /// </summary>
        public bool IsCritical
        {
            get => _isCritical;
            set
            {
                _isCritical = value;
                UpdateDisplay();
            }
        }
        
        #endregion
        
        #region Unity Callbacks
        
        protected override void OnInitialize()
        {
            base.OnInitialize();

            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            _startPosition = transform.position;
            _originalScale = transform.localScale;
            
            // 开始动画
            PlayFloatAnimation();
            
            Debug.Log($"[DamageTextUI] 伤害文字UI初始化完成 - Damage: {_damageValue}, Critical: {_isCritical}");
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 设置伤害信息
        /// </summary>
        /// <param name="damage">伤害值（负数表示治疗）</param>
        /// <param name="isCritical">是否暴击</param>
        public void SetDamage(float damage, bool isCritical = false)
        {
            _damageValue = damage;
            _isCritical = isCritical;
            _isHeal = damage < 0;
            UpdateDisplay();
        }
        
        /// <summary>
        /// 设置治疗信息
        /// </summary>
        /// <param name="healAmount">治疗量</param>
        public void SetHeal(float healAmount)
        {
            SetDamage(-Mathf.Abs(healAmount), false);
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// 更新显示
        /// </summary>
        private void UpdateDisplay()
        {
            if (damageText == null) return;
            
            // 设置文本内容
            string prefix = _isHeal ? "+" : "-";
            string text = $"{prefix}{Mathf.Abs(_damageValue):F0}";
            
            if (_isCritical && !_isHeal)
            {
                text = $"<size=120%><b>{text}</b></size>";
            }
            
            damageText.text = text;
            
            // 设置颜色
            Color targetColor = _isHeal ? healColor : (_isCritical ? criticalDamageColor : normalDamageColor);
            damageText.color = targetColor;
        }
        
        /// <summary>
        /// 播放飘字动画
        /// </summary>
        private void PlayFloatAnimation()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }
            _animationCoroutine = StartCoroutine(FloatAnimationCoroutine());
        }
        
        /// <summary>
        /// 飘字动画协程
        /// </summary>
        private IEnumerator FloatAnimationCoroutine()
        {
            float elapsed = 0f;
            Vector3 targetPosition = _startPosition + Vector3.up * floatHeight;
            
            // 添加随机偏移
            Vector3 randomOffset = new Vector3(
                Random.Range(-0.5f, 0.5f),
                0f,
                Random.Range(-0.5f, 0.5f)
            );
            targetPosition += randomOffset;
            
            while (elapsed < floatDuration)
            {
                float progress = elapsed / floatDuration;
                
                // 位置动画
                float heightProgress = floatCurve.Evaluate(progress);
                Vector3 currentPosition = Vector3.Lerp(_startPosition, targetPosition, heightProgress);
                
                // 暴击震动效果
                if (_isCritical && enableCriticalShake && progress < 0.3f)
                {
                    Vector3 shake = new Vector3(
                        Random.Range(-shakeIntensity, shakeIntensity),
                        Random.Range(-shakeIntensity, shakeIntensity),
                        0f
                    ) * (1f - progress / 0.3f);
                    currentPosition += shake;
                }
                
                transform.position = currentPosition;
                
                // 缩放动画
                float scaleMultiplier = _isCritical ? criticalScaleMultiplier : 1f;
                float scaleProgress = scaleCurve.Evaluate(progress);
                transform.localScale = _originalScale * scaleProgress * scaleMultiplier;
                
                // 透明度动画
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = alphaCurve.Evaluate(progress);
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 动画结束，销毁UI
            if (_manager != null)
            {
                _manager.DestroyUI(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        #endregion
        
        #region Validation
        
        private void OnValidate()
        {
            if (floatHeight <= 0f)
                floatHeight = 2f;
                
            if (floatDuration <= 0f)
                floatDuration = 1.5f;
                
            if (criticalScaleMultiplier <= 0f)
                criticalScaleMultiplier = 1.5f;
        }
        
        #endregion
    }
}