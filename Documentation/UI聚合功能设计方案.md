# UGF.WorldUI - UI聚合功能设计方案

## 1. 功能概述

### 1.1 问题背景

在游戏中，当大量同类型世界空间UI元素（如血条、伤害数字、交互提示）聚集在相近位置时，会出现以下问题：

- **视觉混乱**：大量UI重叠遮挡，玩家难以分辨
- **性能浪费**：大量重叠UI仍然各自更新和渲染
- **信息冗余**：同一位置的多个同类UI传递的信息重复低效

### 1.2 解决方案

UI聚合（UI Aggregation）功能在运行时动态检测空间上邻近的同类UI元素，将其合并为一个聚合UI元素，统一展示聚合信息（如数量、"xN"、总计值等）。当聚合组内的UI元素移开或销毁后，聚合自动解除。

### 1.3 典型场景

| 场景 | 聚合前 | 聚合后 |
|------|--------|--------|
| 怪物群血条 | N个独立血条重叠 | 1个聚合血条（显示数量+总计） |
| 批量伤害数字 | N个伤害数字堆叠 | 1个"xN"累计伤害数字 |
| 交互提示 | N个"按E交互"重叠 | 1个"附近可交互"提示 |
| 名称标签 | N个名称重叠 | 1个聚合名称（"怪物 x5"） |

---

## 2. 核心架构

### 2.1 系统组成

```
UIAggregationSystem (聚合系统)
├── UIAggregationConfig (聚合配置)
├── AggregationGroup (聚合组) - 一组被聚合的UI
│   ├── SourceUIs (原始UI列表)
│   ├── AggregatedUI (聚合后的展示UI)
│   └── AggregationState (聚合状态)
├── ProximityDetector (邻近检测器)
│   ├── SpatialGrid (空间网格)
│   └── OverlapDetector (屏幕重叠检测)
└── AggregationRenderer (聚合渲染器)
    └── AggregatedDisplay (聚合显示策略)
```

### 2.2 与现有系统的关系

```
WorldSpaceUIManager (现有)
├── UIGroup ─────────── 关联 ──→ UIAggregationSystem (新增)
├── CullingSystem                        │
├── UpdateScheduler                      │
└── UIObjectPoolManager                  │
                                         │
WorldSpaceUIComponent (现有)             │
├── 新增 IAggregatable 接口              │
└── 新增 AggregationState 状态           │
```

### 2.3 类职责设计

#### UIAggregationSystem
- **命名空间**: `UGF.WorldUI`
- **职责**: 聚合主控制器，管理所有聚合组
- **功能**:
  - 注册/注销可聚合UI
  - 定期执行邻近检测
  - 创建/解散聚合组
  - 聚合UI生命周期管理

#### AggregationGroup
- **命名空间**: `UGF.WorldUI`
- **职责**: 代表一个聚合组，管理被聚合的源UI和聚合展示UI
- **功能**:
  - 维护源UI列表
  - 生成/更新聚合展示UI
  - 处理成员加入/离开
  - 计算聚合位置和显示数据

#### ProximityDetector
- **命名空间**: `UGF.WorldUI`
- **职责**: 空间邻近检测
- **功能**:
  - 基于空间哈希网格的快速邻近查询
  - 屏幕空间重叠检测
  - 世界空间距离检测

---

## 3. 聚合策略

### 3.1 聚合触发条件（可配置）

```csharp
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
```

#### 各模式对镜头缩放的响应

| 模式 | 拉远（缩小） | 拉近（放大） | 适用场景 |
|------|-------------|-------------|----------|
| `WorldDistance` | 无变化 | 无变化 | 不希望聚合状态随镜头变化 |
| `ScreenDistance` | **世界距离不变，屏幕距离变小 → 更易触发聚合** | **屏幕距离变大 → 更易解散聚合** | 需要随镜头缩放动态聚合/解散 |
| `ScreenOverlap` | **UI变密 → 更易重叠触发聚合** | **UI变疏 → 重叠消失自动解散** | 密集UI重叠时聚合 |
| `Both` | 仅当世界距离+屏幕距离都满足才触发 | 同左 | 保守策略，减少频繁切换 |
| `Either` | 世界距离不变所以始终满足 → 等效于 WorldDistance | 同左 | — |

> **推荐**：需要"放大解散、缩小聚合"的场景，使用 `ScreenDistance` 或 `ScreenOverlap` 模式。这两种模式的核心逻辑是**将世界坐标投影到屏幕空间后进行比较**，因此当镜头拉远时屏幕上的UI间距缩小，自然触发聚合；镜头拉近时屏幕间距扩大，自然触发解散。

### 3.2 聚合展示策略

```csharp
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
```

### 3.3 聚合位置计算策略

```csharp
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
```

### 3.4 聚合解散条件

- 聚合组成员数低于最小阈值（`minGroupSize`，默认2）
- 聚合组成员间距离超过解散阈值（`disbandDistance`）
- 聚合组成员被销毁
- 聚合组总生命周期超时（`groupLifetime`，0=永久）

---

## 4. 详细设计

### 4.1 IAggregatable 接口

```csharp
namespace UGF.WorldUI
{
    /// <summary>
    /// 可聚合UI组件接口 - 实现此接口的WorldSpaceUIComponent可参与聚合
    /// </summary>
    public interface IAggregatable
    {
        /// <summary>聚合类型标识（同类型才能聚合，如 "HealthBar"、"DamageText"）</summary>
        string AggregationType { get; }

        /// <summary>
        /// 聚合子键（同类型但内容不同的UI不聚合）。
        /// 如 Boss血条="Boss"、小怪血条="Minion"、NPC血条="NPC"，
        /// 三者虽然 AggregationType 都是 "HealthBar"，但 Key 不同 → 不聚合。
        /// 默认返回 null 表示仅按 AggregationType 分组。
        /// </summary>
        string AggregationKey { get; }

        /// <summary>用于聚合计算的值（如血条当前值、伤害数字值）</summary>
        float AggregationValue { get; }

        /// <summary>在聚合中显示的优先级（越高越优先被选为代表）</summary>
        int AggregationPriority { get; }

        /// <summary>获取聚合展示数据（由聚合系统调用）</summary>
        object GetAggregationDisplayData();

        /// <summary>被聚合时调用（原始UI进入聚合状态）</summary>
        void OnAggregated(AggregationGroup group);

        /// <summary>从聚合中释放时调用（原始UI退出聚合状态）</summary>
        void OnDeaggregated(AggregationGroup group);
    }

    /// <summary>
    /// 可聚合UI基类（便捷基类，替代直接实现IAggregatable）
    /// </summary>
    public abstract class AggregatableUIComponent : WorldSpaceUIComponent, IAggregatable
    {
        [Header("聚合设置")]
        [SerializeField] protected string _aggregationType;
        [SerializeField] protected string _aggregationKey;
        [SerializeField] protected int _aggregationPriority = 0;

        public string AggregationType => _aggregationType;
        public string AggregationKey => _aggregationKey;
        public abstract float AggregationValue { get; }
        public int AggregationPriority => _aggregationPriority;

        public abstract object GetAggregationDisplayData();

        public virtual void OnAggregated(AggregationGroup group)
        {
            // 默认：进入聚合时隐藏自身
            SetVisible(false);
        }

        public virtual void OnDeaggregated(AggregationGroup group)
        {
            // 默认：退出聚合时恢复显示
            SetVisible(true);
        }
    }
}
```

#### 聚合匹配规则

两个UI能否聚合需**同时满足**以下条件：

```
1. AggregationType 相同          （都是 "HealthBar"）
2. AggregationKey 相同            （都是 null 或都是 "Boss"）
3. 属于同一个 UIGroup             （都在 "EnemyUI" 分组）
4. 满足 triggerMode 的距离/重叠条件
```

> **AggregationKey 设计意图**：`AggregationType` 定义"可聚合族"（如所有血条），`AggregationKey` 在族内再切分出"可聚合子类"。Key 为 null 时退化为仅按 Type 分组。Key 为 `""`（空字符串）与 null 语义相同。

### 4.2 UIAggregationConfig 配置类

```csharp
namespace UGF.WorldUI
{
    [Serializable]
    public class UIAggregationConfig
    {
        [Header("基础设置")]
        [Tooltip("全局启用/禁用聚合")]
        public bool enableAggregation = true;

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

        [Header("性能设置")]
        [Tooltip("聚合检测间隔（秒）")]
        [Range(0.05f, 2f)]
        public float detectionInterval = 0.2f;

        [Tooltip("每帧最大检测的UI数量")]
        [Range(10, 500)]
        public int maxDetectionsPerFrame = 100;

        [Tooltip("空间网格单元大小（世界空间）")]
        [Range(1f, 50f)]
        public float spatialGridCellSize = 5f;

        [Header("调试")]
        [Tooltip("显示聚合调试信息")]
        public bool showDebugInfo = false;

        [Tooltip("在Scene视图中显示聚合组边界")]
        public bool showAggregationBounds = false;

        public static UIAggregationConfig CreateDefault()
        {
            return new UIAggregationConfig();
        }
    }
}
```

### 4.3 UIAggregationSystem 核心类

```csharp
namespace UGF.WorldUI
{
    public class UIAggregationSystem
    {
        // 聚合组列表
        private readonly Dictionary<string, List<AggregationGroup>> _aggregationGroupsByType
            = new Dictionary<string, List<AggregationGroup>>();

        // 已聚合的UI → 所属聚合组 映射
        private readonly Dictionary<IAggregatable, AggregationGroup> _uiToGroupMap
            = new Dictionary<IAggregatable, AggregationGroup>();

        // 所有可聚合UI注册表
        private readonly List<IAggregatable> _registeredUIs = new List<IAggregatable>();

        // 空间网格（快速邻近查询）
        private SpatialGrid _spatialGrid;

        // 配置
        private readonly UIAggregationConfig _config;

        // 更新控制
        private float _lastDetectionTime;

        #region Public API

        /// <summary>注册可聚合UI</summary>
        public void RegisterUI(IAggregatable ui);
        /// <summary>注销可聚合UI</summary>
        public void UnregisterUI(IAggregatable ui);
        /// <summary>获取UI是否已被聚合</summary>
        public bool IsAggregated(IAggregatable ui);
        /// <summary>获取UI所属的聚合组</summary>
        public AggregationGroup GetAggregationGroup(IAggregatable ui);
        /// <summary>强制解散聚合组</summary>
        public void DisbandGroup(AggregationGroup group);
        /// <summary>强制解散所有聚合组</summary>
        public void DisbandAll();

        #endregion

        #region Update

        /// <summary>更新聚合系统（由WorldSpaceUIManager.Update调用）</summary>
        public void Update()
        {
            if (!_config.enableAggregation) return;
            if (Time.time - _lastDetectionTime < _config.detectionInterval) return;

            _lastDetectionTime = Time.time;
            UpdateSpatialGrid();
            DetectAndFormGroups();
            UpdateExistingGroups();
            CleanupEmptyGroups();
        }

        #endregion
    }
}
```

### 4.4 AggregationGroup 聚合组

```csharp
namespace UGF.WorldUI
{
    public class AggregationGroup
    {
        /// <summary>聚合类型标识</summary>
        public string AggregationType { get; }

        /// <summary>聚合子键</summary>
        public string AggregationKey { get; }

        /// <summary>聚合组内源UI列表</summary>
        public IReadOnlyList<IAggregatable> Members => _members.AsReadOnly();

        /// <summary>当前成员数</summary>
        public int MemberCount => _members.Count;

        /// <summary>聚合展示UI实例</summary>
        public WorldSpaceUIComponent AggregatedUI { get; }

        /// <summary>聚合组中心位置</summary>
        public Vector3 CenterPosition { get; private set; }

        /// <summary>创建时间</summary>
        public float CreateTime { get; }

        // 内部成员列表
        private readonly List<IAggregatable> _members;

        /// <summary>添加成员</summary>
        public bool AddMember(IAggregatable ui);
        /// <summary>移除成员</summary>
        public bool RemoveMember(IAggregatable ui);
        /// <summary>更新聚合组（重新计算位置、更新展示UI）</summary>
        public void UpdateGroup();
        /// <summary>检查成员是否仍在聚合范围内</summary>
        public void ValidateMembers();
        /// <summary>获取聚合计算数据（Count/Sum/Max等）</summary>
        public AggregationData ComputeAggregationData();
    }

    /// <summary>聚合计算结果</summary>
    public struct AggregationData
    {
        public int count;
        public float sum;
        public float max;
        public float average;
        public IAggregatable mostImportant;
    }
}
```

### 4.5 SpatialGrid 空间网格

```csharp
namespace UGF.WorldUI
{
    /// <summary>
    /// 空间哈希网格 - 用于O(1)邻近查询
    /// </summary>
    internal class SpatialGrid
    {
        private readonly float _cellSize;
        private readonly Dictionary<long, List<IAggregatable>> _cells;

        public SpatialGrid(float cellSize);

        /// <summary>插入UI到网格</summary>
        public void Insert(IAggregatable ui, Vector3 position);

        /// <summary>从网格移除UI</summary>
        public void Remove(IAggregatable ui, Vector3 position);

        /// <summary>查询指定位置附近的所有UI</summary>
        public List<IAggregatable> QueryNearby(Vector3 position, float radius);

        /// <summary>清空网格</summary>
        public void Clear();

        private long GetCellKey(int x, int y, int z);
        private void GetCellCoords(Vector3 position, out int x, out int y, out int z);
    }
}
```

### 4.6 聚合检测算法

```
每帧检测流程：

1. UpdateSpatialGrid()
   - 清空空间网格
   - 将所有已注册且未聚合的UI重新插入网格

2. DetectAndFormGroups() [分批执行，每帧处理 maxDetectionsPerFrame 个]
   FOR EACH 未聚合的 UI (每帧处理上限个):
     a. 从空间网格查询 worldDistance 范围内的邻近UI
     b. 筛选同 AggregationType 的UI
     c. 根据 triggerMode 判定是否触发聚合:

        WorldDistance:
          → 邻近同类型UI数量 >= minGroupSize
        
        ScreenDistance:
          → 将候选UI世界坐标投影到屏幕空间
          → 计算屏幕归一化距离（除以 Screen.height）
          → 距离 < screenDistanceThreshold 的UI数量 >= minGroupSize
        
        ScreenOverlap:
          → 对每个候选UI计算其屏幕空间包围矩形
          → 与主UI矩形做相交检测
          → 重叠的UI数量 >= minGroupSize

     d. 创建 AggregationGroup, 将触发UI和满足条件的UI加入
     e. 生成聚合展示UI (通过 UIGroup 的 CreateUI)

3. UpdateExistingGroups()
   FOR EACH 现有聚合组:
     a. 调用 group.ValidateMembers() 验证成员
         - 根据 triggerMode 重新计算每个成员是否仍在聚合范围内
         - 对于 ScreenDistance/ScreenOverlap: 镜头缩放会自然导致距离变化，成员自动离开
     b. 如果成员数 < minGroupSize → 解散聚合组
     c. 调用 group.UpdateGroup() 更新位置和展示

4. CleanupEmptyGroups()
   - 移除无成员的聚合组
   - 归还/销毁聚合展示UI
```

#### ScreenDistance 计算（关键：响应镜头缩放）

```csharp
/// <summary>
/// 计算两个世界空间UI在屏幕上的归一化距离
/// 该距离随镜头缩放而变化：拉远→距离变小，拉近→距离变大
/// </summary>
private float CalculateScreenDistance(Vector3 worldPosA, Vector3 worldPosB)
{
    var camera = WorldSpaceUIManager.Instance.UICamera;
    if (camera == null) return float.MaxValue;

    var screenA = camera.WorldToScreenPoint(worldPosA);
    var screenB = camera.WorldToScreenPoint(worldPosB);

    // 任一在屏幕后方 → 视为无限远
    if (screenA.z <= 0 || screenB.z <= 0) return float.MaxValue;

    // 归一化到屏幕高度（使阈值与分辨率无关）
    float dx = (screenA.x - screenB.x) / Screen.height;
    float dy = (screenA.y - screenB.y) / Screen.height;
    return Mathf.Sqrt(dx * dx + dy * dy);
}
```

> **镜头缩放行为示例**：
> - 透视相机 FOV=60, 两个UI世界距离3m, 距离相机20m → screenDistance ≈ 0.05
> - 拉远（FOV=30 或相机后移）→ screenDistance ≈ 0.02 → **触发聚合**
> - 拉近（FOV=90 或相机靠近）→ screenDistance ≈ 0.15 → **触发解散**

### 4.7 屏幕空间重叠检测

```csharp
/// <summary>
/// 检测两个UI在屏幕空间是否重叠
/// </summary>
private bool CheckScreenOverlap(WorldSpaceUIComponent a, WorldSpaceUIComponent b)
{
    var camera = WorldSpaceUIManager.Instance.UICamera;
    if (camera == null) return false;

    var screenA = camera.WorldToScreenPoint(a.transform.position);
    var screenB = camera.WorldToScreenPoint(b.transform.position);

    // 检查是否在屏幕后方
    if (screenA.z <= 0 || screenB.z <= 0) return false;

    // 计算屏幕空间距离（按屏幕高度归一化）
    float screenDist = Vector2.Distance(
        new Vector2(screenA.x / Screen.width, screenA.y / Screen.height),
        new Vector2(screenB.x / Screen.width, screenB.y / Screen.height)
    );

    return screenDist < _config.screenOverlapThreshold;
}
```

---

## 5. 与现有系统的集成

### 5.1 WorldSpaceUIManager 改动

```csharp
// 新增字段
private UIAggregationSystem _aggregationSystem;

// 新增属性
public UIAggregationSystem AggregationSystem => _aggregationSystem;

// Initialize() 中新增
_aggregationSystem = new UIAggregationSystem(_globalConfig.aggregationConfig);

// Update() 中新增
_aggregationSystem?.Update();

// 新增 Public API
public void RegisterAggregatableUI(IAggregatable ui);
public void UnregisterAggregatableUI(IAggregatable ui);
```

### 5.2 CreateUI / DestroyUI 改动

```csharp
// CreateUI 末尾增加
if (uiComponent is IAggregatable aggregatable)
{
    _aggregationSystem?.RegisterUI(aggregatable);
}

// DestroyUI 开头增加
if (uiComponent is IAggregatable aggregatable)
{
    _aggregationSystem?.UnregisterUI(aggregatable);
}
```

### 5.3 WorldSpaceUIComponent 改动

```csharp
// 新增聚合相关状态
public enum AggregationState
{
    Normal,       // 正常显示
    Aggregated,   // 已被聚合（自身隐藏）
    Aggregator,   // 作为聚合展示UI（代表一个聚合组）
}
```

### 5.4 UIGroupConfig 改动

```csharp
// 新增
[Header("聚合设置")]
[Tooltip("该分组启用UI聚合")]
public bool enableAggregation = true;

[Tooltip("该分组的聚合配置（为空则使用全局配置）")]
public UIAggregationConfig aggregationConfig;
```

### 5.4b UIGroup 运行时 API 改动

参照现有的 `SetCullingEnabled` / `IsCullingEnabled` 模式，为 UIGroup 增加聚合控制 API：

```csharp
// UIGroup.cs 新增成员

#region Aggregation Management

/// <summary>
/// 设置分组聚合启用状态
/// </summary>
public void SetAggregationEnabled(bool enabled)
{
    _config.enableAggregation = enabled;

    // 如果禁用聚合，立即解散该分组下所有聚合组
    if (!enabled)
    {
        WorldSpaceUIManager.Instance?.AggregationSystem?.DisbandGroupsInGroup(this);
    }

    Debug.Log($"[UIGroup] {_name} 聚合状态设置为: {enabled}");
}

/// <summary>
/// 获取聚合启用状态
/// </summary>
public bool IsAggregationEnabled()
{
    return _config.enableAggregation;
}

/// <summary>
/// 设置该分组的聚合配置（覆盖全局配置）
/// </summary>
public void SetAggregationConfig(UIAggregationConfig config)
{
    _config.aggregationConfig = config;
}

/// <summary>
/// 获取该分组生效的聚合配置（优先级：分组配置 > 全局配置）
/// </summary>
public UIAggregationConfig GetEffectiveAggregationConfig()
{
    return _config.aggregationConfig
        ?? WorldSpaceUIManager.Instance?.GlobalConfig?.aggregationConfig
        ?? UIAggregationConfig.CreateDefault();
}

#endregion
```

### 5.4c UIAggregationSystem 按分组过滤逻辑

聚合系统在检测和处理时需要按分组隔离：

```csharp
// UIAggregationSystem 内部

// 注册时记录 UI 所属分组
private readonly Dictionary<IAggregatable, UIGroup> _uiToGroupMap = ...;

public void RegisterUI(IAggregatable ui)
{
    var component = ui as WorldSpaceUIComponent;
    var group = component?.Group;
    if (group == null) return;

    // 仅当分组启用聚合时才注册
    if (!group.IsAggregationEnabled()) return;

    _uiToGroupMap[ui] = group;
    _registeredUIs.Add(ui);
}

// 检测时：不同分组/不同Key的UI不互相聚合
private List<IAggregatable> FindCandidates(IAggregatable source)
{
    var sourceGroup = _uiToGroupMap[source];
    var nearby = _spatialGrid.QueryNearby(sourcePosition, worldDistance);

    // 筛选：同类型 + 同Key + 同分组 + 满足距离条件
    return nearby.Where(candidate =>
        candidate.AggregationType == source.AggregationType       // 同族
        && candidate.AggregationKey == source.AggregationKey     // 同子类（null==null, "Boss"=="Boss"）
        && _uiToGroupMap[candidate] == sourceGroup               // 同分组
        && PassesDistanceCheck(source, candidate)
    ).ToList();
}

/// <summary>解散指定分组下的所有聚合组</summary>
public void DisbandGroupsInGroup(UIGroup group) { ... }
```

### 5.5 WorldSpaceUIManager 按分组控制 API

```csharp
// WorldSpaceUIManager.cs 新增（参照现有 SetGroupCullingEnabled 模式）

/// <summary>
/// 设置分组聚合启用状态
/// </summary>
public void SetGroupAggregationEnabled(string groupName, bool enabled)
{
    var group = GetGroup(groupName);
    group?.SetAggregationEnabled(enabled);
}

/// <summary>
/// 获取分组聚合启用状态
/// </summary>
public bool IsGroupAggregationEnabled(string groupName)
{
    var group = GetGroup(groupName);
    return group?.IsAggregationEnabled() ?? false;
}
```

### 5.6 配置生效优先级

```
UI聚合配置生效优先级（从高到低）:
  1. UIGroupConfig.aggregationConfig     （分组专属配置）
  2. WorldSpaceUIManagerConfig.aggregationConfig （全局配置）
  3. UIAggregationConfig.CreateDefault() （硬编码默认值）

启用/禁用控制：
  1. UIGroupConfig.enableAggregation = false → 即使全局开启，该分组也不聚合
  2. WorldSpaceUIManagerConfig.aggregationConfig.enableAggregation = false → 全局关闭
  3. 仅 enableAggregation = true 的分组中的UI参与聚合检测
```

### 5.7 WorldSpaceUIManagerConfig 改动

```csharp
// 新增
[Header("聚合设置")]
public UIAggregationConfig aggregationConfig = UIAggregationConfig.CreateDefault();
```

---

## 6. AggregatedUI 聚合展示预制体

### 6.1 内置聚合展示UI类型

```csharp
/// <summary>聚合血条UI - 显示"怪物群"总血量</summary>
public class AggregatedHealthBarUI : AggregatableUIComponent { }

/// <summary>聚合伤害数字 - 显示累计伤害（xN 形式）</summary>
public class AggregatedDamageTextUI : AggregatableUIComponent { }

/// <summary>聚合交互提示 - 显示"附近可交互"</summary>
public class AggregatedInteractionUI : AggregatableUIComponent { }
```

### 6.2 聚合展示UI映射

```csharp
// 在 UIAggregationConfig 或系统初始化时配置
// AggregationType → 聚合展示UI预制体 的映射
public Dictionary<string, GameObject> aggregationPrefabMap;
```

### 6.3 聚合展示数据更新

```csharp
// AggregationGroup.UpdateGroup() 中
var data = ComputeAggregationData();
var aggregator = AggregatedUI as IAggregatable;

// 根据 displayMode 更新聚合展示
switch (displayMode)
{
    case AggregationDisplayMode.Count:
        aggregator.SetDisplayText($"x{data.count}");
        break;
    case AggregationDisplayMode.Sum:
        aggregator.SetDisplayText($"{data.sum:F0}");
        break;
    case AggregationDisplayMode.FirstWithCount:
        var firstData = data.mostImportant.GetAggregationDisplayData();
        aggregator.SetDisplayData(firstData, data.count);
        break;
    // ...
}
```

---

## 7. 文件变更清单

### 7.1 新增文件

```
Runtime/
├── Core/
│   ├── UIAggregationSystem.cs       # 聚合系统主控制器
│   ├── AggregationGroup.cs          # 聚合组
│   ├── SpatialGrid.cs               # 空间哈希网格
│   └── IAggregatable.cs             # 可聚合接口 + AggregatableUIComponent基类
├── Configs/
│   └── UIAggregationConfig.cs       # 聚合配置
└── Components/
    ├── AggregatedHealthBarUI.cs      # 聚合血条示例
    ├── AggregatedDamageTextUI.cs     # 聚合伤害数字示例
    └── AggregatedInteractionUI.cs    # 聚合交互提示示例
```

### 7.2 修改文件

```
Runtime/
├── Core/
│   ├── WorldSpaceUIManager.cs       # 新增 AggregationSystem 字段；SetGroupAggregationEnabled / IsGroupAggregationEnabled
│   ├── WorldSpaceUIComponent.cs     # 新增 AggregationState 状态
│   └── UIGroup.cs                   # 新增 SetAggregationEnabled / IsAggregationEnabled / SetAggregationConfig / GetEffectiveAggregationConfig
├── Configs/
│   ├── WorldSpaceUIManagerConfig.cs # 新增 aggregationConfig 字段
│   └── UIGroupConfig.cs             # 新增 enableAggregation + aggregationConfig 字段
```

---

## 8. 实现阶段

### 阶段一：核心框架（基础聚合逻辑）
1. `IAggregatable` 接口 + `AggregatableUIComponent` 基类
2. `UIAggregationConfig` 配置类
3. `SpatialGrid` 空间网格
4. `AggregationGroup` 聚合组
5. `UIAggregationSystem` 主系统（基础检测+聚合/解散）

### 阶段二：系统集成
1. 集成到 `WorldSpaceUIManager`（注册/注销/Update）
2. `WorldSpaceUIComponent` 增加聚合状态
3. `WorldSpaceUIManagerConfig` 和 `UIGroupConfig` 增加聚合配置
4. `CreateUI`/`DestroyUI` 自动注册/注销

### 阶段三：显示与优化
1. 聚合展示UI预制体与映射
2. 屏幕空间重叠检测
3. 聚合动画效果（聚合/解散过渡）
4. 性能优化（分批检测、缓存）

### 阶段四：编辑器与调试
1. 聚合系统调试面板
2. Scene视图聚合组可视化
3. 示例场景

---

## 9. 使用示例

### 9.1 让现有UI支持聚合

```csharp
// 方式一：实现 IAggregatable 接口
public class DamageTextUI : WorldSpaceUIComponent, IAggregatable
{
    public string AggregationType => "DamageText";
    public string AggregationKey => null;           // 伤害数字都同质，不需区分
    public float AggregationValue => _damageAmount;
    public int AggregationPriority => (int)_damageAmount;

    public object GetAggregationDisplayData() => _damageAmount;

    public void OnAggregated(AggregationGroup group) => SetVisible(false);
    public void OnDeaggregated(AggregationGroup group) => SetVisible(true);
}

// 方式二：继承 AggregatableUIComponent，通过 AggregationKey 区分不同内容的血条
public class HealthBarUI : AggregatableUIComponent
{
    public override float AggregationValue => _currentHealth;
    public override object GetAggregationDisplayData() => _currentHealth / _maxHealth;

    // 运行时设置 Key：Boss血条不和小怪血条聚合
    public void SetAsBoss()    => _aggregationKey = "Boss";
    public void SetAsMinion()  => _aggregationKey = "Minion";
    public void SetAsNPC()     => _aggregationKey = "NPC";
}
```

```csharp
// 创建时自动区分
var bossBar = WorldSpaceUIManager.Instance.CreateUI<HealthBarUI>(prefab, bossPos, "EnemyUI");
bossBar.SetAsBoss();
// → AggregationType="HealthBar", AggregationKey="Boss"

var minionBar1 = WorldSpaceUIManager.Instance.CreateUI<HealthBarUI>(prefab, minionPos1, "EnemyUI");
minionBar1.SetAsMinion();
var minionBar2 = WorldSpaceUIManager.Instance.CreateUI<HealthBarUI>(prefab, minionPos2, "EnemyUI");
minionBar2.SetAsMinion();
// → 两个 Minion 血条空间接近时会聚合，但 Boss 血条不会和它们聚合
```

### 9.2 配置聚合

```csharp
// 全局启用聚合
var config = WorldSpaceUIManager.Instance.GlobalConfig;
config.aggregationConfig = new UIAggregationConfig
{
    enableAggregation = true,
    triggerMode = AggregationTriggerMode.ScreenDistance,
    screenDistanceThreshold = 0.08f,   // 屏幕 8% 距离内聚合
    displayMode = AggregationDisplayMode.FirstWithCount,
    detectionInterval = 0.2f
};

// 为特定分组配置聚合（覆盖全局）
var damageGroup = WorldSpaceUIManager.Instance.CreateGroup("DamageText", new UIGroupConfig
{
    enableAggregation = true,
    maxInstances = 200
});
damageGroup.SetAggregationConfig(new UIAggregationConfig
{
    enableAggregation = true,
    triggerMode = AggregationTriggerMode.ScreenDistance,
    screenDistanceThreshold = 0.05f,  // 伤害数字更敏感的聚合阈值
    displayMode = AggregationDisplayMode.Sum
});

// 运行时按分组开关聚合
WorldSpaceUIManager.Instance.SetGroupAggregationEnabled("DamageText", false); // 关闭
WorldSpaceUIManager.Instance.SetGroupAggregationEnabled("DamageText", true);  // 开启

// 查询分组聚合状态
bool isEnabled = WorldSpaceUIManager.Instance.IsGroupAggregationEnabled("DamageText");
```

### 9.3 手动控制聚合

```csharp
// 注册/注销
var system = WorldSpaceUIManager.Instance.AggregationSystem;
system.RegisterUI(myUI);

// 查询状态
if (system.IsAggregated(myUI))
{
    var group = system.GetAggregationGroup(myUI);
    Debug.Log($"已被聚合，组成员数: {group.MemberCount}");
}

// 强制解散所有聚合
system.DisbandAll();
```

---

## 10. 性能考量

| 优化项 | 策略 |
|--------|------|
| 邻近查询 | 空间哈希网格，O(1)插入/查询 |
| 检测频率 | 可配置检测间隔（默认0.2s），分摊开销 |
| 分批处理 | 每帧限制最大检测UI数量 |
| 已被聚合的UI | 不再参与检测，减少计算量 |
| 聚合展示UI | 复用对象池，避免GC |
| 屏幕空间重叠 | 仅在 triggerMode 需要时才计算 |
