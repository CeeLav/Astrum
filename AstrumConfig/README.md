# AstrumConfig

> ⚙️ 配置数据目录 | Configuration Data Directory

本目录包含游戏运行时需要的所有配置数据和协议定义。

---

## 📦 目录说明

### 📊 Tables/
游戏配置表（CSV、Excel）

```
Tables/
├── Datas/          # 配置数据文件
│   ├── Skill/      # 技能配置
│   ├── Role/       # 角色配置
│   └── ...
├── Defines/        # 配置定义（XML）
└── output/         # 生成的二进制文件（.bytes）
```

### 📡 Proto/
网络协议定义（Protocol Buffers）

```
Proto/
├── game.proto      # 游戏协议
├── room.proto      # 房间协议
└── ...
```

---

## 🔧 工具使用

### 生成客户端配置
```bash
cd Tables/
gen_client.bat      # Windows
```

### 生成服务器配置
```bash
cd Tables/
gen.bat             # Windows
./gen.sh            # Linux/Mac
```

---

## 📚 相关文档

- [表格配置说明](../Docs/06-Configuration%20配置系统/Table-Config%20表格配置说明.md) - 配置表结构和规范
- [Luban使用指南](../Docs/04-EditorTools%20编辑器工具/Luban-Guide%20Luban使用指南.md) - 配置生成工具
- [CSV框架](../Docs/04-EditorTools%20编辑器工具/CSV-Framework%20CSV框架.md) - 配置框架快速参考

---

## ⚠️ 注意事项

1. **不要手动修改 output/ 目录** - 该目录由工具自动生成
2. **CSV 编码**: 使用 UTF-8 with BOM
3. **Excel 格式**: .xlsx 格式，首行为字段名
4. **修改配置后**: 必须重新运行生成脚本

---

**返回**: [项目首页](../README.md) | [文档中心](../Docs/)
