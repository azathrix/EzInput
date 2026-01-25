# EzInput 单元测试

## 导入方式

1. 将 `Tests~` 文件夹重命名为 `Tests`（去掉波浪号）
2. Unity 会自动识别并导入测试
3. 打开 Window > General > Test Runner 运行测试

## 测试覆盖

### 工具类测试
- **TokenTests** - Token 唯一标识符测试
  - 创建有效令牌
  - 令牌唯一性
  - 默认令牌无效
  - 相等性比较

- **OverlayableValueTests** - 可叠加值基础测试
  - 默认值返回
  - 设置/移除值
  - 优先级机制

- **OverlayableValueAdvancedTests** - 可叠加值高级测试
  - 使用现有令牌更新值
  - 更新优先级
  - 清空所有条目

- **OverlayableValueEventTests** - 事件通知测试
  - 值变化时触发事件
  - 值相同时不触发
  - 移除/清空时触发

### 数据结构测试
- **UIKeyDataTests** - UI 按键数据测试
- **GameKeyDataTests** - 游戏按键数据测试
- **InputEventsTests** - 输入事件结构测试

### 枚举测试
- **EnumsTests** - 枚举值验证

## 注意事项

- 测试依赖 Unity Test Framework
- 部分测试需要 Input System 包
- EzInputSystem 的集成测试需要完整的框架环境
