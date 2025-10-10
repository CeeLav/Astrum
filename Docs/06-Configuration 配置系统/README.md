# ⚙️ 配置系统文档

## 📖 文档列表

### [表格配置说明](Table-Config%20表格配置说明.md)
完整的配置表结构和使用说明

**核心内容**:
- 配置表结构设计
- 表格字段定义规范
- 数据类型说明
- 配置生成流程
- 常见问题解决

**配置表类型**:
- 技能配置 (SkillTable, SkillActionTable, SkillEffectTable)
- 角色配置 (RoleTable, EntityModelTable)
- 物品配置
- 关卡配置

---

## 🔧 配置工具

### Luban 配置生成器
- **工具目录**: `AstrumTool/Luban/`
- **配置目录**: `AstrumConfig/Tables/`
- **使用指南**: [Luban使用指南](../04-EditorTools%20编辑器工具/Luban-Guide%20Luban使用指南.md)

### 生成脚本
```bash
# 生成客户端配置
cd AstrumConfig/Tables/
gen_client.bat

# 生成服务器配置
gen.bat        # Windows
./gen.sh       # Linux/Mac
```

---

## 📁 配置目录结构

```
AstrumConfig/
├── Tables/
│   ├── Datas/          # 配置数据文件 (CSV/Excel)
│   │   ├── Skill/      # 技能配置
│   │   ├── Role/       # 角色配置
│   │   └── ...
│   ├── Defines/        # 配置定义 (XML)
│   └── output/         # 生成输出 (.bytes)
└── Proto/              # 协议定义
```

---

## 🔗 相关文档

- [编辑器工具](../04-EditorTools%20编辑器工具/) - CSV框架、Luban使用
- [技能系统](../02-CombatSystem%20战斗系统/Skill-System%20技能系统.md) - 技能配置表设计
- [AstrumConfig/README.md](../../AstrumConfig/README.md) - 配置目录说明

---

## ⚠️ 注意事项

1. **不要手动修改 output/ 目录** - 由工具自动生成
2. **CSV 编码**: 使用 UTF-8 with BOM
3. **Excel 格式**: .xlsx，首行为字段名
4. **修改后必须重新生成** - 运行 gen_client.bat

---

**返回**: [文档中心](../README.md)

