using System;
using System.Collections.Generic;
using System.IO;
using Astrum.CommonBase;
using cfg;
using Luban;
using cfg.Entity;

namespace Astrum.LogicCore.Managers
{
    /// <summary>
    /// 配置管理器，负责加载和管理游戏配置数据
    /// </summary>
    public class ConfigManager : Singleton<ConfigManager>
    {
        private Tables _tables;
        private bool _isInitialized = false;
        private string _configPath;

        /// <summary>
        /// 配置表是否已初始化
        /// </summary>
        public new bool IsInitialized => _isInitialized;

        /// <summary>
        /// 配置表实例
        /// </summary>
        public Tables Tables => _tables;

        /// <summary>
        /// 初始化配置管理器
        /// </summary>
        /// <param name="configPath">配置文件路径</param>
        public void Initialize(string configPath = "Config")
        {
            if (_isInitialized)
            {
                ASLogger.Instance.Warning("ConfigManager already initialized");
                return;
            }

            try
            {
                _configPath = configPath;
                ASLogger.Instance.Info($"Initializing ConfigManager with path: {configPath}");
                
                // 创建 Tables 实例
                _tables = new Tables(LoadBytes);
                
                _isInitialized = true;
                ASLogger.Instance.Info("ConfigManager initialized successfully");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"Failed to initialize ConfigManager: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 加载 .bytes 文件
        /// </summary>
        /// <param name="fileName">文件名（不含扩展名）</param>
        /// <returns>ByteBuf 对象</returns>
        private ByteBuf LoadBytes(string fileName)
        {
            try
            {
                var filePath = $"{_configPath}/{fileName}.bytes";
                
                if (File.Exists(filePath))
                {
                    var bytes = File.ReadAllBytes(filePath);
                    ASLogger.Instance.Debug($"Loaded config file: {filePath}", "Config.Load");
                    return new ByteBuf(bytes);
                }
                else
                {
                    ASLogger.Instance.Warning($"Config file not found: {filePath}");
                    return new ByteBuf();
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"Failed to load config file {fileName}.bytes: {ex.Message}");
                return new ByteBuf();
            }
        }

        #region 通用配置访问

        /// <summary>
        /// 重新加载配置
        /// </summary>
        /// <param name="configPath">配置文件路径</param>
        public void ReloadConfig(string configPath = null)
        {
            try
            {
                ASLogger.Instance.Info("Reloading configuration...");
                
                // 清理现有配置
                _tables = null;
                _isInitialized = false;
                
                // 重新初始化
                Initialize(configPath ?? _configPath);
                
                ASLogger.Instance.Info("Configuration reloaded successfully");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"Failed to reload configuration: {ex.Message}");
                throw;
            }
        }



        /// <summary>
        /// 获取配置路径
        /// </summary>
        /// <returns>配置路径</returns>
        public string GetConfigPath()
        {
            return _configPath;
        }

        /// <summary>
        /// 检查配置文件是否存在
        /// </summary>
        /// <param name="fileName">文件名（不含扩展名）</param>
        /// <returns>是否存在</returns>
        public bool HasConfigFile(string fileName)
        {
            if (!_isInitialized || string.IsNullOrEmpty(fileName))
            {
                return false;
            }

            var filePath = $"{_configPath}/{fileName}.bytes";
            return File.Exists(filePath);
        }

        /// <summary>
        /// 获取配置文件的字节大小
        /// </summary>
        /// <param name="fileName">文件名（不含扩展名）</param>
        /// <returns>字节大小，如果不存在返回0</returns>
        public long GetConfigFileSize(string fileName)
        {
            if (!_isInitialized || string.IsNullOrEmpty(fileName))
            {
                return 0;
            }

            var filePath = $"{_configPath}/{fileName}.bytes";
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                return fileInfo.Length;
            }

            return 0;
        }

        /// <summary>
        /// 获取配置目录下的所有 .bytes 文件
        /// </summary>
        /// <returns>文件名列表（不含扩展名）</returns>
        public List<string> GetAvailableConfigFiles()
        {
            var configFiles = new List<string>();
            
            if (!_isInitialized || !Directory.Exists(_configPath))
            {
                return configFiles;
            }

            try
            {
                var files = Directory.GetFiles(_configPath, "*.bytes", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    configFiles.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"Error getting available config files: {ex.Message}");
            }

            return configFiles;
        }

        #endregion

        /// <summary>
        /// 关闭配置管理器
        /// </summary>
        public void Shutdown()
        {
            if (!_isInitialized) return;

            try
            {
                _tables = null;
                _isInitialized = false;
                _configPath = null;
                
                ASLogger.Instance.Info("ConfigManager shutdown completed");
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"Error during ConfigManager shutdown: {ex.Message}");
            }
        }
    }
}