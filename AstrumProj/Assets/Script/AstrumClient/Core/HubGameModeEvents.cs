using Astrum.CommonBase;
using Astrum.LogicCore.Data;

namespace Astrum.Client.Core
{
    /// <summary>
    /// 开始探索请求事件
    /// </summary>
    public class StartExplorationRequestEventData : EventData
    {
        // 暂无额外数据
    }

    /// <summary>
    /// 玩家数据变化事件
    /// </summary>
    public class PlayerDataChangedEventData : EventData
    {
        public PlayerProgressData ProgressData { get; set; }
        
        public PlayerDataChangedEventData(PlayerProgressData progressData)
        {
            ProgressData = progressData;
        }
    }
}

