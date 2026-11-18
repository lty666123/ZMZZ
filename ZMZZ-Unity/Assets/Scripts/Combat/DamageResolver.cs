using UnityEngine;
using ZMZZ.CameraSystem;
using ZMZZ.Core;

namespace ZMZZ.Combat
{
    public struct HitResult
    {
        /// <summary>这次攻击是否被「消费」掉了（命中了、被格挡了、被弹反了都算）。</summary>
        public bool Consumed;
        public BlockOutcome Outcome;
        public float DamageDealt;
    }

    /// <summary>
    /// 伤害规则的唯一裁决处。攻击顺序：无敌 → 弹反 → 格挡 → 破防 → 命中。
    /// 把规则集中在一处，是为了以后调平衡时不用满项目找散落的伤害代码。
    /// </summary>
    public static class DamageResolver
    {
        public static HitResult Resolve(ref DamageInfo info, Hurtbox target)
        {
            var result = new HitResult { Outcome = BlockOutcome.NotBlocked };
            if (target == null || target.IsDead) return result;

            var tuning = CombatTuning.Current;
            var guard = target.guard;

            // 1. 无敌帧：什么都不发生，但攻击不算消费，后续帧还能命中
            if (guard != null && guard.IsInvulnerable) return result;

            // 2. 防守判定
            BlockOutcome outcome = BlockOutcome.NotBlocked;
            if (guard != null) outcome = guard.EvaluateBlock(ref info);
            result.Outcome = outcome;

            switch (outcome)
            {
                case BlockOutcome.Parried:
                {
                    // 弹反成功：攻击者被削韧 + 打断连招，防守方毫发无伤
                    var attackerBox = info.attackerHurtbox;
                    if (attackerBox != null)
                    {
                        if (attackerBox.stagger != null)
                            attackerBox.stagger.ApplyStaggerDamage(tuning.parryStaggerDamage);
                        if (attackerBox.interruptible != null)
                            attackerBox.interruptible.Interrupt(tuning.parryAttackerInterruptFrames, "被弹反");
                    }

                    HitStopManager.Instance?.Request(tuning.parryHitStopFrames);
                    CameraShake.Instance?.AddImpulse(tuning.parryShakeStrength, 0.30f);
                    HitSpark.Spawn(info.hitPoint, new Color(1f, 0.92f, 0.35f, 1f), 0.7f, 2.6f, 0.28f);
                    guard.OnBlockResolved(ref info, outcome);
                    result.Consumed = true;
                    return result;
                }

                case BlockOutcome.Blocked:
                {
                    // 普通格挡：不吃伤害，但消耗体力并退后一点
                    guard.OnBlockResolved(ref info, outcome);
                    ApplyKnockback(ref info, target, 0.35f);
                    HitStopManager.Instance?.Request(Mathf.Max(2, info.hitStopFrames - 1));
                    CameraShake.Instance?.AddImpulse(tuning.hitShakeStrength * 0.7f, 0.10f);
                    HitSpark.Spawn(info.hitPoint, new Color(0.75f, 0.85f, 1f, 1f), 0.5f, 1.6f, 0.18f);
                    result.Consumed = true;
                    return result;
                }

                case BlockOutcome.GuardBroken:
                {
                    // 体力见底：格挡失败，吃一部分伤害 + 长硬直
                    guard.OnBlockResolved(ref info, outcome);
                    float reduced = info.damage * tuning.guardBreakDamageScale;
                    result.DamageDealt = ApplyHit(ref info, target, reduced);
                    result.Consumed = true;
                    return result;
                }
            }

            // 3. 直接命中
            float scaled = info.damage * target.DamageMultiplier;
            result.DamageDealt = ApplyHit(ref info, target, scaled);
            result.Consumed = true;
            return result;
        }

        static float ApplyHit(ref DamageInfo info, Hurtbox target, float damage)
        {
            var tuning = CombatTuning.Current;
            float dealt = target.health != null ? target.health.ApplyDamage(damage) : 0f;
            if (target.stagger != null) target.stagger.ApplyStaggerDamage(info.staggerDamage);

            ApplyKnockback(ref info, target, 1f);
            HitStopManager.Instance?.Request(info.hitStopFrames);
            CameraShake.Instance?.AddImpulse(tuning.hitShakeStrength, 0.12f);

            Color sparkColor = target.DamageMultiplier > 1.001f
                ? new Color(1f, 0.45f, 0.25f, 1f)     // 破防期间的命中更红
                : new Color(1f, 1f, 1f, 1f);
            HitSpark.Spawn(info.hitPoint, sparkColor, 0.5f, 2.0f, 0.20f);
            return dealt;
        }

        static void ApplyKnockback(ref DamageInfo info, Hurtbox target, float scale)
        {
            if (target.body == null) return;
            float direction = info.attacker != null
                ? Mathf.Sign(target.Center.x - info.attacker.position.x)
                : 1f;
            if (Mathf.Approximately(direction, 0f)) direction = 1f;
            float force = info.knockback * scale * target.knockbackScale;
            var velocity = target.body.velocity;
            target.body.velocity = new Vector2(velocity.x + direction * force, velocity.y + force * 0.15f);
        }
    }
}
