namespace Astrum.LogicCore.Components
{
    /// <summary>
    /// 生命值组件，管理实体的生命值
    /// </summary>
    public class HealthComponent : BaseComponent
    {
        /// <summary>
        /// 当前生命值
        /// </summary>
        public int CurrentHealth { get; set; }

        /// <summary>
        /// 最大生命值
        /// </summary>
        public int MaxHealth { get; set; }

        /// <summary>
        /// 是否已死亡
        /// </summary>
        public bool IsDead => CurrentHealth <= 0;

        /// <summary>
        /// 生命值百分比
        /// </summary>
        public float HealthPercentage => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f;

        public HealthComponent() : base() { }

        public HealthComponent(int maxHealth) : base()
        {
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        /// <summary>
        /// 受到伤害
        /// </summary>
        /// <param name="damage">伤害值</param>
        /// <returns>实际受到的伤害</returns>
        public int TakeDamage(int damage)
        {
            if (damage <= 0) return 0;

            int actualDamage = Math.Min(damage, CurrentHealth);
            CurrentHealth -= actualDamage;
            
            if (CurrentHealth < 0)
                CurrentHealth = 0;

            return actualDamage;
        }

        /// <summary>
        /// 治疗
        /// </summary>
        /// <param name="healAmount">治疗量</param>
        /// <returns>实际治疗量</returns>
        public int Heal(int healAmount)
        {
            if (healAmount <= 0 || IsDead) return 0;

            int actualHeal = Math.Min(healAmount, MaxHealth - CurrentHealth);
            CurrentHealth += actualHeal;

            return actualHeal;
        }

        /// <summary>
        /// 设置最大生命值
        /// </summary>
        /// <param name="newMaxHealth">新的最大生命值</param>
        /// <param name="healToFull">是否治疗到满血</param>
        public void SetMaxHealth(int newMaxHealth, bool healToFull = false)
        {
            MaxHealth = Math.Max(1, newMaxHealth);
            
            if (healToFull)
            {
                CurrentHealth = MaxHealth;
            }
            else if (CurrentHealth > MaxHealth)
            {
                CurrentHealth = MaxHealth;
            }
        }

        /// <summary>
        /// 完全恢复生命值
        /// </summary>
        public void FullHeal()
        {
            CurrentHealth = MaxHealth;
        }
    }
}
