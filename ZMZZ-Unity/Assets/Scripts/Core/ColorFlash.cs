using UnityEngine;

namespace ZMZZ.Core
{
    /// <summary>
    /// 方块角色的所有「演出」：受击闪红、格挡变蓝、无敌闪烁、Boss 起手预警。
    /// 换成真美术后，这里改成触发 Animator 即可，调用方不用动。
    /// </summary>
    public class ColorFlash : MonoBehaviour
    {
        public SpriteRenderer target;
        public Color baseColor = Color.white;

        [Header("无敌闪烁")]
        public float blinkInterval = 0.06f;
        public float blinkAlpha = 0.35f;

        Color _flashColor = Color.clear;
        float _flashTimer;
        Color _tint = Color.clear;
        bool _blinking;

        void Awake()
        {
            if (target == null) target = GetComponent<SpriteRenderer>();
            if (target != null && baseColor == Color.white) baseColor = target.color;
            Apply();
        }

        /// <summary>短促闪光（受击 / 弹反 / 命中）。</summary>
        public void Flash(Color color, float duration)
        {
            _flashColor = color;
            _flashTimer = duration;
        }

        /// <summary>持续染色（格挡、Boss 破防等状态）。传 Color.clear 取消。</summary>
        public void SetTint(Color color)
        {
            _tint = color;
        }

        /// <summary>无敌期间闪烁。</summary>
        public void SetBlink(bool on)
        {
            _blinking = on;
        }

        void Update()
        {
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                if (_flashTimer <= 0f) _flashColor = Color.clear;
            }
            Apply();
        }

        void Apply()
        {
            if (target == null) return;

            Color color = baseColor;
            if (_tint.a > 0f) color = Color.Lerp(color, _tint, _tint.a);
            if (_flashTimer > 0f && _flashColor.a > 0f) color = _flashColor;

            if (_blinking)
            {
                float phase = Mathf.Repeat(Time.time / Mathf.Max(0.01f, blinkInterval), 2f);
                if (phase < 1f)
                {
                    color.a *= blinkAlpha;
                }
            }

            target.color = color;
        }
    }
}
