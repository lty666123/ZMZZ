using System;
using UnityEngine;

namespace ZMZZ.Combat
{
    /// <summary>
    /// 韧性条（精英与 Boss 专属，小怪不挂这个组件）。
    /// 弹反削一大截，普通攻击只削一点点；跌破阈值就「破防」，期间受到的伤害增加。
    /// 韧性必须会回复，否则玩家可以龟缩慢慢磨死 Boss。
    /// </summary>
    public class Stagger : MonoBehaviour
    {
        public float maxStagger = 100f;
        [Tooltip("跌到这个值以下就破防")]
        public float breakThreshold = 30f;
        public float regenPerSecond = 9f;
        [Tooltip("多久没被削韧之后开始回复")]
        public int regenDelayFrames = 180;

        [Header("破防")]
        public float brokenDamageMultiplier = 1.5f;
        public int brokenFrames = 240;

        public float Current { get; private set; }
        public bool IsBroken { get; private set; }
        public float Normalized { get { return maxStagger <= 0f ? 0f : Mathf.Clamp01(Current / maxStagger); } }

        /// <summary>(当前韧性, 最大韧性)</summary>
        public event Action<float, float> Changed;
        public event Action Broken;
        public event Action Recovered;

        int _frame;
        int _lastStaggerHitFrame = -9999;
        int _brokenFramesLeft;

        public float DamageMultiplier
        {
            get { return IsBroken ? brokenDamageMultiplier : 1f; }
        }

        void Awake()
        {
            Current = maxStagger;
        }

        void FixedUpdate()
        {
            _frame++;

            if (IsBroken)
            {
                _brokenFramesLeft--;
                if (_brokenFramesLeft <= 0) EndBroken();
                return;
            }

            if (_frame - _lastStaggerHitFrame >= regenDelayFrames && Current < maxStagger)
            {
                Current = Mathf.Min(maxStagger, Current + regenPerSecond * Time.fixedDeltaTime);
                Raise();
            }
        }

        public void ResetToFull()
        {
            IsBroken = false;
            _brokenFramesLeft = 0;
            Current = maxStagger;
            Raise();
        }

        /// <summary>削韧。返回是否因此进入破防。</summary>
        public bool ApplyStaggerDamage(float amount)
        {
            if (IsBroken || amount <= 0f) return false;

            _lastStaggerHitFrame = _frame;
            Current = Mathf.Max(0f, Current - amount);
            Raise();

            if (Current <= breakThreshold)
            {
                EnterBroken();
                return true;
            }
            return false;
        }

        void EnterBroken()
        {
            IsBroken = true;
            _brokenFramesLeft = brokenFrames;
            Broken?.Invoke();
        }

        void EndBroken()
        {
            IsBroken = false;
            _brokenFramesLeft = 0;
            Current = maxStagger;
            _lastStaggerHitFrame = _frame;
            Raise();
            Recovered?.Invoke();
        }

        void Raise()
        {
            Changed?.Invoke(Current, maxStagger);
        }
    }
}
