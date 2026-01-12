using Astrum.LogicCore.Core;
using Astrum.LogicCore.ViewRead;
using Astrum.LogicCore.Stats;
using TrueSync;

namespace Astrum.LogicCore.Components
{
    public partial class DerivedStatsComponent
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
                
                ViewReadFrameSync.Register<DerivedStatsComponent, ViewRead>(
                    componentTypeId: ComponentTypeId,
                    createViewRead: comp => new ViewRead(
                        entityId: comp.EntityId,
                        isValid: true,
                        maxHP: comp.Get(StatType.HP),
                        maxMana: comp.Get(StatType.MAX_MANA),
                        attack: comp.Get(StatType.ATK),
                        defense: comp.Get(StatType.DEF),
                        speed: comp.Get(StatType.SPD)),
                    createInvalid: entityId => ViewRead.Invalid(entityId)
                );
                
                _viewReadRegistered = true;
            }
        }

        public readonly struct ViewRead
        {
            public readonly long EntityId;
            public readonly bool IsValid;
            public readonly FP MaxHP;
            public readonly FP MaxMana;
            public readonly FP Attack;
            public readonly FP Defense;
            public readonly FP Speed;

            public ViewRead(long entityId, bool isValid, FP maxHP, FP maxMana, FP attack, FP defense, FP speed)
            {
                EntityId = entityId;
                IsValid = isValid;
                MaxHP = maxHP;
                MaxMana = maxMana;
                Attack = attack;
                Defense = defense;
                Speed = speed;
            }

            public static ViewRead Invalid(long entityId)
            {
                return new ViewRead(entityId, false, FP.Zero, FP.Zero, FP.Zero, FP.Zero, FP.Zero);
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





