using UnityEngine;
using Astrum.CommonBase;
using Astrum.LogicCore.Core;
using Astrum.Client.Managers.GameModes;
using Astrum.Client.Core;

namespace Astrum.Client.UI
{
    /// <summary>
    /// 延迟显示组件 - 在屏幕上显示网络延迟和帧同步信息
    /// </summary>
    public class LatencyDisplay : MonoBehaviour
    {
        private ClientLSController _clientLSController;
        private bool _showLatency = true;
        private float _lastUpdateTime = 0f;
        private const float UPDATE_INTERVAL = 0.1f; // 每0.1秒更新一次引用，避免每帧查找
        
        private void Update()
        {
            // 按L键切换显示
            if (UnityEngine.Input.GetKeyDown(KeyCode.L))
            {
                _showLatency = !_showLatency;
            }
            
            // 定期更新ClientLSController引用（避免每帧查找，提高性能）
            if (Time.time - _lastUpdateTime > UPDATE_INTERVAL)
            {
                UpdateClientLSController();
                _lastUpdateTime = Time.time;
            }
        }
        
        /// <summary>
        /// 更新ClientLSController引用
        /// </summary>
        private void UpdateClientLSController()
        {
            // 尝试通过GameDirector获取当前GameMode
            try
            {
                var gameDirector = GameDirector.Instance;
                if (gameDirector?.CurrentGameMode is MultiplayerGameMode multiplayerMode)
                {
                    var room = multiplayerMode.MainRoom;
                    if (room?.LSController is ClientLSController clientLS)
                    {
                        // 如果引用发生变化，更新它
                        if (_clientLSController != clientLS)
                        {
                            _clientLSController = clientLS;
                        }
                        return; // 找到有效引用，退出
                    }
                }
            }
            catch
            {
                // 如果获取失败，忽略错误，下次再试
            }
            
            // 如果没有找到有效引用，清空当前引用
            if (_clientLSController != null)
            {
                _clientLSController = null;
            }
        }
        
        private void OnGUI()
        {
            if (!_showLatency) return;
            
            if (_clientLSController == null)
            {
                GUI.Label(new Rect(10, 10, 300, 20), "延迟信息: 未连接");
                return;
            }
            
            // 获取延迟信息
            long rtt = TimeInfo.Instance.ServerTimeDiff;
            int authorityFrame = _clientLSController.AuthorityFrame;
            int processedAuthorityFrame = _clientLSController.ProcessedAuthorityFrame;
            int predictionFrame = _clientLSController.PredictionFrame;
            
            // 计算帧延迟
            int frameDelay = predictionFrame - processedAuthorityFrame;
            
            // 显示延迟信息（左上角）
            int yPos = 10;
            int lineHeight = 20;
            
            GUI.color = Color.white;
            GUI.Label(new Rect(10, yPos, 400, lineHeight), $"网络延迟 (RTT): {rtt}ms");
            yPos += lineHeight;
            
            
            GUI.color = Color.white;
            GUI.Label(new Rect(10, yPos, 400, lineHeight), $"权威帧: {authorityFrame}");
            yPos += lineHeight;
            
            GUI.Label(new Rect(10, yPos, 400, lineHeight), $"已处理权威帧: {processedAuthorityFrame}");
            yPos += lineHeight;
            
            GUI.Label(new Rect(10, yPos, 400, lineHeight), $"预测帧: {predictionFrame}");
            yPos += lineHeight;
            
            GUI.color = frameDelay > 5 ? Color.red : (frameDelay > 3 ? Color.yellow : Color.green);
            GUI.Label(new Rect(10, yPos, 400, lineHeight), $"帧延迟: {frameDelay} 帧");
            yPos += lineHeight;
            
            GUI.color = Color.gray;
            GUI.Label(new Rect(10, yPos, 400, lineHeight), "按 L 键切换显示");
        }
        
    }
}

