using System;
using System.Numerics;

using Newtonsoft.Json;

namespace Astrum.Network
{
    /// <summary>
    /// 网络消息类型枚举
    /// </summary>
    public enum MessageType
    {
        // 连接相关
        Connect,
        Disconnect,
        Heartbeat,
        
        // 通用消息
        Data,
        
        // 系统相关
        Error,
        Info
    }

    /// <summary>
    /// 网络消息基类
    /// </summary>
    public abstract class NetworkMessage
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        public MessageType Type { get; set; }
        
        /// <summary>
        /// 消息ID
        /// </summary>
        public string MessageId { get; set; }
        
        /// <summary>
        /// 发送者ID
        /// </summary>
        public string SenderId { get; set; }
        
        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// 消息数据
        /// </summary>
        public object Data { get; set; }

        protected NetworkMessage()
        {
            MessageId = Guid.NewGuid().ToString();
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// 序列化为JSON
        /// </summary>
        public virtual string ToJson()
        {
            try
            {
                return JsonConvert.SerializeObject(this, new JsonSerializerSettings
                {
                    Formatting = Formatting.None,
                    ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"序列化消息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从JSON反序列化
        /// </summary>
        public static T FromJson<T>(string json) where T : NetworkMessage
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
                {
                    ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"反序列化消息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建响应消息
        /// </summary>
        public virtual NetworkMessage CreateResponse()
        {
            return new InfoMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                SenderId = "Server",
                Timestamp = DateTime.Now,
                Data = "Message received"
            };
        }
    }

    /// <summary>
    /// 连接消息
    /// </summary>
    public class ConnectMessage : NetworkMessage
    {
        public ConnectMessage()
        {
            Type = MessageType.Connect;
        }

        public string PlayerName { get; set; }
        public string Version { get; set; }
    }

    /// <summary>
    /// 断开连接消息
    /// </summary>
    public class DisconnectMessage : NetworkMessage
    {
        public DisconnectMessage()
        {
            Type = MessageType.Disconnect;
        }

        public string Reason { get; set; }
    }

    /// <summary>
    /// 心跳消息
    /// </summary>
    public class HeartbeatMessage : NetworkMessage
    {
        public HeartbeatMessage()
        {
            Type = MessageType.Heartbeat;
        }
    }

    /// <summary>
    /// 通用数据消息
    /// </summary>
    public class DataMessage : NetworkMessage
    {
        public DataMessage()
        {
            Type = MessageType.Data;
        }

        /// <summary>
        /// 数据类型标识
        /// </summary>
        public string DataType { get; set; }
        
        /// <summary>
        /// 数据内容
        /// </summary>
        public object Data { get; set; }
    }

    /// <summary>
    /// 错误消息
    /// </summary>
    public class ErrorMessage : NetworkMessage
    {
        public ErrorMessage()
        {
            Type = MessageType.Error;
        }

        public string ErrorCode { get; set; }
        public string ErrorText { get; set; }
    }

    /// <summary>
    /// 信息消息
    /// </summary>
    public class InfoMessage : NetworkMessage
    {
        public InfoMessage()
        {
            Type = MessageType.Info;
        }

        public string Info { get; set; }
    }
} 