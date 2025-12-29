using Astrum.LogicCore.Core;
using Astrum.LogicCore.ViewRead;
using Astrum.LogicCore.Stats;
using TrueSync;

namespace Astrum.LogicCore.Components
{
    public partial class DynamicStatsComponent
    {
        private static bool _viewReadRegistered = false;
        private static readonly object _registerLock = new object();

        /// <summary>
        /// 延迟注册 ViewRead 导出器（避免与 MemoryPack 生成的静态构造函数冲突）
        /// </summary>
        private static void EnsureViewReadRegistered()
        {
            if (_viewReadRegistered) return;
            
            lock (_registerLock)
            {
                if (_viewReadRegistered) return;
                
                ViewReadFrameSync.Register<DynamicStatsComponent, ViewRead>(
                    componentTypeId: ComponentTypeId,
                    createViewRead: comp => new ViewRead(
                        entityId: comp.EntityId,
                        isValid: true,
                        currentHP: comp.Get(DynamicResourceType.CURRENT_HP),
                        currentMana: comp.Get(DynamicResourceType.CURRENT_MANA),
                        shield: comp.Get(DynamicResourceType.SHIELD),
                        energy: comp.Get(DynamicResourceType.ENERGY),
                        rage: comp.Get(DynamicResourceType.RAGE),
                        combo: comp.Get(DynamicResourceType.COMBO)),
                    createInvalid: entityId => ViewRead.Invalid(entityId)
                );
                
                _viewReadRegistered = true;
            }
        }

        public readonly struct ViewRead
        {
            public readonly long EntityId;
            public readonly bool IsValid;
            public readonly FP CurrentHP;
            public readonly FP CurrentMana;
            public readonly FP Shield;
            public readonly FP Energy;
            public readonly FP Rage;
            public readonly FP Combo;

            public ViewRead(long entityId, bool isValid, FP currentHP, FP currentMana, FP shield, FP energy, FP rage, FP combo)
            {
                EntityId = entityId;
                IsValid = isValid;
                CurrentHP = currentHP;
                CurrentMana = currentMana;
                Shield = shield;
                Energy = energy;
                Rage = rage;
                Combo = combo;
            }

            public static ViewRead Invalid(long entityId)
            {
                return new ViewRead(entityId, false, FP.Zero, FP.Zero, FP.Zero, FP.Zero, FP.Zero, FP.Zero);
            }
        }

        public static bool TryGetViewRead(World world, long entityId, out ViewRead read)
        {
            EnsureViewReadRegistered();
            
            if (world?.ViewReads == null)
            {
                read = default;
                return false;
            }

            return world.ViewReads.TryGet(entityId, ComponentTypeId, out read);
        }
    }
}



