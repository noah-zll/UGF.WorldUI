# UGF.WorldUI Package 设计文档

## Package 信息

- **Package Name**: com.ugf.worldui
- **Display Name**: UGF World UI
- **Version**: 1.0.0
- **Unity Version**: 2022.3+
- **Description**: 统一的Unity世界空间UI管理系统，支持血条、伤害数字、交互提示等各类世界空间UI元素的创建、管理和优化

## 1. 概述

本Package提供了一个完整的Unity世界空间UI管理解决方案，通过统一的API接口和高效的管理机制，帮助开发者轻松创建和管理各种类型的世界空间UI元素。

### 1.1 主要特性

- 🎯 **统一管理**: 提供一致的API接口管理所有世界空间UI
- 📦 **分组系统**: 支持UI分组管理，便于批量操作和性能优化
- ⚡ **性能优化**: 内置对象池、视锥剔除、批量渲染等优化机制
- 🔧 **易于扩展**: 支持自定义UI组件和分组行为
- 💾 **内存友好**: 自动生命周期管理，避免内存泄漏
- 🛠️ **开发工具**: 提供Editor工具和调试面板

### 1.2 安装方式

#### 通过 Package Manager 安装

1. 打开 Unity Package Manager (Window > Package Manager)
2. 点击左上角的 "+" 按钮
3. 选择 "Add package from git URL"
4. 输入: `https://github.com/your-org/UGF.WorldUI.git`

#### 通过 manifest.json 安装

在项目的 `Packages/manifest.json` 文件中添加:

```json
{
  "dependencies": {
    "com.ugf.worldui": "1.0.0"
  }
}
```

### 1.3 依赖关系

- **Unity**: 2022.3.0f1 或更高版本
- **Unity UI**: com.unity.ugui (内置)
- **Unity Mathematics**: com.unity.mathematics (可选，用于性能优化)

## 2. 设计目标

- **🎯 统一管理**: 提供统一的接口来创建和管理所有世界空间UI
- **📦 分组管理**: 支持将不同类型的UI分组管理，便于批量操作
- **⚡ 性能优化**: 通过对象池和批量渲染优化性能
- **🔧 易于扩展**: 支持自定义UI组件和分组
- **💾 内存管理**: 自动管理UI生命周期，避免内存泄漏
- **🛠️ 开发友好**: 提供完善的Editor工具和调试支持

## 3. Package 结构

### 3.1 目录结构

```
com.ugf.worldui/
├── package.json                    # Package 元数据
├── README.md                       # 快速开始指南
├── CHANGELOG.md                    # 版本更新日志
├── LICENSE.md                      # 许可证
├── Documentation~/                 # 文档目录
│   ├── 设计文档.md
│   ├── API文档.md
│   └── 使用指南.md
├── Runtime/                        # 运行时代码
│   ├── Core/                       # 核心系统
│   │   ├── WorldSpaceUIManager.cs
│   │   ├── UIGroup.cs
│   │   ├── WorldSpaceUIComponent.cs
│   │   └── UIObjectPool.cs
│   ├── Components/                 # UI组件
│   │   ├── HealthBarUI.cs
│   │   ├── DamageTextUI.cs
│   │   └── InteractionUI.cs
│   ├── Configs/                    # 配置类
│   │   ├── UIGroupConfig.cs
│   │   └── WorldSpaceUIConfig.cs
│   └── UGF.WorldUI.asmdef         # 程序集定义
├── Editor/                         # 编辑器代码
│   ├── Tools/                      # 编辑器工具
│   │   ├── WorldUIDebugWindow.cs
│   │   └── WorldUISettingsProvider.cs
│   ├── Inspectors/                 # 自定义Inspector
│   └── UGF.WorldUI.Editor.asmdef  # 编辑器程序集定义
├── Tests/                          # 测试代码
│   ├── Runtime/
│   └── Editor/
└── Samples~/                       # 示例代码
    ├── Basic Usage/
    ├── Advanced Features/
    └── Performance Demo/
```

### 3.2 命名空间结构

```csharp
namespace UGF.WorldUI
{
    // 核心管理类
    public class WorldSpaceUIManager { }
    public class UIGroup { }
    
    // 基础组件
    public abstract class WorldSpaceUIComponent { }
    
    // 配置类
    public class UIGroupConfig { }
    public class WorldSpaceUIConfig { }
}

namespace UGF.WorldUI.Components
{
    // 具体UI组件实现
    public class HealthBarUI : WorldSpaceUIComponent { }
    public class DamageTextUI : WorldSpaceUIComponent { }
}

namespace UGF.WorldUI.Editor
{
    // 编辑器工具
    public class WorldUIDebugWindow : EditorWindow { }
}
```

### 3.3 核心架构

```
WorldSpaceUIManager (单例管理器)
├── UIGroupManager (分组管理器)
│   ├── DefaultGroup (默认分组)
│   └── CustomGroups... (自定义分组)
├── UIObjectPool (对象池系统)
├── CullingSystem (剔除系统)
└── UpdateScheduler (更新调度器)
```

### 3.4 类职责设计

#### WorldSpaceUIManager
- **命名空间**: `UGF.WorldUI`
- **职责**: 主管理器，提供统一的API接口
- **功能**:
  - 创建和销毁UI实例
  - 管理UI分组
  - 处理相机跟随和渲染
  - 对象池管理
  - 性能监控和调试

#### UIGroup
- **命名空间**: `UGF.WorldUI`
- **职责**: UI分组管理
- **功能**:
  - 管理同类型UI实例
  - 批量显示/隐藏操作
  - 分组级别的配置管理
  - 性能优化（剔除、LOD等）

#### WorldSpaceUIComponent
- **命名空间**: `UGF.WorldUI`
- **职责**: 世界空间UI基类
- **功能**:
  - 世界坐标跟随
  - 相机朝向对齐
  - 生命周期管理
  - 动画和效果支持

## 4. 核心功能设计

### 4.1 泛型创建方法

```csharp
public T CreateUI<T>(GameObject prefab, Vector3 worldPosition, string groupName = "Default") where T : WorldSpaceUIComponent
```

**参数说明**：
- `T`：UI组件类型
- `prefab`：UI预制体
- `worldPosition`：世界坐标位置
- `groupName`：分组名称，默认为"Default"

### 4.2 分组管理

```csharp
// 创建新分组
public UIGroup CreateGroup(string groupName, UIGroupConfig config = null)

// 获取分组
public UIGroup GetGroup(string groupName)

// 删除分组
public void RemoveGroup(string groupName)

// 分组批量操作
public void SetGroupVisible(string groupName, bool visible)
public void SetGroupActive(string groupName, bool active)
```

### 4.3 对象池管理

```csharp
// 从池中获取UI实例
public T GetFromPool<T>(GameObject prefab) where T : WorldSpaceUIComponent

// 返回到池中
public void ReturnToPool<T>(T uiComponent) where T : WorldSpaceUIComponent

// 预热对象池
public void WarmupPool<T>(GameObject prefab, int count) where T : WorldSpaceUIComponent
```

## 5. 配置系统

### 5.1 UIGroupConfig

```csharp
[Serializable]
public class UIGroupConfig
{
    public int sortingOrder = 0;           // 渲染层级
    public float updateInterval = 0.016f;   // 更新间隔
    public int maxInstances = 100;          // 最大实例数
    public bool enableCulling = true;       // 启用视锥剔除
    public float cullingDistance = 50f;     // 剔除距离
    public bool enablePooling = true;       // 启用对象池
    public int poolSize = 20;               // 对象池大小
}
```

### 5.2 WorldSpaceUIConfig

```csharp
[Serializable]
public class WorldSpaceUIConfig
{
    public bool followTarget = true;         // 跟随目标
    public bool faceCamera = true;           // 面向相机
    public Vector3 offset = Vector3.zero;   // 位置偏移
    public float lifeTime = -1f;             // 生命周期（-1为永久）
    public AnimationCurve scaleCurve;        // 缩放曲线
    public AnimationCurve alphaCurve;        // 透明度曲线
}
```

## 6. 使用示例

### 6.1 基础使用

```csharp
// 创建血条UI
var healthBar = WorldSpaceUIManager.Instance.CreateUI<HealthBarUI>(
    healthBarPrefab, 
    playerTransform.position + Vector3.up * 2f, 
    "HealthBar"
);

// 创建伤害文字
var damageText = WorldSpaceUIManager.Instance.CreateUI<DamageTextUI>(
    damageTextPrefab, 
    hitPosition, 
    "DamageText"
);
```

### 6.2 分组管理

```csharp
// 创建自定义分组
var config = new UIGroupConfig
{
    sortingOrder = 10,
    maxInstances = 50,
    enableCulling = true,
    cullingDistance = 30f
};
WorldSpaceUIManager.Instance.CreateGroup("InteractionUI", config);

// 批量隐藏血条
WorldSpaceUIManager.Instance.SetGroupVisible("HealthBar", false);
```

### 6.3 高级功能

```csharp
// 预热对象池
WorldSpaceUIManager.Instance.WarmupPool<DamageTextUI>(damageTextPrefab, 20);

// 设置相机
WorldSpaceUIManager.Instance.SetUICamera(uiCamera);

// 全局配置
WorldSpaceUIManager.Instance.SetGlobalConfig(new WorldSpaceUIManagerConfig
{
    enableAutoCleanup = true,
    cleanupInterval = 5f,
    maxTotalInstances = 500
});
```

## 7. 性能优化策略

### 7.1 渲染优化
- **批量渲染**：同类型UI使用相同材质，支持GPU Instancing
- **视锥剔除**：不在视野内的UI不进行更新和渲染
- **距离剔除**：超出指定距离的UI自动隐藏
- **LOD系统**：根据距离调整UI细节级别

### 7.2 更新优化
- **分帧更新**：将UI更新分散到多帧执行
- **脏标记**：只更新发生变化的UI元素
- **时间片管理**：控制每帧UI更新的时间消耗

### 7.3 内存优化
- **对象池**：复用UI实例，减少GC压力
- **自动清理**：定期清理过期的UI实例
- **懒加载**：按需创建UI分组和实例

## 8. 扩展性设计

### 8.1 自定义UI组件

```csharp
public class CustomUI : WorldSpaceUIComponent
{
    protected override void OnInitialize()
    {
        // 自定义初始化逻辑
    }
    
    protected override void OnUpdate()
    {
        // 自定义更新逻辑
    }
}
```

### 8.2 自定义分组行为

```csharp
public class CustomUIGroup : UIGroup
{
    protected override void OnGroupUpdate()
    {
        // 自定义分组更新逻辑
    }
}
```

## 9. 事件系统

```csharp
// UI生命周期事件
public event Action<WorldSpaceUIComponent> OnUICreated;
public event Action<WorldSpaceUIComponent> OnUIDestroyed;
public event Action<string, UIGroup> OnGroupCreated;
public event Action<string> OnGroupRemoved;

// 性能监控事件
public event Action<int> OnInstanceCountChanged;
public event Action<float> OnUpdateTimeChanged;
```

## 10. 调试和监控

### 10.1 调试面板
- 实时显示各分组的UI数量
- 性能统计（更新时间、渲染调用次数等）
- 对象池使用情况
- 内存使用统计

### 10.2 Gizmos绘制
- 在Scene视图中显示UI边界框
- 显示剔除距离范围
- 显示UI层级关系

## 11. 实现计划

### 阶段一：核心框架
1. 实现WorldSpaceUIManager主类
2. 实现UIGroup分组管理
3. 实现WorldSpaceUIComponent基类
4. 基础的创建和销毁功能

### 阶段二：性能优化
1. 实现对象池系统
2. 添加视锥剔除功能
3. 实现分帧更新机制
4. 添加批量渲染支持

### 阶段三：高级功能
1. 配置系统完善
2. 事件系统实现
3. 调试工具开发
4. 性能监控面板

### 阶段四：优化和完善
1. 性能调优
2. 内存优化
3. 文档完善
4. 单元测试

## 12. 总结

本设计文档提供了一个完整的Unity世界空间UI管理器解决方案，具有良好的扩展性、性能和易用性。通过分组管理、对象池、性能优化等机制，能够有效管理大量的世界空间UI元素，满足各种游戏场景的需求。