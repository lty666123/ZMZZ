using System;
using UnityEngine;

namespace ZMZZ.Combat
{
    /// <summary>
    /// 体力。闪避与格挡共用同一条资源 —— 这样玩家必须做取舍，而不是无脑滚。
    /// 停手一段时间后开始回复。
    /// </summary>
    public class Stamina : MonoBehaviour
    {
        public float max = 100f;
        public float regenPerSecond = 30f;
        public int regenDelayFrames = 30;

        public float Current { get; private set; }
        public bool IsEmpty { get { return Current <= 0.01f; } }
        public float Normalized { get { return max <= 0f ? 0f : Mathf.Clamp01(Current / max); } }

        public event Action<float, float> Changed;

        int _frame;
        int _lastSpendFrame = -9999;

        void Awake()
        {
            Current = max;
        }

        void FixedUpdate()
        {
            _frame++;
            if (_frame - _lastSpendFrame >= regenDelayFrames && Current < max)
            {
                Current = Mathf.Min(max, Current + regenPerSecond * Time.fixedDeltaTime);
                Changed?.Invoke(Current, max);
            }
        }

        public bool Has(float amount)
        {
            return Current >= amount;
        }

        public void ResetToFull()
        {
            Current = max;
            _lastSpendFrame = -9999;
            Changed?.Invoke(Current, max);
        }

        /// <summary>够就扣掉并返回 true；不够则不扣、返回 false。</summary>
        public bool TrySpend(float amount)
        {
            if (Current < amount) return false;
            Spend(amount);
            return true;
        }

        /// <summary>无视余额直接扣（用于被破防时清空）。</summary>
        public void Spend(float amount)
        {
            Current = Mathf.Max(0f, Current - amount);
            _lastSpendFrame = _frame;
            Changed?.Invoke(Current, max);
        }

        public float SecondsSinceLastSpend
        {
            get { return (_frame - _lastSpendFrame) * Time.fixedDeltaTime; }
        }
    }
}
