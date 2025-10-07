using UnityEngine;
using UnityEditor;
using Astrum.Editor.RoleEditor.Persistence;

namespace Astrum.Editor.RoleEditor.Test
{
    /// <summary>
    /// CSV读写测试（使用通用框架）
    /// </summary>
    public static class CSVTest
    {
        [MenuItem("Tools/Role & Skill Editor/Test/Test CSV Read")]
        public static void TestRead()
        {
            Debug.Log("=== Testing CSV Read (Generic Framework) ===");
            
            var roles = RoleDataReader.ReadRoleData();
            
            Debug.Log($"Loaded {roles.Count} roles:");
            
            foreach (var role in roles)
            {
                Debug.Log($"  [{role.RoleId}] {role.RoleName}");
                Debug.Log($"    Entity: {role.EntityId}, Archetype: {role.ArchetypeName}");
                Debug.Log($"    Model: {role.ModelName} ({role.ModelPath})");
                Debug.Log($"    Type: {role.RoleType}");
                Debug.Log($"    Stats: ATK={role.BaseAttack}, DEF={role.BaseDefense}, HP={role.BaseHealth}, SPD={role.BaseSpeed}");
            }
            
            Debug.Log("=== CSV Read Test Complete ===");
        }
        
        [MenuItem("Tools/Role & Skill Editor/Test/Test CSV Write")]
        public static void TestWrite()
        {
            Debug.Log("=== Testing CSV Write (Generic Framework) ===");
            
            var roles = RoleDataReader.ReadRoleData();
            
            if (roles.Count == 0)
            {
                Debug.LogWarning("No roles to write");
                return;
            }
            
            bool success = RoleDataWriter.WriteRoleData(roles);
            
            if (success)
            {
                Debug.Log("CSV Write Test: SUCCESS");
            }
            else
            {
                Debug.LogError("CSV Write Test: FAILED");
            }
            
            Debug.Log("=== CSV Write Test Complete ===");
        }
        
        [MenuItem("Tools/Role & Skill Editor/Test/Test Round Trip")]
        public static void TestRoundTrip()
        {
            Debug.Log("=== Testing Round Trip (Read -> Write -> Read) ===");
            
            // 1. 读取
            var originalRoles = RoleDataReader.ReadRoleData();
            Debug.Log($"Step 1: Loaded {originalRoles.Count} roles");
            
            // 2. 写入
            bool writeSuccess = RoleDataWriter.WriteRoleData(originalRoles);
            Debug.Log($"Step 2: Write {(writeSuccess ? "SUCCESS" : "FAILED")}");
            
            if (!writeSuccess)
            {
                Debug.LogError("Round Trip Test FAILED at write step");
                return;
            }
            
            // 3. 重新读取
            var reloadedRoles = RoleDataReader.ReadRoleData();
            Debug.Log($"Step 3: Reloaded {reloadedRoles.Count} roles");
            
            // 4. 比较
            bool dataMatch = originalRoles.Count == reloadedRoles.Count;
            
            if (dataMatch)
            {
                Debug.Log("=== Round Trip Test: SUCCESS ===");
            }
            else
            {
                Debug.LogError("=== Round Trip Test: FAILED ===");
            }
        }
    }
}

