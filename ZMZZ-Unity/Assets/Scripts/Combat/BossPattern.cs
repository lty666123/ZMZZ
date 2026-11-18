using System;
using UnityEngine;

namespace ZMZZ.Combat
{
    /// <summary>Boss 的一套连招：若干招按顺序打出，中间留出固定间隔。</summary>
    [Serializable]
    public class BossPattern
    {
        public string label = "pattern";
        [Tooltip("随机挑选时的权重，越大越常出")]
        public int weight = 1;

        [Header("触发距离")]
        public float minRange = 0f;
        public float maxRange = 3.2f;

        [Header("节奏")]
        [Tooltip("两次出招之间的间隔帧")]
        public int stepGapFrames = 5;
        [Tooltip("整套连招打完之后的下一次冷却")]
        public int cooldownFrames = 150;
        [Tooltip("整套打完后额外附加的硬直，作为玩家的输出窗口")]
        public int recoveryFrames = 50;

        [Header("招式序列")]
        public AttackProfile[] steps = new AttackProfile[0];

        public int TotalFrames
        {
            get
            {
                int total = 0;
                for (int i = 0; i < steps.Length; i++)
                {
                    total += steps[i].TotalFrames;
                    if (i < steps.Length - 1) total += stepGapFrames;
                }
                return total;
            }
        }
    }
}
