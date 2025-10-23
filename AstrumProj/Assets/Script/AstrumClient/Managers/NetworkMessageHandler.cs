using System;
using System.Collections.Generic;
using Astrum.CommonBase;
using Astrum.Generated;
using UnityEngine;

namespace Astrum.Client.Managers
{
    /// <summary>
    /// 网络消息处理器 - 处理 Login/Room 相关的网络消息
    /// </summary>
    public class NetworkMessageHandler : Singleton<NetworkMessageHandler>
    {
        public void Initialize()
        {
            // 注册网络消息处理器
            RegisterNetworkMessageHandlers();
            
            ASLogger.Instance.Info("NetworkMessageHandler: 初始化完成");
        }

        /// <summary>
        /// 注册网络消息处理器（仅 Login/Room 相关）
        /// </summary>
        private void RegisterNetworkMessageHandlers()
        {
            var networkManager = NetworkManager.Instance;
            if (networkManager != null)
            {
                // 注册 Login 和 Room 相关的消息处理器
                networkManager.OnLoginResponse += OnLoginResponse;
                networkManager.OnCreateRoomResponse += OnCreateRoomResponse;
                networkManager.OnJoinRoomResponse += OnJoinRoomResponse;
                networkManager.OnLeaveRoomResponse += OnLeaveRoomResponse;
                networkManager.OnGetRoomListResponse += OnGetRoomListResponse;
                networkManager.OnRoomUpdateNotification += OnRoomUpdateNotification;
                networkManager.OnHeartbeatResponse += OnHeartbeatResponse;
                
                ASLogger.Instance.Info("NetworkMessageHandler: Login/Room 消息处理器注册完成");
            }
            else
            {
                ASLogger.Instance.Warning("NetworkMessageHandler: NetworkManager 不存在，无法注册消息处理器");
            }
        }

        /// <summary>
        /// 取消注册网络消息处理器
        /// </summary>
        private void UnregisterNetworkMessageHandlers()
        {
            var networkManager = NetworkManager.Instance;
            if (networkManager != null)
            {
                // 取消注册 Login 和 Room 相关的消息处理器
                networkManager.OnLoginResponse -= OnLoginResponse;
                networkManager.OnCreateRoomResponse -= OnCreateRoomResponse;
                networkManager.OnJoinRoomResponse -= OnJoinRoomResponse;
                networkManager.OnLeaveRoomResponse -= OnLeaveRoomResponse;
                networkManager.OnGetRoomListResponse -= OnGetRoomListResponse;
                networkManager.OnRoomUpdateNotification -= OnRoomUpdateNotification;
                networkManager.OnHeartbeatResponse -= OnHeartbeatResponse;
                
                ASLogger.Instance.Info("NetworkMessageHandler: Login/Room 消息处理器取消注册完成");
            }
        }

        public void Shutdown()
        {
            // 取消注册网络消息处理器
            UnregisterNetworkMessageHandlers();
            
            ASLogger.Instance.Info("NetworkMessageHandler: 已关闭");
        }

        #region 网络消息事件处理器

        /// <summary>
        /// 处理登录响应
        /// </summary>
        private void OnLoginResponse(LoginResponse response)
        {
            try
            {
                ASLogger.Instance.Info("NetworkMessageHandler: 收到登录响应");

                if (response.Success)
                {
                    // 使用用户管理器处理登录响应
                    UserManager.Instance?.HandleLoginResponse(response);
                }
                else
                {
                    ASLogger.Instance.Error($"NetworkMessageHandler: 登录失败 - {response.Message}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"NetworkMessageHandler: 处理登录响应时发生异常 - {ex.Message}");
            }
        }

        /// <summary>
        /// 处理创建房间响应
        /// </summary>
        private void OnCreateRoomResponse(CreateRoomResponse response)
        {
            try
            {
                ASLogger.Instance.Info("NetworkMessageHandler: 收到创建房间响应");

                if (response.Success)
                {
                    // 使用房间系统管理器处理创建房间响应
                    RoomSystemManager.Instance?.HandleCreateRoomResponse(response);
                }
                else
                {
                    ASLogger.Instance.Error($"NetworkMessageHandler: 创建房间失败 - {response.Message}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"NetworkMessageHandler: 处理创建房间响应时发生异常 - {ex.Message}");
            }
        }

        /// <summary>
        /// 处理加入房间响应
        /// </summary>
        private void OnJoinRoomResponse(JoinRoomResponse response)
        {
            try
            {
                ASLogger.Instance.Info("NetworkMessageHandler: 收到加入房间响应");

                if (response.Success)
                {
                    // 使用房间系统管理器处理加入房间响应
                    RoomSystemManager.Instance?.HandleJoinRoomResponse(response);
                }
                else
                {
                    ASLogger.Instance.Error($"NetworkMessageHandler: 加入房间失败 - {response.Message}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"NetworkMessageHandler: 处理加入房间响应时发生异常 - {ex.Message}");
            }
        }

        /// <summary>
        /// 处理离开房间响应
        /// </summary>
        private void OnLeaveRoomResponse(LeaveRoomResponse response)
        {
            try
            {
                ASLogger.Instance.Info("NetworkMessageHandler: 收到离开房间响应");

                if (response.Success)
                {
                    // 使用房间系统管理器处理离开房间响应
                    RoomSystemManager.Instance?.HandleLeaveRoomResponse(response);
                }
                else
                {
                    ASLogger.Instance.Error($"NetworkMessageHandler: 离开房间失败 - {response.Message}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"NetworkMessageHandler: 处理离开房间响应时发生异常 - {ex.Message}");
            }
        }

        /// <summary>
        /// 处理获取房间列表响应
        /// </summary>
        private void OnGetRoomListResponse(GetRoomListResponse response)
        {
            try
            {
                ASLogger.Instance.Info("NetworkMessageHandler: 收到获取房间列表响应");

                if (response.Success)
                {
                    // 使用房间系统管理器处理获取房间列表响应
                    RoomSystemManager.Instance?.HandleRoomListResponse(response);
                }
                else
                {
                    ASLogger.Instance.Error($"NetworkMessageHandler: 获取房间列表失败 - {response.Message}");
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"NetworkMessageHandler: 处理获取房间列表响应时发生异常 - {ex.Message}");
            }
        }

        /// <summary>
        /// 处理房间更新通知
        /// </summary>
        private void OnRoomUpdateNotification(RoomUpdateNotification notification)
        {
            try
            {
                ASLogger.Instance.Info("NetworkMessageHandler: 收到房间更新通知");

                // 使用房间系统管理器处理房间更新通知
                RoomSystemManager.Instance?.HandleRoomUpdateNotification(notification);
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"NetworkMessageHandler: 处理房间更新通知时发生异常 - {ex.Message}");
            }
        }

        /// <summary>
        /// 处理心跳响应
        /// </summary>
        private void OnHeartbeatResponse(HeartbeatResponse response)
        {
            try
            {
                ASLogger.Instance.Debug("NetworkMessageHandler: 收到心跳响应");
                // 心跳响应通常不需要特殊处理
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"NetworkMessageHandler: 处理心跳响应时发生异常 - {ex.Message}");
            }
        }

        #endregion
    }
}
