using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UGF.WorldUI
{
    /// <summary>
    /// UI分组管理器
    /// </summary>
    public class UIGroup : IDisposable
    {
        #region Fields
        
        private readonly string _name;
        private readonly UIGroupConfig _config;
        private readonly List<WorldSpaceUIComponent> _uiComponents;
        private readonly GameObject _groupObject;
        
        private bool _isVisible = true;
        private bool _isActive = true;
        private bool _disposed = false;
        private Camera _camera => WorldSpaceUIManager.Instance?.UICamera;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// 分组名称
        /// </summary>
        public string Name => _name;
        
        /// <summary>
        /// 分组配置
        /// </summary>
        public UIGroupConfig Config => _config;
        
        /// <summary>
        /// 分组Transform
        /// </summary>
        public Transform Transform => _groupObject?.transform;
        
        /// <summary>
        /// 当前UI数量
        /// </summary>
        public int Count => _uiComponents.Count;
        
        /// <summary>
        /// 是否可见
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set => SetVisible(value);
        }
        
        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set => SetActive(value);
        }
        
        /// <summary>
        /// 所有UI组件（只读）
        /// </summary>
        public IReadOnlyList<WorldSpaceUIComponent> UIComponents => _uiComponents.AsReadOnly();
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">分组名称</param>
        /// <param name="config">分组配置</param>
        /// <param name="parent">父Transform</param>
        public UIGroup(string name, UIGroupConfig config, Transform parent)
        {
            _name = name;
            _config = config ?? new UIGroupConfig();
            _uiComponents = new List<WorldSpaceUIComponent>();
            
            // 创建分组GameObject
            _groupObject = new GameObject($"[UIGroup] {name}");
            _groupObject.transform.SetParent(parent);
            
            // 设置Canvas排序
            SetupCanvas();
            
            Debug.Log($"[UIGroup] 创建分组: {name}");
        }
        
        #endregion
        
        #region Setup
        
        private void SetupCanvas()
        {
            // 添加Canvas组件（World Space UI需要Canvas）
            var canvas = _groupObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = _groupObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                // 设置世界空间相机
                if (_camera != null)
                {
                    canvas.worldCamera = _camera;
                }
            }
            
            // 设置排序
            if (_config.sortingOrder != 0 || !string.IsNullOrEmpty(_config.sortingLayerName))
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = _config.sortingOrder;
                
                if (!string.IsNullOrEmpty(_config.sortingLayerName))
                {
                    canvas.sortingLayerName = _config.sortingLayerName;
                }
            }
            
            // 设置CanvasScaler World模式
            if (_config.enableWorldSpaceScaler)
            {
                SetupCanvasScaler();
            }

            // 添加GraphicRaycaster用于交互
            var graphicRaycaster = _groupObject.GetComponent<GraphicRaycaster>();
            if (graphicRaycaster == null)
            {
                _groupObject.AddComponent<GraphicRaycaster>();
            }
        }
        
        /// <summary>
        /// 设置CanvasScaler World模式
        /// </summary>
        private void SetupCanvasScaler()
        {
            var canvasScaler = _groupObject.GetComponent<CanvasScaler>();
            if (canvasScaler == null)
            {
                canvasScaler = _groupObject.AddComponent<CanvasScaler>();
            }
            
            // 配置为World Space模式
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            
            // 检测相机投影模式并应用相应的缩放因子
            if (_camera != null && _camera.orthographic && _config.enableOrthographicOptimization)
            {
                canvasScaler.scaleFactor = _config.orthographicScaleFactor;
                Debug.Log($"[UIGroup] 分组 {_name} 检测到正交相机，使用正交缩放因子: {_config.orthographicScaleFactor}");
            }
            else
            {
                canvasScaler.scaleFactor = _config.worldSpaceScaleFactor;
                Debug.Log($"[UIGroup] 分组 {_name} 使用透视相机缩放因子: {_config.worldSpaceScaleFactor}");
            }
        }
        
        #endregion
        
        #region Canvas Management
        
        /// <summary>
        /// 设置世界空间相机
        /// </summary>
        public void SetWorldCamera()
        {
            var canvas = _groupObject.GetComponent<Canvas>();
            if (canvas != null && _camera != null)
            {
                canvas.worldCamera = _camera;
                Debug.Log($"[UIGroup] 分组 {_name} 已设置世界空间相机: {_camera?.name}");
            }
        }
        
        /// <summary>
        /// 更新CanvasScaler设置
        /// </summary>
        /// <param name="scaleFactor">缩放因子</param>
        public void UpdateCanvasScaler(float scaleFactor)
        {
            if (!_config.enableWorldSpaceScaler) return;
            
            var canvasScaler = _groupObject.GetComponent<CanvasScaler>();
            if (canvasScaler != null)
            {
                canvasScaler.scaleFactor = scaleFactor;
                _config.worldSpaceScaleFactor = scaleFactor;
                Debug.Log($"[UIGroup] 分组 {_name} 已更新缩放因子: {scaleFactor}");
            }
        }
        
        /// <summary>
        /// 刷新CanvasScaler设置（根据当前相机投影模式）
        /// </summary>
        public void RefreshCanvasScaler()
        {
            if (!_config.enableWorldSpaceScaler) return;
            
            var canvasScaler = _groupObject.GetComponent<CanvasScaler>();
            if (canvasScaler != null)
            { 
                if (_camera != null && _camera.orthographic && _config.enableOrthographicOptimization)
                {
                    canvasScaler.scaleFactor = _config.orthographicScaleFactor;
                    Debug.Log($"[UIGroup] 分组 {_name} 刷新为正交相机缩放因子: {_config.orthographicScaleFactor}");
                }
                else
                {
                    canvasScaler.scaleFactor = _config.worldSpaceScaleFactor;
                    Debug.Log($"[UIGroup] 分组 {_name} 刷新为透视相机缩放因子: {_config.worldSpaceScaleFactor}");
                }
            }
        }
        
        /// <summary>
        /// 获取Canvas组件
        /// </summary>
        /// <returns>Canvas组件</returns>
        public Canvas GetCanvas()
        {
            return _groupObject?.GetComponent<Canvas>();
        }
        
        /// <summary>
        /// 获取CanvasScaler组件
        /// </summary>
        /// <returns>CanvasScaler组件</returns>
        public CanvasScaler GetCanvasScaler()
        {
            return _groupObject?.GetComponent<CanvasScaler>();
        }
        
        /// <summary>
        /// 获取GraphicRaycaster组件
        /// </summary>
        /// <returns>GraphicRaycaster组件</returns>
        public GraphicRaycaster GetGraphicRaycaster()
        {
            return _groupObject?.GetComponent<GraphicRaycaster>();
        }
        
        #endregion
        
        #region UI Management
        
        /// <summary>
        /// 添加UI组件
        /// </summary>
        /// <param name="uiComponent">UI组件</param>
        /// <returns>是否添加成功</returns>
        public bool AddUI(WorldSpaceUIComponent uiComponent)
        {
            if (uiComponent == null)
            {
                Debug.LogError("[UIGroup] UI组件不能为空");
                return false;
            }
            
            // 检查数量限制
            if (_config.maxInstances > 0 && _uiComponents.Count >= _config.maxInstances)
            {
                Debug.LogWarning($"[UIGroup] 分组 {_name} 已达到最大实例数量: {_config.maxInstances}");
                
                // 如果启用了自动清理，尝试清理最旧的UI
                if (_config.enableAutoRemoveOldest)
                {
                    RemoveOldestUI();
                }
                else
                {
                    return false;
                }
            }
            
            if (!_uiComponents.Contains(uiComponent))
            {
                _uiComponents.Add(uiComponent);

                // 只有当父级不是当前分组时才设置父级
                if (uiComponent.transform.parent != Transform)
                {
                    uiComponent.transform.SetParent(Transform);
                }
                
                // 应用分组状态
                ApplyGroupState(uiComponent);

                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 移除UI组件
        /// </summary>
        /// <param name="uiComponent">UI组件</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveUI(WorldSpaceUIComponent uiComponent)
        {
            if (uiComponent == null) return false;
            
            return _uiComponents.Remove(uiComponent);
        }
        
        /// <summary>
        /// 清理过期的UI
        /// </summary>
        public void CleanupExpiredUIs()
        {
            var expiredUIs = _uiComponents.Where(ui => ui != null && ui.IsExpired).ToList();
            
            foreach (var ui in expiredUIs)
            {
                RemoveUI(ui);
                if (ui.gameObject != null)
                {
                    UnityEngine.Object.Destroy(ui.gameObject);
                }
            }
            
            // 清理空引用
            _uiComponents.RemoveAll(ui => ui == null);
        }
        
        /// <summary>
        /// 移除最旧的UI
        /// </summary>
        private void RemoveOldestUI()
        {
            if (_uiComponents.Count > 0)
            {
                var oldestUI = _uiComponents[0];
                RemoveUI(oldestUI);
                
                if (oldestUI != null && oldestUI.gameObject != null)
                {
                    UnityEngine.Object.Destroy(oldestUI.gameObject);
                }
            }
        }
        
        /// <summary>
        /// 清除所有UI组件
        /// </summary>
        public void ClearUI()
        {
            // 销毁所有UI组件
            foreach (var ui in _uiComponents.ToList())
            {
                if (ui != null && ui.gameObject != null)
                {
                    UnityEngine.Object.Destroy(ui.gameObject);
                }
            }
            
            // 清空列表
            _uiComponents.Clear();
            
            Debug.Log($"[UIGroup] 已清除分组 {_name} 中的所有UI");
        }
        
        #endregion
        
        #region Group State
        
        /// <summary>
        /// 设置分组可见性
        /// </summary>
        /// <param name="visible">是否可见</param>
        public void SetVisible(bool visible)
        {
            if (_isVisible == visible) return;
            
            _isVisible = visible;
            _groupObject.SetActive(_isActive && _isVisible);
            
            // 通知所有UI组件
            foreach (var ui in _uiComponents)
            {
                if (ui != null)
                {
                    ui.OnGroupVisibilityChanged(visible);
                }
            }
        }
        
        /// <summary>
        /// 设置分组激活状态
        /// </summary>
        /// <param name="active">是否激活</param>
        public void SetActive(bool active)
        {
            if (_isActive == active) return;
            
            _isActive = active;
            _groupObject.SetActive(_isActive && _isVisible);
            
            // 通知所有UI组件
            foreach (var ui in _uiComponents)
            {
                if (ui != null)
                {
                    ui.OnGroupActiveChanged(active);
                }
            }
        }
        
        /// <summary>
        /// 应用分组状态到UI组件
        /// </summary>
        /// <param name="uiComponent">UI组件</param>
        private void ApplyGroupState(WorldSpaceUIComponent uiComponent)
        {
            if (uiComponent == null) return;
            
            uiComponent.OnGroupVisibilityChanged(_isVisible);
            uiComponent.OnGroupActiveChanged(_isActive);
        }
        
        #endregion
        
        #region Update
        
        /// <summary>
        /// 更新分组（由UpdateScheduler调用）
        /// </summary>
        public void Update()
        {
            if (!_isActive) return;
            
            OnGroupUpdate();
            
            // 清理无效的UI组件引用
            for (int i = _uiComponents.Count - 1; i >= 0; i--)
            {
                var ui = _uiComponents[i];
                if (ui == null)
                {
                    _uiComponents.RemoveAt(i);
                }
            }
        }
        
        /// <summary>
        /// 分组自定义更新逻辑（可被子类重写）
        /// </summary>
        protected virtual void OnGroupUpdate()
        {
            // 子类可以重写此方法实现自定义更新逻辑
        }
        
        #endregion
        
        #region Culling
        
        /// <summary>
        /// 执行剔除检查
        /// </summary>
        /// <param name="cameraPosition">相机位置</param>
        /// <param name="cameraForward">相机前方向</param>
        /// <param name="frustumPlanes">视锥体平面</param>
        public void PerformCulling(Vector3 cameraPosition, Vector3 cameraForward, Plane[] frustumPlanes)
        {
            if (!_config.enableCulling) return;
            
            foreach (var ui in _uiComponents)
            {
                if (ui == null) continue;
                
                var distance = Vector3.Distance(cameraPosition, ui.transform.position);
                var inRange = distance <= _config.cullingDistance;
                var inFrustum = true;
                
                if (inRange && frustumPlanes != null)
                {
                    var bounds = ui.GetBounds();
                    inFrustum = GeometryUtility.TestPlanesAABB(frustumPlanes, bounds);
                }
                
                ui.SetCulled(!(inRange && inFrustum));
            }
        }
        
        #endregion
        
        #region Statistics
        
        /// <summary>
        /// 获取分组统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        public UIGroupStats GetStats()
        {
            var activeCount = _uiComponents.Count(ui => ui != null && ui.gameObject.activeInHierarchy);
            var visibleCount = _uiComponents.Count(ui => ui != null && ui.IsVisible);
            var culledCount = _uiComponents.Count(ui => ui != null && ui.IsCulled);
            
            return new UIGroupStats
            {
                Name = _name,
                TotalCount = _uiComponents.Count,
                ActiveCount = activeCount,
                VisibleCount = visibleCount,
                CulledCount = culledCount,
                IsGroupVisible = _isVisible,
                IsGroupActive = _isActive
            };
        }
        
        #endregion
        
        #region Dispose
        
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            
            // 清理所有UI组件
            foreach (var ui in _uiComponents.ToList())
            {
                if (ui != null && ui.gameObject != null)
                {
                    UnityEngine.Object.Destroy(ui.gameObject);
                }
            }
            _uiComponents.Clear();
            
            // 销毁分组GameObject
            if (_groupObject != null)
            {
                UnityEngine.Object.Destroy(_groupObject);
            }
            
            _disposed = true;
            
            Debug.Log($"[UIGroup] 销毁分组: {_name}");
        }
        
        #endregion
    }
    
    /// <summary>
    /// UI分组统计信息
    /// </summary>
    [Serializable]
    public struct UIGroupStats
    {
        public string Name;
        public int TotalCount;
        public int ActiveCount;
        public int VisibleCount;
        public int CulledCount;
        public bool IsGroupVisible;
        public bool IsGroupActive;
    }
}