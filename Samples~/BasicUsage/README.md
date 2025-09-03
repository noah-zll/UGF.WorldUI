# UGF.WorldUI 示例项目

本示例展示了如何使用UGF.WorldUI包创建和管理世界空间UI元素。

## 示例内容

### 1. 基础示例 (BasicSample)
- **血条UI (HealthBarUI)**: 显示角色血量的世界空间血条
- **伤害数字 (DamageTextUI)**: 显示伤害数值的飘字效果
- **交互提示 (InteractionUI)**: 显示可交互物体的提示信息

### 2. 高级示例 (AdvancedSample)
- **性能优化**: 展示对象池、视锥剔除等优化功能
- **分组管理**: 演示UI分组的批量操作
- **自定义UI组件**: 展示如何扩展自定义UI组件

### 3. 性能测试 (PerformanceTest)
- **大量UI实例**: 测试系统在大量UI实例下的性能表现
- **性能监控**: 实时显示性能统计信息

## 目录结构

```
WorldUISample/
├── Scripts/                    # 示例脚本
│   ├── Components/            # UI组件实现
│   │   ├── HealthBarUI.cs
│   │   ├── DamageTextUI.cs
│   │   └── InteractionUI.cs
│   ├── Controllers/           # 控制器脚本
│   │   ├── SampleController.cs
│   │   └── PerformanceTestController.cs
│   └── Utils/                # 工具类
│       └── SampleUtilities.cs
├── Prefabs/                   # 预制体
│   ├── UI/                   # UI预制体
│   │   ├── HealthBar.prefab
│   │   ├── DamageText.prefab
│   │   └── InteractionPrompt.prefab
│   └── Characters/           # 角色预制体
│       ├── Player.prefab
│       └── Enemy.prefab
├── Scenes/                    # 示例场景
│   ├── BasicSample.unity
│   ├── AdvancedSample.unity
│   └── PerformanceTest.unity
├── Materials/                 # 材质资源
├── Textures/                 # 贴图资源
└── Configs/                  # 配置文件
    ├── DefaultUIConfig.asset
    └── PerformanceTestConfig.asset
```

## 快速开始

1. 打开 `BasicSample.unity` 场景
2. 运行场景，观察各种世界空间UI的效果
3. 查看 `SampleController.cs` 了解API使用方法
4. 尝试修改配置文件来调整UI行为

## 主要特性演示

- ✅ 世界空间UI创建和管理
- ✅ UI分组和批量操作
- ✅ 对象池优化
- ✅ 视锥剔除
- ✅ 生命周期管理
- ✅ 动画效果
- ✅ 性能监控
- ✅ 自定义扩展

## 注意事项

- 确保项目中已正确导入UGF.WorldUI包
- 建议在Unity 2021.3或更高版本中运行
- 示例场景需要配置UI Camera