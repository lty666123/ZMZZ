using System;
using UnityEngine;

namespace ZMZZ.Combat
{
    /// <summary>
    /// 一次攻击的帧数据。前摇 / 判定 / 后摇是动作游戏的基础语言：
    /// 前摇决定「能不能被躲开」，判定决定「能不能打中」，后摇决定「打完挨不挨打」。
    /// </summary>
    [Serializable]
    public class AttackProfile
    {
        public string label = "attack";

        [Header("帧数（60fps 基准）")]
        [Tooltip("前摇：出招到判定生效。越长越容易被看破、被躲掉")]
        public int startupFrames = 8;
        [Tooltip("判定：能打到人的窗口")]
        public int activeFrames = 4;
        [Tooltip("后摇：判定结束后的僵直，是攻防转换的关键")]
        public int recoveryFrames = 14;

        [Header("伤害")]
        public float damage = 12f;
        [Tooltip("对韧性条的削减。弹反才是主要削韧手段，普通攻击只削一点点")]
        public float staggerDamage = 4f;
        public float knockback = 3f;
        public int hitStopFrames = 3;

        [Header("判定盒（相对朝向）")]
        public float boxWidth = 1.6f;
        public float boxHeight = 1.2f;
        public float boxOffsetX = 0.95f;
        public float boxOffsetY = 0.05f;

        [Header("行为")]
        [Tooltip("取消勾选 = 不可格挡，玩家只能用闪避躲（用来逼玩家用满工具箱）")]
        public bool blockable = true;
        [Tooltip("判定期间的向前位移速度，冲刺类招式用")]
        public float moveForwardSpeed = 0f;

        [Header("收招")]
        [Tooltip("这一招结束后的破绽窗口（帧）。Boss 重击后给玩家的输出机会")]
        public int breakWindowFrames = 0;

        public int TotalFrames
        {
            get { return startupFrames + activeFrames + recoveryFrames; }
        }
    }
}
