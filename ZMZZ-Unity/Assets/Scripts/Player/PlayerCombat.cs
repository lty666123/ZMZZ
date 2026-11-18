using UnityEngine;
using ZMZZ.Combat;
using ZMZZ.Core;

namespace ZMZZ.Player
{
    public enum PlayerState
    {
        Free = 0,
        Attack = 1,
        Dodge = 2,
        Hurt = 3,
        /// <summary>被打破防等长硬直</summary>
        Stun = 4,
        Dead = 5,
    }

    /// <summary>
    /// 玩家的战斗控制：三连段、闪避、格挡、弹反。
    /// 它同时实现 IDamageGuard —— 也就是说「格挡和弹反的规则」由它说了算，
    /// 而「伤害怎么结算」由 DamageResolver 说了算，两者职责分明。
    /// </summary>
    public class PlayerCombat : MonoBehaviour, IDamageGuard, IInterruptible
    {
        public PlayerInputReader Input;
        public PlayerMotor Motor;
        public AttackRunner Runner;
        public Health Health;
        public Stamina Stamina;
        public ColorFlash Flash;

        [Tooltip("格挡时显示在身前的方块，同时也是「弹反窗口」的视觉提示")]
        public SpriteRenderer GuardVisual;

        public PlayerState State { get; private set; } = PlayerState.Free;
        public bool IsBlocking { get; private set; }
        public int ComboIndex { get { return _comboIndex; } }
        /// <summary>是否处于弹反窗口内，UI 显示用</summary>
        public bool InParryWindow
        {
            get { return IsBlocking && _blockFrames <= CombatTuning.Current.parryWindowFrames; }
        }

        CombatTuning _tuning;
        int _stateFrame;
        int _comboIndex;
        int _dodgeCooldown;
        int _invulnFrames;
        int _hurtFrames;
        int _stunFrames;
        int _blockFrames;
        int _blockHoldLock;
        float _dodgeDirection;

        static readonly Color ParryFlashColor = new Color(1f, 0.95f, 0.45f, 1f);
        static readonly Color BlockFlashColor = new Color(0.72f, 0.86f, 1f, 1f);
        static readonly Color HurtFlashColor = new Color(1f, 0.4f, 0.4f, 1f);
        static readonly Color BlockTintColor = new Color(0.55f, 0.78f, 1f, 0.45f);
        static readonly Color DodgeTintColor = new Color(0.6f, 0.95f, 0.9f, 0.35f);

        void Awake()
        {
            _tuning = CombatTuning.Current;

            if (Health != null)
            {
                Health.maxHealth = _tuning.playerMaxHealth;
                Health.ResetToFull();
                Health.Damaged += OnDamaged;
                Health.Died += OnDied;
            }

            if (Stamina != null)
            {
                Stamina.max = _tuning.playerMaxStamina;
                Stamina.regenPerSecond = _tuning.staminaRegenPerSecond;
                Stamina.regenDelayFrames = _tuning.staminaRegenDelayFrames;
                Stamina.ResetToFull();
            }
        }

        void OnDestroy()
        {
            if (Health != null)
            {
                Health.Damaged -= OnDamaged;
                Health.Died -= OnDied;
            }
        }

        void FixedUpdate()
        {
            var tuning = CombatTuning.Current;

            _stateFrame++;
            if (_invulnFrames > 0) _invulnFrames--;
            if (_dodgeCooldown > 0) _dodgeCooldown--;
            if (_blockHoldLock > 0) _blockHoldLock--;

            if (State != PlayerState.Dead)
            {
                Motor.TickInput(Input.MoveX, Input.JumpHeld);

                switch (State)
                {
                    case PlayerState.Free: TickFree(); break;
                    case PlayerState.Attack: TickAttack(); break;
                    case PlayerState.Dodge: TickDodge(); break;
                    case PlayerState.Hurt: TickHurt(); break;
                    case PlayerState.Stun: TickStun(); break;
                }
            }

            UpdateVisuals();
        }

        // ---------------- 各状态 ----------------

        void TickFree()
        {
            var tuning = CombatTuning.Current;

            Motor.CanMove = true;
            Motor.SpeedMultiplier = 1f;

            // 跳跃：把手感参数交给 Motor 统一处理（土狼时间、松手截断）
            if (Input.JumpBuffered) Motor.QueueJump();
            else Motor.CancelJumpQueue();

            // 格挡的开始与结束
            bool wantBlock = Input.BlockHeld;
            if (wantBlock && !IsBlocking)
            {
                IsBlocking = true;
                _blockFrames = 0;
                _blockHoldLock = tuning.blockMinHoldFrames;
            }
            else if (!wantBlock && IsBlocking && _blockHoldLock <= 0)
            {
                IsBlocking = false;
            }

            if (IsBlocking)
            {
                _blockFrames++;
                Motor.SpeedMultiplier = tuning.blockMoveSpeedMultiplier;
            }
            else
            {
                _blockFrames = 0;
            }

            // 出招优先级：攻击 > 闪避
            if (Input.ConsumeAttack())
            {
                StartAttack(1);
                return;
            }
            if (Input.ConsumeDodge())
            {
                TryDodge();
            }
        }

        void TickAttack()
        {
            Motor.CanMove = false;
            Motor.CancelJumpQueue();

            if (!Runner.IsRunning)
            {
                EndAttack();
                return;
            }

            // 后摇期间是「取消窗口」：可以接下一段连招，也可以闪避取消
            if (Runner.Phase == AttackPhase.Recovery)
            {
                if (_comboIndex < 3 && Input.ConsumeAttack())
                {
                    StartAttack(_comboIndex + 1);
                    return;
                }
                if (Input.ConsumeDodge())
                {
                    TryDodge();
                }
            }
        }

        void TickDodge()
        {
            var tuning = CombatTuning.Current;
            Motor.CanMove = false;

            // 前 3/4 的位移由闪避本身决定，之后交还控制
            int moveFrames = Mathf.RoundToInt(tuning.dodgeFrames * 0.75f);
            if (_stateFrame <= moveFrames)
            {
                var velocity = Motor.Body.velocity;
                Motor.Body.velocity = new Vector2(_dodgeDirection * tuning.dodgeSpeed, velocity.y);
            }

            if (_stateFrame >= tuning.dodgeFrames)
            {
                Flash.SetTint(Color.clear);
                State = PlayerState.Free;
                _stateFrame = 0;
                Motor.CanMove = true;
            }
        }

        void TickHurt()
        {
            Motor.CanMove = false;
            if (_stateFrame >= _hurtFrames)
            {
                EnterFree();
            }
        }

        void TickStun()
        {
            Motor.CanMove = false;
            if (_stateFrame >= _stunFrames)
            {
                EnterFree();
            }
        }

        void EnterFree()
        {
            State = PlayerState.Free;
            _stateFrame = 0;
            _comboIndex = 0;
            Motor.CanMove = true;
        }

        // ---------------- 动作 ----------------

        void StartAttack(int index)
        {
            var tuning = CombatTuning.Current;
            AttackProfile profile;
            if (index <= 1) profile = tuning.attack1;
            else if (index == 2) profile = tuning.attack2;
            else profile = tuning.attack3;

            _comboIndex = index;
            State = PlayerState.Attack;
            _stateFrame = 0;
            IsBlocking = false;
            _blockFrames = 0;
            Motor.CanMove = false;
            Runner.Run(profile, Motor.Facing);
        }

        void EndAttack()
        {
            _comboIndex = 0;
            EnterFree();
        }

        bool TryDodge()
        {
            var tuning = CombatTuning.Current;

            if (_dodgeCooldown > 0) return false;
            if (Stamina != null && !Stamina.TrySpend(tuning.dodgeStaminaCost))
            {
                Flash.Flash(new Color(1f, 0.6f, 0.3f, 1f), 0.08f);
                return false;
            }

            StartDodge();
            return true;
        }

        void StartDodge()
        {
            var tuning = CombatTuning.Current;

            State = PlayerState.Dodge;
            _stateFrame = 0;
            _dodgeCooldown = tuning.dodgeCooldownFrames;
            Runner.Cancel();
            IsBlocking = false;
            _blockFrames = 0;
            Motor.CancelJumpQueue();

            float direction = Mathf.Abs(Input.MoveX) > 0.01f ? Mathf.Sign(Input.MoveX) : Motor.Facing;
            _dodgeDirection = direction;
            Motor.SetFacing(direction);
            Flash.SetTint(DodgeTintColor);
        }

        // ---------------- 事件 ----------------

        void OnDamaged(float amount)
        {
            if (State == PlayerState.Dead) return;

            var tuning = CombatTuning.Current;
            State = PlayerState.Hurt;
            _stateFrame = 0;
            _hurtFrames = tuning.playerHurtFrames;
            _invulnFrames = tuning.playerInvulnFrames;
            Runner.Cancel();
            IsBlocking = false;
            _blockFrames = 0;
            Motor.CanMove = false;
            Flash.SetTint(Color.clear);
            Flash.Flash(HurtFlashColor, 0.12f);
        }

        void OnDied()
        {
            State = PlayerState.Dead;
            Input.InputEnabled = false;
            Runner.Cancel();
            Motor.CanMove = false;
            Motor.CancelJumpQueue();
            IsBlocking = false;
            Flash.SetBlink(false);
            Flash.SetTint(Color.clear);

            var body = Motor.Body;
            body.velocity = new Vector2(0f, body.velocity.y);
        }

        // ---------------- IDamageGuard ----------------

        public bool IsInvulnerable
        {
            get
            {
                var tuning = CombatTuning.Current;
                if (_invulnFrames > 0) return true;
                if (State != PlayerState.Dodge) return false;
                return _stateFrame >= tuning.dodgeInvulnStart && _stateFrame <= tuning.dodgeInvulnEnd;
            }
        }

        public BlockOutcome EvaluateBlock(ref DamageInfo info)
        {
            var tuning = CombatTuning.Current;

            if (!IsBlocking || State != PlayerState.Free) return BlockOutcome.NotBlocked;
            if (!info.blockable) return BlockOutcome.NotBlocked;   // 不可格挡的招式只能闪避

            // 只能挡正面
            float directionToAttacker = info.attacker != null
                ? Mathf.Sign(info.attacker.position.x - transform.position.x)
                : Motor.Facing;
            if (Mathf.Approximately(directionToAttacker, 0f)) directionToAttacker = Motor.Facing;
            if (!Mathf.Approximately(directionToAttacker, Motor.Facing)) return BlockOutcome.NotBlocked;

            // 体力不够 = 被打破防
            if (Stamina != null && !Stamina.Has(tuning.blockStaminaCost)) return BlockOutcome.GuardBroken;

            // 举盾后的前几帧 = 弹反
            if (_blockFrames <= tuning.parryWindowFrames) return BlockOutcome.Parried;

            return BlockOutcome.Blocked;
        }

        public void OnBlockResolved(ref DamageInfo info, BlockOutcome outcome)
        {
            var tuning = CombatTuning.Current;

            switch (outcome)
            {
                case BlockOutcome.Blocked:
                    if (Stamina != null) Stamina.Spend(tuning.blockStaminaCost);
                    Flash.Flash(BlockFlashColor, 0.08f);
                    break;

                case BlockOutcome.Parried:
                    // 弹反不消耗体力，这是它比普通格挡强的地方
                    Flash.Flash(ParryFlashColor, 0.16f);
                    break;

                case BlockOutcome.GuardBroken:
                    if (Stamina != null) Stamina.Spend(Stamina.Current);
                    Interrupt(tuning.guardBreakInterruptFrames, "体力耗尽被破防");
                    Flash.Flash(new Color(1f, 0.5f, 0.2f, 1f), 0.25f);
                    break;
            }
        }

        // ---------------- IInterruptible ----------------

        public void Interrupt(int frames, string reason)
        {
            if (State == PlayerState.Dead) return;
            State = PlayerState.Stun;
            _stateFrame = 0;
            _stunFrames = frames;
            Runner.Cancel();
            IsBlocking = false;
            _blockFrames = 0;
            Motor.CanMove = false;
        }

        // ---------------- 视觉 ----------------

        void UpdateVisuals()
        {
            var tuning = CombatTuning.Current;

            Flash.SetBlink(_invulnFrames > 0);

            if (IsBlocking)
                Flash.SetTint(BlockTintColor);
            else if (State != PlayerState.Dodge)
                Flash.SetTint(Color.clear);

            if (GuardVisual != null)
            {
                GuardVisual.enabled = IsBlocking || State == PlayerState.Dodge;
                if (GuardVisual.enabled)
                {
                    float facing = Motor.Facing;
                    GuardVisual.transform.localPosition = new Vector3(0.62f * facing, 0.1f, 0f);
                    GuardVisual.transform.localScale = new Vector3(0.34f, 1.5f, 1f);
                    // 弹反窗口内变亮，让玩家「看得见」那个窗口
                    bool inWindow = InParryWindow;
                    GuardVisual.color = inWindow
                        ? new Color(1f, 0.85f, 0.2f, 0.95f)
                        : new Color(0.4f, 0.62f, 1f, 0.45f);
                }
            }
        }

        /// <summary>给 HUD 用的一句话状态描述。</summary>
        public string StateLabel
        {
            get
            {
                switch (State)
                {
                    case PlayerState.Free: return IsBlocking ? (InParryWindow ? "格挡·弹反窗口" : "格挡") : "自由";
                    case PlayerState.Attack: return "攻击 " + _comboIndex;
                    case PlayerState.Dodge: return "闪避";
                    case PlayerState.Hurt: return "受击";
                    case PlayerState.Stun: return "硬直";
                    case PlayerState.Dead: return "死亡";
                }
                return "-";
            }
        }
    }
}
