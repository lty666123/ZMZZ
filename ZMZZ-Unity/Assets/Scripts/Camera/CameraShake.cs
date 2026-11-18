using UnityEngine;

namespace ZMZZ.CameraSystem
{
    /// <summary>镜头震动。用缩放时间，所以会乖乖冻在命中停顿里。</summary>
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        public Vector3 Offset { get; private set; }

        float _strength;
        float _duration;
        float _elapsed;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void AddImpulse(float strength, float duration)
        {
            _strength = Mathf.Max(_strength, strength);
            _duration = Mathf.Max(_duration, duration);
            _elapsed = 0f;
        }

        void LateUpdate()
        {
            if (_duration <= 0f)
            {
                Offset = Vector3.zero;
                return;
            }

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            float amp = _strength * (1f - t) * (1f - t);
            Offset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * amp;

            if (t >= 1f)
            {
                _duration = 0f;
                _strength = 0f;
                Offset = Vector3.zero;
            }
        }
    }
}
