using Astrum.CommonBase;
using Astrum.LogicCore.Data;

namespace Astrum.Client.Core
{
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

