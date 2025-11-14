using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using AstrumServer.Core;
using Astrum.CommonBase;

namespace AstrumServer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Astrum服务器正在启动...");
            
            // 配置ASLogger
            ConfigureASLogger();
            
            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }
        
        /// <summary>
        /// 配置ASLogger
        /// </summary>
        private static void ConfigureASLogger()
        {
            try
            {
                // 设置日志级别为Debug，确保所有日志都能输出
                ASLogger.Instance.MinLevel = Astrum.CommonBase.LogLevel.Debug;
                
                // 启用时间戳显示
                ASLogger.Instance.ShowTimestamp = true;
                ASLogger.Instance.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
                
                // 添加控制台日志处理器
                ASLogger.Instance.AddConsoleHandler(useColors: true);
                
                // 添加文件日志处理器
                var logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server.log");
                ASLogger.Instance.AddFileHandler(logFilePath, maxFileSize: 10, maxFileCount: 5);
                
                Console.WriteLine("ASLogger配置完成，日志级别设置为Debug，时间戳已启用");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"配置ASLogger时出错: {ex.Message}");
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<GameServer>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    // 移除Microsoft.Extensions.Logging的控制台输出，只使用ASLogger
                });
    }
}