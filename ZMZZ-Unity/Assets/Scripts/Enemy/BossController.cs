using UnityEngine;
using ZMZZ.CameraSystem;
using ZMZZ.Combat;
using ZMZZ.Core;

namespace ZMZZ.Enemy
{
    public enum BossState
    {
        Approach = 0,
        Pattern = 1,
        /// <summary>连招打完后的硬直，也就是玩家的输出窗口</summary>
        Recover = 2,
        /// <summary>被弹反打断</summary>
        Stunned = 3,
        Broken = 4,
        Dead = 5,
    }

    /// <summary>
    /// Boss：连招驱动的近战 AI。
    /// 设计要点：所有招式都有颜色区分的前摇（黄 = 可格挡可弹反，红 = 只能闪避），
    /// 并且连招可以被弹反打断 —— 这是玩家「读懂它」之后得到的回报。
    /// </summary>
    public class BossController : MonoBehaviour, IInterruptible
    {
        public Transform Target;
        public Rigidbody2D Body;
        public AttackRunner Runner;
        public Health Health;
        public Stagger Stagger;
        public ColorFlash Flash;

        public BossState State { get; private set; } = BossState.Approach;

        static readonly Color TelegraphBlockable = new Color(1f, 0.85f, 0.25f, 0.55f);
        static readonly Color TelegraphUnblockable = new Color(1f, 0.25f, 0.18f, 0.65f);
        static readonly Color BrokenTint = new Color(1f, 0.55f, 0.9f, 0.5f);
        static readonly Color DeadTint = new Color(0.25f, 0.15f, 0.2f, 0.7f);

        CombatTuning _tuning;
        BossPattern _pattern;
        int[] _patternCooldowns;
        int _stepIndex;
        int _gapFrames;
        int _recoverFramesLeft;
        int _stunFramesLeft;
        int _thinkCooldown;
        float _facing = -1f;
        string _actionLabel = "待机";

        public string ActionLabel { get { return _actionLabel; } }
        public BossPattern CurrentPattern { get { return _pattern; } }

        void Awake()
        {
            _tuning = CombatTuning.Current;

            if (Health != null)
            {
                Health.maxHealth = _tuning.bossMaxHealth;
                Health.ResetToFull();
                Health.Died += OnDied;
            }

            if (Stagger != null)
            {
                Stagger.maxStagger = _tuning.bossMaxStagger;
                Stagger.breakThreshold = _tuning.bossStaggerBreakThreshold;
                Stagger.regenPerSecond = _tuning.bossStaggerRegenPerSecond;
                Stagger.regenDelayFrames = _tuning.bossStaggerRegenDelayFrames;
                Stagger.brokenDamageMultiplier = _tuning.bossBrokenDamageMultiplier;
                Stagger.brokenFrames = _tuning.bossBrokenFrames;
                Stagger.ResetToFull();
                Stagger.Broken += OnStaggerBroken;
                Stagger.Recovered += OnStaggerRecovered;
            }

            if (Body != null) Body.gravityScale = _tuning.gravityScale;

            _patternCooldowns = new int[_tuning.bossPatterns.Length];
        }

        void OnDestroy()
        {
            if (Health != null) Health.Died -= OnDied;
            if (Stagger != null)
            {
                Stagger.Broken -= OnStaggerBroken;
                Stagger.Recovered -= OnStaggerRecovered;
            }
        }

        void FixedUpdate()
        {
            var tuning = CombatTuning.Current;

            for (int i = 0; i < _patternCooldowns.Length; i++)
                if (_patternCooldowns[i] > 0) _patternCooldowns[i]--;

            if (_thinkCooldown > 0) _thinkCooldown--;

            switch (State)
            {
                case BossState.Approach: TickApproach(); break;
                case BossState.Pattern: TickPattern(); break;
                case BossState.Recover:
                    StopHorizontal();
                    if (--_recoverFramesLeft <= 0) State = BossState.Approach;
                    break;
                case BossState.Stunned:
                    StopHorizontal();
                    if (--_stunFramesLeft <= 0) State = BossState.Approach;
                    break;
                case BossState.Broken:
                    StopHorizontal();
                    if (--_stunFramesLeft <= 0) State = BossState.Approach;
                    break;
                case BossState.Dead:
                    StopHorizontal();
                    break;
            }

            UpdateVisuals();
        }

        void TickApproach()
        {
            var tuning = CombatTuning.Current;
            if (Target == null) return;

            FaceTarget();

            float distance = Mathf.Abs(Target.position.x - transform.position.x);
            float direction = Mathf.Sign(Target.position.x - transform.position.x);
            var velocity = Body.velocity;

            if (distance > tuning.bossPreferredDistance)
            {
                velocity.x = direction * tuning.bossMoveSpeed;
                _actionLabel = "接近";
            }
            else if (distance < tuning.bossRetreatDistance)
            {
                velocity.x = -direction * tuning.bossMoveSpeed * 0.6f;
                _actionLabel = "后撤";
            }
            else
            {
                velocity.x = 0f;
                _actionLabel = "对峙";
            }

            Body.velocity = velocity;

            if (_thinkCooldown > 0) return;
            _thinkCooldown = tuning.bossThinkIntervalFrames;

            int chosen = ChoosePattern(distance);
            if (chosen >= 0) StartPattern(chosen);
        }

        int ChoosePattern(float distance)
        {
            var tuning = CombatTuning.Current;
            int totalWeight = 0;
            for (int i = 0; i < tuning.bossPatterns.Length; i++)
            {
                if (_patternCooldowns[i] > 0) continue;
                var p = tuning.bossPatterns[i];
                if (distance < p.minRange || distance > p.maxRange) continue;
                if (p.steps == null || p.steps.Length == 0) continue;
                totalWeight += Mathf.Max(1, p.weight);
            }
            if (totalWeight <= 0) return -1;

            int roll = Random.Range(0, totalWeight);
            for (int i = 0; i < tuning.bossPatterns.Length; i++)
            {
                if (_patternCooldowns[i] > 0) continue;
                var p = tuning.bossPatterns[i];
                if (distance < p.minRange || distance > p.maxRange) continue;
                if (p.steps == null || p.steps.Length == 0) continue;
                roll -= Mathf.Max(1, p.weight);
                if (roll < 0) return i;
            }
            return -1;
        }

        void StartPattern(int index)
        {
            var tuning = CombatTuning.Current;
            _pattern = tuning.bossPatterns[index];
            _patternCooldowns[index] = _pattern.cooldownFrames;
            _stepIndex = 0;
            _gapFrames = 0;
            State = BossState.Pattern;
            RunCurrentStep();
        }

        void RunCurrentStep()
        {
            var step = _pattern.steps[_stepIndex];
            _actionLabel = _pattern.label + " · " + step.label;
            Runner.Run(step, _facing);
        }

        void TickPattern()
        {
            var tuning = CombatTuning.Current;

            // 起手的第一帧才转向，之后锁死 —— 玩家可以绕背躲开
            if (Runner.IsRunning && Runner.Phase == AttackPhase.Startup && Runner.PhaseFrame == 1)
                FaceTarget();

            if (Runner.IsRunning)
            {
                // 判定期间维持面向，不再转向
                return;
            }

            if (_gapFrames < _pattern.stepGapFrames)
            {
                _gapFrames++;
                StopHorizontal();
                return;
            }

            _gapFrames = 0;
            _stepIndex++;

            if (_stepIndex < _pattern.steps.Length)
            {
                RunCurrentStep();
                return;
            }

            FinishPattern();
        }

        void FinishPattern()
        {
            var tuning = CombatTuning.Current;
            int recovery = _pattern.recoveryFrames;

            // 最后一招自带的破绽窗口可以和整套的收招取较大值
            var last = _pattern.steps[_pattern.steps.Length - 1];
            if (last.breakWindowFrames > recovery) recovery = last.breakWindowFrames;

            _recoverFramesLeft = recovery;
            _thinkCooldown = Mathf.Max(_thinkCooldown, tuning.bossGlobalCooldownFrames);
            _actionLabel = "收招（破绽）";
            State = BossState.Recover;
        }

        void FaceTarget()
        {
            if (Target == null) return;
            _facing = Target.position.x >= transform.position.x ? 1f : -1f;
        }

        void StopHorizontal()
        {
            if (Body == null) return;
            var velocity = Body.velocity;
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, 40f * Time.fixedDeltaTime);
            Body.velocity = velocity;
        }

        // ---------------- 事件 ----------------

        void OnStaggerBroken()
        {
            var tuning = CombatTuning.Current;
            Runner.Cancel();
            State = BossState.Broken;
            _stunFramesLeft = tuning.bossBrokenStunFrames;
            _actionLabel = "破防！";
            Flash.Flash(new Color(1f, 0.8f, 1f, 1f), 0.25f);
            CameraShake.Instance?.AddImpulse(0.6f, 0.4f);
        }

        void OnStaggerRecovered()
        {
            if (State == BossState.Broken) State = BossState.Approach;
        }

        void OnDied()
        {
            Runner.Cancel();
            State = BossState.Dead;
            _actionLabel = "被击败";
            Flash.SetTint(DeadTint);
            CameraShake.Instance?.AddImpulse(0.5f, 0.5f);
        }

        // ---------------- IInterruptible ----------------

        public void Interrupt(int frames, string reason)
        {
            if (State == BossState.Dead) return;
            Runner.Cancel();
            State = BossState.Stunned;
            _stunFramesLeft = frames;
            _actionLabel = "被打断：" + reason;
        }

        // ---------------- 视觉 ----------------

        void UpdateVisuals()
        {
            if (State == BossState.Dead)
            {
                Flash.SetTint(DeadTint);
                return;
            }

            if (State == BossState.Broken)
            {
                Flash.SetTint(BrokenTint);
                return;
            }

            if (Runner.IsRunning && Runner.Phase == AttackPhase.Startup && Runner.Profile != null)
            {
                // 前摇颜色 = 招式语言：黄色可格挡可弹反，红色只能闪避
                Flash.SetTint(Runner.Profile.blockable ? TelegraphBlockable : TelegraphUnblockable);
                return;
            }

            Flash.SetTint(Color.clear);
        }
    }
}
