using UnityEngine;

namespace ZMZZ.Combat
{
    /// <summary>
    /// 全部战斗数值的唯一出处。运行时改这里 = 立刻看到手感变化（Play 模式下改会实时生效）。
    /// 资产位置：Assets/Resources/CombatTuning.asset
    ///
    /// 调试建议：先调「前摇 / 判定 / 后摇」和 hitStop，这四个决定了 80% 的手感。
    /// </summary>
    [CreateAssetMenu(fileName = "CombatTuning", menuName = "ZMZZ/Combat Tuning")]
    public class CombatTuning : ScriptableObject
    {
        static CombatTuning _current;

        public static CombatTuning Current
        {
            get
            {
                if (_current == null)
                {
                    _current = Resources.Load<CombatTuning>("CombatTuning");
                    if (_current == null)
                    {
                        Debug.LogWarning("[ZMZZ] 没找到 Resources/CombatTuning.asset，使用内存中的默认值");
                        _current = CreateInstance<CombatTuning>();
                    }
                }
                return _current;
            }
        }

        [Header("玩家 · 移动")]
        public float moveSpeed = 6.5f;
        public float moveAccel = 90f;
        public float airAccel = 45f;
        public float jumpHeight = 3.2f;
        public float gravityScale = 3.6f;
        [Range(0f, 1f)] public float jumpCutMultiplier = 0.45f;
        [Tooltip("土狼时间：离开平台后仍能起跳的帧数")]
        public int coyoteFrames = 6;
        [Tooltip("跳跃缓冲：落地前提前按跳，落地瞬间自动起跳")]
        public int jumpBufferFrames = 6;
        public float maxFallSpeed = 26f;

        [Header("玩家 · 生命与体力")]
        public float playerMaxHealth = 100f;
        public float playerMaxStamina = 100f;
        public float staminaRegenPerSecond = 30f;
        public int staminaRegenDelayFrames = 30;
        [Tooltip("受击后的无敌帧")]
        public int playerInvulnFrames = 48;
        [Tooltip("受击硬直")]
        public int playerHurtFrames = 18;

        [Header("玩家 · 三连段")]
        public AttackProfile attack1 = new AttackProfile
        {
            label = "斩击一",
            startupFrames = 8, activeFrames = 4, recoveryFrames = 14,
            damage = 12f, staggerDamage = 4f, knockback = 2.5f, hitStopFrames = 3,
            boxWidth = 1.6f, boxHeight = 1.2f, boxOffsetX = 0.95f,
            blockable = true,
        };
        public AttackProfile attack2 = new AttackProfile
        {
            label = "斩击二",
            startupFrames = 8, activeFrames = 4, recoveryFrames = 16,
            damage = 12f, staggerDamage = 4f, knockback = 2.5f, hitStopFrames = 3,
            boxWidth = 1.7f, boxHeight = 1.3f, boxOffsetX = 1.0f,
            blockable = true,
        };
        public AttackProfile attack3 = new AttackProfile
        {
            label = "斩击三",
            startupFrames = 12, activeFrames = 6, recoveryFrames = 26,
            damage = 20f, staggerDamage = 8f, knockback = 5f, hitStopFrames = 5,
            boxWidth = 2.1f, boxHeight = 1.6f, boxOffsetX = 1.15f,
            blockable = true,
        };
        [Tooltip("连段输入缓冲：后摇期间提前按攻击键仍然接得上")]
        public int comboBufferFrames = 8;

        [Header("玩家 · 闪避")]
        public int dodgeFrames = 22;
        public int dodgeInvulnStart = 4;
        public int dodgeInvulnEnd = 16;
        public float dodgeSpeed = 14f;
        public float dodgeStaminaCost = 20f;
        public int dodgeCooldownFrames = 10;

        [Header("玩家 · 格挡与弹反")]
        [Tooltip("按格挡后的前 N 帧内被打到 = 弹反。这个数字是最需要反复试玩的一个")]
        public int parryWindowFrames = 8;
        public float blockStaminaCost = 14f;
        [Tooltip("体力不足时被打破防：清空体力 + 长硬直")]
        public int guardBreakStaminaThreshold = 1;
        public float blockMoveSpeedMultiplier = 0.45f;
        [Tooltip("举盾后至少维持的帧数，防止点一下就放")]
        public int blockMinHoldFrames = 12;
        public float blockRetreatScale = 0.35f;

        [Header("玩家 · 弹反的反击收益")]
        public float parryStaggerDamage = 22f;
        public int parryHitStopFrames = 12;
        [Tooltip("弹反成功后，攻击者被强行打断的帧数")]
        public int parryAttackerInterruptFrames = 40;
        public float parryShakeStrength = 0.45f;
        public float hitShakeStrength = 0.12f;
        public int guardBreakInterruptFrames = 50;
        [Range(0f, 1f)] public float guardBreakDamageScale = 0.6f;

        [Header("Boss · 属性")]
        public float bossMaxHealth = 450f;
        public float bossMaxStagger = 100f;
        public float bossStaggerBreakThreshold = 30f;
        public float bossStaggerRegenPerSecond = 9f;
        public int bossStaggerRegenDelayFrames = 180;
        public float bossBrokenDamageMultiplier = 1.5f;
        public int bossBrokenFrames = 240;
        public float bossMoveSpeed = 2.8f;
        public float bossPreferredDistance = 2.8f;
        public float bossRetreatDistance = 1.5f;
        public int bossThinkIntervalFrames = 10;
        [Tooltip("Boss 被击退的倍率，越小越像重甲单位")]
        public float bossKnockbackScale = 0.25f;
        [Tooltip("破防时的额外硬直帧数")]
        public int bossBrokenStunFrames = 240;

        [Header("Boss · 连招表")]
        public BossPattern[] bossPatterns = new BossPattern[]
        {
            new BossPattern
            {
                label = "三连斩",
                weight = 4, minRange = 0f, maxRange = 3.4f,
                stepGapFrames = 5, cooldownFrames = 150, recoveryFrames = 45,
                steps = new AttackProfile[]
                {
                    new AttackProfile
                    {
                        label = "三连斩·一",
                        startupFrames = 14, activeFrames = 4, recoveryFrames = 12,
                        damage = 8f, staggerDamage = 3f, knockback = 2.5f, hitStopFrames = 3,
                        boxWidth = 1.9f, boxHeight = 1.5f, boxOffsetX = 1.15f, boxOffsetY = 0.1f,
                        blockable = true,
                    },
                    new AttackProfile
                    {
                        label = "三连斩·二",
                        startupFrames = 13, activeFrames = 4, recoveryFrames = 12,
                        damage = 8f, staggerDamage = 3f, knockback = 2.5f, hitStopFrames = 3,
                        boxWidth = 1.9f, boxHeight = 1.5f, boxOffsetX = 1.15f, boxOffsetY = 0.1f,
                        blockable = true,
                    },
                    new AttackProfile
                    {
                        label = "三连斩·三",
                        startupFrames = 18, activeFrames = 5, recoveryFrames = 22,
                        damage = 12f, staggerDamage = 5f, knockback = 4.5f, hitStopFrames = 4,
                        boxWidth = 2.2f, boxHeight = 1.8f, boxOffsetX = 1.25f, boxOffsetY = 0.1f,
                        blockable = true,
                    },
                },
            },
            new BossPattern
            {
                label = "突进斩（不可格挡）",
                weight = 3, minRange = 3.0f, maxRange = 7.5f,
                stepGapFrames = 5, cooldownFrames = 220, recoveryFrames = 60,
                steps = new AttackProfile[]
                {
                    new AttackProfile
                    {
                        label = "突进斩",
                        startupFrames = 24, activeFrames = 10, recoveryFrames = 20,
                        damage = 14f, staggerDamage = 6f, knockback = 6f, hitStopFrames = 5,
                        boxWidth = 2.0f, boxHeight = 1.6f, boxOffsetX = 1.2f, boxOffsetY = 0.1f,
                        blockable = false,
                        moveForwardSpeed = 11f,
                    },
                },
            },
            new BossPattern
            {
                label = "重砸（有大破绽）",
                weight = 2, minRange = 0f, maxRange = 3.8f,
                stepGapFrames = 5, cooldownFrames = 260, recoveryFrames = 70,
                steps = new AttackProfile[]
                {
                    new AttackProfile
                    {
                        label = "重砸",
                        startupFrames = 30, activeFrames = 6, recoveryFrames = 30,
                        damage = 20f, staggerDamage = 8f, knockback = 5f, hitStopFrames = 6,
                        boxWidth = 2.8f, boxHeight = 2.4f, boxOffsetX = 1.35f, boxOffsetY = 0.2f,
                        blockable = true,
                        breakWindowFrames = 90,
                    },
                },
            },
        };
        public int bossGlobalCooldownFrames = 35;

        [Header("关卡与出生点")]
        public Vector2 playerSpawn = new Vector2(-6f, 1.2f);
        public Vector2 bossSpawn = new Vector2(6f, 1.7f);
        public float arenaHalfWidth = 14f;
    }
}
