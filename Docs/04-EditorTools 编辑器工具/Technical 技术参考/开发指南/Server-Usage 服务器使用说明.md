# AstrumServer 使用说明

## 问题解决

### Telnet显示问题
您遇到的telnet逐字符显示和乱码问题是因为telnet的输入模式和编码问题导致的。我已经做了以下改进：

1. **修改了服务器代码**：
   - 添加了telnet协议支持
   - 改进了字符编码处理
   - 添加了欢迎消息
   - 过滤了不可打印字符

2. **创建了专门的测试客户端**：
   - `test_client.ps1` - 通用测试客户端
   - `telnet_test.ps1` - 专门处理编码问题的客户端

## 推荐的测试方法

### 方法1：使用PowerShell测试客户端（推荐）
1. 确保服务器正在运行
2. 打开新的PowerShell窗口
3. 运行测试客户端：
   ```powershell
   cd D:\Develop\Projects\Astrum\AstrumServer
   .\test_client.ps1
   ```

### 方法2：使用专门的telnet测试客户端
```powershell
cd D:\Develop\Projects\Astrum\AstrumServer
.\telnet_test.ps1
```

### 方法3：使用真正的telnet（如果已安装）
1. 以管理员身份运行PowerShell
2. 执行安装命令：
   ```powershell
   dism /online /Enable-Feature /FeatureName:TelnetClient
   ```
3. 重启PowerShell
4. 连接服务器：
   ```powershell
   telnet localhost 8888
   ```

## 服务器命令

现在服务器支持以下命令：
- `help` - 显示可用命令
- `status` - 显示服务器状态
- `clients` - 显示当前连接数
- `quit` - 退出连接

## 测试步骤

1. **启动服务器**：
   ```powershell
   cd D:\Develop\Projects\Astrum\AstrumServer\AstrumServer
   dotnet run
   ```

2. **测试连接**（选择一种方法）：
   ```powershell
   # 方法1：通用测试客户端
   cd D:\Develop\Projects\Astrum\AstrumServer
   .\test_client.ps1
   
   # 方法2：telnet专用测试客户端
   .\telnet_test.ps1
   
   # 方法3：真正的telnet
   telnet localhost 8888
   ```

3. **输入命令测试**：
   - 输入 `help` 查看命令
   - 输入 `status` 查看状态
   - 输入 `quit` 退出

## 改进内容

- ✅ 修复了telnet逐字符显示问题
- ✅ 添加了telnet协议支持
- ✅ 改进了字符编码处理
- ✅ 添加了消息缓冲处理
- ✅ 实现了基本命令系统
- ✅ 创建了多个测试客户端
- ✅ 添加了彩色输出和更好的用户体验
- ✅ 添加了欢迎消息
- ✅ 过滤了不可打印字符

## 故障排除

### 如果仍然看到乱码：
1. 使用 `telnet_test.ps1` 而不是真正的telnet
2. 确保PowerShell使用UTF-8编码
3. 检查Windows区域设置

### 如果连接失败：
1. 确保服务器正在运行
2. 检查防火墙设置
3. 确认端口8888没有被其他程序占用 