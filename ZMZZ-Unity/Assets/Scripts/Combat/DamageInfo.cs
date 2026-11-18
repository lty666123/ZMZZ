using UnityEngine;

namespace ZMZZ.Combat
{
    /// <summary>格挡判定的结果。</summary>
    public enum BlockOutcome
    {
        /// <summary>没有格挡（没按格挡、方向不对、或这一招不可格挡）。</summary>
        NotBlocked = 0,
        /// <summary>普通格挡成功：不吃伤害，但扣体力。</summary>
        Blocked = 1,
        /// <summary>弹反成功：不吃伤害、不扣体力，并且反击削对方韧性。</summary>
        Parried = 2,
        /// <summary>体力不够，被破防：吃伤害 + 长硬直。</summary>
        GuardBroken = 3,
    }

    /// <summary>一次攻击命中的全部信息，由攻击方构造、受击方解析。</summary>
    public struct DamageInfo
    {
        public Team attackerTeam;
        public Transform attacker;      // 攻击者根节点，用于计算击退方向
        public Hurtbox attackerHurtbox; // 攻击者的受击盒，被弹反时要削它的韧性
        public Vector2 hitPoint;        // 命中点，用于特效与数字
        public float damage;
        public float staggerDamage;     // 对韧性条的削减
        public float knockback;         // 击退力度
        public int hitStopFrames;
        public bool blockable;          // false = 只能闪避，格挡无效
        public string sourceName;       // 招式名，调试用
    }
}
