# UGF World UI

[![Unity Version](https://img.shields.io/badge/Unity-2022.3%2B-blue.svg)](https://unity3d.com/get-unity/download)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.md)
[![Version](https://img.shields.io/badge/Version-1.0.0-orange.svg)](package.json)

统一的Unity世界空间UI管理系统，支持血条、伤害数字、交互提示等各类世界空间UI元素的创建、管理和优化。

## ✨ 主要特性

- 🎯 **统一管理**: 提供一致的API接口管理所有世界空间UI
- 📦 **分组系统**: 支持UI分组管理，便于批量操作和性能优化
- ⚡ **性能优化**: 内置对象池、视锥剔除、批量渲染等优化机制
- 🔧 **易于扩展**: 支持自定义UI组件和分组行为
- 💾 **内存友好**: 自动生命周期管理，避免内存泄漏
- 🛠️ **开发工具**: 提供Editor工具和调试面板

## 📦 安装

### 通过 Package Manager 安装

1. 打开 Unity Package Manager (Window > Package Manager)
2. 点击左上角的 "+" 按钮
3. 选择 "Add package from git URL"
4. 输入: `https://github.com/ugf-team/UGF.WorldUI.git`

### 通过 manifest.json 安装

在项目的 `Packages/manifest.json` 文件中添加:

```json
{
  "dependencies": {
    "com.ugf.worldui": "1.0.0"
  }
}
```

## 🚀 快速开始

### 基础使用

```csharp
using UGF.WorldUI;
using UGF.WorldUI.Components;

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

### 分组管理

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

## 📚 文档

- [设计文档](Documentation~/Unity世界空间UI管理器设计文档.md) - 详细的设计方案和架构说明
- [API文档](Documentation~/API文档.md) - 完整的API参考
- [使用指南](Documentation~/使用指南.md) - 详细的使用教程

## 🎮 示例

查看 `Samples~` 目录中的示例项目:

- **Basic Usage**: 基础使用示例
- **Advanced Features**: 高级功能演示
- **Performance Demo**: 性能优化案例

## 🔧 系统要求

- Unity 2022.3.0f1 或更高版本
- Unity UI (com.unity.ugui)

## 📄 许可证

本项目采用 [MIT 许可证](LICENSE.md)。

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📞 支持

如有问题，请通过以下方式联系我们:

- GitHub Issues: [提交问题](https://github.com/ugf-team/UGF.WorldUI/issues)
- Email: support@ugf.com