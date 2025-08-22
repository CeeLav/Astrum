using System;
using System.Threading.Tasks;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.Client.Core;
using UnityEngine;
using MemoryPack;

namespace Astrum.Client.Managers
{
    /// <summary>
    /// 客户端用户管理器 - 管理用户状态和身份
    /// </summary>
    public class UserManager : Singleton<UserManager>
    {
        // 用户状态
        public UserInfo CurrentUser { get; private set; }
        public bool IsLoggedIn => CurrentUser != null;
        public string UserId => CurrentUser?.Id ?? "";
        public string DisplayName => CurrentUser?.DisplayName ?? "";
        
        // 事件
        public event Action<UserInfo> OnUserLoggedIn;
        public event Action OnUserLoggedOut;
        public event Action<string> OnLoginError;
        
        // 登录状态
        private bool isLoggingIn = false;
        
        /// <summary>
        /// 初始化用户管理器
        /// </summary>
        public void Initialize()
        {
            ASLogger.Instance.Info("UserManager: 初始化完成");
        }
        
        /// <summary>
        /// 自动登录（连接成功后调用）
        /// </summary>
        public async Task<bool> AutoLoginAsync()
        {
            if (IsLoggedIn)
            {
                ASLogger.Instance.Warning("UserManager: 用户已登录");
                return true;
            }
            
            if (isLoggingIn)
            {
                ASLogger.Instance.Warning("UserManager: 正在登录中，请稍候");
                return false;
            }
            
            try
            {
                isLoggingIn = true;
                ASLogger.Instance.Info("UserManager: 开始自动登录...");
                
                // 创建登录请求
                var loginRequest = LoginRequest.Create();
                loginRequest.DisplayName = $"Player_{UnityEngine.Random.Range(1000, 9999)}";
                
                // 发送登录请求
                var networkManager = GameApplication.Instance?.NetworkManager;
                if (networkManager == null)
                {
                    ASLogger.Instance.Error("UserManager: NetworkManager不存在");
                    OnLoginError?.Invoke("网络管理器不存在");
                    return false;
                }
                
                if (!networkManager.IsConnected())
                {
                    ASLogger.Instance.Error("UserManager: 网络未连接");
                    OnLoginError?.Invoke("网络未连接");
                    return false;
                }
                
                // 直接发送登录请求
                networkManager.Send(loginRequest);
                ASLogger.Instance.Info("UserManager: 登录请求已发送");
                
                // 等待登录响应（这里需要等待服务器响应）
                // 实际实现中，登录成功会通过消息回调处理
                return true;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"UserManager: 自动登录失败 - {ex.Message}");
                OnLoginError?.Invoke($"登录失败: {ex.Message}");
                return false;
            }
            finally
            {
                isLoggingIn = false;
            }
        }
        
        /// <summary>
        /// 处理登录响应
        /// </summary>
        public void HandleLoginResponse(LoginResponse response)
        {
            try
            {
                if (response.Success)
                {
                    // 使用响应中的用户信息
                    CurrentUser = response.User;
                    if (CurrentUser != null)
                    {
                        ASLogger.Instance.Info($"UserManager: 用户登录成功 - ID: {CurrentUser.Id}, Name: {CurrentUser.DisplayName}");
                        
                        // 触发登录成功事件
                        OnUserLoggedIn?.Invoke(CurrentUser);
                    }
                    else
                    {
                        ASLogger.Instance.Error("UserManager: 登录响应中用户信息为空");
                        OnLoginError?.Invoke("登录响应中用户信息为空");
                    }
                }
                else
                {
                    ASLogger.Instance.Error($"UserManager: 登录失败 - {response.Message}");
                    OnLoginError?.Invoke(response.Message);
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"UserManager: 处理登录响应时出错 - {ex.Message}");
                OnLoginError?.Invoke($"处理登录响应失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 登出
        /// </summary>
        public void Logout()
        {
            if (!IsLoggedIn)
            {
                ASLogger.Instance.Warning("UserManager: 用户未登录");
                return;
            }
            
            ASLogger.Instance.Info("UserManager: 用户登出");
            
            CurrentUser = null;
            OnUserLoggedOut?.Invoke();
        }
        
        /// <summary>
        /// 更新用户房间信息
        /// </summary>
        public void UpdateUserRoom(string roomId)
        {
            if (CurrentUser != null)
            {
                CurrentUser.CurrentRoomId = roomId;
                ASLogger.Instance.Info($"UserManager: 用户房间更新 - RoomId: {roomId}");
            }
        }
        
        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            CurrentUser = null;
            OnUserLoggedIn = null;
            OnUserLoggedOut = null;
            OnLoginError = null;
        }
    }
}
