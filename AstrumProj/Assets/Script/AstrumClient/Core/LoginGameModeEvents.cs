using Astrum.CommonBase;

namespace Astrum.Client.Core
{
    /// <summary>
    /// 登录状态变化事件数据
    /// </summary>
    public class LoginStateChangedEventData : EventData
    {
        public int ConnectionState { get; set; }
        public int MatchState { get; set; }
    }

    /// <summary>
    /// 登录错误事件数据
    /// </summary>
    public class LoginErrorEventData : EventData
    {
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// 用户已登录事件数据
    /// </summary>
    public class UserLoggedInEventData : EventData
    {
        public string UserId { get; set; }
        public string DisplayName { get; set; }
    }
}
