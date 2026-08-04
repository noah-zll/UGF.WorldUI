using System;
using System.Collections.Generic;
using UnityEngine;

namespace UGF.WorldUI
{
    /// <summary>
    /// UI聚合系统主控制器 - 管理所有聚合组的创建、更新和解散
    /// </summary>
    public class UIAggregationSystem
    {
        #region Fields

        // 聚合组（GroupId → 聚合组列表，同一GroupId下可能有多个空间不连续的聚合组）
        private readonly Dictionary<string, List<AggregationGroup>> _aggregationGroupsByType
            = new Dictionary<string, List<AggregationGroup>>();

        // 已聚合的UI → 所属聚合组 映射
        private readonly Dictionary<IAggregatable, AggregationGroup> _uiToGroupMap
            = new Dictionary<IAggregatable, AggregationGroup>();

        // UI → 所属UIGroup 映射
        private readonly Dictionary<IAggregatable, UIGroup> _uiToUIGroupMap
            = new Dictionary<IAggregatable, UIGroup>();

        // 所有已注册的可聚合UI
        private readonly List<IAggregatable> _registeredUIs = new List<IAggregatable>();

        // 空间网格（快速邻近查询）
        private SpatialGrid _spatialGrid;

        // 更新控制
        private float _lastDetectionTime;

        // 邻近查询缓存（复用避免GC）
        private readonly List<IAggregatable> _nearbyCache = new List<IAggregatable>();

        // 聚合展示UI预制体映射（AggregationType → Prefab）
        private readonly Dictionary<string, GameObject> _aggregationPrefabMap
            = new Dictionary<string, GameObject>();

        // 聚合展示UI → 其所代表的聚合组 映射（用于级联聚合）
        private readonly Dictionary<IAggregatable, AggregationGroup> _aggregatorToGroupMap
            = new Dictionary<IAggregatable, AggregationGroup>();

        // UI离开聚合组的时间戳（冷静期，防止阈值边界反复横跳）
        private readonly Dictionary<IAggregatable, float> _detachCooldowns
            = new Dictionary<IAggregatable, float>();

        #endregion

        #region Properties

        /// <summary>当前聚合组总数</summary>
        public int GroupCount
        {
            get
            {
                int count = 0;
                foreach (var list in _aggregationGroupsByType.Values)
                {
                    count += list.Count;
                }
                return count;
            }
        }

        /// <summary>已注册UI总数</summary>
        public int RegisteredCount => _registeredUIs.Count;

        /// <summary>已被聚合的UI总数</summary>
        public int AggregatedCount => _uiToGroupMap.Count;

        #endregion

        #region Constructor

        public UIAggregationSystem()
        {
            var globalConfig = WorldSpaceUIManager.Instance?.GlobalConfig;
            float cellSize = globalConfig != null ? globalConfig.aggregationSpatialGridCellSize : 5f;
            _spatialGrid = new SpatialGrid(cellSize);
        }

        #endregion

        #region Prefab Registration

        /// <summary>
        /// 注册聚合展示UI预制体
        /// </summary>
        /// <param name="aggregationType">聚合类型</param>
        /// <param name="prefab">聚合展示UI预制体</param>
        public void RegisterAggregationPrefab(string aggregationType, GameObject prefab)
        {
            if (string.IsNullOrEmpty(aggregationType) || prefab == null)
            {
                Debug.LogError("[UIAggregationSystem] 聚合类型或预制体不能为空");
                return;
            }

            _aggregationPrefabMap[aggregationType] = prefab;
        }

        /// <summary>
        /// 获取聚合展示UI预制体
        /// </summary>
        public GameObject GetAggregationPrefab(string aggregationType)
        {
            _aggregationPrefabMap.TryGetValue(aggregationType, out var prefab);
            return prefab;
        }

        /// <summary>
        /// 根据IAggregatable获取聚合展示UI预制体
        /// 查询优先级：分组配置.aggregationPrefab > 系统级RegisterAggregationPrefab
        /// </summary>
        private GameObject GetAggregationPrefab(IAggregatable ui)
        {
            // 1. 先检查UIGroup配置中的聚合预制体
            if (_uiToUIGroupMap.TryGetValue(ui, out var uiGroup))
            {
                var effectiveConfig = uiGroup.GetEffectiveAggregationConfig();
                if (effectiveConfig != null && effectiveConfig.aggregationPrefab != null)
                {
                    return effectiveConfig.aggregationPrefab;
                }
            }

            // 2. 回退到系统级注册的预制体
            var sysPrefab = GetAggregationPrefab(ui.AggregationType);
            if (sysPrefab == null)
            {
                Debug.LogWarning($"[UIAggregationSystem] 聚合展示预制体未找到: Type={ui.AggregationType}。" +
                    "请在 UIGroupConfig.aggregationConfig.aggregationPrefab 中设置预制体，或调用 RegisterAggregationPrefab()。");
            }
            return sysPrefab;
        }

        #endregion

        #region Aggregator Mapping

        /// <summary>
        /// 注册聚合展示UI与其所代表的聚合组的映射（内部使用，用于级联聚合）
        /// </summary>
        internal void RegisterAggregatorGroup(IAggregatable aggregator, AggregationGroup group)
        {
            if (aggregator == null || group == null) return;
            _aggregatorToGroupMap[aggregator] = group;
        }

        /// <summary>
        /// 注销聚合展示UI的映射
        /// </summary>
        internal void UnregisterAggregatorGroup(IAggregatable aggregator)
        {
            _aggregatorToGroupMap.Remove(aggregator);
        }

        /// <summary>
        /// 获取聚合展示UI所代表的聚合组
        /// </summary>
        private AggregationGroup GetAggregatorGroup(IAggregatable aggregator)
        {
            _aggregatorToGroupMap.TryGetValue(aggregator, out var group);
            return group;
        }

        #endregion

        #region UI Registration

        /// <summary>
        /// 注册可聚合UI
        /// </summary>
        public void RegisterUI(IAggregatable ui)
        {
            if (ui == null) return;

            var component = ui as WorldSpaceUIComponent;
            if (component == null || component.Group == null) return;

            var group = component.Group;

            // 仅当分组启用聚合时才注册
            if (!group.IsAggregationEnabled()) return;

            // 避免重复注册
            if (_registeredUIs.Contains(ui)) return;

            _uiToUIGroupMap[ui] = group;
            _registeredUIs.Add(ui);
        }

        /// <summary>
        /// 注销可聚合UI
        /// </summary>
        public void UnregisterUI(IAggregatable ui)
        {
            if (ui == null) return;

            // 如果该UI已被聚合，先从聚合组中移除
            if (_uiToGroupMap.TryGetValue(ui, out var group))
            {
                group.RemoveMember(ui);
                _uiToGroupMap.Remove(ui);

                // 检查聚合组是否应解散
                if (group.MemberCount == 0)
                {
                    RemoveGroup(group);
                }
            }

            _registeredUIs.Remove(ui);
            _uiToUIGroupMap.Remove(ui);
        }

        /// <summary>
        /// 获取UI是否已被聚合
        /// </summary>
        public bool IsAggregated(IAggregatable ui)
        {
            return _uiToGroupMap.ContainsKey(ui);
        }

        /// <summary>
        /// 从聚合映射中移除UI（内部使用，成员离开聚合组时调用）
        /// </summary>
        internal void RemoveUIFromMap(IAggregatable ui)
        {
            _uiToGroupMap.Remove(ui);
        }

        /// <summary>
        /// 记录UI离开聚合组的时间（冷静期，防阈值边界反复横跳）
        /// </summary>
        internal void MarkDetachCooldown(IAggregatable ui)
        {
            _detachCooldowns[ui] = Time.time;
        }

        /// <summary>
        /// UI是否在冷静期内
        /// </summary>
        private bool IsInCooldown(IAggregatable ui)
        {
            if (!_detachCooldowns.TryGetValue(ui, out float detachTime))
                return false;
            var globalConfig = WorldSpaceUIManager.Instance?.GlobalConfig;
            float interval = globalConfig != null ? globalConfig.aggregationDetectionInterval : 0.2f;
            if (Time.time - detachTime < interval * 1.5f)
                return true;
            _detachCooldowns.Remove(ui);  // 冷静期过期，清理
            return false;
        }

        /// <summary>
        /// 获取UI所属的聚合组
        /// </summary>
        public AggregationGroup GetAggregationGroup(IAggregatable ui)
        {
            _uiToGroupMap.TryGetValue(ui, out var group);
            return group;
        }

        #endregion

        #region Update

        /// <summary>
        /// 更新聚合系统（由WorldSpaceUIManager.Update调用）
        /// </summary>
        public void Update()
        {
            var globalConfig = WorldSpaceUIManager.Instance?.GlobalConfig;
            if (globalConfig == null) return;
            if (!globalConfig.enableAggregation) return;
            if (Time.time - _lastDetectionTime < globalConfig.aggregationDetectionInterval) return;

            _lastDetectionTime = Time.time;

            UpdateSpatialGrid();
            DetectAndFormGroups();
            UpdateExistingGroups();
            MergeNearbyGroups();
            CleanupEmptyGroups();
        }

        #endregion

        #region Detection

        /// <summary>
        /// 获取UI的实时世界位置（_worldPosition 对跟随目标的UI是陈旧的）
        /// </summary>
        private static Vector3 GetActualPosition(IAggregatable ui)
        {
            var mb = ui as MonoBehaviour;
            return mb != null ? mb.transform.position : ui.WorldPosition;
        }

        private void UpdateSpatialGrid()
        {
            // 重建空间网格
            _spatialGrid.Clear();

            foreach (var ui in _registeredUIs)
            {
                if (ui == null) continue;
                // 已被聚合的UI不加入网格（不再参与检测）
                if (_uiToGroupMap.ContainsKey(ui)) continue;

                _spatialGrid.Insert(ui, GetActualPosition(ui));
            }
        }

        private void DetectAndFormGroups()
        {
            // 收集未聚合的UI（聚合展示UI不参与检测，它们已有自己的组）
            var candidates = new List<IAggregatable>();
            foreach (var ui in _registeredUIs)
            {
                if (ui != null && !_uiToGroupMap.ContainsKey(ui) && !IsInCooldown(ui)
                    && ui.AggregationState != AggregationStateType.Aggregator)
                {
                    candidates.Add(ui);
                }
            }

            if (candidates.Count == 0) return;

            // === 图聚类：构建连通图，不受处理顺序影响 ===
            int n = candidates.Count;
            var uf = new UnionFind(n);

            // 预计算屏幕位置
            var camera = WorldSpaceUIManager.Instance?.UICamera;
            var screenPositions = new Vector2?[n];
            if (camera != null)
            {
                for (int i = 0; i < n; i++)
                {
                    var pos = camera.WorldToScreenPoint(GetActualPosition(candidates[i]));
                    if (pos.z > 0) screenPositions[i] = new Vector2(pos.x, pos.y);
                }
            }

            // 两两比较，构建连通边
            for (int i = 0; i < n; i++)
            {
                var a = candidates[i];
                if (!_uiToUIGroupMap.TryGetValue(a, out var groupA)) continue;
                var configA = groupA.GetEffectiveAggregationConfig();

                for (int j = i + 1; j < n; j++)
                {
                    var b = candidates[j];
                    if (!_uiToUIGroupMap.TryGetValue(b, out var groupB)) continue;
                    if (groupA != groupB) continue;
                    if (!a.CanAggregateWith(b)) continue;

                    // 使用 configA 的阈值（同 UIGroup 配置一致）
                    if (IsWithinFormingThreshold(a, b, configA, screenPositions[i], screenPositions[j]))
                    {
                        uf.Union(i, j);
                    }
                }
            }

            // 按连通分量分组
            var componentMap = new Dictionary<int, List<int>>();
            for (int i = 0; i < n; i++)
            {
                int root = uf.Find(i);
                if (!componentMap.ContainsKey(root))
                    componentMap[root] = new List<int>();
                componentMap[root].Add(i);
            }

            // 为每个 ≥ minGroupSize 的分量创建聚合组，先分裂超半径的再建组
            var rejected = new List<int>(); // 被踢出的成员，将重新聚类
            foreach (var kvp in componentMap)
            {
                var indices = kvp.Value;
                var first = candidates[indices[0]];
                if (!_uiToUIGroupMap.TryGetValue(first, out var sourceGroup)) continue;
                var effectiveConfig = sourceGroup.GetEffectiveAggregationConfig();

                if (indices.Count < effectiveConfig.minGroupSize) continue;

                // 分裂超半径分量，使每个子组都稳定
                var result = SplitByRadius(indices, candidates, effectiveConfig, screenPositions);
                foreach (var sub in result.validClusters)
                {
                    if (sub.Count < effectiveConfig.minGroupSize)
                    {
                        rejected.AddRange(sub);
                        continue;
                    }

                    var prefab = GetAggregationPrefab(candidates[sub[0]]);
                    var group = new AggregationGroup(candidates[sub[0]].AggregationType, sourceGroup, effectiveConfig, prefab);

                    foreach (int idx in sub)
                    {
                        var ui = candidates[idx];
                        group.AddMember(ui);
                        _uiToGroupMap[ui] = group;
                    }

                    AddGroup(group);
                }
                rejected.AddRange(result.rejected);
            }

            // 将被踢出的成员重新聚类
            if (rejected.Count > 0)
            {
                ReclusterRejected(rejected, candidates, screenPositions);
            }

            // 剩余的孤立 UI（分量 < minGroupSize）尝试加入已有组
            foreach (var ui in candidates)
            {
                if (_uiToGroupMap.ContainsKey(ui)) continue;
                TryJoinExistingGroup(ui);
            }
        }

        private struct SplitResult
        {
            public List<List<int>> validClusters;
            public List<int> rejected;
        }

        /// <summary>
        /// 按半径分裂连通分量，使每个子组都在解散半径内，建组后不会被 ValidateMembers 拆散。
        /// </summary>
        private SplitResult SplitByRadius(List<int> indices, List<IAggregatable> candidates,
            UIAggregationConfig config, Vector2?[] screenPositions)
        {
            var result = new SplitResult { validClusters = new List<List<int>>(), rejected = new List<int>() };
            if (indices.Count <= 1)
            {
                result.validClusters.Add(new List<int>(indices));
                return result;
            }

            float disbandRadius = GetDisbandRadius(config);
            if (disbandRadius <= 0)
            {
                result.validClusters.Add(new List<int>(indices));
                return result;
            }

            var remaining = new List<int>(indices);

            while (remaining.Count > 0)
            {
                var center = ComputeScreenCenter(remaining, candidates, screenPositions);
                if (!center.HasValue)
                {
                    result.rejected.AddRange(remaining);
                    break;
                }

                // 找出离中心最远的成员
                int farthestIdx = -1;
                float maxDist = 0f;
                for (int i = remaining.Count - 1; i >= 0; i--)
                {
                    int idx = remaining[i];
                    var sp = screenPositions[idx];
                    if (!sp.HasValue) continue;
                    float d = Vector2.Distance(center.Value, sp.Value) / Screen.height;
                    if (d > maxDist) { maxDist = d; farthestIdx = i; }
                }

                if (maxDist <= disbandRadius)
                {
                    result.validClusters.Add(new List<int>(remaining));
                    break;
                }

                // 移除最远的成员
                result.rejected.Add(remaining[farthestIdx]);
                remaining.RemoveAt(farthestIdx);
            }

            return result;
        }

        /// <summary>
        /// 对被踢出的成员重新跑一次聚类，处理环形分裂后的剩余成员。
        /// </summary>
        private void ReclusterRejected(List<int> rejected, List<IAggregatable> candidates,
            Vector2?[] screenPositions)
        {
            int n = rejected.Count;
            var uf = new UnionFind(n);

            for (int i = 0; i < n; i++)
            {
                int ai = rejected[i];
                var a = candidates[ai];
                if (!_uiToUIGroupMap.TryGetValue(a, out var groupA)) continue;
                var configA = groupA.GetEffectiveAggregationConfig();

                for (int j = i + 1; j < n; j++)
                {
                    int bi = rejected[j];
                    var b = candidates[bi];
                    if (!_uiToUIGroupMap.TryGetValue(b, out var groupB)) continue;
                    if (groupA != groupB) continue;
                    if (!a.CanAggregateWith(b)) continue;

                    if (IsWithinFormingThreshold(a, b, configA, screenPositions[ai], screenPositions[bi]))
                    {
                        uf.Union(i, j);
                    }
                }
            }

            var compMap = new Dictionary<int, List<int>>();
            for (int i = 0; i < n; i++)
            {
                int root = uf.Find(i);
                if (!compMap.ContainsKey(root))
                    compMap[root] = new List<int>();
                compMap[root].Add(rejected[i]);
            }

            foreach (var kvp in compMap)
            {
                int firstIdx = kvp.Value[0];
                var first = candidates[firstIdx];
                if (!_uiToUIGroupMap.TryGetValue(first, out var sourceGroup)) continue;
                var effectiveConfig = sourceGroup.GetEffectiveAggregationConfig();

                if (kvp.Value.Count < effectiveConfig.minGroupSize) continue;

                var prefab = GetAggregationPrefab(first);
                var group = new AggregationGroup(first.AggregationType, sourceGroup, effectiveConfig, prefab);

                foreach (int idx in kvp.Value)
                {
                    var ui = candidates[idx];
                    group.AddMember(ui);
                    _uiToGroupMap[ui] = group;
                }

                AddGroup(group);
            }
        }

        private static Vector2? ComputeScreenCenter(List<int> indices, List<IAggregatable> candidates,
            Vector2?[] screenPositions)
        {
            Vector2 sum = Vector2.zero;
            int count = 0;
            foreach (int idx in indices)
            {
                var sp = screenPositions[idx];
                if (sp.HasValue) { sum += sp.Value; count++; }
            }
            return count > 0 ? sum / count : (Vector2?)null;
        }

        private static float GetDisbandRadius(UIAggregationConfig config)
        {
            switch (config.triggerMode)
            {
                case AggregationTriggerMode.WorldDistance:
                case AggregationTriggerMode.Both:
                case AggregationTriggerMode.Either:
                    return config.disbandDistance / 10f; // 世界距离折算屏幕（粗略）
                case AggregationTriggerMode.ScreenDistance:
                    return config.screenDistanceThreshold * 0.65f;
                case AggregationTriggerMode.ScreenOverlap:
                    return config.screenOverlapThreshold * 0.65f;
                default:
                    return 0f;
            }
        }
        private static bool IsWithinFormingThreshold(IAggregatable a, IAggregatable b,
            UIAggregationConfig config, Vector2? screenA, Vector2? screenB)
        {
            switch (config.triggerMode)
            {
                case AggregationTriggerMode.WorldDistance:
                    return Vector3.Distance(GetActualPosition(a), GetActualPosition(b)) <= config.disbandDistance;
                case AggregationTriggerMode.ScreenDistance:
                    // 用解散阈值建边，保证组一旦形成就是稳定的
                    return screenA.HasValue && screenB.HasValue
                        && ScreenDistance(screenA.Value, screenB.Value) < config.screenDistanceThreshold * 0.65f;
                case AggregationTriggerMode.ScreenOverlap:
                    return screenA.HasValue && screenB.HasValue
                        && ScreenOverlapDistance(screenA.Value, screenB.Value) < config.screenOverlapThreshold * 0.65f;
                case AggregationTriggerMode.Both:
                    return Vector3.Distance(GetActualPosition(a), GetActualPosition(b)) <= config.disbandDistance
                        && screenA.HasValue && screenB.HasValue
                        && ScreenDistance(screenA.Value, screenB.Value) < config.screenDistanceThreshold * 0.65f;
                case AggregationTriggerMode.Either:
                default:
                    return Vector3.Distance(GetActualPosition(a), GetActualPosition(b)) <= config.disbandDistance
                        || (screenA.HasValue && screenB.HasValue
                            && ScreenDistance(screenA.Value, screenB.Value) < config.screenDistanceThreshold * 0.65f);
            }
        }

        private static float ScreenDistance(Vector2 a, Vector2 b)
        {
            float dx = (a.x - b.x) / Screen.height;
            float dy = (a.y - b.y) / Screen.height;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private static float ScreenOverlapDistance(Vector2 a, Vector2 b)
        {
            float dx = (a.x - b.x) / Screen.width;
            float dy = (a.y - b.y) / Screen.height;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 并查集，用于连通分量计算
        /// </summary>
        private class UnionFind
        {
            private readonly int[] _parent;
            public UnionFind(int n) { _parent = new int[n]; for (int i = 0; i < n; i++) _parent[i] = i; }
            public int Find(int x) { return _parent[x] == x ? x : (_parent[x] = Find(_parent[x])); }
            public void Union(int a, int b) { _parent[Find(a)] = Find(b); }
        }

        /// <summary>
        /// 尝试将游离UI加入已有聚合组
        /// </summary>
        private bool TryJoinExistingGroup(IAggregatable ui)
        {
            // 聚合展示UI不加入已有组
            if (ui.AggregationState == AggregationStateType.Aggregator)
                return false;

            if (!_uiToUIGroupMap.TryGetValue(ui, out var uiGroup))
                return false;

            foreach (var kvp in _aggregationGroupsByType)
            {
                foreach (var group in kvp.Value)
                {
                    if (group.UIGroup != uiGroup) continue;
                    if (group.MemberCount >= group.Config.maxGroupSize) continue;

                    // 检查该UI是否能与组内成员聚合
                    if (group.Members.Count > 0 && !ui.CanAggregateWith(group.Members[0]))
                        continue;

                    // 检查与组中心的距离（屏幕模式用屏幕距离，世界模式用世界距离）
                    if (!PassesGroupDistanceCheck(ui, group))
                        continue;

                    // 加入聚合组
                    group.AddMember(ui);
                    _uiToGroupMap[ui] = group;
                    return true;
                }
            }
            return false;
        }

        private bool TryFormGroup(IAggregatable source)
        {
            if (!_uiToUIGroupMap.TryGetValue(source, out var sourceGroup))
                return false;

            // 使用该分组生效的聚合配置（分组配置 > 全局配置）
            var effectiveConfig = sourceGroup.GetEffectiveAggregationConfig();

            // 屏幕空间模式：直接遍历所有注册UI，不用空间网格（世界距离无意义）
            if (IsScreenBasedTrigger(effectiveConfig))
            {
                _nearbyCache.Clear();
                foreach (var ui in _registeredUIs)
                {
                    if (ui != null && ui != source && !_uiToGroupMap.ContainsKey(ui))
                    {
                        _nearbyCache.Add(ui);
                    }
                }
            }
            else
            {
                _spatialGrid.QueryNearby(GetActualPosition(source), effectiveConfig.worldDistance, _nearbyCache);
            }

            // 筛选同类型 + 自定义判断 + 同分组 + 满足距离条件 的候选
            var matchingCandidates = new List<IAggregatable>();
            foreach (var candidate in _nearbyCache)
            {
                if (candidate == source) continue;
                if (_uiToGroupMap.ContainsKey(candidate)) continue;

                if (!source.CanAggregateWith(candidate))
                    continue;

                if (!_uiToUIGroupMap.TryGetValue(candidate, out var candidateGroup))
                    continue;

                if (candidateGroup != sourceGroup)
                    continue;

                if (PassesDistanceCheck(source, candidate, effectiveConfig))
                {
                    matchingCandidates.Add(candidate);
                }
            }

            // 聚合展示UI仅在与另一个聚合展示UI级联时才形成新组
            if (source.AggregationState == AggregationStateType.Aggregator)
            {
                bool hasOtherAggregator = false;
                foreach (var c in matchingCandidates)
                {
                    if (c.AggregationState == AggregationStateType.Aggregator)
                    {
                        hasOtherAggregator = true;
                        break;
                    }
                }
                if (!hasOtherAggregator)
                    return false;
            }

            // 检查是否满足最小成员数
            if (matchingCandidates.Count + 1 < effectiveConfig.minGroupSize)
                return false;

            // 创建聚合组
            var prefab = GetAggregationPrefab(source);
            var group = new AggregationGroup(
                source.AggregationType,
                sourceGroup,
                effectiveConfig,
                prefab
            );

            // source 可能是聚合展示UI，先合并其原组成员
            var sourceGroupOld = GetAggregatorGroup(source);
            if (source.AggregationState == AggregationStateType.Aggregator && sourceGroupOld == null)
            {
                // 聚合展示UI的组已被同一帧内其他source合并，跳过
                return false;
            }

            if (sourceGroupOld != null)
            {
                var sourceMembers = new List<IAggregatable>(sourceGroupOld.Members);
                group.MergeMembers(sourceGroupOld);
                RemoveGroup(sourceGroupOld);
                foreach (var m in sourceMembers)
                {
                    _uiToGroupMap[m] = group;
                }
            }
            else
            {
                group.AddMember(source);
            }
            AddGroup(group);

            foreach (var candidate in matchingCandidates)
            {
                if (group.MemberCount >= effectiveConfig.maxGroupSize)
                    break;

                // 如果候选是聚合展示UI，合并其代表的聚合组
                var aggregatorGroup = GetAggregatorGroup(candidate);
                if (aggregatorGroup != null)
                {
                    var oldMembers = new List<IAggregatable>(aggregatorGroup.Members);
                    group.MergeMembers(aggregatorGroup);
                    RemoveGroup(aggregatorGroup);

                    // 更新合并成员的映射
                    foreach (var oldMember in oldMembers)
                    {
                        _uiToGroupMap[oldMember] = group;
                    }
                }
                else
                {
                    group.AddMember(candidate);
                    _uiToGroupMap[candidate] = group;
                }
            }

            // 仅当source不是聚合展示UI时才加入映射
            if (sourceGroupOld == null)
            {
                _uiToGroupMap[source] = group;
            }

            return true;
        }

        private bool PassesDistanceCheck(IAggregatable a, IAggregatable b, UIAggregationConfig config)
        {
            switch (config.triggerMode)
            {
                case AggregationTriggerMode.WorldDistance:
                    return true; // 空间网格已做世界距离筛选

                case AggregationTriggerMode.ScreenDistance:
                    return CheckScreenDistance(a, b, config);

                case AggregationTriggerMode.ScreenOverlap:
                    return CheckScreenOverlap(a, b, config);

                case AggregationTriggerMode.Both:
                    // 空间网格已筛世界距离（IsScreenBasedTrigger 排除了 Both），此处只需屏幕距离
                    return CheckScreenDistance(a, b, config);

                case AggregationTriggerMode.Either:
                default:
                    // 遍历全部UI，世界距离或屏幕距离任一满足即可
                    return CheckWorldDistance(a, b, config) || CheckScreenDistance(a, b, config);
            }
        }

        private static bool CheckWorldDistance(IAggregatable a, IAggregatable b, UIAggregationConfig config)
        {
            return Vector3.Distance(GetActualPosition(a), GetActualPosition(b)) <= config.worldDistance;
        }

        /// <summary>
        /// 是否需要遍历全部UI做候选收集。
        /// Both 模式用空间网格先筛世界距离，不在此列。
        /// </summary>
        private static bool IsScreenBasedTrigger(UIAggregationConfig config)
        {
            return config.triggerMode == AggregationTriggerMode.ScreenDistance
                || config.triggerMode == AggregationTriggerMode.ScreenOverlap
                || config.triggerMode == AggregationTriggerMode.Either;
        }

        /// <summary>
        /// 检查UI与聚合组的距离。聚类已完成主体分组，此处仅需检查与组内成员的邻近度。
        /// </summary>
        private bool PassesGroupDistanceCheck(IAggregatable ui, AggregationGroup group)
        {
            var config = group.Config;
            Vector3 pos = GetActualPosition(ui);
            switch (config.triggerMode)
            {
                case AggregationTriggerMode.WorldDistance:
                    return Vector3.Distance(pos, group.CenterPosition) <= config.worldDistance;
                case AggregationTriggerMode.ScreenDistance:
                    return IsCloseToAnyMember(pos, group, config.screenDistanceThreshold);
                case AggregationTriggerMode.ScreenOverlap:
                    return IsCloseToAnyMember(pos, group, config.screenOverlapThreshold);
                case AggregationTriggerMode.Both:
                    return Vector3.Distance(pos, group.CenterPosition) <= config.worldDistance
                        && IsCloseToAnyMember(pos, group, config.screenDistanceThreshold);
                case AggregationTriggerMode.Either:
                default:
                    return Vector3.Distance(pos, group.CenterPosition) <= config.worldDistance
                        || IsCloseToAnyMember(pos, group, config.screenDistanceThreshold);
            }
        }

        /// <summary>
        /// 检查 worldPos 是否与组内任一成员的屏幕距离在阈值内
        /// </summary>
        private static bool IsCloseToAnyMember(Vector3 worldPos, AggregationGroup group, float threshold)
        {
            var camera = WorldSpaceUIManager.Instance?.UICamera;
            if (camera == null) return false;

            var screenPos = camera.WorldToScreenPoint(worldPos);
            if (screenPos.z <= 0) return false;

            foreach (var member in group.Members)
            {
                var mb = member as MonoBehaviour;
                if (mb == null) continue;
                var memberScreen = camera.WorldToScreenPoint(mb.transform.position);
                if (memberScreen.z <= 0) continue;

                float dx = (screenPos.x - memberScreen.x) / Screen.height;
                float dy = (screenPos.y - memberScreen.y) / Screen.height;
                if (Mathf.Sqrt(dx * dx + dy * dy) < threshold)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 屏幕距离检查（重载，接受世界坐标）
        /// </summary>
        private static bool CheckScreenDistance(Vector3 worldPosA, Vector3 worldPosB, UIAggregationConfig config)
        {
            var camera = WorldSpaceUIManager.Instance?.UICamera;
            if (camera == null) return false;
            var screenA = camera.WorldToScreenPoint(worldPosA);
            var screenB = camera.WorldToScreenPoint(worldPosB);
            if (screenA.z <= 0 || screenB.z <= 0) return false;
            float dx = (screenA.x - screenB.x) / Screen.height;
            float dy = (screenA.y - screenB.y) / Screen.height;
            return Mathf.Sqrt(dx * dx + dy * dy) < config.screenDistanceThreshold;
        }

        /// <summary>
        /// 屏幕距离检查（接受显式阈值，用于 ScreenOverlap 等自定义阈值的模式）
        /// </summary>
        private static bool CheckScreenDistanceWithThreshold(Vector3 worldPosA, Vector3 worldPosB, float threshold)
        {
            var camera = WorldSpaceUIManager.Instance?.UICamera;
            if (camera == null) return false;
            var screenA = camera.WorldToScreenPoint(worldPosA);
            var screenB = camera.WorldToScreenPoint(worldPosB);
            if (screenA.z <= 0 || screenB.z <= 0) return false;
            float dx = (screenA.x - screenB.x) / Screen.height;
            float dy = (screenA.y - screenB.y) / Screen.height;
            return Mathf.Sqrt(dx * dx + dy * dy) < threshold;
        }

        private static bool CheckScreenDistance(IAggregatable a, IAggregatable b, UIAggregationConfig config)
        {
            var camera = WorldSpaceUIManager.Instance?.UICamera;
            if (camera == null) return false;

            var screenA = camera.WorldToScreenPoint(GetActualPosition(a));
            var screenB = camera.WorldToScreenPoint(GetActualPosition(b));

            if (screenA.z <= 0 || screenB.z <= 0) return false;

            float dx = (screenA.x - screenB.x) / Screen.height;
            float dy = (screenA.y - screenB.y) / Screen.height;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);

            return dist < config.screenDistanceThreshold;
        }

        private static bool CheckScreenOverlap(IAggregatable a, IAggregatable b, UIAggregationConfig config)
        {
            var camera = WorldSpaceUIManager.Instance?.UICamera;
            if (camera == null) return false;

            var screenA = camera.WorldToScreenPoint(GetActualPosition(a));
            var screenB = camera.WorldToScreenPoint(GetActualPosition(b));

            if (screenA.z <= 0 || screenB.z <= 0) return false;

            float dist = Vector2.Distance(
                new Vector2(screenA.x / Screen.width, screenA.y / Screen.height),
                new Vector2(screenB.x / Screen.width, screenB.y / Screen.height)
            );

            return dist < config.screenOverlapThreshold;
        }

        #endregion

        #region Group Management

        private void AddGroup(AggregationGroup group)
        {
            var groupId = group.GroupId;
            if (!_aggregationGroupsByType.TryGetValue(groupId, out var list))
            {
                list = new List<AggregationGroup>();
                _aggregationGroupsByType[groupId] = list;
            }
            list.Add(group);
        }

        private void RemoveGroup(AggregationGroup group)
        {
            var groupId = group.GroupId;
            if (_aggregationGroupsByType.TryGetValue(groupId, out var list))
            {
                list.Remove(group);
                if (list.Count == 0)
                {
                    _aggregationGroupsByType.Remove(groupId);
                }
            }
        }

        private void UpdateExistingGroups()
        {
            // 先收集需要解散的组，避免遍历时修改字典
            var groupsToDisband = new List<AggregationGroup>();

            foreach (var kvp in _aggregationGroupsByType)
            {
                for (int i = kvp.Value.Count - 1; i >= 0; i--)
                {
                    var group = kvp.Value[i];

                    // 检查生命周期超时
                    if (group.IsExpired())
                    {
                        groupsToDisband.Add(group);
                        continue;
                    }

                    // 更新聚合组中心位置（动态跟随成员移动）
                    if (group.MemberCount > 0)
                    {
                        group.UpdateGroup();
                    }

                    // 验证成员是否仍在聚合范围内
                    group.ValidateMembers(this);

                    // 检查是否低于最小成员数
                    if (group.MemberCount < group.Config.minGroupSize)
                    {
                        groupsToDisband.Add(group);
                    }
                }
            }

            // 统一解散收集到的组
            foreach (var group in groupsToDisband)
            {
                DisbandGroupInternal(group);
            }
        }

        private void MergeNearbyGroups()
        {
            foreach (var kvp in _aggregationGroupsByType)
            {
                var groups = kvp.Value;
                for (int i = groups.Count - 1; i >= 0; i--)
                {
                    var groupA = groups[i];
                    if (groupA == null || groupA.MemberCount == 0) continue;
                    var configA = groupA.Config;

                    for (int j = i - 1; j >= 0; j--)
                    {
                        var groupB = groups[j];
                        if (groupB == null || groupB.MemberCount == 0) continue;
                        if (groupA.UIGroup != groupB.UIGroup) continue;
                        if (groupA.MemberCount + groupB.MemberCount > configA.maxGroupSize) continue;

                        // 检查两组是否可合并（类型兼容 + 距离满足）
                        if (groupA.Members.Count > 0 && groupB.Members.Count > 0
                            && !groupA.Members[0].CanAggregateWith(groupB.Members[0]))
                            continue;

                        if (AreGroupCentersClose(groupA, groupB))
                        {
                            MergeGroupInto(groupB, groupA);
                            break; // groupB 已销毁，跳出内层循环
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 检查两个组的中心距离是否满足合并条件。
        /// 使用解散阈值（而非形成阈值），确保合并后成员都在有效范围内，不会立即被踢出。
        /// </summary>
        private bool AreGroupCentersClose(AggregationGroup a, AggregationGroup b)
        {
            var config = a.Config;
            switch (config.triggerMode)
            {
                case AggregationTriggerMode.WorldDistance:
                    return Vector3.Distance(a.CenterPosition, b.CenterPosition) <= config.disbandDistance;
                case AggregationTriggerMode.ScreenDistance:
                    // 解散用 0.65 倍，合并也用 0.65 倍确保稳定性
                    return CheckScreenDistanceWithThreshold(a.CenterPosition, b.CenterPosition,
                        config.screenDistanceThreshold * 0.65f);
                case AggregationTriggerMode.ScreenOverlap:
                    return CheckScreenDistanceWithThreshold(a.CenterPosition, b.CenterPosition,
                        config.screenOverlapThreshold * 0.65f);
                case AggregationTriggerMode.Both:
                    return Vector3.Distance(a.CenterPosition, b.CenterPosition) <= config.disbandDistance
                        && CheckScreenDistanceWithThreshold(a.CenterPosition, b.CenterPosition,
                            config.screenDistanceThreshold * 0.65f);
                case AggregationTriggerMode.Either:
                default:
                    return Vector3.Distance(a.CenterPosition, b.CenterPosition) <= config.disbandDistance
                        || CheckScreenDistanceWithThreshold(a.CenterPosition, b.CenterPosition,
                            config.screenDistanceThreshold * 0.65f);
            }
        }

        /// <summary>
        /// 将 from 的所有成员合并到 to 中，然后销毁 from
        /// </summary>
        private void MergeGroupInto(AggregationGroup from, AggregationGroup to)
        {
            var members = new List<IAggregatable>(from.Members);
            foreach (var member in members)
            {
                // 直接从 from 移除（不触发 OnDeaggregated，避免闪烁）
                from.RemoveMemberDirect(member);
                to.AddMember(member);
                _uiToGroupMap[member] = to;
            }
            from.UpdateGroup(); // 触发 DestroyAggregatedUI
            RemoveGroup(from);

            // 合并后立即验证，但不加冷却（是结构性清理，非振荡防护）
            to.ValidateMembers(this, false);
        }

        private void CleanupEmptyGroups()
        {
            foreach (var kvp in _aggregationGroupsByType)
            {
                kvp.Value.RemoveAll(g =>
                {
                    if (g.MemberCount == 0)
                    {
                        foreach (var ui in g.Members)
                        {
                            _uiToGroupMap.Remove(ui);
                        }
                        return true;
                    }
                    return false;
                });
            }

            // 清理空列表
            var emptyKeys = new List<string>();
            foreach (var kvp in _aggregationGroupsByType)
            {
                if (kvp.Value.Count == 0)
                {
                    emptyKeys.Add(kvp.Key);
                }
            }
            foreach (var key in emptyKeys)
            {
                _aggregationGroupsByType.Remove(key);
            }
        }

        private void DisbandGroupInternal(AggregationGroup group)
        {
            // 释放所有成员
            var members = new List<IAggregatable>(group.Members);
            foreach (var member in members)
            {
                _uiToGroupMap.Remove(member);
                _detachCooldowns[member] = Time.time;

                var component = member as WorldSpaceUIComponent;
                if (component != null && component)
                {
                    member.OnDeaggregated(group);
                }
                group.RemoveMemberDirect(member);
            }
            group.UpdateGroup();  // 触发聚合展示UI回收
            RemoveGroup(group);
        }

        #endregion

        #region Force Operations

        /// <summary>
        /// 解散指定分组下的所有聚合组
        /// </summary>
        public void DisbandGroupsInGroup(UIGroup uiGroup)
        {
            if (uiGroup == null) return;

            var groupsToDisband = new List<AggregationGroup>();
            foreach (var kvp in _aggregationGroupsByType)
            {
                foreach (var group in kvp.Value)
                {
                    if (group.UIGroup == uiGroup)
                    {
                        groupsToDisband.Add(group);
                    }
                }
            }

            foreach (var group in groupsToDisband)
            {
                DisbandGroupInternal(group);
            }
        }

        /// <summary>
        /// 强制解散指定聚合组
        /// </summary>
        public void DisbandGroup(AggregationGroup group)
        {
            if (group == null) return;
            DisbandGroupInternal(group);
        }

        /// <summary>
        /// 强制解散所有聚合组
        /// </summary>
        public void DisbandAll()
        {
            var allGroups = new List<AggregationGroup>();
            foreach (var kvp in _aggregationGroupsByType)
            {
                allGroups.AddRange(kvp.Value);
            }

            foreach (var group in allGroups)
            {
                DisbandGroupInternal(group);
            }
        }

        #endregion

        #region Dispose

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            DisbandAll();
            _registeredUIs.Clear();
            _uiToGroupMap.Clear();
            _uiToUIGroupMap.Clear();
            _aggregationPrefabMap.Clear();
            _spatialGrid?.Clear();
            _nearbyCache.Clear();
        }

        #endregion
    }
}
