using System;
using UnityEngine;

namespace ZMZZ.Combat
{
    /// <summary>生命值。只负责加减与广播，不做任何防守判定。</summary>
    public class Health : MonoBehaviour
    {
        public float maxHealth = 100f;

        public float Current { get; private set; }
        public bool IsDead { get { return Current <= 0f; } }
        public float Normalized { get { return maxHealth <= 0f ? 0f : Mathf.Clamp01(Current / maxHealth); } }

        /// <summary>(当前血量, 最大血量)</summary>
        public event Action<float, float> Changed;
        /// <summary>受到伤害时触发，参数为实际扣掉的血量</summary>
        public event Action<float> Damaged;
        public event Action Died;

        void Awake()
        {
            Current = maxHealth;
        }

        public void ResetToFull()
        {
            Current = maxHealth;
            Changed?.Invoke(Current, maxHealth);
        }

        /// <summary>返回实际扣掉的血量。</summary>
        public float ApplyDamage(float amount)
        {
            if (amount <= 0f || IsDead) return 0f;
            float before = Current;
            Current = Mathf.Max(0f, Current - amount);
            float dealt = before - Current;
            Changed?.Invoke(Current, maxHealth);
            Damaged?.Invoke(dealt);
            if (Current <= 0f) Died?.Invoke();
            return dealt;
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || IsDead) return;
            Current = Mathf.Min(maxHealth, Current + amount);
            Changed?.Invoke(Current, maxHealth);
        }
    }
}
