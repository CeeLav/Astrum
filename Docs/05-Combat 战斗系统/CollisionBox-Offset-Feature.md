# 技能碰撞盒偏移量功能

## 📅 更新日期
**2025-10-12**

---

## 📋 功能概述

为技能系统的碰撞盒配置添加了**偏移量（Offset）**支持，允许碰撞盒相对角色中心进行位置偏移。

---

## 🎯 格式说明

### **旧格式（向后兼容）**
```
Box:5x2x1          # 盒子：宽5x高2x深1，中心位于角色中心
Sphere:3.0         # 球形：半径3，中心位于角色中心
Capsule:2x5        # 胶囊：半径2x高度5，中心位于角色中心
Point              # 点碰撞，位于角色中心
```

### **新格式（带偏移量）**
```
Box:5x2x1@0,1,0           # 盒子，向上偏移1米
Sphere:3.0@0,0.5,1.5      # 球形，Y轴偏移0.5米，Z轴偏移1.5米（向前）
Capsule:2x5@0,1.5,0       # 胶囊，向上偏移1.5米
Point@0,0,2               # 点碰撞，向前偏移2米
```

### **格式规则**
- **基础格式**：`类型:尺寸参数`
- **带偏移**：`类型:尺寸参数@偏移X,偏移Y,偏移Z`
- **偏移量单位**：米（m）
- **坐标系**：
  - **X轴**：左（-）/ 右（+）
  - **Y轴**：下（-）/ 上（+）
  - **Z轴**：后（-）/ 前（+）

---

## 💡 使用场景

### **1. 向前攻击**
```
Box:3x2x4@0,1,2       # 攻击盒向前偏移2米，向上偏移1米
```
适用于：剑气、冲击波等向前释放的技能

### **2. 头部/胸部检测**
```
Sphere:1.0@0,1.5,0    # 球形检测范围在角色胸部位置
```
适用于：射击技能的碰撞检测、头部判定等

### **3. 地面范围技能**
```
Sphere:5.0@0,-0.5,0   # 球形范围贴地，向下偏移0.5米
```
适用于：地震、地刺等地面AOE技能

### **4. 武器攻击范围**
```
Box:2x1x3@0,1,1.5     # 武器攻击盒在手臂前方
```
适用于：近战武器的攻击判定

---

## 🛠️ 编辑器使用

### **1. 打开技能动作编辑器**
- `Astrum → Editor 编辑器 → SkillAction Editor`

### **2. 选择技能效果事件**
- 在时间轴上双击碰撞类型的事件

### **3. 配置碰撞盒**

#### **类型和尺寸**
- **碰撞盒类型**：下拉选择（Box/Sphere/Capsule/Point）
- **尺寸参数**：根据类型输入宽度、高度、半径等

#### **偏移量设置（高级）**
- **本地偏移 (X,Y,Z)**：
  - X：左右偏移
  - Y：上下偏移
  - Z：前后偏移
- **提示信息**：自动显示偏移方向和距离

### **4. 实时预览**
- 修改参数后，预览窗口**立即**显示新的碰撞盒位置
- 黄色线框表示技能碰撞盒
- 拖动时间轴查看不同帧的碰撞盒

---

## 📊 配置示例

### **示例1：前冲斩击**
```csv
# SkillActionTable.csv
actionId,triggerFrames
5001,"Frame10:Collision(Box:3x2x4@0,1,2):4001"
```
- 第10帧触发
- 盒子碰撞：宽3x高2x深4
- 偏移：向前2米，向上1米

### **示例2：旋转攻击**
```csv
actionId,triggerFrames
5002,"Frame5-15:Collision(Sphere:4@0,1,0):4002"
```
- 第5-15帧持续触发
- 球形碰撞：半径4米
- 偏移：向上1米（腰部位置）

### **示例3：地面震击**
```csv
actionId,triggerFrames
5003,"Frame20:Collision(Sphere:6@0,-0.5,0):4003"
```
- 第20帧触发
- 球形碰撞：半径6米
- 偏移：向下0.5米（贴地）

---

## 🔧 技术实现

### **1. 解析器（CollisionInfoParser）**
```csharp
// 支持 @ 符号分离偏移量
var shape = CollisionInfoParser.Parse("Box:5x2x1@0,1,0");

// shape.LocalOffset = TSVector(0, 1, 0)
```

### **2. 编辑器（CollisionInfoEditor）**
```csharp
// 解析偏移量
var (shapeType, parameters, offset) = ParseCollisionInfo(collisionInfo);

// 生成带偏移的字符串
string result = $"Box:5x2x1@{offset.x},{offset.y},{offset.z}";
```

### **3. 预览（CollisionShapePreview）**
```csharp
// 自动应用偏移量绘制
DrawShape(shape, color);  // shape.LocalOffset 已包含偏移
```

### **4. 运行时（HitManager）**
```csharp
// 转换为世界坐标（自动应用偏移和旋转）
var worldTransform = shape.ToWorldTransform(casterPos, casterRot);
// WorldCenter = casterPos + casterRot * LocalOffset
```

---

## ✅ 向后兼容性

### **测试用例**

| 旧格式 | 解析结果 | 状态 |
|--------|---------|------|
| `Box:5x2x1` | Offset=(0,0,0) | ✅ 兼容 |
| `Sphere:3.0` | Offset=(0,0,0) | ✅ 兼容 |
| `Capsule:2x5` | Offset=(0,0,0) | ✅ 兼容 |
| `Point` | Offset=(0,0,0) | ✅ 兼容 |

### **混合使用**
```csv
# 旧格式和新格式可以混合使用
triggerFrames,"Frame5:Collision(Box:5x2x1):4001,Frame10:Collision(Sphere:3@0,1,0):4002"
```

---

## 🚀 未来扩展

### **可能的功能**
1. **可视化拖动**：在预览窗口中直接拖动碰撞盒位置
2. **预设模板**：常用偏移量的快捷配置
3. **旋转支持**：`@Offset:Rotation` 格式（如果需要）
4. **动画偏移**：偏移量随帧变化的动画曲线

---

## 📝 注意事项

1. **偏移量是相对的**：偏移量相对角色中心，会随角色旋转而旋转
2. **单位是米**：配置时注意单位换算
3. **性能影响**：偏移量计算在运行时执行，性能开销可忽略
4. **零偏移优化**：如果不需要偏移，不要添加 `@0,0,0`，保持旧格式更简洁

---

## 🔗 相关文件

- **解析器**：`AstrumLogic/SkillSystem/CollisionInfoParser.cs`
- **编辑器UI**：`Editor/RoleEditor/UI/CollisionInfoEditor.cs`
- **预览渲染**：`Editor/RoleEditor/Services/CollisionShapePreview.cs`
- **运行时**：`AstrumLogic/Physics/CollisionShape.cs`

---

## 📞 技术支持

如有问题，请参考：
- 技能系统文档：`Docs/05-SkillSystem 技能系统/`
- 编辑器工具文档：`Docs/04-EditorTools 编辑器工具/`

