# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2024-01-XX

### Added
- 初始版本发布
- WorldSpaceUIManager 核心管理器
- UIGroup 分组管理系统
- WorldSpaceUIComponent 基础组件类
- UIObjectPool 对象池系统
- 基础UI组件实现（HealthBarUI, DamageTextUI, InteractionUI）
- 配置系统（UIGroupConfig, WorldSpaceUIConfig）
- 性能优化功能（视锥剔除、距离剔除、批量渲染）
- Editor工具和调试面板
- 完整的示例项目
- 详细的文档和API参考

### Features
- 🎯 统一的世界空间UI管理接口
- 📦 灵活的分组管理系统
- ⚡ 高性能的对象池和剔除机制
- 🔧 可扩展的组件架构
- 💾 自动内存管理
- 🛠️ 完善的开发工具支持

### Performance
- 支持大量UI元素的高效管理
- 内置视锥剔除和距离剔除
- 对象池减少GC压力
- 分帧更新优化
- 批量渲染支持

### Documentation
- 完整的设计文档
- API参考文档
- 使用指南和最佳实践
- 示例项目和演示场景

---

## 版本说明

### 版本号格式

本项目遵循 [语义化版本](https://semver.org/lang/zh-CN/) 规范：

- **主版本号**：不兼容的API修改
- **次版本号**：向下兼容的功能性新增
- **修订号**：向下兼容的问题修正

### 变更类型

- **Added** - 新增功能
- **Changed** - 功能变更
- **Deprecated** - 即将移除的功能
- **Removed** - 已移除的功能
- **Fixed** - 问题修复
- **Security** - 安全性改进