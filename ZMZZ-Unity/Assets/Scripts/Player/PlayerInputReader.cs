using UnityEngine;
using ZMZZ.Combat;

namespace ZMZZ.Player
{
    /// <summary>
    /// 输入读取与缓冲。用旧版 Input（零依赖，原型期够用）；
    /// 后续要接手柄 / 重映射时，只需把这里换成新 Input System，其它代码不用改。
    ///
    /// 按键：A/D 或 ←/→ 移动，空格 跳，J 攻击，K 闪避，L 格挡（按住）。
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        public float MoveX { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool BlockHeld { get; private set; }

        /// <summary>攻击指令还在缓冲窗口内</summary>
        public bool AttackBuffered { get { return _attackBuffer > 0; } }
        /// <summary>跳跃指令还在缓冲窗口内</summary>
        public bool JumpBuffered { get { return _jumpBuffer > 0; } }

        public bool InputEnabled = true;

        int _attackBuffer;
        int _jumpBuffer;
        int _dodgeBuffer;
        int _dodgeBufferWindow = 8;

        CombatTuning _tuning;

        void Awake()
        {
            _tuning = CombatTuning.Current;
        }

        void Update()
        {
            if (!InputEnabled || Core.GamePause.IsPaused)
            {
                MoveX = 0f;
                JumpHeld = false;
                BlockHeld = false;
                return;
            }

            float left = (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) ? 1f : 0f;
            float right = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) ? 1f : 0f;
            MoveX = right - left;

            JumpHeld = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
            BlockHeld = Input.GetKey(KeyCode.L) || Input.GetKey(KeyCode.RightShift);

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                _jumpBuffer = _tuning.jumpBufferFrames;

            if (Input.GetKeyDown(KeyCode.J))
                _attackBuffer = _tuning.comboBufferFrames;

            if (Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.LeftShift))
                _dodgeBuffer = _dodgeBufferWindow;
        }

        void FixedUpdate()
        {
            if (_attackBuffer > 0) _attackBuffer--;
            if (_jumpBuffer > 0) _jumpBuffer--;
            if (_dodgeBuffer > 0) _dodgeBuffer--;
        }

        public bool ConsumeAttack()
        {
            if (_attackBuffer <= 0) return false;
            _attackBuffer = 0;
            return true;
        }

        public bool ConsumeJump()
        {
            if (_jumpBuffer <= 0) return false;
            _jumpBuffer = 0;
            return true;
        }

        public bool ConsumeDodge()
        {
            if (_dodgeBuffer <= 0) return false;
            _dodgeBuffer = 0;
            return true;
        }

        public void ClearBuffers()
        {
            _attackBuffer = 0;
            _jumpBuffer = 0;
            _dodgeBuffer = 0;
        }
    }
}
