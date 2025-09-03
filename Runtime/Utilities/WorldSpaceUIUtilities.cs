using System;
using System.Collections.Generic;
using UnityEngine;
using UGF.WorldUI;

namespace UGF.WorldUI.Utilities
{
    /// <summary>
    /// 世界空间UI工具类
    /// </summary>
    public static class WorldSpaceUIUtilities
    {
        #region Distance Calculations
        
        /// <summary>
        /// 计算两点之间的距离
        /// </summary>
        /// <param name="from">起始点</param>
        /// <param name="to">目标点</param>
        /// <returns>距离</returns>
        public static float CalculateDistance(Vector3 from, Vector3 to)
        {
            return Vector3.Distance(from, to);
        }
        
        /// <summary>
        /// 计算两点之间的平方距离（性能优化版本）
        /// </summary>
        /// <param name="from">起始点</param>
        /// <param name="to">目标点</param>
        /// <returns>平方距离</returns>
        public static float CalculateDistanceSqr(Vector3 from, Vector3 to)
        {
            return (to - from).sqrMagnitude;
        }
        
        /// <summary>
        /// 计算到相机的距离
        /// </summary>
        /// <param name="position">世界位置</param>
        /// <param name="camera">相机</param>
        /// <returns>距离</returns>
        public static float CalculateDistanceToCamera(Vector3 position, Camera camera)
        {
            if (camera == null) return float.MaxValue;
            
            if (camera.orthographic)
            {
                // 正交相机：计算到相机平面的距离（Z轴距离）
                var cameraForward = camera.transform.forward;
                var cameraPosition = camera.transform.position;
                var toPosition = position - cameraPosition;
                return Mathf.Abs(Vector3.Dot(toPosition, cameraForward));
            }
            else
            {
                // 透视相机：使用欧几里得距离
                return Vector3.Distance(position, camera.transform.position);
            }
        }
        
        /// <summary>
        /// 计算到相机的平方距离
        /// </summary>
        /// <param name="position">世界位置</param>
        /// <param name="camera">相机</param>
        /// <returns>平方距离</returns>
        public static float CalculateDistanceToCameraSqr(Vector3 position, Camera camera)
        {
            if (camera == null) return float.MaxValue;
            
            if (camera.orthographic)
            {
                // 正交相机：计算到相机平面的平方距离
                var cameraForward = camera.transform.forward;
                var cameraPosition = camera.transform.position;
                var toPosition = position - cameraPosition;
                var distance = Vector3.Dot(toPosition, cameraForward);
                return distance * distance;
            }
            else
            {
                // 透视相机：使用欧几里得平方距离
                return (position - camera.transform.position).sqrMagnitude;
            }
        }
        
        /// <summary>
        /// 计算正交相机距离（基于配置模式）
        /// </summary>
        /// <param name="position">世界位置</param>
        /// <param name="camera">正交相机</param>
        /// <param name="distanceMode">距离计算模式</param>
        /// <param name="fixedDistance">固定距离值（当模式为FixedDistance时使用）</param>
        /// <returns>计算出的距离</returns>
        public static float CalculateOrthographicDistance(Vector3 position, Camera camera, OrthographicDistanceMode distanceMode, float fixedDistance = 10f)
        {
            if (camera == null || !camera.orthographic) return float.MaxValue;
            
            switch (distanceMode)
            {
                case OrthographicDistanceMode.ZDepth:
                    // 使用Z轴深度
                    var cameraForward = camera.transform.forward;
                    var cameraPosition = camera.transform.position;
                    var toPosition = position - cameraPosition;
                    return Mathf.Abs(Vector3.Dot(toPosition, cameraForward));
                    
                case OrthographicDistanceMode.FixedDistance:
                    // 使用固定距离
                    return fixedDistance;
                    
                case OrthographicDistanceMode.CameraSize:
                    // 基于相机尺寸计算
                    return camera.orthographicSize;
                    
                default:
                    return CalculateDistanceToCamera(position, camera);
            }
        }
        
        /// <summary>
        /// 检查位置是否在正交相机视野内
        /// </summary>
        /// <param name="position">世界位置</param>
        /// <param name="camera">正交相机</param>
        /// <returns>是否在视野内</returns>
        public static bool IsPositionInOrthographicView(Vector3 position, Camera camera)
        {
            if (camera == null || !camera.orthographic) return false;
            
            var viewportPoint = camera.WorldToViewportPoint(position);
            return viewportPoint.x >= 0 && viewportPoint.x <= 1 && 
                   viewportPoint.y >= 0 && viewportPoint.y <= 1 && 
                   viewportPoint.z >= camera.nearClipPlane && viewportPoint.z <= camera.farClipPlane;
        }
        
        /// <summary>
        /// 获取正交相机的世界空间边界
        /// </summary>
        /// <param name="camera">正交相机</param>
        /// <param name="distance">距离相机的距离</param>
        /// <returns>边界矩形（世界坐标）</returns>
        public static Bounds GetOrthographicWorldBounds(Camera camera, float distance = 0f)
        {
            if (camera == null || !camera.orthographic) return new Bounds();
            
            var height = camera.orthographicSize * 2f;
            var width = height * camera.aspect;
            var center = camera.transform.position + camera.transform.forward * distance;
            
            return new Bounds(center, new Vector3(width, height, 0f));
        }
        
        /// <summary>
        /// 计算正交相机的最佳UI缩放因子
        /// </summary>
        /// <param name="camera">正交相机</param>
        /// <param name="referenceSize">参考尺寸</param>
        /// <param name="mode">距离模式</param>
        /// <returns>缩放因子</returns>
        public static float CalculateOrthographicScaleFactor(Camera camera, float referenceSize = 10f, OrthographicDistanceMode mode = OrthographicDistanceMode.CameraSize)
        {
            if (camera == null || !camera.orthographic) return 1f;
            
            switch (mode)
            {
                case OrthographicDistanceMode.CameraSize:
                    return camera.orthographicSize / referenceSize;
                    
                case OrthographicDistanceMode.FixedDistance:
                    return 1f;
                    
                case OrthographicDistanceMode.ZDepth:
                    // 基于屏幕分辨率的动态缩放
                    var screenHeight = Screen.height;
                    var targetHeight = camera.orthographicSize * 2f * 100f; // 100 pixels per unit
                    return screenHeight / targetHeight;
                    
                default:
                    return 1f;
            }
        }
        
        /// <summary>
        /// 获取正交相机视野内的UI元素数量估算
        /// </summary>
        /// <param name="camera">正交相机</param>
        /// <param name="uiPositions">UI位置列表</param>
        /// <returns>视野内的UI数量</returns>
        public static int GetVisibleUICountInOrthographicView(Camera camera, Vector3[] uiPositions)
        {
            if (camera == null || !camera.orthographic || uiPositions == null) return 0;
            
            int count = 0;
            foreach (var position in uiPositions)
            {
                if (IsPositionInOrthographicView(position, camera))
                {
                    count++;
                }
            }
            return count;
        }
        
        #endregion
        
        #region Screen Space Calculations
        
        /// <summary>
        /// 世界坐标转屏幕坐标
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <param name="camera">相机</param>
        /// <returns>屏幕坐标</returns>
        public static Vector3 WorldToScreenPoint(Vector3 worldPosition, Camera camera)
        {
            if (camera == null) return Vector3.zero;
            return camera.WorldToScreenPoint(worldPosition);
        }
        
        /// <summary>
        /// 屏幕坐标转世界坐标
        /// </summary>
        /// <param name="screenPosition">屏幕坐标</param>
        /// <param name="camera">相机</param>
        /// <param name="distance">距离相机的距离</param>
        /// <returns>世界坐标</returns>
        public static Vector3 ScreenToWorldPoint(Vector3 screenPosition, Camera camera, float distance = 10f)
        {
            if (camera == null) return Vector3.zero;
            screenPosition.z = distance;
            return camera.ScreenToWorldPoint(screenPosition);
        }
        
        /// <summary>
        /// 检查点是否在屏幕内
        /// </summary>
        /// <param name="screenPosition">屏幕坐标</param>
        /// <returns>是否在屏幕内</returns>
        public static bool IsPointOnScreen(Vector3 screenPosition)
        {
            return screenPosition.x >= 0 && screenPosition.x <= Screen.width &&
                   screenPosition.y >= 0 && screenPosition.y <= Screen.height &&
                   screenPosition.z > 0;
        }
        
        /// <summary>
        /// 检查世界坐标是否在屏幕内
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <param name="camera">相机</param>
        /// <returns>是否在屏幕内</returns>
        public static bool IsWorldPointOnScreen(Vector3 worldPosition, Camera camera)
        {
            if (camera == null) return false;
            Vector3 screenPoint = camera.WorldToScreenPoint(worldPosition);
            return IsPointOnScreen(screenPoint);
        }
        
        #endregion
        
        #region Frustum Culling
        
        /// <summary>
        /// 检查点是否在相机视锥内
        /// </summary>
        /// <param name="point">世界坐标点</param>
        /// <param name="camera">相机</param>
        /// <returns>是否在视锥内</returns>
        public static bool IsPointInCameraFrustum(Vector3 point, Camera camera)
        {
            if (camera == null) return false;
            
            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
            return GeometryUtility.TestPlanesAABB(frustumPlanes, new Bounds(point, Vector3.zero));
        }
        
        /// <summary>
        /// 检查边界框是否在相机视锥内
        /// </summary>
        /// <param name="bounds">边界框</param>
        /// <param name="camera">相机</param>
        /// <returns>是否在视锥内</returns>
        public static bool IsBoundsInCameraFrustum(Bounds bounds, Camera camera)
        {
            if (camera == null) return false;
            
            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
            return GeometryUtility.TestPlanesAABB(frustumPlanes, bounds);
        }
        
        #endregion
        
        #region Angle Calculations
        
        /// <summary>
        /// 计算两个向量之间的角度
        /// </summary>
        /// <param name="from">起始向量</param>
        /// <param name="to">目标向量</param>
        /// <returns>角度（度）</returns>
        public static float CalculateAngle(Vector3 from, Vector3 to)
        {
            return Vector3.Angle(from, to);
        }
        
        /// <summary>
        /// 计算物体到相机的角度
        /// </summary>
        /// <param name="objectPosition">物体位置</param>
        /// <param name="camera">相机</param>
        /// <returns>角度（度）</returns>
        public static float CalculateAngleToCamera(Vector3 objectPosition, Camera camera)
        {
            if (camera == null) return 0f;
            
            Vector3 directionToObject = (objectPosition - camera.transform.position).normalized;
            Vector3 cameraForward = camera.transform.forward;
            
            return Vector3.Angle(cameraForward, directionToObject);
        }
        
        /// <summary>
        /// 检查物体是否在相机视角范围内
        /// </summary>
        /// <param name="objectPosition">物体位置</param>
        /// <param name="camera">相机</param>
        /// <param name="maxAngle">最大角度</param>
        /// <returns>是否在视角范围内</returns>
        public static bool IsInCameraViewAngle(Vector3 objectPosition, Camera camera, float maxAngle)
        {
            if (camera == null) return false;
            
            float angle = CalculateAngleToCamera(objectPosition, camera);
            return angle <= maxAngle;
        }
        
        #endregion
        
        #region Rotation Utilities
        
        /// <summary>
        /// 计算朝向相机的旋转
        /// </summary>
        /// <param name="position">物体位置</param>
        /// <param name="camera">相机</param>
        /// <param name="upVector">上方向</param>
        /// <returns>朝向相机的旋转</returns>
        public static Quaternion CalculateLookAtCameraRotation(Vector3 position, Camera camera, Vector3? upVector = null)
        {
            if (camera == null) return Quaternion.identity;
            
            Vector3 up = upVector ?? Vector3.up;
            Vector3 direction = (camera.transform.position - position).normalized;
            
            return Quaternion.LookRotation(direction, up);
        }
        
        /// <summary>
        /// 计算Billboard旋转（始终面向相机）
        /// </summary>
        /// <param name="camera">相机</param>
        /// <returns>Billboard旋转</returns>
        public static Quaternion CalculateBillboardRotation(Camera camera)
        {
            if (camera == null) return Quaternion.identity;
            
            return Quaternion.LookRotation(camera.transform.forward, camera.transform.up);
        }
        
        #endregion
        
        #region LOD Calculations
        
        /// <summary>
        /// 根据距离计算LOD等级
        /// </summary>
        /// <param name="distance">距离</param>
        /// <param name="lodDistances">LOD距离数组</param>
        /// <returns>LOD等级</returns>
        public static int CalculateLODLevel(float distance, float[] lodDistances)
        {
            if (lodDistances == null || lodDistances.Length == 0) return 0;
            
            for (int i = 0; i < lodDistances.Length; i++)
            {
                if (distance <= lodDistances[i])
                {
                    return i;
                }
            }
            
            return lodDistances.Length;
        }
        
        /// <summary>
        /// 根据距离计算缩放因子
        /// </summary>
        /// <param name="distance">距离</param>
        /// <param name="minDistance">最小距离</param>
        /// <param name="maxDistance">最大距离</param>
        /// <param name="minScale">最小缩放</param>
        /// <param name="maxScale">最大缩放</param>
        /// <returns>缩放因子</returns>
        public static float CalculateDistanceScale(float distance, float minDistance, float maxDistance, float minScale = 0.5f, float maxScale = 1.0f)
        {
            if (distance <= minDistance) return maxScale;
            if (distance >= maxDistance) return minScale;
            
            float t = (distance - minDistance) / (maxDistance - minDistance);
            return Mathf.Lerp(maxScale, minScale, t);
        }
        
        #endregion
        
        #region Bounds Utilities
        
        /// <summary>
        /// 计算UI组件的边界框
        /// </summary>
        /// <param name="transform">变换组件</param>
        /// <param name="size">UI大小</param>
        /// <returns>边界框</returns>
        public static Bounds CalculateUIBounds(Transform transform, Vector2 size)
        {
            if (transform == null) return new Bounds();
            
            Vector3 center = transform.position;
            Vector3 boundsSize = new Vector3(size.x, size.y, 0.1f);
            
            return new Bounds(center, boundsSize);
        }
        
        /// <summary>
        /// 计算多个边界框的合并边界
        /// </summary>
        /// <param name="bounds">边界框列表</param>
        /// <returns>合并后的边界框</returns>
        public static Bounds CombineBounds(IEnumerable<Bounds> bounds)
        {
            Bounds combinedBounds = new Bounds();
            bool first = true;
            
            foreach (var bound in bounds)
            {
                if (first)
                {
                    combinedBounds = bound;
                    first = false;
                }
                else
                {
                    combinedBounds.Encapsulate(bound);
                }
            }
            
            return combinedBounds;
        }
        
        #endregion
        
        #region Performance Utilities
        
        /// <summary>
        /// 获取当前帧率
        /// </summary>
        /// <returns>帧率</returns>
        public static float GetCurrentFrameRate()
        {
            return 1.0f / Time.unscaledDeltaTime;
        }
        
        /// <summary>
        /// 获取内存使用量（MB）
        /// </summary>
        /// <returns>内存使用量</returns>
        public static long GetMemoryUsageMB()
        {
            return GC.GetTotalMemory(false) / (1024 * 1024);
        }
        
        /// <summary>
        /// 检查是否需要性能优化
        /// </summary>
        /// <param name="targetFrameRate">目标帧率</param>
        /// <param name="maxMemoryMB">最大内存使用量（MB）</param>
        /// <returns>是否需要优化</returns>
        public static bool ShouldOptimizePerformance(float targetFrameRate = 30f, long maxMemoryMB = 512)
        {
            float currentFrameRate = GetCurrentFrameRate();
            long currentMemory = GetMemoryUsageMB();
            
            return currentFrameRate < targetFrameRate || currentMemory > maxMemoryMB;
        }
        
        #endregion
        
        #region Color Utilities
        
        /// <summary>
        /// 根据距离计算透明度
        /// </summary>
        /// <param name="distance">距离</param>
        /// <param name="fadeStartDistance">开始淡出距离</param>
        /// <param name="fadeEndDistance">完全淡出距离</param>
        /// <returns>透明度值</returns>
        public static float CalculateDistanceAlpha(float distance, float fadeStartDistance, float fadeEndDistance)
        {
            if (distance <= fadeStartDistance) return 1.0f;
            if (distance >= fadeEndDistance) return 0.0f;
            
            float t = (distance - fadeStartDistance) / (fadeEndDistance - fadeStartDistance);
            return 1.0f - t;
        }
        
        /// <summary>
        /// 应用颜色透明度
        /// </summary>
        /// <param name="color">原始颜色</param>
        /// <param name="alpha">透明度</param>
        /// <returns>应用透明度后的颜色</returns>
        public static Color ApplyAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }
        
        #endregion
        
        #region Animation Utilities
        
        /// <summary>
        /// 计算缓动值
        /// </summary>
        /// <param name="t">时间参数（0-1）</param>
        /// <param name="easingType">缓动类型</param>
        /// <returns>缓动后的值</returns>
        public static float CalculateEasing(float t, EasingType easingType)
        {
            t = Mathf.Clamp01(t);
            
            switch (easingType)
            {
                case EasingType.Linear:
                    return t;
                case EasingType.EaseIn:
                    return t * t;
                case EasingType.EaseOut:
                    return 1f - (1f - t) * (1f - t);
                case EasingType.EaseInOut:
                    return t < 0.5f ? 2f * t * t : 1f - 2f * (1f - t) * (1f - t);
                case EasingType.Bounce:
                    return CalculateBounceEasing(t);
                default:
                    return t;
            }
        }
        
        /// <summary>
        /// 计算弹跳缓动
        /// </summary>
        /// <param name="t">时间参数</param>
        /// <returns>弹跳缓动值</returns>
        private static float CalculateBounceEasing(float t)
        {
            if (t < 1f / 2.75f)
            {
                return 7.5625f * t * t;
            }
            else if (t < 2f / 2.75f)
            {
                t -= 1.5f / 2.75f;
                return 7.5625f * t * t + 0.75f;
            }
            else if (t < 2.5f / 2.75f)
            {
                t -= 2.25f / 2.75f;
                return 7.5625f * t * t + 0.9375f;
            }
            else
            {
                t -= 2.625f / 2.75f;
                return 7.5625f * t * t + 0.984375f;
            }
        }
        
        #endregion
        
        #region Validation Utilities
        
        /// <summary>
        /// 验证相机是否有效
        /// </summary>
        /// <param name="camera">相机</param>
        /// <returns>是否有效</returns>
        public static bool IsValidCamera(Camera camera)
        {
            return camera != null && camera.gameObject.activeInHierarchy && camera.enabled;
        }
        
        /// <summary>
        /// 验证变换是否有效
        /// </summary>
        /// <param name="transform">变换</param>
        /// <returns>是否有效</returns>
        public static bool IsValidTransform(Transform transform)
        {
            return transform != null && transform.gameObject.activeInHierarchy;
        }
        
        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        /// <param name="config">配置</param>
        /// <returns>是否有效</returns>
        public static bool IsValidConfig(WorldSpaceUIConfig config)
        {
            return config != null && config.Validate();
        }
        
        #endregion
        
        #region Math Utilities
        
        /// <summary>
        /// 安全除法（避免除零）
        /// </summary>
        /// <param name="numerator">分子</param>
        /// <param name="denominator">分母</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>除法结果</returns>
        public static float SafeDivide(float numerator, float denominator, float defaultValue = 0f)
        {
            return Mathf.Approximately(denominator, 0f) ? defaultValue : numerator / denominator;
        }
        
        /// <summary>
        /// 平滑阻尼
        /// </summary>
        /// <param name="current">当前值</param>
        /// <param name="target">目标值</param>
        /// <param name="velocity">速度引用</param>
        /// <param name="smoothTime">平滑时间</param>
        /// <param name="maxSpeed">最大速度</param>
        /// <param name="deltaTime">时间增量</param>
        /// <returns>平滑后的值</returns>
        public static float SmoothDamp(float current, float target, ref float velocity, float smoothTime, float maxSpeed = Mathf.Infinity, float deltaTime = -1f)
        {
            if (deltaTime < 0f) deltaTime = Time.deltaTime;
            return Mathf.SmoothDamp(current, target, ref velocity, smoothTime, maxSpeed, deltaTime);
        }
        
        /// <summary>
        /// 向量平滑阻尼
        /// </summary>
        /// <param name="current">当前值</param>
        /// <param name="target">目标值</param>
        /// <param name="velocity">速度引用</param>
        /// <param name="smoothTime">平滑时间</param>
        /// <param name="maxSpeed">最大速度</param>
        /// <param name="deltaTime">时间增量</param>
        /// <returns>平滑后的值</returns>
        public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 velocity, float smoothTime, float maxSpeed = Mathf.Infinity, float deltaTime = -1f)
        {
            if (deltaTime < 0f) deltaTime = Time.deltaTime;
            return Vector3.SmoothDamp(current, target, ref velocity, smoothTime, maxSpeed, deltaTime);
        }
        
        #endregion
    }
    
    /// <summary>
    /// 缓动类型枚举
    /// </summary>
    public enum EasingType
    {
        Linear,
        EaseIn,
        EaseOut,
        EaseInOut,
        Bounce
    }
    
    /// <summary>
    /// 世界空间UI扩展方法
    /// </summary>
    public static class WorldSpaceUIExtensions
    {
        /// <summary>
        /// 获取或添加组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="gameObject">游戏对象</param>
        /// <returns>组件</returns>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }
        
        /// <summary>
        /// 设置层级递归
        /// </summary>
        /// <param name="transform">变换</param>
        /// <param name="layer">层级</param>
        public static void SetLayerRecursively(this Transform transform, int layer)
        {
            transform.gameObject.layer = layer;
            foreach (Transform child in transform)
            {
                child.SetLayerRecursively(layer);
            }
        }
        
        /// <summary>
        /// 设置激活状态递归
        /// </summary>
        /// <param name="transform">变换</param>
        /// <param name="active">激活状态</param>
        public static void SetActiveRecursively(this Transform transform, bool active)
        {
            transform.gameObject.SetActive(active);
            foreach (Transform child in transform)
            {
                child.SetActiveRecursively(active);
            }
        }
        
        /// <summary>
        /// 获取所有子对象的组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="transform">变换</param>
        /// <param name="includeInactive">是否包含非激活对象</param>
        /// <returns>组件列表</returns>
        public static List<T> GetComponentsInChildrenList<T>(this Transform transform, bool includeInactive = false) where T : Component
        {
            return new List<T>(transform.GetComponentsInChildren<T>(includeInactive));
        }
        
        /// <summary>
        /// 安全销毁游戏对象
        /// </summary>
        /// <param name="gameObject">游戏对象</param>
        /// <param name="delay">延迟时间</param>
        public static void SafeDestroy(this GameObject gameObject, float delay = 0f)
        {
            if (gameObject != null)
            {
                if (Application.isPlaying)
                {
                    if (delay > 0f)
                    {
                        UnityEngine.Object.Destroy(gameObject, delay);
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(gameObject);
                    }
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(gameObject);
                }
            }
        }
        
        /// <summary>
        /// 安全销毁组件
        /// </summary>
        /// <param name="component">组件</param>
        /// <param name="delay">延迟时间</param>
        public static void SafeDestroy(this Component component, float delay = 0f)
        {
            if (component != null)
            {
                if (Application.isPlaying)
                {
                    if (delay > 0f)
                    {
                        UnityEngine.Object.Destroy(component, delay);
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(component);
                    }
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(component);
                }
            }
        }
        
        /// <summary>
        /// 重置变换
        /// </summary>
        /// <param name="transform">变换</param>
        public static void ResetTransform(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
        
        /// <summary>
        /// 复制变换
        /// </summary>
        /// <param name="transform">目标变换</param>
        /// <param name="source">源变换</param>
        public static void CopyTransform(this Transform transform, Transform source)
        {
            if (source != null)
            {
                transform.position = source.position;
                transform.rotation = source.rotation;
                transform.localScale = source.localScale;
            }
        }
        
        /// <summary>
        /// 检查是否在相机视野内
        /// </summary>
        /// <param name="renderer">渲染器</param>
        /// <param name="camera">相机</param>
        /// <returns>是否在视野内</returns>
        public static bool IsVisibleFrom(this Renderer renderer, Camera camera)
        {
            if (renderer == null || camera == null) return false;
            
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
            return GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
        }
        
        /// <summary>
        /// 获取Canvas的排序顺序
        /// </summary>
        /// <param name="canvas">Canvas</param>
        /// <returns>排序顺序</returns>
        public static int GetSortingOrder(this Canvas canvas)
        {
            return canvas != null ? canvas.sortingOrder : 0;
        }
        
        /// <summary>
        /// 设置Canvas的排序顺序
        /// </summary>
        /// <param name="canvas">Canvas</param>
        /// <param name="sortingOrder">排序顺序</param>
        public static void SetSortingOrder(this Canvas canvas, int sortingOrder)
        {
            if (canvas != null)
            {
                canvas.sortingOrder = sortingOrder;
            }
        }
    }
}