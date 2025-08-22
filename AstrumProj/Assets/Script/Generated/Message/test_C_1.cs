using MemoryPack;
using System.Collections.Generic;
using Astrum.CommonBase;

namespace Astrum.Generated
{
    // 测试消息定义
    [MemoryPackable]
    [MessageAttribute(2)]
    [ResponseType(nameof(TestResponse))]
    public partial class TestMessage : MessageObject
    {
        public static TestMessage Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(TestMessage), isFromPool) as TestMessage;
        }

        /// <summary>
        /// 消息ID
        /// </summary>
        [MemoryPackOrder(0)]
        public int id { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        [MemoryPackOrder(1)]
        public string content { get; set; }

        /// <summary>
        /// 数字列表
        /// </summary>
        [MemoryPackOrder(2)]
        public List<int> numbers { get; set; } = new();

        /// <summary>
        /// 键值对数据
        /// </summary>
        [MemoryPackOrder(3)]
        public Dictionary<string, int> data { get; set; } = new();
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.id = default;
            this.content = default;
            this.numbers.Clear();
            this.data.Clear();

            ObjectPool.Instance.Recycle(this);
        }
    }

    [MemoryPackable]
    [MessageAttribute(3)]
    public partial class TestResponse : MessageObject
    {
        public static TestResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(TestResponse), isFromPool) as TestResponse;
        }

        /// <summary>
        /// 是否成功
        /// </summary>
        [MemoryPackOrder(0)]
        public bool success { get; set; }

        /// <summary>
        /// 响应消息
        /// </summary>
        [MemoryPackOrder(1)]
        public string message { get; set; }

        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }

            this.success = default;
            this.message = default;

            ObjectPool.Instance.Recycle(this);
        }
    }

    public static class test
    {
        public const ushort TestMessage = 2;
        public const ushort TestResponse = 3;
    }
}