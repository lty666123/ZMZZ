using System;
using UnityEngine;

namespace ZMZZ.Combat
{
    public enum AttackPhase
    {
        Done = 0,
        Startup = 1,
        Active = 2,
        Recovery = 3,
    }

    /// <summary>
    /// 按帧数据跑完一次攻击：前摇 → 判定 → 后摇。
    /// 玩家连段和 Boss 连招都用它，保证双方的时间语言完全一致。
    /// </summary>
    public class AttackRunner : MonoBehaviour
    {
        public Hitbox hitbox;
        public Rigidbody2D body;

        [Tooltip("判定期间是否接管水平速度（冲刺类招式需要）")]
        public bool controlHorizontalVelocity = true;

        public event Action<AttackPhase> PhaseChanged;
        public event Action<AttackProfile> Finished;
        /// <summary>命中回调，参数为受击方与判定结果</summary>
        public event Action<Hurtbox, BlockOutcome> Landed;

        public AttackProfile Profile { get { return _profile; } }
        public AttackPhase Phase { get { return _phase; } }
        public bool IsRunning { get { return _running; } }
        public int TotalFrame { get { return _totalFrame; } }
        public int PhaseFrame { get { return _phaseFrame; } }

        AttackProfile _profile;
        AttackPhase _phase = AttackPhase.Done;
        bool _running;
        float _facing = 1f;
        int _phaseFrame;
        int _totalFrame;

        void Awake()
        {
            if (hitbox != null) hitbox.HitLanded += OnHitLanded;
        }

        void OnDestroy()
        {
            if (hitbox != null) hitbox.HitLanded -= OnHitLanded;
        }

        void OnHitLanded(Hurtbox target, BlockOutcome outcome)
        {
            Landed?.Invoke(target, outcome);
        }

        public void Run(AttackProfile profile, float facing)
        {
            if (profile == null) return;
            _profile = profile;
            _facing = facing >= 0f ? 1f : -1f;
            _running = true;
            _phaseFrame = 0;
            _totalFrame = 0;
            EnterPhase(profile.startupFrames > 0 ? AttackPhase.Startup : AttackPhase.Active);
        }

        public void Cancel()
        {
            if (!_running) return;
            if (hitbox != null) hitbox.End();
            _running = false;
            _phase = AttackPhase.Done;
        }

        void FixedUpdate()
        {
            if (!_running || _profile == null) return;

            _phaseFrame++;
            _totalFrame++;

            switch (_phase)
            {
                case AttackPhase.Startup:
                    if (_phaseFrame >= _profile.startupFrames)
                        EnterPhase(_profile.activeFrames > 0 ? AttackPhase.Active : AttackPhase.Recovery);
                    break;

                case AttackPhase.Active:
                    ApplyForwardMovement();
                    if (_phaseFrame >= _profile.activeFrames)
                        EnterPhase(AttackPhase.Recovery);
                    break;

                case AttackPhase.Recovery:
                    if (_phaseFrame >= _profile.recoveryFrames)
                        Finish();
                    break;
            }
        }

        void ApplyForwardMovement()
        {
            if (!controlHorizontalVelocity || body == null) return;
            if (Mathf.Approximately(_profile.moveForwardSpeed, 0f)) return;
            body.velocity = new Vector2(_profile.moveForwardSpeed * _facing, body.velocity.y);
        }

        void EnterPhase(AttackPhase phase)
        {
            _phase = phase;
            _phaseFrame = 0;

            if (hitbox != null)
            {
                if (phase == AttackPhase.Active) hitbox.Begin(_profile, _facing);
                else hitbox.End();
            }

            PhaseChanged?.Invoke(phase);
        }

        void Finish()
        {
            if (hitbox != null) hitbox.End();
            _running = false;
            _phase = AttackPhase.Done;
            Finished?.Invoke(_profile);
        }
    }
}
