using UnityEngine;
using System;
using Astrum.CommonBase;
using Astrum.View.Components;
using Astrum.View.Core;

namespace Astrum.View.EntityViews
{
    /// <summary>
    /// 单位视图
    /// 负责单位实体的视觉表现
    /// </summary>
    public class UnitView : EntityView
    {
        // 单位类型
        private string _unitType = "";
        
        // Unity组件引用
        private Renderer _modelRenderer;
        private Animator _animator;
        private GameObject _healthBarUI;
        private GameObject _selectionIndicator;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="unitType">单位类型</param>
        public UnitView(string unitType = "") : base("Unit")
        {
            _unitType = unitType;
        }


        protected override void OnInitialize()
        {
            
        }

        protected override void OnUpdateView(float deltaTime)
        {
            
        }

        protected override void OnSyncWithEntity(long entityId)
        {
            
        }

        /// <summary>
        /// 获取UnitView需要的视图组件类型列表
        /// </summary>
        public override Type[] GetRequiredViewComponentTypes()
        {
            return new Type[]
            {
                typeof(ModelViewComponent), // 管理模型表现
                typeof(TransViewComponent), // 管理位置
                typeof(AnimationViewComponent) // 管理动画播放
                // 你可以根据需要添加更多组件类型，如血条、选中指示等
            };
        }
    }
    

}
