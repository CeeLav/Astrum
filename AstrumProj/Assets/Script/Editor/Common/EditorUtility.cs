using System;
using System.IO;
using UnityEngine;

namespace Astrum.Editor
{
    /// <summary>
    /// 编辑器通用工具类 - 可被所有编辑器工具使用
    /// </summary>
    public static class AstrumEditorUtility
    {
        private const string LOG_PREFIX = "[AstrumEditor]";
        
        // ===== 路径工具 =====
        
        /// <summary>将Unity项目路径转换为绝对路径</summary>
        public static string ToAbsolutePath(string unityPath)
        {
            if (string.IsNullOrEmpty(unityPath)) return string.Empty;
            
            if (unityPath.StartsWith("Assets/"))
            {
                unityPath = unityPath.Substring(7);
            }
            
            return Path.Combine(Application.dataPath, unityPath);
        }
        
        /// <summary>将绝对路径转换为Unity项目路径</summary>
        public static string ToUnityPath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) return string.Empty;
            
            string dataPath = Application.dataPath;
            if (absolutePath.StartsWith(dataPath))
            {
                string relativePath = absolutePath.Substring(dataPath.Length);
                return "Assets" + relativePath.Replace("\\", "/");
            }
            
            return absolutePath.Replace("\\", "/");
        }
        
        /// <summary>确保目录存在</summary>
        public static void EnsureDirectoryExists(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Debug.Log($"{LOG_PREFIX} Created directory: {directory}");
            }
        }
        
        /// <summary>备份文件</summary>
        public static bool BackupFile(string filePath, int keepCount = 5)
        {
            try
            {
                if (!File.Exists(filePath)) return false;
                
                string directory = Path.GetDirectoryName(filePath);
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string extension = Path.GetExtension(filePath);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupPath = Path.Combine(directory, $"{fileName}_backup_{timestamp}{extension}");
                
                File.Copy(filePath, backupPath, true);
                Debug.Log($"{LOG_PREFIX} Backup created: {backupPath}");
                
                CleanOldBackups(directory, fileName, extension, keepCount);
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to backup file: {filePath}\n{ex}");
                return false;
            }
        }
        
        /// <summary>清理旧备份文件</summary>
        private static void CleanOldBackups(string directory, string fileName, string extension, int keepCount)
        {
            try
            {
                string pattern = $"{fileName}_backup_*{extension}";
                var backupFiles = Directory.GetFiles(directory, pattern);
                
                if (backupFiles.Length > keepCount)
                {
                    Array.Sort(backupFiles);
                    int deleteCount = backupFiles.Length - keepCount;
                    
                    for (int i = 0; i < deleteCount; i++)
                    {
                        File.Delete(backupFiles[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{LOG_PREFIX} Failed to clean old backups: {ex.Message}");
            }
        }
        
        /// <summary>生成唯一ID</summary>
        public static int GenerateUniqueId(int startValue, System.Collections.Generic.HashSet<int> existingIds)
        {
            int id = startValue;
            while (existingIds.Contains(id))
            {
                id++;
            }
            return id;
        }
        
        /// <summary>格式化时间（帧转秒）</summary>
        public static string FormatFrameTime(int frame, int frameRate)
        {
            float seconds = (float)frame / frameRate;
            return $"{seconds:F2}s (Frame {frame})";
        }
    }
}

