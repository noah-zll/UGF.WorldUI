# WorldUI 示例项目 API 参考文档

## 概述

本文档提供了 WorldUI 示例项目中所有组件和类的详细API参考。包括核心组件、配置类、控制器和测试工具的完整说明。

## 目录

- [UI组件](#ui组件)
  - [HealthBarUI](#healthbarui)
  - [DamageTextUI](#damagetextui)
  - [InteractionUI](#interactionui)
- [控制器](#控制器)
  - [SampleController](#samplecontroller)
- [配置](#配置)
  - [SampleConfig](#sampleconfig)
- [测试工具](#测试工具)
  - [PerformanceTest](#performancetest)
- [事件系统](#事件系统)
- [最佳实践](#最佳实践)

---

## UI组件

### HealthBarUI

血条UI组件，用于显示角色的生命值。

#### 继承关系
```csharp
WorldSpaceUIComponent -> HealthBarUI
```

#### 公共属性

| 属性名 | 类型 | 描述 |
|--------|------|------|
| `CurrentHealth` | `float` | 当前生命值 |
| `MaxHealth` | `float` | 最大生命值 |
| `HealthPercentage` | `float` | 生命值百分比 (0-1) |
| `IsAlive` | `bool` | 是否存活 |

#### 公共方法

##### SetHealth(float current, float max)
设置生命值。

**参数:**
- `current` (float): 当前生命值
- `max` (float): 最大生命值

**示例:**
```csharp
healthBar.SetHealth(80f, 100f);
```

##### TakeDamage(float damage)
造成伤害。

**参数:**
- `damage` (float): 伤害值

**返回值:**
- `float`: 实际造成的伤害

**示例:**
```csharp
float actualDamage = healthBar.TakeDamage(25f);
```

##### Heal(float amount)
治疗。

**参数:**
- `amount` (float): 治疗量

**返回值:**
- `float`: 实际治疗量

**示例:**
```csharp
float actualHeal = healthBar.Heal(30f);
```

##### SetFollowTarget(Transform target, Vector3 offset)
设置跟随目标。

**参数:**
- `target` (Transform): 跟随的目标
- `offset` (Vector3): 位置偏移

**示例:**
```csharp
healthBar.SetFollowTarget(character.transform, Vector3.up * 2.5f);
```

#### 事件

| 事件名 | 类型 | 描述 |
|--------|------|------|
| `OnHealthChanged` | `System.Action<float, float>` | 生命值改变时触发 |
| `OnDamageTaken` | `System.Action<float>` | 受到伤害时触发 |
| `OnHealed` | `System.Action<float>` | 治疗时触发 |
| `OnDeath` | `System.Action` | 死亡时触发 |

**示例:**
```csharp
healthBar.OnHealthChanged += (current, max) => {
    Debug.Log($"Health: {current}/{max}");
};

healthBar.OnDeath += () => {
    Debug.Log("Character died!");
};
```

---

### DamageTextUI

伤害文字UI组件，用于显示浮动的伤害或治疗数值。

#### 继承关系
```csharp
WorldSpaceUIComponent -> DamageTextUI
```

#### 公共属性

| 属性名 | 类型 | 描述 |
|--------|------|------|
| `DamageValue` | `float` | 伤害/治疗数值 |
| `IsCritical` | `bool` | 是否为暴击 |
| `IsHealing` | `bool` | 是否为治疗 |
| `AnimationDuration` | `float` | 动画持续时间 |

#### 公共方法

##### SetDamage(float damage, bool isCritical = false)
设置伤害数值。

**参数:**
- `damage` (float): 伤害值
- `isCritical` (bool): 是否为暴击

**示例:**
```csharp
damageText.SetDamage(45f, true); // 45点暴击伤害
```

##### SetHeal(float heal)
设置治疗数值。

**参数:**
- `heal` (float): 治疗值

**示例:**
```csharp
damageText.SetHeal(25f); // 25点治疗
```

##### PlayAnimation()
播放浮动动画。

**示例:**
```csharp
damageText.PlayAnimation();
```

#### 配置属性

| 属性名 | 类型 | 默认值 | 描述 |
|--------|------|--------|------|
| `floatDistance` | `float` | `2f` | 浮动距离 |
| `floatDuration` | `float` | `1.5f` | 浮动持续时间 |
| `scaleMultiplier` | `float` | `1.2f` | 缩放倍数 |
| `scaleDuration` | `float` | `0.3f` | 缩放持续时间 |
| `criticalScaleMultiplier` | `float` | `1.5f` | 暴击缩放倍数 |
| `criticalShakeIntensity` | `float` | `0.1f` | 暴击震动强度 |

---

### InteractionUI

交互UI组件，用于显示交互提示。

#### 继承关系
```csharp
WorldSpaceUIComponent -> InteractionUI
```

#### 公共属性

| 属性名 | 类型 | 描述 |
|--------|------|------|
| `InteractionText` | `string` | 交互提示文本 |
| `IsInteractable` | `bool` | 是否可交互 |
| `InteractionDistance` | `float` | 交互距离 |

#### 公共方法

##### SetInteraction(string text, Sprite icon, System.Action callback)
设置交互信息。

**参数:**
- `text` (string): 交互文本
- `icon` (Sprite): 交互图标（可为null）
- `callback` (System.Action): 交互回调

**示例:**
```csharp
interactionUI.SetInteraction("打开宝箱", chestIcon, () => {
    Debug.Log("宝箱被打开了！");
});
```

##### TriggerInteraction()
触发交互。

**示例:**
```csharp
interactionUI.TriggerInteraction();
```

##### SetHighlight(bool highlight)
设置高亮状态。

**参数:**
- `highlight` (bool): 是否高亮

**示例:**
```csharp
interactionUI.SetHighlight(true); // 高亮显示
```

#### 事件

| 事件名 | 类型 | 描述 |
|--------|------|------|
| `OnInteractionTriggered` | `System.Action` | 交互触发时调用 |
| `OnHighlightChanged` | `System.Action<bool>` | 高亮状态改变时调用 |

---

## 控制器

### SampleController

示例控制器，演示WorldUI系统的基本使用方法。

#### 公共属性

| 属性名 | 类型 | 描述 |
|--------|------|------|
| `healthBarPrefab` | `GameObject` | 血条预制体 |
| `damageTextPrefab` | `GameObject` | 伤害文字预制体 |
| `interactionPrefab` | `GameObject` | 交互UI预制体 |
| `testCharacters` | `Transform[]` | 测试角色数组 |
| `interactionPoints` | `Transform[]` | 交互点数组 |

#### 配置属性

| 属性名 | 类型 | 默认值 | 描述 |
|--------|------|--------|------|
| `damageKey` | `KeyCode` | `Space` | 造成伤害按键 |
| `healKey` | `KeyCode` | `H` | 治疗按键 |
| `interactKey` | `KeyCode` | `E` | 交互按键 |
| `damageAmount` | `float` | `20f` | 伤害数值 |
| `healAmount` | `float` | `15f` | 治疗数值 |
| `criticalChance` | `float` | `0.2f` | 暴击概率 |

#### 公共方法

##### DealDamageToRandomCharacter()
对随机角色造成伤害。

##### HealRandomCharacter()
治疗随机角色。

##### TriggerNearestInteraction()
触发最近的交互。

#### 事件处理

控制器订阅了以下WorldUI管理器事件：

- `OnUICreated`: UI创建时
- `OnUIDestroyed`: UI销毁时
- `OnGroupCreated`: 分组创建时
- `OnInstanceCountChanged`: 实例数量改变时

---

## 配置

### SampleConfig

示例配置ScriptableObject，用于管理所有示例相关的设置。

#### 创建配置资源

在Project窗口右键选择：`Create > WorldUI Sample > Sample Config`

#### 配置分类

##### 基础设置

| 属性名 | 类型 | 默认值 | 描述 |
|--------|------|--------|------|
| `autoInitialize` | `bool` | `true` | 自动初始化 |
| `showDebugInfo` | `bool` | `true` | 显示调试信息 |
| `enablePerformanceMonitoring` | `bool` | `true` | 启用性能监控 |

##### 全局配置

| 属性名 | 类型 | 默认值 | 描述 |
|--------|------|--------|------|
| `maxTotalInstances` | `int` | `500` | 最大UI实例总数 |
| `cleanupInterval` | `float` | `5f` | 清理间隔（秒） |
| `globalCullingDistance` | `float` | `100f` | 全局剔除距离 |
| `maxUpdatePerFrame` | `int` | `50` | 每帧最大更新数 |

##### 分组配置

每种UI类型都有独立的配置：

**血条配置:**
- `healthBarMaxInstances`: 最大实例数
- `healthBarCullingDistance`: 剔除距离
- `healthBarPoolSize`: 对象池大小
- `healthBarSortingOrder`: 排序层级

**伤害文字配置:**
- `damageTextMaxInstances`: 最大实例数
- `damageTextCullingDistance`: 剔除距离
- `damageTextPoolSize`: 对象池大小
- `damageTextSortingOrder`: 排序层级

**交互UI配置:**
- `interactionMaxInstances`: 最大实例数
- `interactionCullingDistance`: 剔除距离
- `interactionPoolSize`: 对象池大小
- `interactionSortingOrder`: 排序层级

#### 配置方法

##### GetManagerConfig()
获取WorldUI管理器全局配置。

**返回值:**
- `WorldSpaceUIManagerConfig`: 管理器配置

##### GetHealthBarGroupConfig()
获取血条分组配置。

**返回值:**
- `UIGroupConfig`: 血条分组配置

##### ValidateConfig()
验证配置有效性。

**返回值:**
- `bool`: 配置是否有效

**示例:**
```csharp
var config = Resources.Load<SampleConfig>("SampleConfig");
if (config.ValidateConfig())
{
    var managerConfig = config.GetManagerConfig();
    WorldSpaceUIManager.Instance.SetGlobalConfig(managerConfig);
}
```

---

## 测试工具

### PerformanceTest

性能测试组件，用于测试WorldUI系统在大量UI元素下的性能表现。

#### 测试配置

| 属性名 | 类型 | 默认值 | 描述 |
|--------|------|--------|------|
| `maxUICount` | `int` | `1000` | 最大UI数量 |
| `spawnRadius` | `float` | `50f` | 生成半径 |
| `testDuration` | `float` | `60f` | 测试持续时间 |
| `autoStartTest` | `bool` | `false` | 自动开始测试 |
| `batchSize` | `int` | `50` | 批处理大小 |
| `batchInterval` | `float` | `0.1f` | 批处理间隔 |

#### 压力测试配置

| 属性名 | 类型 | 默认值 | 描述 |
|--------|------|--------|------|
| `enableStressTest` | `bool` | `false` | 启用压力测试 |
| `damageInterval` | `float` | `0.05f` | 伤害间隔 |
| `damageTextPerSecond` | `int` | `100` | 每秒伤害文字数 |

#### 公共方法

##### StartTest()
开始性能测试。

##### StopTest()
停止性能测试。

##### CleanupTestUI()
清理所有测试UI。

#### 性能统计

测试完成后会输出以下统计信息：

- 平均FPS
- 最低/最高FPS
- 总创建UI数量
- 总销毁UI数量
- 平均创建时间
- 内存使用量

**示例输出:**
```
=== 性能测试结果 ===
测试持续时间: 60.00 秒
平均FPS: 58.32
最低FPS: 45.12
最高FPS: 60.00
总创建UI数量: 1000
总销毁UI数量: 856
平均创建时间: 2.34 ms
内存使用: 245.67 MB
当前UI总数: 144
分组数量: 3
===================
```

---

## 事件系统

### WorldUI管理器事件

WorldSpaceUIManager 提供了以下事件：

| 事件名 | 参数类型 | 描述 |
|--------|----------|------|
| `OnUICreated` | `WorldSpaceUIComponent` | UI创建时触发 |
| `OnUIDestroyed` | `WorldSpaceUIComponent` | UI销毁时触发 |
| `OnGroupCreated` | `string, UIGroup` | 分组创建时触发 |
| `OnInstanceCountChanged` | `int` | 实例数量改变时触发 |

**订阅示例:**
```csharp
var manager = WorldSpaceUIManager.Instance;

manager.OnUICreated += (ui) => {
    Debug.Log($"UI创建: {ui.GetType().Name}");
};

manager.OnUIDestroyed += (ui) => {
    Debug.Log($"UI销毁: {ui.GetType().Name}");
};

manager.OnInstanceCountChanged += (count) => {
    Debug.Log($"当前UI总数: {count}");
};
```

### UI组件事件

每个UI组件都有自己的事件系统，详见各组件的API说明。

---

## 最佳实践

### 1. 性能优化

#### 使用对象池
```csharp
var config = new UIGroupConfig
{
    enablePooling = true,
    poolSize = 20  // 根据实际需求调整
};
```

#### 启用距离剔除
```csharp
var config = new UIGroupConfig
{
    enableCulling = true,
    cullingDistance = 50f  // 根据游戏需求调整
};
```

#### 限制更新频率
```csharp
var globalConfig = new WorldSpaceUIManagerConfig
{
    maxUpdatePerFrame = 50  // 避免单帧更新过多UI
};
```

### 2. 内存管理

#### 及时销毁不需要的UI
```csharp
// 角色死亡时销毁血条
character.OnDeath += () => {
    if (healthBar != null)
    {
        WorldSpaceUIManager.Instance.DestroyUI(healthBar);
    }
};
```

#### 使用事件取消订阅
```csharp
private void OnDestroy()
{
    var manager = WorldSpaceUIManager.Instance;
    if (manager != null)
    {
        manager.OnUICreated -= OnUICreated;
        manager.OnUIDestroyed -= OnUIDestroyed;
    }
}
```

### 3. 代码组织

#### 使用配置文件
```csharp
// 创建可配置的ScriptableObject
[CreateAssetMenu(fileName = "MyUIConfig", menuName = "My Game/UI Config")]
public class MyUIConfig : ScriptableObject
{
    public float healthBarDistance = 50f;
    public int maxDamageTexts = 100;
    // ... 其他配置
}
```

#### 分离关注点
```csharp
// UI逻辑
public class HealthBarUI : WorldSpaceUIComponent { }

// 游戏逻辑
public class HealthSystem : MonoBehaviour { }

// 控制器负责连接两者
public class UIController : MonoBehaviour { }
```

### 4. 调试和测试

#### 使用性能监控
```csharp
var config = new WorldSpaceUIManagerConfig
{
    enablePerformanceMonitoring = true,
    showDebugInfo = true
};
```

#### 编写单元测试
```csharp
[Test]
public void HealthBar_TakeDamage_ReducesHealth()
{
    var healthBar = CreateHealthBar();
    healthBar.SetHealth(100f, 100f);
    
    float damage = healthBar.TakeDamage(25f);
    
    Assert.AreEqual(25f, damage);
    Assert.AreEqual(75f, healthBar.CurrentHealth);
}
```

---

## 版本历史

### v1.0.0
- 初始版本
- 基础UI组件（血条、伤害文字、交互UI）
- 示例控制器和配置系统
- 性能测试工具
- 完整文档

---

## 许可证

本示例项目遵循与 UGF.WorldUI 包相同的许可证。

---

## 贡献

欢迎提交问题报告和功能请求。在贡献代码时，请确保：

1. 遵循现有的代码风格
2. 添加适当的注释和文档
3. 编写单元测试
4. 更新相关文档

---

*最后更新: 2024年1月*