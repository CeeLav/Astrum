using System.Collections.Generic;
using Astrum.Generated;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Capabilities;
using MemoryPack;
using Astrum.CommonBase;

namespace Astrum.LogicCore.FrameSync
{
    /// <summary>
    /// 帧同步输入组件，用于存储实体的输入数据
    /// </summary>
    [MemoryPackable]
    public partial class LSInputComponent : BaseComponent
    {
        /// <summary>
        /// 组件类型 ID（基于 TypeHash 的稳定哈希值，编译期常量）
        /// </summary>
        public static readonly int ComponentTypeId = TypeHash<LSInputComponent>.GetHash();
        
        /// <summary>
        /// 获取组件的类型 ID
        /// </summary>
        public override int GetComponentTypeId() => ComponentTypeId;
        [MemoryPackIgnore]
        private bool _hasLoggedFirstNonZeroInput;

        /// <summary>
        /// 玩家ID
        /// </summary>
        public long PlayerId { get; set; }

        /// <summary>
        /// 当前帧输入
        /// </summary>
        public LSInput CurrentInput { get; private set; }

        /// <summary>
        /// 上一帧输入
        /// </summary>
        public LSInput PreviousInput { get; private set; }

        /// <summary>
        /// 输入历史
        /// </summary>
        public List<LSInput> InputHistory { get; private set; } = new List<LSInput>();

        /// <summary>
        /// 最大历史记录数量
        /// </summary>
        public int MaxHistoryCount { get; set; } = 30;

        [MemoryPackConstructor]
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
            // 只在首次收到非零输入时输出一条日志，避免刷屏
            if (!_hasLoggedFirstNonZeroInput)
            {
                bool hasMove = input.MoveX != 0 || input.MoveY != 0;
                bool hasButton = input.Dash || input.Attack || input.Skill1 || input.Skill2;
                if (hasMove || hasButton)
                {
                    _hasLoggedFirstNonZeroInput = true;
                    //ASLogger.Instance.Info(
                    //    $"LSInputComponent: PlayerId={PlayerId} 首次收到输入 Frame={input.Frame}, MoveX={input.MoveX}, MoveY={input.MoveY}, Dash={input.Dash}, Attack={input.Attack}, Skill1={input.Skill1}, Skill2={input.Skill2}");
                }
            }

            // 保存上一帧输入
            PreviousInput = CurrentInput;
            
            // 设置当前输入
            CurrentInput = input;

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
                Frame = CurrentInput.Frame
            };
            
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

            bool currentState = GetInputState(CurrentInput, inputType);
            bool previousState =  GetInputState(PreviousInput, inputType) ;

            return currentState && !previousState;
        }

        /// <summary>
        /// 检查按键是否刚刚释放
        /// </summary>
        /// <param name="inputType">输入类型</param>
        /// <returns>是否刚刚释放</returns>
        public bool IsInputJustReleased(InputType inputType)
        {

            bool currentState = GetInputState(CurrentInput, inputType);
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
            InputHistory.Add(input);
            
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

        /// <summary>
        /// 重写 ToString 方法，避免循环引用
        /// </summary>
        /// <returns>字符串表示</returns>
        public override string ToString()
        {
            return $"LSInputComponent[PlayerId={PlayerId}, CurrentFrame={CurrentInput?.Frame ?? -1}, HistoryCount={InputHistory.Count}]";
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
