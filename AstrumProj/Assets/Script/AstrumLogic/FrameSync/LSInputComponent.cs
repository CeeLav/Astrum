using System.Collections.Generic;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;

namespace Astrum.LogicCore.FrameSync
{
    /// <summary>
    /// 帧同步输入组件，用于存储实体的输入数据
    /// </summary>
    public class LSInputComponent : BaseComponent
    {
        /// <summary>
        /// 玩家ID
        /// </summary>
        public long PlayerId { get; set; }

        /// <summary>
        /// 当前帧输入
        /// </summary>
        public LSInput? CurrentInput { get; private set; }

        /// <summary>
        /// 上一帧输入
        /// </summary>
        public LSInput? PreviousInput { get; private set; }

        /// <summary>
        /// 输入历史
        /// </summary>
        public List<LSInput> InputHistory { get; private set; } = new List<LSInput>();

        /// <summary>
        /// 最大历史记录数量
        /// </summary>
        public int MaxHistoryCount { get; set; } = 30;

        public LSInputComponent() : base() { }

        public LSInputComponent(int playerId) : base()
        {
            PlayerId = playerId;
        }

        public override void OnAttachToEntity(Entity entity)
        {
            PlayerId = entity.UniqueId;
            base.OnAttachToEntity(entity);    
        }

        /// <summary>
        /// 设置输入
        /// </summary>
        /// <param name="input">输入数据</param>
        public void SetInput(LSInput input)
        {
            if (input == null) return;

            // 保存上一帧输入
            PreviousInput = CurrentInput?.Clone();
            
            // 设置当前输入
            CurrentInput = input.Clone();

            // 添加到历史记录
            AddToHistory(CurrentInput);
        }

        /// <summary>
        /// 获取输入变化
        /// </summary>
        /// <returns>从上一帧到当前帧的输入变化</returns>
        public LSInput GetInputDelta()
        {
            var delta = new LSInput
            {
                PlayerId = PlayerId,
                Frame = CurrentInput?.Frame ?? 0
            };

            if (CurrentInput == null) return delta;

            if (PreviousInput == null)
            {
                // 如果没有上一帧输入，当前输入就是变化
                delta.MoveX = CurrentInput.MoveX;
                delta.MoveY = CurrentInput.MoveY;
                delta.Attack = CurrentInput.Attack;
                delta.Skill1 = CurrentInput.Skill1;
                delta.Skill2 = CurrentInput.Skill2;
            }
            else
            {
                // 计算变化
                delta.MoveX = CurrentInput.MoveX - PreviousInput.MoveX;
                delta.MoveY = CurrentInput.MoveY - PreviousInput.MoveY;
                delta.Attack = CurrentInput.Attack && !PreviousInput.Attack; // 按键按下
                delta.Skill1 = CurrentInput.Skill1 && !PreviousInput.Skill1;
                delta.Skill2 = CurrentInput.Skill2 && !PreviousInput.Skill2;
            }

            return delta;
        }

        /// <summary>
        /// 检查按键是否刚刚按下
        /// </summary>
        /// <param name="inputType">输入类型</param>
        /// <returns>是否刚刚按下</returns>
        public bool IsInputJustPressed(InputType inputType)
        {
            if (CurrentInput == null) return false;

            bool currentState = GetInputState(CurrentInput, inputType);
            bool previousState = PreviousInput != null ? GetInputState(PreviousInput, inputType) : false;

            return currentState && !previousState;
        }

        /// <summary>
        /// 检查按键是否刚刚释放
        /// </summary>
        /// <param name="inputType">输入类型</param>
        /// <returns>是否刚刚释放</returns>
        public bool IsInputJustReleased(InputType inputType)
        {
            if (PreviousInput == null) return false;

            bool currentState = CurrentInput != null ? GetInputState(CurrentInput, inputType) : false;
            bool previousState = GetInputState(PreviousInput, inputType);

            return !currentState && previousState;
        }

        /// <summary>
        /// 检查按键是否持续按住
        /// </summary>
        /// <param name="inputType">输入类型</param>
        /// <returns>是否持续按住</returns>
        public bool IsInputHeld(InputType inputType)
        {
            if (CurrentInput == null) return false;
            return GetInputState(CurrentInput, inputType);
        }

        /// <summary>
        /// 获取历史输入
        /// </summary>
        /// <param name="framesBack">往前多少帧</param>
        /// <returns>历史输入，如果不存在返回null</returns>
        public LSInput? GetHistoryInput(int framesBack)
        {
            if (framesBack < 0 || framesBack >= InputHistory.Count)
                return null;

            return InputHistory[InputHistory.Count - 1 - framesBack];
        }

        /// <summary>
        /// 清理输入历史
        /// </summary>
        public void ClearHistory()
        {
            InputHistory.Clear();
        }

        /// <summary>
        /// 添加到历史记录
        /// </summary>
        /// <param name="input">输入数据</param>
        private void AddToHistory(LSInput input)
        {
            InputHistory.Add(input.Clone());
            
            // 限制历史记录数量
            while (InputHistory.Count > MaxHistoryCount)
            {
                InputHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// 获取指定输入的状态
        /// </summary>
        /// <param name="input">输入数据</param>
        /// <param name="inputType">输入类型</param>
        /// <returns>输入状态</returns>
        private bool GetInputState(LSInput input, InputType inputType)
        {
            return inputType switch
            {
                InputType.Attack => input.Attack,
                InputType.Skill1 => input.Skill1,
                InputType.Skill2 => input.Skill2,
                _ => false
            };
        }
    }

    /// <summary>
    /// 输入类型枚举
    /// </summary>
    public enum InputType
    {
        Attack,
        Skill1,
        Skill2
    }
}
