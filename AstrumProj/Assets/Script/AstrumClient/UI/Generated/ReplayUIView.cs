// 此文件用于编写UI逻辑代码
// 第一次生成后，可以手动编辑，不会被重新生成覆盖

using UnityEngine;
using UnityEngine.UI;
using System;
using Astrum.Client.UI.Core;
using Astrum.Client.Core;
using Astrum.Client.Managers.GameModes;

namespace Astrum.Client.UI.Generated
{
    /// <summary>
    /// ReplayUIView 逻辑部分
    /// 用于编写UI的业务逻辑代码
    /// </summary>
    public partial class ReplayUIView : UIBase
    {
        #region Fields

        /// <summary>
        /// 缓存的 ReplayGameMode 引用
        /// </summary>
        private ReplayGameMode _replayGameMode;

        /// <summary>
        /// 是否正在拖动进度条（避免拖动时触发跳转）
        /// </summary>
        private bool _isDraggingSlider = false;

        #endregion

        #region Virtual Methods

        /// <summary>
        /// 初始化完成后的回调
        /// </summary>
        protected virtual void OnInitialize()
        {
            // 绑定按钮事件
            if (playButton != null)
            {
                playButton.onClick.AddListener(OnPlayButtonClicked);
            }
            
            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(OnPauseButtonClicked);
            }
            
            // 绑定进度条事件
            if (sliderSlider != null)
            {
                sliderSlider.onValueChanged.AddListener(OnSliderValueChanged);
                
                // 添加 EventTrigger 以处理拖动开始和结束
                var eventTrigger = sliderSlider.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (eventTrigger == null)
                {
                    eventTrigger = sliderSlider.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                }
                
                var pointerDown = new UnityEngine.EventSystems.EventTrigger.Entry();
                pointerDown.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
                pointerDown.callback.AddListener((data) => { _isDraggingSlider = true; });
                eventTrigger.triggers.Add(pointerDown);
                
                var pointerUp = new UnityEngine.EventSystems.EventTrigger.Entry();
                pointerUp.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
                pointerUp.callback.AddListener((data) => { OnSliderDragEnd(); });
                eventTrigger.triggers.Add(pointerUp);
            }
            
            // 初始化 UI 显示
            RefreshReplayGameMode();
            UpdateUI();
        }

        /// <summary>
        /// 显示时的回调
        /// </summary>
        protected virtual void OnShow()
        {
            RefreshReplayGameMode();
            UpdateUI();
        }

        /// <summary>
        /// 隐藏时的回调
        /// </summary>
        protected virtual void OnHide()
        {
            // 清理引用
            _replayGameMode = null;
        }

        /// <summary>
        /// 更新回调（由 UIManager.Update() 驱动）
        /// </summary>
        public override void Update()
        {
            if (!IsInitialized || uiRefs == null || !uiRefs.gameObject.activeInHierarchy)
                return;
            
            // 移除 Update 中的输入检测，改用 EventTrigger
            
            RefreshReplayGameMode();
            UpdateUI();
        }

        #endregion

        #region Business Logic

        /// <summary>
        /// 刷新 ReplayGameMode 引用（从 GameDirector 获取）
        /// </summary>
        private void RefreshReplayGameMode()
        {
            var gameDirector = GameDirector.Instance;
            if (gameDirector == null)
            {
                _replayGameMode = null;
                return;
            }
            
            _replayGameMode = gameDirector.CurrentGameMode as ReplayGameMode;
        }

        /// <summary>
        /// 更新 UI 显示
        /// </summary>
        private void UpdateUI()
        {
            if (_replayGameMode == null)
            {
                // 如果没有 ReplayGameMode，隐藏或禁用 UI
                if (playButton != null) playButton.interactable = false;
                if (pauseButton != null) pauseButton.interactable = false;
                if (sliderSlider != null) sliderSlider.interactable = false;
                if (frameText != null) frameText.text = "0 / 0";
                if (timeText != null) timeText.text = "00:00 / 00:00";
                return;
            }
            
            // 更新播放/暂停按钮状态
            bool isPlaying = _replayGameMode.IsPlaying;
            if (playButton != null)
            {
                playButton.interactable = !isPlaying;
                playButton.gameObject.SetActive(!isPlaying);
            }
            if (pauseButton != null)
            {
                pauseButton.interactable = isPlaying;
                pauseButton.gameObject.SetActive(isPlaying);
            }
            
            // 更新进度条（仅在非拖动状态下）
            if (sliderSlider != null && !_isDraggingSlider)
            {
                float progress = _replayGameMode.Progress;
                sliderSlider.value = progress;
            }
            
            // 更新帧数显示
            if (frameText != null)
            {
                int currentFrame = _replayGameMode.CurrentFrame;
                int totalFrames = _replayGameMode.TotalFrames;
                frameText.text = $"{currentFrame} / {totalFrames}";
            }
            
            // 更新时间显示（相对时间，从0开始）
            if (timeText != null)
            {
                float currentTime = _replayGameMode.CurrentTimeSeconds;
                float duration = _replayGameMode.DurationSeconds;
                string currentTimeStr = FormatTime(currentTime);
                string durationStr = FormatTime(duration);
                timeText.text = $"{currentTimeStr} / {durationStr}";
            }
        }

        /// <summary>
        /// 格式化时间（秒）为 mm:ss 或 hh:mm:ss 格式
        /// </summary>
        private string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.FloorToInt(seconds);
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int secs = totalSeconds % 60;
            
            if (hours > 0)
            {
                return $"{hours:D2}:{minutes:D2}:{secs:D2}";
            }
            else
            {
                return $"{minutes:D2}:{secs:D2}";
            }
        }

        /// <summary>
        /// 播放按钮点击
        /// </summary>
        private void OnPlayButtonClicked()
        {
            if (_replayGameMode != null)
            {
                _replayGameMode.Play();
            }
        }

        /// <summary>
        /// 暂停按钮点击
        /// </summary>
        private void OnPauseButtonClicked()
        {
            if (_replayGameMode != null)
            {
                _replayGameMode.Pause();
            }
        }

        /// <summary>
        /// 进度条值变化（拖动时）
        /// </summary>
        private void OnSliderValueChanged(float value)
        {
            // 标记正在拖动
            _isDraggingSlider = true;
            
            // 注意：这里不立即跳转，等待拖动结束
            // Unity Slider 没有 onEndDrag，需要在 Update 中检测鼠标释放
        }

        /// <summary>
        /// 进度条拖动结束（需要在 Update 中检测）
        /// </summary>
        private void OnSliderDragEnd()
        {
            if (_replayGameMode != null && sliderSlider != null)
            {
                float progress = sliderSlider.value;
                _replayGameMode.Seek(progress);
            }
            
            _isDraggingSlider = false;
        }

        #endregion
    }
}
