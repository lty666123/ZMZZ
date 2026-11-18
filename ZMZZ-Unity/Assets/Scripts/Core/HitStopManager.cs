using UnityEngine;

namespace ZMZZ.Core
{
    /// <summary>
    /// 命中停顿（hitstop）：命中瞬间把整个游戏冻结若干帧，是打击感最主要的来源。
    /// 用非缩放时间倒计时，因此冻结期间自己还能推进。
    /// 同时负责「调试慢动作」：hitstop 结束后恢复到玩家设定的基准时间流速。
    /// </summary>
    public class HitStopManager : MonoBehaviour
    {
        public static HitStopManager Instance { get; private set; }

        /// <summary>玩家设定的基准时间流速（1 = 正常，0.25 = 慢动作调试）。</summary>
        public float BaseTimeScale { get; private set; } = 1f;

        int _framesRemaining;
        float _frameUnit = 1f / 60f;
        bool _frozen;

        /// <summary>调试用的慢动作档位，按 T 循环。</summary>
        static readonly float[] DebugScales = { 1f, 0.5f, 0.25f, 0.1f };
        int _debugScaleIndex;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _frameUnit = Time.fixedDeltaTime;
            Time.timeScale = BaseTimeScale;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Time.timeScale = 1f;
            }
        }

        /// <summary>请求冻结若干帧（1 帧 = 一个 FixedUpdate 步长）。</summary>
        public void Request(int frames)
        {
            if (frames <= 0) return;
            if (frames > _framesRemaining) _framesRemaining = frames;
        }

        public bool IsFrozen { get { return _frozen; } }

        void Update()
        {
            // 暂停期间不要碰时间流速，否则会把暂停顶掉
            if (GamePause.IsPaused) return;

            if (_framesRemaining > 0)
            {
                if (!_frozen)
                {
                    _frozen = true;
                    Time.timeScale = 0f;
                }

                // 用非缩放时间推进，冻结自己也照样倒计时
                _frameAccumulator += Time.unscaledDeltaTime;
                while (_frameAccumulator >= _frameUnit && _framesRemaining > 0)
                {
                    _frameAccumulator -= _frameUnit;
                    _framesRemaining--;
                }

                if (_framesRemaining <= 0)
                {
                    _frozen = false;
                    Time.timeScale = BaseTimeScale;
                }
            }
            else
            {
                _frameAccumulator = 0f;
                if (!Mathf.Approximately(Time.timeScale, BaseTimeScale))
                    Time.timeScale = BaseTimeScale;
            }

            if (Input.GetKeyDown(KeyCode.T))
            {
                _debugScaleIndex = (_debugScaleIndex + 1) % DebugScales.Length;
                BaseTimeScale = DebugScales[_debugScaleIndex];
                if (!_frozen) Time.timeScale = BaseTimeScale;
            }
        }

        float _frameAccumulator;
    }
}
