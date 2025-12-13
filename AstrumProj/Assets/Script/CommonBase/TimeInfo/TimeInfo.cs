using System;

namespace Astrum.CommonBase
{
    public class TimeInfo : Singleton<TimeInfo>
    {
        private int timeZone;
        
        public int TimeZone
        {
            get
            {
                return this.timeZone;
            }
            set
            {
                this.timeZone = value;
                dt = dt1970.AddHours(TimeZone);
            }
        }
        
        private DateTime dt1970;
        private DateTime dt;
        
        // ping消息会设置该值，原子操作
        public long ServerMinusClientTime { private get; set; }
        
        public long RTT { get; set; }

        public long FrameTime { get; private set; }

        /// <summary>
        /// 服务器时间与客户端时间的差值（由客户端从服务端时间戳估算并平滑）
        /// </summary>
        private long _serverTimeDiff;

        /// <summary>
        /// 时间差上升时的平滑因子（0 &lt; _timeDiffRiseFactor &lt; 1）
        /// </summary>
        private const float _timeDiffRiseFactor = 0.6f;

        /// <summary>
        /// 时间差下降时的平滑因子（0 &lt; _timeDiffFallFactor &lt; 1）
        /// </summary>
        private const float _timeDiffFallFactor = 0.005f;

        private bool _timeDiffInitialized = false;

        /// <summary>
        /// 只读访问：当前估算的服务器时间差
        /// </summary>
        public long ServerTimeDiff => _serverTimeDiff;
        
        public void Awake()
        {
            this.dt1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            this.dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            this.FrameTime = this.ClientNow();
        }

        public void Update()
        {
            // 赋值long型是原子操作，线程安全
            this.FrameTime = this.ClientNow();
        }
        
        /// <summary> 
        /// 根据时间戳获取时间 
        /// </summary>  
        public DateTime ToDateTime(long timeStamp)
        {
            return dt.AddTicks(timeStamp * 10000);
        }
        
        // 线程安全
        public long ClientNow()
        {
            return (DateTime.UtcNow.Ticks - this.dt1970.Ticks) / 10000;
        }
        
        public long ServerNow()
        {
            return ClientNow() + _serverTimeDiff + 33;
        }
        
        public long ClientFrameTime()
        {
            return this.FrameTime;
        }
        
        public long ServerFrameTime()
        {
            return this.FrameTime + _serverTimeDiff;
        }

        /// <summary>
        /// 更新服务器时间戳，计算时间差（平滑过渡）
        /// 采用上升容易、下降缓慢的平滑策略
        /// </summary>
        /// <param name="serverTimestamp">服务器时间戳（毫秒）</param>
        public void UpdateServerTime(long serverTimestamp)
        {
            // 计算新的时间差（目标值）
            long newTimeDiff = ClientNow() - serverTimestamp;

            if (!_timeDiffInitialized)
            {
                // 第一次初始化时，直接设置时间差
                _serverTimeDiff = newTimeDiff;
                _timeDiffInitialized = true;
                return;
            }

            // 动态选择平滑因子：上升时使用较大因子，下降时使用较小因子
            float smoothFactor = newTimeDiff > _serverTimeDiff ? _timeDiffRiseFactor : _timeDiffFallFactor;

            // 使用指数平滑算法平滑时间差的变化
            // 公式：current_diff = alpha * new_diff + (1 - alpha) * current_diff
            long smoothedDiff = (long)(smoothFactor * newTimeDiff + (1 - smoothFactor) * _serverTimeDiff);
            _serverTimeDiff = smoothedDiff;
        }
        
        public long Transition(DateTime d)
        {
            return (d.Ticks - dt.Ticks) / 10000;
        }
    }
}