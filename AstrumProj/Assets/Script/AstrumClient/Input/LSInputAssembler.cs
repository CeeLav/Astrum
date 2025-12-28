using UnityEngine;
using System;
using System.Collections.Generic;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.LogicCore.Managers;
using cfg.Input;

namespace Astrum.Client.Input
{
    /// <summary>
    /// LSInput组装器 - 将逻辑动作转换为帧同步输入
    /// 负责将IRawInputProvider的输出组装为LSInput，包含相机方向转换和定点数转换
    /// </summary>
    public static class LSInputAssembler
    {
        private static Dictionary<string, LSInputFieldMappingTable> _fieldMappings;
        private static bool _initialized = false;
        
        /// <summary>
        /// 初始化（加载配置表）
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            
            _fieldMappings = new Dictionary<string, LSInputFieldMappingTable>();
            
            try
            {
                var configManager = TableConfig.Instance;
                if (!configManager.IsInitialized || configManager.Tables?.TbLSInputFieldMappingTable == null)
                {
                    ASLogger.Instance.Error("LSInputAssembler: TbLSInputFieldMappingTable is null", "Input.Assembler");
                    return;
                }
                
                foreach (var mapping in configManager.Tables.TbLSInputFieldMappingTable.DataList)
                {
                    if (mapping != null && !string.IsNullOrEmpty(mapping.LsInputField))
                    {
                        _fieldMappings[mapping.LsInputField] = mapping;
                    }
                }
                
                ASLogger.Instance.Info($"LSInputAssembler: 加载了 {_fieldMappings.Count} 个LSInput字段映射", "Input.Assembler");
                _initialized = true;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"LSInputAssembler: 初始化失败 - {ex.Message}", "Input.Assembler");
            }
        }
        
        /// <summary>
        /// 从原始输入组装LSInput
        /// </summary>
        /// <param name="provider">输入提供者</param>
        /// <param name="playerId">玩家ID</param>
        /// <param name="camera">相机（用于方向转换）</param>
        /// <param name="characterTransform">角色Transform（用于鼠标位置转换）</param>
        /// <returns>组装好的LSInput</returns>
        public static LSInput AssembleFromRawInput(
            IRawInputProvider provider,
            long playerId,
            Camera camera = null,
            Transform characterTransform = null)
        {
            if (!_initialized) Initialize();
            
            var input = new LSInput { PlayerId = playerId };
            
            // 处理移动输入（需要相机方向转换）
            AssembleMovement(input, provider, camera);
            
            // 处理按钮输入
            AssembleButtons(input, provider);
            
            // 处理鼠标位置输入
            AssembleMousePosition(input, provider, camera, characterTransform);
            
            // 设置客户端时间戳
            input.Timestamp = TimeInfo.Instance.ClientNow();
            
            return input;
        }
        
        /// <summary>
        /// 组装移动输入
        /// </summary>
        private static void AssembleMovement(LSInput input, IRawInputProvider provider, Camera camera)
        {
            // 获取原始轴输入
            float horizontal = provider.GetAxis("MoveHorizontal");
            float vertical = provider.GetAxis("MoveVertical");
            
            // 如果没有输入，直接返回
            if (Mathf.Abs(horizontal) < 0.01f && Mathf.Abs(vertical) < 0.01f)
            {
                input.MoveX = 0;
                input.MoveY = 0;
                return;
            }
            
            // 归一化输入
            Vector2 rawInput = new Vector2(horizontal, vertical);
            if (rawInput.magnitude > 1f)
                rawInput.Normalize();
            
            // 转换到世界空间（考虑相机方向）
            Vector2 worldMove = TransformToWorldSpace(rawInput, camera);
            
            // 转换为定点数
            input.MoveX = ToFixedPoint(worldMove.x);
            input.MoveY = ToFixedPoint(worldMove.y);
            
            /*ASLogger.Instance.Debug(
                $"LSInputAssembler: Movement - Raw({horizontal:F2}, {vertical:F2}) -> World({worldMove.x:F2}, {worldMove.y:F2}) -> Fixed({input.MoveX}, {input.MoveY})",
                "Input.Assembler"
            );*/
        }
        
        /// <summary>
        /// 组装按钮输入
        /// </summary>
        private static void AssembleButtons(LSInput input, IRawInputProvider provider)
        {
            // 根据配置表映射按钮
            foreach (var kvp in _fieldMappings)
            {
                string fieldName = kvp.Key;
                var mapping = kvp.Value;
                
                // 只处理DirectButton类型的映射
                if (string.Equals(mapping.MappingType, "DirectButton", StringComparison.OrdinalIgnoreCase))
                {
                    // SourceActions是单个动作ID
                    bool value = provider.GetButton(mapping.SourceActions);
                    SetBoolField(input, fieldName, value);
                    
                    if (value)
                    {
                        //ASLogger.Instance.Debug($"LSInputAssembler: Button '{fieldName}' = true (from '{mapping.SourceActions}')", "Input.Assembler");
                    }
                }
            }
        }
        
        /// <summary>
        /// 组装鼠标位置输入
        /// </summary>
        private static void AssembleMousePosition(
            LSInput input,
            IRawInputProvider provider,
            Camera camera,
            Transform characterTransform)
        {
            bool hasMouseX = _fieldMappings.TryGetValue("MouseWorldX", out var mappingX) &&
                             string.Equals(mappingX.MappingType, "MousePosition", StringComparison.OrdinalIgnoreCase);
            bool hasMouseZ = _fieldMappings.TryGetValue("MouseWorldZ", out var mappingZ) &&
                             string.Equals(mappingZ.MappingType, "MousePosition", StringComparison.OrdinalIgnoreCase);
            
            if (!hasMouseX && !hasMouseZ)
            {
                ASLogger.Instance.Warning("LSInputAssembler: Mouse mappings not enabled in config", "Input.Mouse");
                return;
            }
            
            if (provider == null || camera == null || characterTransform == null)
            {
                if (hasMouseX)
                {
                    input.MouseWorldX = 0;
                }
                if (hasMouseZ)
                {
                    input.MouseWorldZ = 0;
                }
                return;
            }
            
            Vector2 screenPos = provider.GetMousePosition();
            Ray ray = camera.ScreenPointToRay(screenPos);
            
            const float mousePlaneOffset = 1f;
            float planeHeight = characterTransform.position.y + mousePlaneOffset;
            Plane plane = new Plane(Vector3.up, new Vector3(0f, planeHeight, 0f));
            
            if (!plane.Raycast(ray, out float distance))
            {
                if (hasMouseX)
                {
                    input.MouseWorldX = 0;
                }
                if (hasMouseZ)
                {
                    input.MouseWorldZ = 0;
                }
                return;
            }
            
            Vector3 worldPos = ray.GetPoint(distance);
            
            if (hasMouseX)
            {
                input.MouseWorldX = ToFixedPoint(worldPos.x);
            }
            if (hasMouseZ)
            {
                input.MouseWorldZ = ToFixedPoint(worldPos.z);
            }
            

        }
        
        /// <summary>
        /// 转换到世界空间（考虑相机方向）
        /// 复用InputManager的逻辑
        /// </summary>
        private static Vector2 TransformToWorldSpace(Vector2 input, Camera cam)
        {
            if (cam == null)
            {
                // 如果没有相机，直接返回输入（不转换）
                return input;
            }
            
            // 获取相机方向（忽略Y轴）
            Vector3 forward = cam.transform.forward;
            Vector3 right = cam.transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            
            // 计算世界空间移动向量
            Vector3 worldMove = forward * input.y + right * input.x;
            
            // 返回XZ平面的移动向量
            return new Vector2(worldMove.x, worldMove.z);
        }
        
        /// <summary>
        /// 转换为Q31.32定点数
        /// </summary>
        private static long ToFixedPoint(float value)
        {
            return (long)(value * (double)(1L << 32));
        }
        
        /// <summary>
        /// 设置布尔字段
        /// 使用switch避免反射，提高性能
        /// </summary>
        private static void SetBoolField(LSInput input, string fieldName, bool value)
        {
            switch (fieldName)
            {
                case "Attack":
                    input.Attack = value;
                    break;
                    
                case "Skill1":
                    input.Skill1 = value;
                    break;
                    
                case "Skill2":
                    input.Skill2 = value;
                    break;
                    
                case "Roll":
                    input.Roll = value;
                    break;
                    
                case "Dash":
                    input.Dash = value;
                    break;
                    
                default:
                    ASLogger.Instance.Warning($"LSInputAssembler: 未知的LSInput字段 '{fieldName}'", "Input.Assembler");
                    break;
            }
        }
    }
}

