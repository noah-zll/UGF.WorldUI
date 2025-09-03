using UnityEngine;
using UnityEngine.UI;
using UGF.WorldUI;

namespace WorldUISample.Components
{
    /// <summary>
    /// 血条UI组件 - 显示角色血量的世界空间血条
    /// </summary>
    public class HealthBarUI : WorldSpaceUIComponent
    {
        [Header("血条设置")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image fillImage;
        [SerializeField] private Text healthText;
        [SerializeField] private Gradient healthColorGradient;
        
        [Header("动画设置")]
        [SerializeField] private float smoothTime = 0.2f;
        [SerializeField] private bool enableDamageShake = true;
        [SerializeField] private float shakeIntensity = 0.1f;
        [SerializeField] private float shakeDuration = 0.3f;
        
        // 私有字段
        private float _currentHealth;
        private float _maxHealth;
        private float _targetHealth;
        private float _healthVelocity;
        private Vector3 _originalPosition;
        private Coroutine _shakeCoroutine;
        
        #region Properties
        
        /// <summary>
        /// 当前血量
        /// </summary>
        public float CurrentHealth
        {
            get => _currentHealth;
            set
            {
                var oldHealth = _currentHealth;
                _targetHealth = Mathf.Clamp(value, 0f, _maxHealth);
                
                // 如果血量减少且启用震动效果
                if (enableDamageShake && value < oldHealth)
                {
                    PlayDamageShake();
                }
            }
        }
        
        /// <summary>
        /// 最大血量
        /// </summary>
        public float MaxHealth
        {
            get => _maxHealth;
            set
            {
                _maxHealth = Mathf.Max(0f, value);
                UpdateHealthDisplay();
            }
        }
        
        /// <summary>
        /// 血量百分比
        /// </summary>
        public float HealthPercentage => _maxHealth > 0 ? _currentHealth / _maxHealth : 0f;
        
        #endregion
        
        #region Unity Callbacks
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            // 缓存原始位置
            _originalPosition = transform.localPosition;
            
            // 初始化血量
            _currentHealth = _maxHealth;
            _targetHealth = _maxHealth;
            
            // 初始化UI
            UpdateHealthDisplay();
            
            Debug.Log($"[HealthBarUI] 血条UI初始化完成 - MaxHealth: {_maxHealth}");
        }
        
        protected override void OnUpdate()
        {
            base.OnUpdate();
            
            // 平滑更新血量显示
            if (Mathf.Abs(_currentHealth - _targetHealth) > 0.01f)
            {
                _currentHealth = Mathf.SmoothDamp(_currentHealth, _targetHealth, ref _healthVelocity, smoothTime);
                UpdateHealthDisplay();
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 设置血量
        /// </summary>
        /// <param name="current">当前血量</param>
        /// <param name="max">最大血量</param>
        public void SetHealth(float current, float max)
        {
            _maxHealth = Mathf.Max(0f, max);
            CurrentHealth = current;
        }
        
        /// <summary>
        /// 造成伤害
        /// </summary>
        /// <param name="damage">伤害值</param>
        public void TakeDamage(float damage)
        {
            CurrentHealth -= damage;
        }
        
        /// <summary>
        /// 恢复血量
        /// </summary>
        /// <param name="heal">恢复值</param>
        public void Heal(float heal)
        {
            CurrentHealth += heal;
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// 更新血量显示
        /// </summary>
        private void UpdateHealthDisplay()
        {
            if (healthSlider != null)
            {
                healthSlider.value = HealthPercentage;
            }
            
            if (fillImage != null && healthColorGradient != null)
            {
                fillImage.color = healthColorGradient.Evaluate(HealthPercentage);
            }
            
            if (healthText != null)
            {
                healthText.text = $"{_currentHealth:F0}/{_maxHealth:F0}";
            }
        }
        
        /// <summary>
        /// 播放受伤震动效果
        /// </summary>
        private void PlayDamageShake()
        {
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
            }
            _shakeCoroutine = StartCoroutine(ShakeCoroutine());
        }
        
        /// <summary>
        /// 震动协程
        /// </summary>
        private System.Collections.IEnumerator ShakeCoroutine()
        {
            float elapsed = 0f;
            
            while (elapsed < shakeDuration)
            {
                float progress = elapsed / shakeDuration;
                float intensity = shakeIntensity * (1f - progress);
                
                Vector3 randomOffset = new Vector3(
                    Random.Range(-intensity, intensity),
                    Random.Range(-intensity, intensity),
                    0f
                );
                
                transform.localPosition = _originalPosition + randomOffset;
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 恢复原始位置
            transform.localPosition = _originalPosition;
            _shakeCoroutine = null;
        }
        
        #endregion
        
        #region Validation
        
        private void OnValidate()
        {
            if (_maxHealth <= 0f)
                _maxHealth = 100f;
                
            if (smoothTime <= 0f)
                smoothTime = 0.2f;
        }
        
        #endregion
    }
}