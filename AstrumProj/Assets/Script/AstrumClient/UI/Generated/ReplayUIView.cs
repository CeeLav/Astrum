// 此文件用于编写UI逻辑代码
// 第一次生成后，可以手动编辑，不会被重新生成覆盖

using UnityEngine;
using UnityEngine.UI;
using System;
using Astrum.Client.UI.Core;
using Astrum.Client.Managers.GameModes;
using Astrum.Client.Core;
using Astrum.CommonBase;

namespace Astrum.Client.UI.Generated
{
    /// <summary>
    /// ReplayUIView 逻辑部分
    /// 用于编写UI的业务逻辑代码
    /// </summary>
    public partial class ReplayUIView
    {
        #region Fields

        private ReplayGameMode _replayGameMode;
        private bool _isDraggingSlider = false;

        #endregion

        #region Virtual Methods

        /// <summary>
        /// 初始化完成后的回调
        /// </summary>
        protected override void OnInitialize()
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
                
                // 添加拖拽开始/结束事件（通过EventTrigger或直接监听）
                var eventTrigger = sliderSlider.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (eventTrigger == null)
                {
                    eventTrigger = sliderSlider.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                }

                // 拖拽开始
                var pointerDown = new UnityEngine.EventSystems.EventTrigger.Entry();
                pointerDown.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
                pointerDown.callback.AddListener((data) => { _isDraggingSlider = true; });
                eventTrigger.triggers.Add(pointerDown);

                // 拖拽结束
                var pointerUp = new UnityEngine.EventSystems.EventTrigger.Entry();
                pointerUp.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
                pointerUp.callback.AddListener((data) => 
                { 
                    _isDraggingSlider = false;
                    OnSliderDragEnd();
                });
                eventTrigger.triggers.Add(pointerUp);
            }

            // 初始化UI状态
            UpdateUI();
        }

        /// <summary>
        /// 显示时的回调
        /// </summary>
        protected override void OnShow()
        {
            // 通过 GameDirector 获取 ReplayGameMode 引用
            RefreshReplayGameMode();
            UpdateUI();
        }

        /// <summary>
        /// 隐藏时的回调
        /// </summary>
        protected override void OnHide()
        {
            _replayGameMode = null;
        }

        /// <summary>
        /// 更新回调（每帧调用）
        /// </summary>
        protected override void OnUpdate()
        {
            // 刷新 ReplayGameMode 引用（防止引用丢失）
            RefreshReplayGameMode();
            
            // 更新UI显示
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
            if (gameDirector != null && gameDirector.CurrentGameMode is ReplayGameMode replayMode)
            {
                _replayGameMode = replayMode;
            }
            else
            {
                _replayGameMode = null;
            }
        }

        /// <summary>
        /// 更新UI显示
        /// </summary>
        public void UpdateUI()
        {
            if (_replayGameMode == null) return;

            // 更新播放/暂停按钮状态
            if (playButton != null)
            {
                playButton.gameObject.SetActive(!_replayGameMode.IsPlaying);
            }

            if (pauseButton != null)
            {
                pauseButton.gameObject.SetActive(_replayGameMode.IsPlaying);
            }

            // 更新进度条（仅在非拖拽时更新）
            if (sliderSlider != null && !_isDraggingSlider)
            {
                sliderSlider.value = _replayGameMode.Progress;
            }

            // 更新帧数显示
            if (frameText != null)
            {
                int currentFrame = _replayGameMode.CurrentFrame;
                int totalFrames = _replayGameMode.TotalFrames;
                frameText.text = $"帧: {currentFrame} / {totalFrames}";
            }

            // 更新时间显示
            if (timeText != null)
            {
                float currentTime = _replayGameMode.CurrentTimeSeconds;
                float totalTime = _replayGameMode.DurationSeconds;
                timeText.text = $"{FormatTime(currentTime)} / {FormatTime(totalTime)}";
            }
        }

        /// <summary>
        /// 格式化时间显示（秒 -> MM:SS）
        /// </summary>
        private string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{minutes:D2}:{secs:D2}";
        }

        /// <summary>
        /// 播放按钮点击
        /// </summary>
        private void OnPlayButtonClicked()
        {
            if (_replayGameMode != null)
            {
                _replayGameMode.Play();
                UpdateUI();
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
                UpdateUI();
            }
        }

        /// <summary>
        /// 进度条值改变
        /// </summary>
        private void OnSliderValueChanged(float value)
        {
            // 拖拽时只更新显示，不立即跳转
            if (_isDraggingSlider)
            {
                // 可以在这里更新预览信息
            }
        }

        /// <summary>
        /// 进度条拖拽结束
        /// </summary>
        private void OnSliderDragEnd()
        {
            if (_replayGameMode != null && sliderSlider != null)
            {
                // 计算目标帧
                float progress = sliderSlider.value;
                int targetFrame = Mathf.RoundToInt(progress * _replayGameMode.TotalFrames);
                
                // 跳转到目标帧
                _replayGameMode.SeekToFrame(targetFrame);
                UpdateUI();
            }
        }

        #endregion
    }
}
