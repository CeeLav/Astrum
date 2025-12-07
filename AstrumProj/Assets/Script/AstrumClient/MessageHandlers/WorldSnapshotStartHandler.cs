using System.Threading.Tasks;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.Network.MessageHandlers;

namespace Astrum.Client.MessageHandlers
{
    /// <summary>
    /// 世界快照开始消息处理器
    /// 接收世界数据传输开始的信号，准备接收分片
    /// </summary>
    [MessageHandler(typeof(WorldSnapshotStart))]
    public class WorldSnapshotStartHandler : MessageHandlerBase<WorldSnapshotStart>
    {
        public override async Task HandleMessageAsync(WorldSnapshotStart message)
        {
            try
            {
                ASLogger.Instance.Info(
                    $"WorldSnapshotStartHandler: 收到世界快照开始通知 - 房间: {message.roomId}, 总分片数: {message.totalChunks}, 总大小: {message.totalSize} bytes",
                    "FrameSync.Client");

                // 创建接收器，准备接收分片
                WorldSnapshotChunkHandler.PrepareReceiver(
                    message.roomId,
                    message.totalChunks,
                    message.totalSize);
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"WorldSnapshotStartHandler: 处理世界快照开始通知时发生异常 - {ex.Message}", "FrameSync.Client");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
            }
            
            await Task.CompletedTask;
        }
    }
}

