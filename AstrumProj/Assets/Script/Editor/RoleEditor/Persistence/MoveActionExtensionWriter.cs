using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Astrum.Editor.RoleEditor.Data;
using Astrum.Editor.RoleEditor.Persistence.Core;
using Astrum.Editor.RoleEditor.Persistence.Mappings;

namespace Astrum.Editor.RoleEditor.Persistence
{
    /// <summary>
    /// 移动动作扩展数据写入器
    /// 写入 MoveActionTable
    /// </summary>
    public static class MoveActionExtensionWriter
    {
        private const string LOG_PREFIX = "[MoveActionExtensionWriter]";
        
        /// <summary>
        /// 写入所有移动扩展数据
        /// </summary>
        public static bool WriteAll(List<ActionEditorData> moveActions)
        {
            if (moveActions == null || moveActions.Count == 0)
            {
                Debug.LogWarning($"{LOG_PREFIX} No move actions to write");
                return true; // 空数据不算失败
            }
            
            try
            {
                // 转换为表数据
                var tableDataList = new List<MoveActionTableData>();
                foreach (var action in moveActions)
                {
                    if (action.MoveExtension != null)
                    {
                        var tableData = ConvertToTableData(action);
                        tableDataList.Add(tableData);
                        
                        // 调试日志
                        if (tableData.RootMotionData != null && tableData.RootMotionData.Count > 0)
                        {
                            Debug.Log($"{LOG_PREFIX} Writing MoveAction {tableData.ActionId} with {tableData.RootMotionData.Count} root motion data points");
                        }
                        else
                        {
                            Debug.LogWarning($"{LOG_PREFIX} MoveAction {tableData.ActionId} has no root motion data");
                        }
                    }
                }
                
                if (tableDataList.Count == 0)
                {
                    Debug.LogWarning($"{LOG_PREFIX} No valid move extensions to write");
                    return true;
                }
                
                // 读取现有数据并合并
                var config = MoveActionTableData.GetTableConfig();
                var existing = LubanCSVReader.ReadTable<MoveActionTableData>(config);
                var cache = new Dictionary<int, MoveActionTableData>();
                
                foreach (var entry in existing)
                {
                    if (!cache.ContainsKey(entry.ActionId))
                    {
                        cache[entry.ActionId] = entry;
                    }
                }
                
                // 更新新数据
                foreach (var entry in tableDataList)
                {
                    cache[entry.ActionId] = entry;
                }
                
                // 排序并写入
                var merged = cache.Values.OrderBy(data => data.ActionId).ToList();
                bool success = LubanCSVWriter.WriteTable(config, merged, enableBackup: true);
                
                if (success)
                {
                    Debug.Log($"{LOG_PREFIX} Successfully wrote {tableDataList.Count} move extensions (total {merged.Count} records)");
                }
                else
                {
                    Debug.LogError($"{LOG_PREFIX} Failed to write MoveActionTable");
                }
                
                return success;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to write move extensions: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// 转换编辑器数据为表数据
        /// </summary>
        private static MoveActionTableData ConvertToTableData(ActionEditorData action)
        {
            var extension = action.MoveExtension;
            
            // 自动计算移动速度（基于 RootMotionData）
            int calculatedSpeed = CalculateMoveSpeed(action.ActionId, extension.RootMotionData);
            
            return new MoveActionTableData
            {
                ActionId = action.ActionId,
                MoveSpeed = calculatedSpeed,
                RootMotionData = extension.RootMotionData ?? new List<int>()
            };
        }
        
        /// <summary>
        /// 根据 RootMotionData 计算移动速度
        /// </summary>
        private static int CalculateMoveSpeed(int actionId, List<int> rootMotionData)
        {
            if (rootMotionData == null || rootMotionData.Count <= 1)
            {
                return 0;
            }

            int frameCount = rootMotionData[0];

            if (frameCount <= 0)
            {
                Debug.LogWarning($"{LOG_PREFIX} Move action {actionId} has invalid root motion frame count: {frameCount}");
                return 0;
            }

            int expectedLength = 1 + frameCount * 7;
            if (rootMotionData.Count < expectedLength)
            {
                Debug.LogWarning($"{LOG_PREFIX} Move action {actionId} root motion data length mismatch. Expected {expectedLength}, got {rootMotionData.Count}");
                return 0;
            }

            double totalDistance = 0d;

            for (int frame = 0; frame < frameCount; frame++)
            {
                int baseIndex = 1 + frame * 7;
                double dx = rootMotionData[baseIndex] / 1000.0;
                double dz = rootMotionData[baseIndex + 2] / 1000.0;

                double stepDistance = System.Math.Sqrt(dx * dx + dz * dz);
                totalDistance += stepDistance;
            }

            // Root motion采样为20FPS（50ms一次）
            double totalTimeSeconds = frameCount / 20.0;
            if (totalTimeSeconds <= 0.0)
            {
                return 0;
            }

            double speed = totalDistance / totalTimeSeconds;
            if (double.IsNaN(speed) || double.IsInfinity(speed) || speed <= 0.0)
            {
                return 0;
            }

            int scaledSpeed = UnityEngine.Mathf.FloorToInt((float)(speed * 1000.0));
            int finalSpeed = UnityEngine.Mathf.Max(0, scaledSpeed);
            
            Debug.Log($"{LOG_PREFIX} Calculated speed for ActionId {actionId}: {finalSpeed} (distance={totalDistance:F3}m, time={totalTimeSeconds:F3}s, speed={speed:F3}m/s)");
            
            return finalSpeed;
        }
    }
}

