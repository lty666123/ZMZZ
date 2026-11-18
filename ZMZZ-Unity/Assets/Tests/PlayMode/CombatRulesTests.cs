using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ZMZZ.Combat;
using ZMZZ.Core;

namespace ZMZZ.Tests
{
    /// <summary>
    /// 战斗规则的自动化验证。这些是「设计承诺」，必须能自动跑出来：
    /// 弹反要削韧、韧性见底要破防、破防期间伤害要放大、无敌帧要真的免伤。
    /// </summary>
    public class CombatRulesTests
    {
        /// <summary>测试用的替身：模拟玩家的防守能力，行为与 PlayerCombat 的规则一致。</summary>
        class FakeGuard : MonoBehaviour, IDamageGuard
        {
            public bool Invulnerable;
            public BlockOutcome Result = BlockOutcome.NotBlocked;

            public bool IsInvulnerable { get { return Invulnerable; } }

            public BlockOutcome EvaluateBlock(ref DamageInfo info)
            {
                if (!info.blockable) return BlockOutcome.NotBlocked;
                return Result;
            }

            public void OnBlockResolved(ref DamageInfo info, BlockOutcome outcome) { }
        }

        class FakeInterruptible : MonoBehaviour, IInterruptible
        {
            public int InterruptCount;
            public int LastFrames;
            public string LastReason;

            public void Interrupt(int frames, string reason)
            {
                InterruptCount++;
                LastFrames = frames;
                LastReason = reason;
            }
        }

        static GameObject CreateCombatant(string name, Team team, float maxHealth, bool withStagger,
                                          float staggerThreshold = 30f)
        {
            var go = new GameObject(name);
            go.layer = team == Team.Player ? GameLayers.Player : GameLayers.Enemy;

            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;

            var health = go.AddComponent<Health>();
            health.maxHealth = maxHealth;
            health.ResetToFull();

            var hurtbox = go.AddComponent<Hurtbox>();
            hurtbox.team = team;
            hurtbox.owner = go.transform;
            hurtbox.health = health;
            hurtbox.body = body;

            if (withStagger)
            {
                var stagger = go.AddComponent<Stagger>();
                stagger.maxStagger = 100f;
                stagger.breakThreshold = staggerThreshold;
                stagger.brokenDamageMultiplier = 1.5f;
                stagger.brokenFrames = 240;
                stagger.regenDelayFrames = 180;
                stagger.ResetToFull();
                hurtbox.stagger = stagger;
            }

            return go;
        }

        static DamageInfo MakeInfo(GameObject attacker, GameObject target, float damage, float staggerDamage,
                                   bool blockable = true)
        {
            var info = new DamageInfo();
            info.attackerTeam = attacker.GetComponent<Hurtbox>().team;
            info.attacker = attacker.transform;
            info.attackerHurtbox = attacker.GetComponent<Hurtbox>();
            info.hitPoint = target.transform.position;
            info.damage = damage;
            info.staggerDamage = staggerDamage;
            info.knockback = 0f;
            info.hitStopFrames = 0;
            info.blockable = blockable;
            info.sourceName = "test";
            return info;
        }

        static readonly CombatTuning Tuning = CombatTuning.Current;

        [Test]
        public void Hit_ReducesHealthAndStagger()
        {
            var attacker = CreateCombatant("Attacker", Team.Player, 100f, false);
            var target = CreateCombatant("Target", Team.Enemy, 100f, true);
            var targetStagger = target.GetComponent<Stagger>();

            var info = MakeInfo(attacker, target, 20f, 10f);
            var result = DamageResolver.Resolve(ref info, target.GetComponent<Hurtbox>());

            Assert.IsTrue(result.Consumed);
            Assert.AreEqual(80f, target.GetComponent<Health>().Current, 0.01f);
            Assert.AreEqual(90f, targetStagger.Current, 0.01f);

            Object.DestroyImmediate(attacker);
            Object.DestroyImmediate(target);
        }

        [Test]
        public void StaggerBelowThreshold_EntersBroken_AndDamageIsAmplified()
        {
            var attacker = CreateCombatant("Attacker", Team.Player, 100f, false);
            var target = CreateCombatant("Target", Team.Enemy, 100f, true);
            var targetStagger = target.GetComponent<Stagger>();

            // 削到阈值以下：100 - 80 = 20 <= 30
            bool broken = targetStagger.ApplyStaggerDamage(80f);
            Assert.IsTrue(broken, "韧性跌破阈值后应该进入破防");
            Assert.IsTrue(targetStagger.IsBroken);

            // 破防期间 20 点伤害应该变成 30 点
            var info = MakeInfo(attacker, target, 20f, 0f);
            DamageResolver.Resolve(ref info, target.GetComponent<Hurtbox>());

            Assert.AreEqual(70f, target.GetComponent<Health>().Current, 0.01f);

            Object.DestroyImmediate(attacker);
            Object.DestroyImmediate(target);
        }

        [Test]
        public void Parry_PunishesAttacker_AndTargetTakesNoDamage()
        {
            var attacker = CreateCombatant("Attacker", Team.Player, 100f, true);
            var target = CreateCombatant("Target", Team.Enemy, 100f, true);

            var attackerStagger = attacker.GetComponent<Stagger>();
            var interruptible = attacker.AddComponent<FakeInterruptible>();
            attacker.GetComponent<Hurtbox>().interruptible = interruptible;

            var guard = target.AddComponent<FakeGuard>();
            guard.Result = BlockOutcome.Parried;
            target.GetComponent<Hurtbox>().guard = guard;

            var info = MakeInfo(attacker, target, 20f, 5f);
            var result = DamageResolver.Resolve(ref info, target.GetComponent<Hurtbox>());

            Assert.AreEqual(BlockOutcome.Parried, result.Outcome);
            Assert.AreEqual(0f, result.DamageDealt, 0.01f);
            Assert.AreEqual(100f, target.GetComponent<Health>().Current, 0.01f);

            // 弹反的两大收益：削攻击者韧性 + 打断它的连招
            Assert.AreEqual(100f - Tuning.parryStaggerDamage, attackerStagger.Current, 0.01f);
            Assert.AreEqual(1, interruptible.InterruptCount, "弹反必须能打断攻击者的连招");
            Assert.AreEqual(Tuning.parryAttackerInterruptFrames, interruptible.LastFrames);

            Object.DestroyImmediate(attacker);
            Object.DestroyImmediate(target);
        }

        [Test]
        public void Block_DoesNotDamage_ButStillConsumesAttack()
        {
            var attacker = CreateCombatant("Attacker", Team.Player, 100f, true);
            var target = CreateCombatant("Target", Team.Enemy, 100f, true);

            var guard = target.AddComponent<FakeGuard>();
            guard.Result = BlockOutcome.Blocked;
            target.GetComponent<Hurtbox>().guard = guard;

            var info = MakeInfo(attacker, target, 20f, 5f);
            var result = DamageResolver.Resolve(ref info, target.GetComponent<Hurtbox>());

            Assert.AreEqual(BlockOutcome.Blocked, result.Outcome);
            Assert.AreEqual(100f, target.GetComponent<Health>().Current, 0.01f);
            Assert.IsTrue(result.Consumed);

            Object.DestroyImmediate(attacker);
            Object.DestroyImmediate(target);
        }

        [Test]
        public void UnblockableAttack_IgnoresGuard()
        {
            var attacker = CreateCombatant("Attacker", Team.Player, 100f, true);
            var target = CreateCombatant("Target", Team.Enemy, 100f, true);

            var guard = target.AddComponent<FakeGuard>();
            guard.Result = BlockOutcome.Parried;   // 即使替身想弹反，也不该生效
            target.GetComponent<Hurtbox>().guard = guard;

            var info = MakeInfo(attacker, target, 20f, 5f, blockable: false);
            var result = DamageResolver.Resolve(ref info, target.GetComponent<Hurtbox>());

            Assert.AreEqual(BlockOutcome.NotBlocked, result.Outcome);
            Assert.AreEqual(80f, target.GetComponent<Health>().Current, 0.01f, "不可格挡的招式必须直接命中");

            Object.DestroyImmediate(attacker);
            Object.DestroyImmediate(target);
        }

        [Test]
        public void InvulnerableTarget_IgnoresHit_AndAttackIsNotConsumed()
        {
            var attacker = CreateCombatant("Attacker", Team.Player, 100f, true);
            var target = CreateCombatant("Target", Team.Enemy, 100f, true);

            var guard = target.AddComponent<FakeGuard>();
            guard.Invulnerable = true;
            target.GetComponent<Hurtbox>().guard = guard;

            var info = MakeInfo(attacker, target, 20f, 5f);
            var result = DamageResolver.Resolve(ref info, target.GetComponent<Hurtbox>());

            Assert.IsFalse(result.Consumed);
            Assert.AreEqual(100f, target.GetComponent<Health>().Current, 0.01f);

            Object.DestroyImmediate(attacker);
            Object.DestroyImmediate(target);
        }

        [UnityTest]
        public IEnumerator Stagger_RegeneratesAfterDelay()
        {
            var target = CreateCombatant("Target", Team.Enemy, 100f, true);
            var stagger = target.GetComponent<Stagger>();
            stagger.regenDelayFrames = 0;
            stagger.regenPerSecond = 100f;

            stagger.ApplyStaggerDamage(40f);
            Assert.AreEqual(60f, stagger.Current, 0.01f);

            // 必须等「物理步长」而不是等渲染帧：无头模式下帧率极高，
            // 等 10 个渲染帧可能连一个 FixedUpdate 都没发生。
            for (int i = 0; i < 15; i++) yield return new WaitForFixedUpdate();

            Assert.Greater(stagger.Current, 60f,
                "韧性必须在停止挨打之后回复，否则 Boss 战会退化成站龟");

            Object.DestroyImmediate(target);
        }
    }
}
