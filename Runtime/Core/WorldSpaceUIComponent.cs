using System;
using UnityEngine;
using UnityEngine.UI;
using UGF.WorldUI.Utilities;

namespace UGF.WorldUI
{
    /// <summary>
    /// 世界空间UI组件基类
    /// </summary>
    public abstract class WorldSpaceUIComponent : MonoBehaviour
    {
        #region Fields
        
        [Header("基础设置")]
        [SerializeField] protected WorldSpaceUIConfig _config;
        
        // 管理器引用
        protected WorldSpaceUIManager _manager;
        protected UIGroup _group;
        
        // 跟随目标
        protected Transform _followTarget;
        protected Vector3 _worldPosition;
        protected Vector3 _offset;
        
        // 状态
        protected bool _isInitialized = false;
        protected bool _isVisible = true;
        protected bool _isCulled = false;
        protected bool _isExpired = false;
        
        // 生命周期
        protected float _createTime;
        protected float _lastUpdateTime;
        
        // 缓存组件
        protected Canvas _canvas;
        protected CanvasGroup _canvasGroup;
        protected RectTransform _rectTransform;
        
        // 动画
        protected Coroutine _fadeCoroutine;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// 配置
        /// </summary>
        public WorldSpaceUIConfig Config
        {
            get => _config;
            set => _config = value ?? new WorldSpaceUIConfig();
        }
        
        /// <summary>
        /// 管理器
        /// </summary>
        public WorldSpaceUIManager Manager => _manager;
        
        /// <summary>
        /// 所属分组
        /// </summary>
        public UIGroup Group => _group;
        
        /// <summary>
        /// 跟随目标
        /// </summary>
        public Transform FollowTarget
        {
            get => _followTarget;
            set => SetFollowTarget(value);
        }
        
        /// <summary>
        /// 世界位置
        /// </summary>
        public Vector3 WorldPosition
        {
            get => _worldPosition;
            set => SetWorldPosition(value);
        }
        
        /// <summary>
        /// 位置偏移
        /// </summary>
        public Vector3 Offset
        {
            get => _offset;
            set => _offset = value;
        }
        
        /// <summary>
        /// 是否可见
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible && !_isCulled;
            set => SetVisible(value);
        }
        
        /// <summary>
        /// 是否被剔除
        /// </summary>
        public bool IsCulled
        {
            get => _isCulled;
            set => SetCulled(value);
        }
        
        /// <summary>
        /// 是否过期
        /// </summary>
        public bool IsExpired
        {
            get
            {
                if (_config.lifeTime > 0)
                {
                    return Time.time - _createTime >= _config.lifeTime;
                }
                return _isExpired;
            }
        }
        
        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => _isInitialized;
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public float CreateTime => _createTime;
        
        /// <summary>
        /// 存活时间
        /// </summary>
        public float AliveTime => Time.time - _createTime;
        
        /// <summary>
        /// 生存时间（与AliveTime相同）
        /// </summary>
        public float LifeTime => AliveTime;
        
        #endregion
        
        #region Unity Lifecycle
        
        protected virtual void Awake()
        {
            // 初始化配置
            if (_config == null)
            {
                _config = ScriptableObject.CreateInstance<WorldSpaceUIConfig>();
            }
            
            CacheComponents();
        }
        
        protected virtual void Start()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning($"[{GetType().Name}] UI组件未正确初始化");
            }
        }
        
        protected virtual void Update()
        {
            if (!_isInitialized) return;
            
            UpdatePosition();
            UpdateRotation();
            UpdateScale();
            UpdateAlpha();
            
            OnUpdate();
        }
        
        protected virtual void OnDestroy()
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }
            
            OnDestroyed();
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// 初始化UI组件
        /// </summary>
        /// <param name="manager">管理器</param>
        /// <param name="group">所属分组</param>
        /// <param name="worldPosition">世界位置</param>
        public virtual void Initialize(WorldSpaceUIManager manager, UIGroup group, Vector3 worldPosition)
        {
            _manager = manager;
            _group = group;
            _worldPosition = worldPosition;
            _createTime = Time.time;
            _lastUpdateTime = Time.time;
            _isInitialized = true;
            
            // 设置初始位置
            transform.position = worldPosition + _offset;
            
            // 设置Canvas
            SetupCanvas();
            
            // 应用配置
            ApplyConfig();
            
            // 调用子类初始化
            OnInitialize();
            
            Debug.Log($"[{GetType().Name}] UI组件初始化完成");
        }
        
        /// <summary>
        /// 缓存组件引用
        /// </summary>
        protected virtual void CacheComponents()
        {
            _canvas = GetComponent<Canvas>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();
            
            // 如果没有CanvasGroup，自动添加
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        /// <summary>
        /// 设置Canvas（优先使用父级UIGroup的Canvas）
        /// </summary>
        protected virtual void SetupCanvas()
        {
            // 首先尝试从父级获取Canvas（UIGroup已经设置了Canvas）
            _canvas = GetComponentInParent<Canvas>();
            
            // 如果父级没有Canvas，才在当前GameObject上添加
            if (_canvas == null)
            {
                _canvas = GetComponent<Canvas>();
                if (_canvas == null)
                {
                    _canvas = gameObject.AddComponent<Canvas>();
                    _canvas.renderMode = RenderMode.WorldSpace;
                    _canvas.worldCamera = _manager?.UICamera;
                    
                    // 添加GraphicRaycaster用于交互
                    if (GetComponent<GraphicRaycaster>() == null)
                    {
                        gameObject.AddComponent<GraphicRaycaster>();
                    }
                    
                    Debug.Log($"[{GetType().Name}] 未找到父级Canvas，已在当前GameObject上添加Canvas");
                }
            }
            else
            {
                Debug.Log($"[{GetType().Name}] 使用父级Canvas: {_canvas.name}");
            }
        }
        
        /// <summary>
        /// 应用配置
        /// </summary>
        protected virtual void ApplyConfig()
        {
            if (_config == null) return;
            
            _offset = _config.offset;
            
            // 设置初始透明度
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = _config.enableFadeAnimation ? 0f : 1f;
            }
            
            // 播放淡入动画
            if (_config.enableFadeAnimation)
            {
                FadeIn();
            }
        }
        
        #endregion
        
        #region Position & Rotation
        
        /// <summary>
        /// 设置跟随目标
        /// </summary>
        /// <param name="target">跟随目标</param>
        public virtual void SetFollowTarget(Transform target)
        {
            _followTarget = target;
            _config.followTarget = target != null;
        }
        
        /// <summary>
        /// 设置世界位置
        /// </summary>
        /// <param name="position">世界位置</param>
        public virtual void SetWorldPosition(Vector3 position)
        {
            _worldPosition = position;
            _followTarget = null;
            _config.followTarget = false;
        }
        
        /// <summary>
        /// 更新位置
        /// </summary>
        protected virtual void UpdatePosition()
        {
            Vector3 targetPosition;
            
            if (_config.followTarget && _followTarget != null)
            {
                targetPosition = _followTarget.position + _offset;
            }
            else
            {
                targetPosition = _worldPosition + _offset;
            }
            
            transform.position = targetPosition;
        }
        
        /// <summary>
        /// 更新旋转
        /// </summary>
        protected virtual void UpdateRotation()
        {
            if (!_config.faceCamera || _manager?.UICamera == null) return;
            
            var camera = _manager.UICamera;
            var direction = transform.position - camera.transform.position;
            
            if (direction.sqrMagnitude > 0.001f)
            {
                var rotation = Quaternion.LookRotation(direction.normalized);
                transform.rotation = rotation;
            }
        }
        
        /// <summary>
        /// 更新缩放
        /// </summary>
        protected virtual void UpdateScale()
        {
            if (_config.scaleCurve == null || _config.scaleCurve.length == 0) return;
            
            var normalizedTime = GetNormalizedLifeTime();
            var scale = _config.scaleCurve.Evaluate(normalizedTime);
            
            // 正交相机优化：根据相机类型调整缩放
            if (_manager?.UICamera != null && _manager.UICamera.orthographic && _group?.Config.enableOrthographicOptimization == true)
            {
                // 正交相机下，根据距离模式调整缩放
                var camera = _manager.UICamera;
                var distanceMode = _group.Config.orthographicDistanceMode;
                
                switch (distanceMode)
                {
                    case OrthographicDistanceMode.CameraSize:
                        // 基于相机尺寸的缩放调整
                        var sizeScale = camera.orthographicSize / 10f; // 10为基准尺寸
                        scale *= sizeScale;
                        break;
                        
                    case OrthographicDistanceMode.FixedDistance:
                        // 固定距离模式下保持原始缩放
                        break;
                        
                    case OrthographicDistanceMode.ZDepth:
                        // 基于Z深度的缩放调整
                        var distance = WorldSpaceUIUtilities.CalculateOrthographicDistance(
                            transform.position, camera, distanceMode, _group.Config.orthographicFixedDistance);
                        var depthScale = Mathf.Clamp(distance / 50f, 0.5f, 2f); // 50为基准距离
                        scale *= depthScale;
                        break;
                }
            }
            
            // 应用最大最小缩放值限制
            scale = Mathf.Clamp(scale, _config.minScale, _config.maxScale);
            
            transform.localScale = Vector3.one * scale;
        }
        
        /// <summary>
        /// 更新透明度
        /// </summary>
        protected virtual void UpdateAlpha()
        {
            if (_canvasGroup == null || _config.alphaCurve == null || _config.alphaCurve.length == 0) return;
            
            var normalizedTime = GetNormalizedLifeTime();
            var alpha = _config.alphaCurve.Evaluate(normalizedTime);
            _canvasGroup.alpha = alpha;
        }
        
        /// <summary>
        /// 获取标准化生命周期时间
        /// </summary>
        /// <returns>0-1之间的值</returns>
        protected virtual float GetNormalizedLifeTime()
        {
            if (_config.lifeTime <= 0) return 0f;
            
            return Mathf.Clamp01(AliveTime / _config.lifeTime);
        }
        
        #endregion
        
        #region Visibility
        
        /// <summary>
        /// 设置可见性
        /// </summary>
        /// <param name="visible">是否可见</param>
        public virtual void SetVisible(bool visible)
        {
            if (_isVisible == visible) return;
            
            _isVisible = visible;
            UpdateVisibility();
        }
        
        /// <summary>
        /// 设置剔除状态
        /// </summary>
        /// <param name="culled">是否被剔除</param>
        public virtual void SetCulled(bool culled)
        {
            if (_isCulled == culled) return;
            
            _isCulled = culled;
            UpdateVisibility();
        }
        
        /// <summary>
        /// 更新可见性
        /// </summary>
        protected virtual void UpdateVisibility()
        {
            var shouldBeVisible = _isVisible && !_isCulled;
            gameObject.SetActive(shouldBeVisible);
        }
        
        #endregion
        
        #region Animation
        
        /// <summary>
        /// 淡入动画
        /// </summary>
        public virtual void FadeIn()
        {
            if (_canvasGroup == null) return;
            
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }
            
            _fadeCoroutine = StartCoroutine(FadeCoroutine(0f, 1f, _config.fadeDuration));
        }
        
        /// <summary>
        /// 淡出动画
        /// </summary>
        public virtual void FadeOut()
        {
            if (_canvasGroup == null) return;
            
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }
            
            _fadeCoroutine = StartCoroutine(FadeCoroutine(_canvasGroup.alpha, 0f, _config.fadeDuration));
        }
        
        /// <summary>
        /// 淡入淡出协程
        /// </summary>
        protected virtual System.Collections.IEnumerator FadeCoroutine(float from, float to, float duration)
        {
            var elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / duration;
                _canvasGroup.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }
            
            _canvasGroup.alpha = to;
            _fadeCoroutine = null;
        }
        
        #endregion
        
        #region Group Events
        
        /// <summary>
        /// 分组可见性改变事件
        /// </summary>
        /// <param name="visible">是否可见</param>
        public virtual void OnGroupVisibilityChanged(bool visible)
        {
            // 子类可以重写此方法响应分组可见性变化
        }
        
        /// <summary>
        /// 分组激活状态改变事件
        /// </summary>
        /// <param name="active">是否激活</param>
        public virtual void OnGroupActiveChanged(bool active)
        {
            // 子类可以重写此方法响应分组激活状态变化
        }
        
        #endregion
        
        #region Bounds
        
        /// <summary>
        /// 获取边界框
        /// </summary>
        /// <returns>边界框</returns>
        public virtual Bounds GetBounds()
        {
            if (_rectTransform != null)
            {
                var bounds = new Bounds(transform.position, Vector3.zero);
                var corners = new Vector3[4];
                _rectTransform.GetWorldCorners(corners);
                
                foreach (var corner in corners)
                {
                    bounds.Encapsulate(corner);
                }
                
                return bounds;
            }
            
            return new Bounds(transform.position, Vector3.one);
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 更新UI（由管理器调用）
        /// </summary>
        public virtual void UpdateUI()
        {
            if (!_isInitialized) return;
            
            _lastUpdateTime = Time.time;
        }
        
        /// <summary>
        /// 销毁UI
        /// </summary>
        public virtual void DestroyUI()
        {
            if (_config.enableFadeAnimation)
            {
                FadeOut();
                // 延迟销毁，等待淡出动画完成
                Invoke(nameof(DestroyImmediate), _config.fadeDuration);
            }
            else
            {
                DestroyImmediate();
            }
        }
        
        /// <summary>
        /// 立即销毁
        /// </summary>
        protected virtual void DestroyImmediate()
        {
            _manager?.DestroyUI(this);
        }
        
        /// <summary>
        /// 标记为过期
        /// </summary>
        public virtual void MarkAsExpired()
        {
            _isExpired = true;
        }
        
        #endregion
        
        #region Virtual Methods
        
        /// <summary>
        /// 初始化时调用（子类重写）
        /// </summary>
        protected virtual void OnInitialize()
        {
            // 子类重写此方法实现自定义初始化逻辑
        }
        
        /// <summary>
        /// 每帧更新时调用（子类重写）
        /// </summary>
        protected virtual void OnUpdate()
        {
            // 子类重写此方法实现自定义更新逻辑
        }
        
        /// <summary>
        /// 销毁时调用（子类重写）
        /// </summary>
        protected virtual void OnDestroyed()
        {
            // 子类重写此方法实现自定义销毁逻辑
        }
        
        #endregion
    }
}