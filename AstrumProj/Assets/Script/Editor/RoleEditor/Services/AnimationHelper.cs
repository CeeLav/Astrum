using UnityEngine;
using UnityEditor;
using Animancer;

namespace Astrum.Editor.RoleEditor.Services
{
    /// <summary>
    /// Animancer动画工具类 - 封装Animancer常用操作
    /// </summary>
    public static class AnimationHelper
    {
        private const string LOG_PREFIX = "[AnimationHelper]";
        
        /// <summary>
        /// 获取或添加AnimancerComponent
        /// </summary>
        public static AnimancerComponent GetOrAddAnimancer(GameObject gameObject)
        {
            if (gameObject == null)
            {
                Debug.LogError($"{LOG_PREFIX} GameObject is null");
                return null;
            }
            
            // 检查是否已有AnimancerComponent
            var animancer = gameObject.GetComponent<AnimancerComponent>();
            
            if (animancer == null)
            {
                // 添加AnimancerComponent
                animancer = gameObject.AddComponent<AnimancerComponent>();
                
                // 获取Animator组件
                var animator = gameObject.GetComponent<Animator>();
                if (animator != null)
                {
                    animancer.Animator = animator;
                    Debug.Log($"{LOG_PREFIX} Added AnimancerComponent and linked Animator");
                }
                else
                {
                    Debug.LogWarning($"{LOG_PREFIX} No Animator found on GameObject");
                }
            }
            
            return animancer;
        }
        
        /// <summary>
        /// 通过ActionID播放动画
        /// </summary>
        public static AnimancerState PlayAnimationByActionId(
            AnimancerComponent animancer, 
            int actionId, 
            float fadeTime = 0.25f)
        {
            if (animancer == null)
            {
                Debug.LogError($"{LOG_PREFIX} AnimancerComponent is null");
                return null;
            }
            
            if (actionId <= 0)
            {
                Debug.LogWarning($"{LOG_PREFIX} Invalid actionId: {actionId}");
                return null;
            }
            
            // 1. 加载动画片段
            var animClip = ConfigTableHelper.LoadAnimationClip(actionId);
            if (animClip == null)
            {
                Debug.LogWarning($"{LOG_PREFIX} Failed to load animation for actionId {actionId}");
                return null;
            }
            
            // 2. 播放动画
            try
            {
                var state = animancer.Play(animClip, fadeTime);
                
                if (state != null)
                {
                    state.Speed = 1.0f;
                    Debug.Log($"{LOG_PREFIX} Playing animation for actionId {actionId}: {animClip.name}");
                }
                
                return state;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to play animation: {ex}");
                return null;
            }
        }
        
        /// <summary>
        /// 直接播放动画片段
        /// </summary>
        public static AnimancerState PlayAnimation(
            AnimancerComponent animancer,
            AnimationClip clip,
            float fadeTime = 0.25f,
            float speed = 1.0f)
        {
            if (animancer == null || clip == null)
                return null;
            
            try
            {
                var state = animancer.Play(clip, fadeTime);
                
                if (state != null)
                {
                    state.Speed = speed;
                }
                
                return state;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to play animation clip: {ex}");
                return null;
            }
        }
        
        /// <summary>
        /// 手动更新Animancer（预览模式必需）
        /// </summary>
        public static void EvaluateAnimancer(AnimancerComponent animancer, float deltaTime)
        {
            if (animancer == null) return;
            
            try
            {
                animancer.Evaluate(deltaTime);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to evaluate Animancer: {ex}");
            }
        }
        
        /// <summary>
        /// 停止当前动画
        /// </summary>
        public static void StopAnimation(AnimancerComponent animancer)
        {
            if (animancer == null) return;
            
            try
            {
                animancer.Stop();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to stop animation: {ex}");
            }
        }
        
        /// <summary>
        /// 检查Animancer是否正在播放
        /// </summary>
        public static bool IsPlaying(AnimancerComponent animancer)
        {
            if (animancer == null) return false;
            
            return animancer.IsGraphPlaying;
        }
    }
}

