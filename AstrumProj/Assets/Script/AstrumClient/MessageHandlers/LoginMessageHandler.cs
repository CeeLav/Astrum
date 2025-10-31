using System.Threading.Tasks;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.Network;
using Astrum.Client.Managers;
using Astrum.Network.MessageHandlers;

namespace Astrum.Client.MessageHandlers
{
    /// <summary>
    /// 登录响应消息处理器
    /// </summary>
    [MessageHandler(typeof(LoginResponse))]
    public class LoginMessageHandler : MessageHandlerBase<LoginResponse>
    {
        public override async Task HandleMessageAsync(LoginResponse message)
        {
            try
            {
                ASLogger.Instance.Info($"LoginMessageHandler: 处理登录响应 - Success: {message.Success}, Message: {message.Message}");
                
                if (message.Success)
                {
                    // 直接调用UserManager处理登录响应
                    UserManager.Instance?.HandleLoginResponse(message);
                    ASLogger.Instance.Info("LoginMessageHandler: 登录成功，已通知UserManager");
                }
                else
                {
                    ASLogger.Instance.Error($"LoginMessageHandler: 登录失败 - {message.Message}");
                }
                
                await Task.CompletedTask;
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"LoginMessageHandler: 处理登录响应时发生异常 - {ex.Message}");
            }
        }
    }
}
