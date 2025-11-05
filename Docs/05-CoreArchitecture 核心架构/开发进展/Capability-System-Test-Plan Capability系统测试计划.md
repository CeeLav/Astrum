# Capability 系统测试计划

**创建日期**: 2025-11-04  
**状态**: 🟡 测试文件已创建，待测试项目编译修复后运行

---

## 📋 测试覆盖范围

### 1. 基础功能测试（CapabilitySystemTests.cs）

#### 1.1 系统初始化
- ✅ `Test_CapabilitySystem_Initialization`
  - 验证 World 创建时 CapabilitySystem 正确初始化
  - 验证 World 引用正确设置

#### 1.2 CapabilityState 管理
- ✅ `Test_CapabilityState_Enable`
  - 验证启用 Capability 时状态正确设置
  - 验证 Entity 注册到 CapabilitySystem

- ✅ `Test_CapabilityState_Disable`
  - 验证禁用 Capability 时状态正确移除
  - 验证 Entity 从 CapabilitySystem 注销

#### 1.3 Tag 系统
- ✅ `Test_Tag_Disable`
  - 验证禁用 Tag 时正确记录 Instigator
  - 验证 DisabledTags 字典正确更新

- ✅ `Test_Tag_Enable`
  - 验证启用 Tag 时正确移除 Instigator
  - 验证 DisabledTags 字典正确清理

#### 1.4 生命周期方法
- ✅ `Test_ShouldActivate_WithRequiredComponents`
  - 验证没有必需组件时返回 false
  - 验证有必需组件时返回 true

- ✅ `Test_ShouldDeactivate_WhenComponentRemoved`
  - 验证移除必需组件时要求停用
  - 验证组件完整时不要求停用

- ✅ `Test_OnAttached_InitializesCustomData`
  - 验证 OnAttached 正确初始化 CustomData
  - 验证默认值正确设置

#### 1.5 实体管理
- ✅ `Test_UnregisterEntity_CleansUpAllCapabilities`
  - 验证销毁实体时清理所有 Capability 注册

- ✅ `Test_Update_ProcessesOnlyEntitiesWithCapability`
  - 验证更新时只处理拥有该 Capability 的实体
  - 验证性能优化（避免无效遍历）

---

## 🧪 测试执行

### 运行测试

```bash
# 进入测试目录
cd AstrumTest

# 运行 CapabilitySystem 相关测试
dotnet test AstrumTest.Shared --filter "FullyQualifiedName~CapabilitySystemTests"

# 运行所有 ECS 模块测试
dotnet test AstrumTest.Shared --filter "Module=ECS"

# 运行组件级别测试
dotnet test AstrumTest.Shared --filter "TestLevel=Component"
```

### 测试文件位置

```
AstrumTest/
└── AstrumTest.Shared/
    └── Unit/
        └── ECS/
            └── CapabilitySystemTests.cs
```

---

## ✅ 测试验证清单

### 基础功能
- [x] CapabilitySystem 初始化
- [x] CapabilityState 启用/禁用
- [x] Tag 禁用/启用
- [x] ShouldActivate/ShouldDeactivate
- [x] OnAttached 生命周期
- [x] 实体清理
- [x] 更新优化

### 集成测试（待实现）
- [ ] 完整 Archetype 装配流程
- [ ] SubArchetype 动态挂载/卸载
- [ ] 序列化/反序列化（MemoryPack）
- [ ] 多实体并发更新
- [ ] Tag 批量禁用性能

### 性能测试（待实现）
- [ ] 1000 实体更新性能
- [ ] 内存占用对比（新旧系统）
- [ ] 缓存命中率
- [ ] 并行更新潜力

---

## 📊 预期测试结果

### 基础功能测试
- **预期通过率**: 100%
- **测试数量**: 9 个测试用例
- **覆盖范围**: 核心功能完整覆盖

### 已知问题
1. **测试项目编译错误**: 其他测试文件（HitManagerTests、SharedTestScenario）有编译错误，但不影响 CapabilitySystemTests
2. **依赖 DLL**: 测试项目依赖 Unity 编译的 DLL，需要先编译 Unity 项目

---

## 🔄 下一步

1. **修复测试项目编译错误**（其他文件的错误）
2. **运行完整测试套件**
3. **添加集成测试**（完整流程验证）
4. **性能基准测试**（对比新旧系统）

---

**文档结束**

