# WorldUI 示例项目快速开始指南

## 概述

本指南将帮助您快速上手 UGF.WorldUI 包的示例项目。通过这个示例，您将学会如何使用 WorldUI 系统创建世界空间UI元素，包括血条、伤害文字和交互提示。

## 前置要求

- Unity 2021.3 或更高版本
- 已安装 UGF.WorldUI 包
- 基本的 Unity 开发知识

## 项目结构

```
WorldUISample/
├── Scripts/
│   ├── Components/          # UI组件脚本
│   │   ├── HealthBarUI.cs   # 血条UI组件
│   │   ├── DamageTextUI.cs  # 伤害文字组件
│   │   └── InteractionUI.cs # 交互UI组件
│   ├── Controllers/         # 控制器脚本
│   │   └── SampleController.cs # 示例控制器
│   ├── Tests/              # 测试脚本
│   │   └── PerformanceTest.cs # 性能测试
│   └── Config/             # 配置脚本
│       └── SampleConfig.cs # 示例配置
├── Scenes/                 # 示例场景
│   ├── BasicExample.unity  # 基础示例场景
│   └── PerformanceTest.unity # 性能测试场景
├── Prefabs/               # 预制体文件夹
├── Materials/             # 材质文件夹
└── Documentation/         # 文档文件夹
    ├── QuickStart.md      # 快速开始指南
    └── API_Reference.md   # API参考文档
```

## 快速开始

### 步骤 1：打开示例场景

1. 在 Unity 项目中导航到 `Assets/Game/WorldUISample/Scenes/`
2. 双击打开 `BasicExample.unity` 场景
3. 您将看到一个包含以下元素的场景：
   - 主摄像机
   - 两个测试角色（胶囊体）
   - 一个交互点（立方体）
   - 地面
   - SampleController 游戏对象

### 步骤 2：配置预制体

在开始之前，您需要创建UI预制体：

#### 创建血条预制体

1. 在场景中创建一个新的 Canvas（UI > Canvas）
2. 设置 Canvas 的 Render Mode 为 "World Space"
3. 在 Canvas 下创建血条UI元素：
   - Slider（用于显示血量）
   - Text（用于显示血量数值）
4. 添加 `HealthBarUI` 组件到 Canvas
5. 配置组件的引用
6. 将 Canvas 保存为预制体到 `Prefabs/` 文件夹

#### 创建伤害文字预制体

1. 创建新的 Canvas（World Space）
2. 添加 Text 组件显示伤害数值
3. 添加 `DamageTextUI` 组件
4. 配置动画参数
5. 保存为预制体

#### 创建交互UI预制体

1. 创建新的 Canvas（World Space）
2. 添加交互提示UI元素（Text、Button等）
3. 添加 `InteractionUI` 组件
4. 配置交互参数
5. 保存为预制体

### 步骤 3：配置 SampleController

1. 选择场景中的 `SampleController` 游戏对象
2. 在 Inspector 中配置以下参数：
   - **Health Bar Prefab**: 拖入血条预制体
   - **Damage Text Prefab**: 拖入伤害文字预制体
   - **Interaction Prefab**: 拖入交互UI预制体
   - **Test Characters**: 拖入场景中的测试角色
   - **Interaction Points**: 拖入交互点

### 步骤 4：运行示例

1. 点击 Play 按钮运行场景
2. 您将看到：
   - 角色头顶出现血条
   - 交互点上方出现交互提示
   - 屏幕左上角显示控制说明

### 步骤 5：测试功能

使用以下按键测试不同功能：

- **空格键 (Space)**: 对随机角色造成伤害
- **H 键**: 治疗随机角色
- **E 键**: 触发最近的交互

## 核心概念

### WorldSpaceUIManager

WorldUI 系统的核心管理器，负责：
- 创建和销毁UI实例
- 管理UI分组
- 处理对象池
- 性能优化（剔除、批处理等）

```csharp
// 获取管理器实例
var manager = WorldSpaceUIManager.Instance;

// 创建UI
var healthBar = manager.CreateUI<HealthBarUI>(prefab, position, "HealthBar");

// 销毁UI
manager.DestroyUI(healthBar);
```

### UI分组 (UIGroup)

UI分组用于组织和管理相同类型的UI元素：

```csharp
// 创建分组配置
var config = new UIGroupConfig
{
    sortingOrder = 10,
    maxInstances = 50,

    cullingDistance = 50f,
    enablePooling = true,
    poolSize = 20
};

// 创建分组
manager.CreateGroup("HealthBar", config);
```

### UI组件基类

所有WorldUI组件都继承自 `WorldSpaceUIComponent`：

```csharp
public class HealthBarUI : WorldSpaceUIComponent
{
    protected override void OnInitialize()
    {
        // 初始化逻辑
    }
    
    protected override void OnUpdate()
    {
        // 更新逻辑
    }
}
```

## 性能优化

### 剔除 (Culling)

距离剔除可以隐藏远距离的UI元素：

```csharp
var config = new UIGroupConfig
{
    cullingDistance = 50f  // 50米外的UI将被隐藏
};
```

### 对象池 (Object Pooling)

对象池可以重用UI实例，减少GC压力：

```csharp
var config = new UIGroupConfig
{
    enablePooling = true,
    poolSize = 20  // 池中保持20个实例
};
```

### 批处理更新

限制每帧更新的UI数量：

```csharp
var globalConfig = new WorldSpaceUIManagerConfig
{
    maxUpdatePerFrame = 50  // 每帧最多更新50个UI
};
```

## 性能测试

### 运行性能测试

1. 打开 `PerformanceTest.unity` 场景
2. 运行场景
3. 观察右侧的性能控制面板
4. 点击"开始测试"按钮
5. 观察FPS和内存使用情况

### 测试参数

- **最大UI数量**: 测试创建的UI总数
- **生成半径**: UI分布的范围
- **测试时长**: 性能测试持续时间
- **压力测试**: 启用后会持续创建伤害文字

## 常见问题

### Q: UI不显示怎么办？

A: 检查以下几点：
1. 预制体是否正确配置
2. Canvas 是否设置为 World Space
3. UI组件是否正确添加
4. 摄像机设置是否正确

### Q: 性能不佳怎么优化？

A: 尝试以下优化方法：
1. 启用距离剔除
2. 使用对象池
3. 限制每帧更新数量
4. 减少UI复杂度
5. 使用LOD系统

### Q: 如何自定义UI组件？

A: 继承 `WorldSpaceUIComponent` 并实现必要的方法：

```csharp
public class CustomUI : WorldSpaceUIComponent
{
    protected override void OnInitialize()
    {
        // 初始化自定义逻辑
    }
    
    protected override void OnUpdate()
    {
        // 自定义更新逻辑
    }
    
    protected override void OnDestroy()
    {
        // 清理逻辑
    }
}
```

## 下一步

- 查看 [API参考文档](API_Reference.md) 了解详细的API说明
- 尝试修改示例代码，创建自己的UI组件
- 在自己的项目中集成 WorldUI 系统
- 参考性能测试结果优化您的UI性能

## 支持

如果您在使用过程中遇到问题，请：

1. 查看控制台错误信息
2. 检查组件配置是否正确
3. 参考示例代码
4. 查阅完整的API文档

祝您使用愉快！