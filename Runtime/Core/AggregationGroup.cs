using System;
using System.Collections.Generic;
using UnityEngine;

namespace UGF.WorldUI
{
    /// <summary>
    /// 聚合组 - 管理一组被聚合的UI及其聚合展示UI
    /// </summary>
    public class AggregationGroup
    {
        #region Fields

        private readonly List<IAggregatable> _members = new List<IAggregatable>();
        private readonly UIAggregationConfig _config;
        private readonly UIGroup _uiGroup;

        private WorldSpaceUIComponent _aggregatedUI;
        private GameObject _aggregatedPrefab;
        private float _createTime;

        #endregion

        #region Properties

        /// <summary>聚合类型标识</summary>
        public string AggregationType { get; }

        /// <summary>所属UIGroup</summary>
        public UIGroup UIGroup => _uiGroup;

        /// <summary>聚合组内源UI列表（只读）</summary>
        public IReadOnlyList<IAggregatable> Members => _members.AsReadOnly();

        /// <summary>当前成员数</summary>
        public int MemberCount => _members.Count;

        /// <summary>聚合展示UI实例</summary>
        public WorldSpaceUIComponent AggregatedUI => _aggregatedUI;

        /// <summary>创建时间</summary>
        public float CreateTime => _createTime;

        /// <summary>聚合组唯一标识</summary>
        public string GroupId => AggregationType;

        /// <summary>聚合组中心位置（动态计算，实时跟踪成员位置）</summary>
        public Vector3 CenterPosition => CalculateCenterPosition();

        /// <summary>聚合配置（由所属UIGroup或全局配置决定）</summary>
        public UIAggregationConfig Config => _config;

        #endregion

        #region Constructor

        public AggregationGroup(string aggregationType, UIGroup uiGroup,
            UIAggregationConfig config, GameObject aggregatedPrefab)
        {
            AggregationType = aggregationType;
            _uiGroup = uiGroup;
            _config = config ?? UIAggregationConfig.CreateDefault();
            _aggregatedPrefab = aggregatedPrefab;
            _createTime = Time.time;

            if (_aggregatedPrefab == null)
            {
                Debug.LogWarning($"[AggregationGroup] 聚合展示预制体未配置，聚合组 {AggregationType} 将不显示。请调用 RegisterAggregationPrefab 或设置 UIAggregationConfig.aggregationPrefab");
            }
        }

        #endregion

        #region Static Helpers

        public static string GetGroupId(string type, string key)
        {
            return string.IsNullOrEmpty(key) ? type : $"{type}:{key}";
        }

        #endregion

        #region Member Management

        /// <summary>
        /// 添加成员
        /// </summary>
        public bool AddMember(IAggregatable ui)
        {
            if (ui == null || _members.Contains(ui))
                return false;

            if (_members.Count >= _config.maxGroupSize)
                return false;

            _members.Add(ui);
            ui.OnAggregated(this);

            // 首个成员加入时创建聚合展示UI
            if (_members.Count == 1 && _aggregatedUI == null)
            {
                CreateAggregatedUI();
            }

            UpdateGroup();
            return true;
        }

        /// <summary>
        /// 移除成员
        /// </summary>
        public bool RemoveMember(IAggregatable ui)
        {
            if (ui == null || !_members.Remove(ui))
                return false;

            // 仅在对象未被销毁时回调（外部销毁会触发 UnregisterUI → RemoveMember）
            var component = ui as WorldSpaceUIComponent;
            if (component != null && component)
            {
                ui.OnDeaggregated(this);
            }
            UpdateGroup();
            return true;
        }

        /// <summary>
        /// 更新聚合组（重新计算位置、更新展示UI）
        /// </summary>
        public void UpdateGroup()
        {
            if (_members.Count == 0)
            {
                DestroyAggregatedUI();
                return;
            }

            // 更新聚合展示UI（CenterPosition 是实时计算的，无需手动赋值）
            if (_aggregatedUI != null)
            {
                var aggregator = _aggregatedUI as IAggregatable;
                if (aggregator != null)
                {
                    aggregator.SetAggregationDisplay(this);
                }
            }
        }

        /// <summary>
        /// 验证成员是否仍在聚合范围内
        /// </summary>
        public void ValidateMembers(UIAggregationSystem system, bool markCooldown = true)
        {
            for (int i = _members.Count - 1; i >= 0; i--)
            {
                var member = _members[i];

                // Unity对象可能被外部销毁或隐藏，C#引用非null但底层已无效
                var component = member as WorldSpaceUIComponent;
                if (member == null || component == null || !component || !component.IsVisible)
                {
                    _members.RemoveAt(i);
                    system.RemoveUIFromMap(member);
                    continue;
                }

                // 检查是否超出解散距离
                Vector3 actualPos = component.transform.position;
                bool outOfRange;
                switch (_config.triggerMode)
                {
                    case AggregationTriggerMode.WorldDistance:
                        outOfRange = Vector3.Distance(CenterPosition, actualPos) > _config.disbandDistance;
                        break;
                    case AggregationTriggerMode.ScreenDistance:
                        // 有邻居（本地连通）+ 离中心不太远（防抱团脱离）
                        outOfRange = !HasNeighborInGroup(member, _config.screenDistanceThreshold)
                                  || IsFarFromCenter(actualPos, _config.screenDistanceThreshold * 3f);
                        break;
                    case AggregationTriggerMode.ScreenOverlap:
                        outOfRange = !HasNeighborInGroup(member, _config.screenOverlapThreshold)
                                  || IsFarFromCenter(actualPos, _config.screenOverlapThreshold * 3f);
                        break;
                    case AggregationTriggerMode.Both:
                        outOfRange = Vector3.Distance(CenterPosition, actualPos) > _config.disbandDistance
                                  || !HasNeighborInGroup(member, _config.screenDistanceThreshold)
                                  || IsFarFromCenter(actualPos, _config.screenDistanceThreshold * 3f);
                        break;
                    case AggregationTriggerMode.Either:
                    default:
                        outOfRange = Vector3.Distance(CenterPosition, actualPos) > _config.disbandDistance
                                  && !HasNeighborInGroup(member, _config.screenDistanceThreshold)
                                  || IsFarFromCenter(actualPos, _config.screenDistanceThreshold * 3f);
                        break;
                }

                if (outOfRange)
                {
                    _members.RemoveAt(i);
                    member.OnDeaggregated(this);
                    system.RemoveUIFromMap(member);
                    if (markCooldown)
                        system.MarkDetachCooldown(member);
                }
            }

            if (_members.Count > 0)
            {
                UpdateGroup();
            }
            else
            {
                DestroyAggregatedUI();
            }
        }

        /// <summary>
        /// 屏幕距离检查（解散用，阈值用 disbandDistance 作为归一化距离上限）
        /// </summary>
        private bool CheckScreenDistance(Vector3 worldPosA, Vector3 worldPosB)
        {
            var camera = WorldSpaceUIManager.Instance?.UICamera;
            if (camera == null) return false;
            var screenA = camera.WorldToScreenPoint(worldPosA);
            var screenB = camera.WorldToScreenPoint(worldPosB);
            if (screenA.z <= 0 || screenB.z <= 0) return false;
            float dx = (screenA.x - screenB.x) / Screen.height;
            float dy = (screenA.y - screenB.y) / Screen.height;
            return Mathf.Sqrt(dx * dx + dy * dy) < _config.screenDistanceThreshold;
        }

        /// <summary>
        /// 检查成员在组内是否至少有一个邻居在阈值范围内。
        /// 与聚类使用相同度量，确保一致性。
        /// </summary>
        private bool HasNeighborInGroup(IAggregatable member, float threshold)
        {
            if (_members.Count <= 1) return false;
            var camera = WorldSpaceUIManager.Instance?.UICamera;
            if (camera == null) return false;

            var mb = member as MonoBehaviour;
            if (mb == null) return false;
            var screenPos = camera.WorldToScreenPoint(mb.transform.position);
            if (screenPos.z <= 0) return false;

            foreach (var other in _members)
            {
                if (other == member) continue;
                var otherMb = other as MonoBehaviour;
                if (otherMb == null) continue;
                var otherScreen = camera.WorldToScreenPoint(otherMb.transform.position);
                if (otherScreen.z <= 0) continue;

                float dx = (screenPos.x - otherScreen.x) / Screen.height;
                float dy = (screenPos.y - otherScreen.y) / Screen.height;
                if (Mathf.Sqrt(dx * dx + dy * dy) < threshold)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 检查成员是否远离组中心（用于检测抱团脱离的子群）。
        /// 阈值很大（3x），只对明显分离的成员生效，不会误伤边缘成员。
        /// </summary>
        private bool IsFarFromCenter(Vector3 memberPos, float threshold)
        {
            var camera = WorldSpaceUIManager.Instance?.UICamera;
            if (camera == null) return false;
            var screenCenter = camera.WorldToScreenPoint(CenterPosition);
            var screenMember = camera.WorldToScreenPoint(memberPos);
            if (screenCenter.z <= 0 || screenMember.z <= 0) return false;
            float dx = (screenCenter.x - screenMember.x) / Screen.height;
            float dy = (screenCenter.y - screenMember.y) / Screen.height;
            return Mathf.Sqrt(dx * dx + dy * dy) >= threshold;
        }

        private IAggregatable GetMostImportantMember()
        {
            IAggregatable result = null;
            int maxPriority = int.MinValue;
            foreach (var member in _members)
            {
                if (member.AggregationPriority > maxPriority)
                {
                    maxPriority = member.AggregationPriority;
                    result = member;
                }
            }
            return result ?? _members[0];
        }

        /// <summary>
        /// 设置聚合展示UI预制体
        /// </summary>
        public void SetAggregatedPrefab(GameObject prefab)
        {
            _aggregatedPrefab = prefab;
            // 如果已创建展示UI，重建
            if (_aggregatedUI != null)
            {
                DestroyAggregatedUI();
                CreateAggregatedUI();
            }
        }

        /// <summary>
        /// 检查是否需要因生命周期超时而被解散
        /// </summary>
        public bool IsExpired()
        {
            return _config.groupLifetime > 0 &&
                   Time.time - _createTime >= _config.groupLifetime;
        }

        #endregion

        #region Position Calculation

        private Vector3 CalculateCenterPosition()
        {
            switch (_config.anchorMode)
            {
                case AggregationAnchorMode.FirstMember:
                    return GetMemberPosition(_members[0]);

                case AggregationAnchorMode.MostImportantMember:
                    var most = GetMostImportantMember();
                    return GetMemberPosition(most ?? _members[0]);

                case AggregationAnchorMode.ScreenCenter:
                    var camera = WorldSpaceUIManager.Instance?.UICamera;
                    if (camera == null) goto case AggregationAnchorMode.CenterOfGroup;

                    Vector3 screenSum = Vector3.zero;
                    int screenCount = 0;
                    foreach (var member in _members)
                    {
                        var screenPos = camera.WorldToScreenPoint(GetMemberPosition(member));
                        if (screenPos.z > 0)
                        {
                            screenSum += screenPos;
                            screenCount++;
                        }
                    }

                    if (screenCount > 0)
                    {
                        var avgScreen = screenSum / screenCount;
                        return camera.ScreenToWorldPoint(new Vector3(avgScreen.x, avgScreen.y,
                            camera.nearClipPlane + 1f));
                    }
                    goto case AggregationAnchorMode.CenterOfGroup;

                case AggregationAnchorMode.CenterOfGroup:
                default:
                    Vector3 sum = Vector3.zero;
                    foreach (var member in _members)
                    {
                        sum += GetMemberPosition(member);
                    }
                    return sum / _members.Count;
            }
        }

        /// <summary>
        /// 获取成员的实时世界位置（_worldPosition 对跟随目标的UI是陈旧的）
        /// </summary>
        private static Vector3 GetMemberPosition(IAggregatable member)
        {
            var mb = member as MonoBehaviour;
            return mb != null ? mb.transform.position : member.WorldPosition;
        }

        #endregion

        #region Aggregated UI Management

        private void CreateAggregatedUI()
        {
            if (_aggregatedPrefab == null || _uiGroup == null)
            {
                if (_aggregatedPrefab == null)
                {
                    Debug.LogWarning($"[AggregationGroup] 无法创建聚合展示UI: 预制体为null。类型={AggregationType}, 请确认 UIGroupConfig.aggregationConfig.aggregationPrefab 已设置");
                }
                return;
            }

            var manager = WorldSpaceUIManager.Instance;
            if (manager == null) return;

            // 从对象池获取聚合展示UI
            _aggregatedUI = manager.GetOrCreatePooledUI(_aggregatedPrefab, _uiGroup.Transform);
            if (_aggregatedUI == null)
            {
                Debug.LogError($"[AggregationGroup] 聚合展示预制体缺少WorldSpaceUIComponent: {_aggregatedPrefab.name}");
                return;
            }

            // 调用Initialize确保 _isInitialized=true，这样Update()中的UpdatePosition()才会执行
            _aggregatedUI.Initialize(manager, _uiGroup, CenterPosition);

            var aggregator = _aggregatedUI as IAggregatable;
            if (aggregator != null)
            {
                aggregator.AggregationState = AggregationStateType.Aggregator;
            }

            // 设置聚合展示UI的类型，使其与组内成员一致，确保级联合并时 CanAggregateWith 匹配
            if (_aggregatedUI is AggregatableUIComponent aggComp)
            {
                aggComp.SetAggregationType(AggregationType);
            }

            // 注册聚合展示UI，使其也能参与二级聚合
            manager.AggregationSystem?.RegisterUI(aggregator);
            manager.AggregationSystem?.RegisterAggregatorGroup(aggregator, this);
        }

        /// <summary>
        /// 吸收另一个聚合组的所有成员（不触发可见性回调，避免闪烁）
        /// </summary>
        public void MergeMembers(AggregationGroup other)
        {
            if (other == null) return;
            var members = new List<IAggregatable>(other.Members);
            foreach (var member in members)
            {
                // 直接从旧组移除成员（不调用 OnDeaggregated，避免 SetVisible(true) 造成闪烁）
                other._members.Remove(member);
                // 加入新组
                AddMember(member);
            }

            // 清理旧组
            if (other._members.Count == 0)
            {
                other.DestroyAggregatedUI();
            }
        }

        /// <summary>
        /// 直接移除成员（无回调，用于批量解散等场景）
        /// </summary>
        internal void RemoveMemberDirect(IAggregatable ui)
        {
            _members.Remove(ui);
        }

        private void DestroyAggregatedUI()
        {
            if (_aggregatedUI != null)
            {
                // 从聚合系统注销
                if (_aggregatedUI is IAggregatable aggregator)
                {
                    WorldSpaceUIManager.Instance?.AggregationSystem?.UnregisterAggregatorGroup(aggregator);
                    WorldSpaceUIManager.Instance?.AggregationSystem?.UnregisterUI(aggregator);
                }

                if (_aggregatedUI.gameObject != null)
                {
                    WorldSpaceUIManager.Instance?.ReturnToPool(_aggregatedUI);
                }
            }
            _aggregatedUI = null;
        }

        #endregion
    }
}
