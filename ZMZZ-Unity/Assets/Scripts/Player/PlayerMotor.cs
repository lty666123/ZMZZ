using UnityEngine;
using ZMZZ.Combat;
using ZMZZ.Core;

namespace ZMZZ.Player
{
    /// <summary>
    /// 移动与跳跃的物理层。只管「怎么动」，不管「能不能动」——那一层由 PlayerCombat 决定。
    /// 土狼时间、跳跃缓冲、松手截断，是横版跳跃手感的三件套，都做在这里。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMotor : MonoBehaviour
    {
        public Rigidbody2D Body;
        public Transform GroundCheck;
        public Vector2 GroundCheckSize = new Vector2(0.62f, 0.14f);

        public bool IsGrounded { get; private set; }
        public float Facing { get; private set; } = 1f;
        /// <summary>是否接受移动输入（攻击 / 闪避 / 受击时为 false）</summary>
        public bool CanMove = true;
        /// <summary>速度倍率（格挡时降低）</summary>
        public float SpeedMultiplier = 1f;

        CombatTuning _tuning;
        int _coyoteFrames;
        bool _jumpCutUsed;
        bool _jumpQueued;
        float _horizontalInput;

        public float JumpVelocity
        {
            get
            {
                float gravity = Mathf.Abs(Physics2D.gravity.y) * _tuning.gravityScale;
                return Mathf.Sqrt(2f * gravity * _tuning.jumpHeight);
            }
        }

        void Awake()
        {
            _tuning = CombatTuning.Current;
            if (Body == null) Body = GetComponent<Rigidbody2D>();
            Body.gravityScale = _tuning.gravityScale;
            Body.freezeRotation = true;
        }

        public void SetFacing(float facing)
        {
            if (facing >= 0f) Facing = 1f;
            else Facing = -1f;
        }

        void FixedUpdate()
        {
            var tuning = CombatTuning.Current;

            // 地面检测
            IsGrounded = GroundCheck != null &&
                Physics2D.OverlapBox(GroundCheck.position, GroundCheckSize, 0f, GameLayers.GroundMask) != null;

            if (IsGrounded) _coyoteFrames = tuning.coyoteFrames;
            else if (_coyoteFrames > 0) _coyoteFrames--;

            if (IsGrounded) _jumpCutUsed = false;

            // 水平移动
            var velocity = Body.velocity;
            float target = CanMove ? _horizontalInput * tuning.moveSpeed * SpeedMultiplier : 0f;
            float accel = IsGrounded ? tuning.moveAccel : tuning.airAccel;
            velocity.x = Mathf.MoveTowards(velocity.x, target, accel * Time.fixedDeltaTime);

            // 跳跃
            if (_jumpQueued && (IsGrounded || _coyoteFrames > 0))
            {
                velocity.y = JumpVelocity;
                _jumpQueued = false;
                _coyoteFrames = 0;
                IsGrounded = false;
            }

            // 松手截断：按得越短跳得越低，这是「跳跃手感」的关键之一
            if (!_jumpCutUsed && velocity.y > 0f && !_jumpHeld)
            {
                velocity.y *= tuning.jumpCutMultiplier;
                _jumpCutUsed = true;
            }

            if (velocity.y < -tuning.maxFallSpeed) velocity.y = -tuning.maxFallSpeed;

            Body.velocity = velocity;
        }

        bool _jumpHeld;

        /// <summary>每帧由控制层调用。</summary>
        public void TickInput(float horizontal, bool jumpHeld)
        {
            _horizontalInput = horizontal;
            _jumpHeld = jumpHeld;
            if (Mathf.Abs(horizontal) > 0.01f && CanMove) SetFacing(horizontal);
        }

        public void QueueJump()
        {
            _jumpQueued = true;
        }

        public void CancelJumpQueue()
        {
            _jumpQueued = false;
        }

        void OnDrawGizmosSelected()
        {
            if (GroundCheck == null) return;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(GroundCheck.position, new Vector3(GroundCheckSize.x, GroundCheckSize.y, 0.1f));
        }
    }
}
