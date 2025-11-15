using System;
using System.IO;
using Astrum.CommonBase;
using MemoryPack;

namespace Astrum.Network
{
    public static class MessageSerializeHelper
    {
        public static byte[] Serialize(MessageObject message)
        {
            return MemoryPackHelper.Serialize(message);
        }

        public static void Serialize(MessageObject message, MemoryBuffer stream)
        {
            MemoryPackHelper.Serialize(message, stream);
        }
		
        public static MessageObject Deserialize(Type type, byte[] bytes, int index, int count)
        {
            object o = ObjectPool.Instance.Fetch(type);
            MemoryPackHelper.Deserialize(type, bytes, index, count, ref o);
            return o as MessageObject;
        }

        public static MessageObject Deserialize(Type type, MemoryBuffer stream)
        {
            object o = ObjectPool.Instance.Fetch(type);
            MemoryPackHelper.Deserialize(type, stream, ref o);
            return o as MessageObject;
        }
        
        public static ushort MessageToStream(MemoryBuffer stream, MessageObject message, int headOffset = 0)
        {
            ushort opcode = OpcodeType.Instance.GetOpcode(message.GetType());
            
            stream.Seek(headOffset + Packet.OpcodeLength, SeekOrigin.Begin);
            stream.SetLength(headOffset + Packet.OpcodeLength);
            
            stream.GetBuffer().WriteTo(headOffset, opcode);
            
            MessageSerializeHelper.Serialize(message, stream);
            
            stream.Seek(0, SeekOrigin.Begin);
            return opcode;
        }
        
        public static object ToMessage(AService service, MemoryBuffer memoryStream)
        {
            try
            {
                if (service == null)
                {
                    ASLogger.Instance.Error("MessageSerializeHelper.ToMessage: service is null");
                    return null;
                }
                
                if (memoryStream == null)
                {
                    ASLogger.Instance.Error("MessageSerializeHelper.ToMessage: memoryStream is null");
                    return null;
                }
                
                if (OpcodeType.Instance == null)
                {
                    ASLogger.Instance.Error("MessageSerializeHelper.ToMessage: OpcodeType.Instance is null");
                    return null;
                }
                
                object message = null;
                switch (service.ServiceType)
                {
                    case ServiceType.Outer:
                    {
                        memoryStream.Seek(Packet.OpcodeLength, SeekOrigin.Begin);
                        var buffer = memoryStream.GetBuffer();
                        if (buffer == null)
                        {
                            ASLogger.Instance.Error("MessageSerializeHelper.ToMessage: memoryStream.GetBuffer() returned null");
                            return null;
                        }
                        
                        ushort opcode = BitConverter.ToUInt16(buffer, 0);
                        // ASLogger.Instance.Debug($"MessageSerializeHelper.ToMessage: Outer service, opcode: {opcode}");
                        
                        Type type = OpcodeType.Instance.GetType(opcode);
                        if (type == null)
                        {
                            ASLogger.Instance.Error($"MessageSerializeHelper.ToMessage: OpcodeType.Instance.GetType({opcode}) returned null");
                            return null;
                        }
                        
                        message = Deserialize(type, memoryStream);
                        break;
                    }
                    case ServiceType.Inner:
                    {
                        memoryStream.Seek(Packet.ActorIdLength + Packet.OpcodeLength, SeekOrigin.Begin);
                        var buffer = memoryStream.GetBuffer();
                        if (buffer == null)
                        {
                            ASLogger.Instance.Error("MessageSerializeHelper.ToMessage: memoryStream.GetBuffer() returned null");
                            return null;
                        }
                        
                        ushort opcode = BitConverter.ToUInt16(buffer, Packet.ActorIdLength);
                        // ASLogger.Instance.Debug($"MessageSerializeHelper.ToMessage: Inner service, opcode: {opcode}");
                        
                        Type type = OpcodeType.Instance.GetType(opcode);
                        if (type == null)
                        {
                            ASLogger.Instance.Error($"MessageSerializeHelper.ToMessage: OpcodeType.Instance.GetType({opcode}) returned null");
                            return null;
                        }
                        
                        message = Deserialize(type, memoryStream);
                        break;
                    }
                    default:
                        ASLogger.Instance.Error($"MessageSerializeHelper.ToMessage: Unknown service type: {service.ServiceType}");
                        return null;
                }
                
                return message;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"MessageSerializeHelper.ToMessage: Exception occurred: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return null;
            }
        }
    }
}