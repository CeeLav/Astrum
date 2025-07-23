# 测试LogicCore集成的服务器功能
Write-Host "=== LogicCore集成测试 ===" -ForegroundColor Green

try {
    $client = New-Object System.Net.Sockets.TcpClient
    $client.Connect("localhost", 8888)
    
    if ($client.Connected) {
        Write-Host "连接成功！" -ForegroundColor Green
        
        $stream = $client.GetStream()
        $reader = New-Object System.IO.StreamReader($stream, [System.Text.Encoding]::UTF8)
        $writer = New-Object System.IO.StreamWriter($stream, [System.Text.Encoding]::UTF8)
        $writer.AutoFlush = $true
        
        # 读取欢迎消息
        $welcome = $reader.ReadLine()
        Write-Host "服务器欢迎消息: $welcome" -ForegroundColor Cyan
        
        # 测试命令
        $commands = @("help", "status", "players", "move 1 0 0", "jump", "move 0 0 5", "players", "quit")
        
        foreach ($cmd in $commands) {
            Write-Host "`n发送命令: $cmd" -ForegroundColor Yellow
            $writer.WriteLine($cmd)
            Start-Sleep -Milliseconds 200
            
            # 读取响应
            $response = $reader.ReadLine()
            if ($response) {
                Write-Host "服务器响应: $response" -ForegroundColor Green
            }
            
            if ($cmd -eq "quit") {
                break
            }
        }
        
        $client.Close()
        Write-Host "`n测试完成！" -ForegroundColor Green
    }
}
catch {
    Write-Host "连接失败: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "请确保服务器正在运行" -ForegroundColor Yellow
}
finally {
    if ($client) {
        $client.Close()
    }
} 