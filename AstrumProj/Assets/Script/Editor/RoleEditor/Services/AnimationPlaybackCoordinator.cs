using System;
using Astrum.Editor.RoleEditor.Modules;
using Astrum.Editor.RoleEditor.Timeline;

namespace Astrum.Editor.RoleEditor.Services
{
    /// <summary>
    /// 协调时间轴与动画预览之间的播放同步，避免循环依赖。
    /// </summary>
    public sealed class AnimationPlaybackCoordinator : IDisposable
    {
        private AnimationPreviewModule _preview;
        private TimelineEditorModule _timeline;
        private bool _isTimelineDriving;
        private bool _isPlaybackDriving;

        public event Action<int> OnPreviewFrameChanged;

        public void Attach(AnimationPreviewModule preview, TimelineEditorModule timeline)
        {
            Detach();

            _preview = preview;
            _timeline = timeline;

            if (_preview != null)
            {
                _preview.PreviewFrameAdvanced += HandlePreviewFrameAdvanced;
            }
        }

        public void Detach()
        {
            if (_preview != null)
            {
                _preview.PreviewFrameAdvanced -= HandlePreviewFrameAdvanced;
            }

            _preview = null;
            _timeline = null;
            _isTimelineDriving = false;
            _isPlaybackDriving = false;
        }

        public void HandleTimelineFrameChanged(int frame)
        {
            if (_preview == null)
                return;

            if (_isPlaybackDriving)
                return;

            if (_preview.IsPlaying())
                return;

            _isTimelineDriving = true;
            _preview.SetFrame(frame);
            _isTimelineDriving = false;
        }

        private void HandlePreviewFrameAdvanced(int frame)
        {
            if (_timeline != null)
            {
                _isPlaybackDriving = true;
                _timeline.SetCurrentFrameFromPlayback(frame);
                _isPlaybackDriving = false;
            }

            if (!_isTimelineDriving)
            {
                OnPreviewFrameChanged?.Invoke(frame);
            }
        }

        public void Dispose()
        {
            Detach();
        }
    }
}

