using System;
using System.Collections.Generic;
using Astrum.CommonBase;

namespace Astrum.Network
{
    public class OpcodeType: Singleton<OpcodeType>
    {
        // 初始化后不变，所以主线程，网络线程都可以读
        private readonly DoubleMap<Type, ushort> typeOpcode = new();
        
        private readonly Dictionary<Type, Type> requestResponse = new();
        
        public void Awake()
        {
            HashSet<Type> types = CodeTypes.Instance.GetTypes(typeof (MessageAttribute));
            
            foreach (Type type in types)
            {
                object[] att = type.GetCustomAttributes(typeof (MessageAttribute), false);
                if (att.Length == 0)
                {
                    continue;
                }

                MessageAttribute messageAttribute = att[0] as MessageAttribute;
                if (messageAttribute == null)
                {
                    continue;
                }

                ushort opcode = messageAttribute.Opcode;
                
                if (opcode != 0)
                {
                    this.typeOpcode.Add(type, opcode);
                }
                else
                {
                    ASLogger.Instance.Warning($"OpcodeType.Awake: 跳过opcode为0的类型 {type.Name}");
                }

                // 检查request response
                if (typeof (IRequest).IsAssignableFrom(type))
                {
                    var attrs = type.GetCustomAttributes(typeof (ResponseTypeAttribute), false);
                    if (attrs.Length == 0)
                    {
                        ASLogger.Instance.Error($"not found responseType: {type}");
                        continue;
                    }

                    ResponseTypeAttribute responseTypeAttribute = attrs[0] as ResponseTypeAttribute;
                    this.requestResponse.Add(type, CodeTypes.Instance.GetType($"ET.{responseTypeAttribute.Type}"));
                }
            }
        }
        
        public ushort GetOpcode(Type type)
        {
            var opcode = this.typeOpcode.GetValueByKey(type);
            if (opcode == 0)
            {
                ASLogger.Instance.Warning($"OpcodeType.GetOpcode: 类型 {type.Name} 没有找到对应的opcode");
                ASLogger.Instance.Warning($"OpcodeType.GetOpcode: 当前注册的类型数量: {this.typeOpcode.Keys.Count}");
            }
            return opcode;
        }

        public Type GetType(ushort opcode)
        {
            Type type = this.typeOpcode.GetKeyByValue(opcode);
            if (type == null)
            {
                throw new Exception($"OpcodeType not found type: {opcode}");
            }
            return type;
        }

        public Type GetResponseType(Type request)
        {
            if (!this.requestResponse.TryGetValue(request, out Type response))
            {
                throw new Exception($"not found response type, request type: {request.FullName}");
            }

            return response;
        }
    }
}