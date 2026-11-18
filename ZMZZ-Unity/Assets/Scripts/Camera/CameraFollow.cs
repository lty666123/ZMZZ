using UnityEngine;

namespace ZMZZ.CameraSystem
{
    /// <summary>
    /// 跟随镜头：平滑跟随 + 按移动方向前瞻 + 边界限制。
    /// 震动位移加在平滑结果之上，这样震动不会被平滑吃掉。
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform Target;
        [Tooltip("越小越跟手，越大越飘")]
        public float SmoothTime = 0.12f;
        public Vector2 Offset = new Vector2(0f, 1.4f);
        [Tooltip("朝移动方向的前瞻距离")]
        public float LookAhead = 1.4f;
        [Tooltip("前瞻对应的参考速度")]
        public float LookAheadReferenceSpeed = 6f;

        public bool ClampX = true;
        public float MinX = -13f;
        public float MaxX = 13f;

        public CameraShake Shake;

        Vector3 _basePosition;
        Vector3 _velocity;
        Rigidbody2D _targetBody;

        void Start()
        {
            if (Target != null)
            {
                _targetBody = Target.GetComponent<Rigidbody2D>();
                _basePosition = ComputeDesired();
                transform.position = _basePosition;
            }
        }

        void LateUpdate()
        {
            if (Target == null) return;

            Vector3 desired = ComputeDesired();
            _basePosition = Vector3.SmoothDamp(_basePosition, desired, ref _velocity, SmoothTime);

            Vector3 shakeOffset = Shake != null ? Shake.Offset : Vector3.zero;
            transform.position = _basePosition + shakeOffset;
        }

        Vector3 ComputeDesired()
        {
            float velocityX = _targetBody != null ? _targetBody.velocity.x : 0f;
            float ahead = Mathf.Clamp(velocityX / LookAheadReferenceSpeed, -1f, 1f) * LookAhead;

            float x = Target.position.x + Offset.x + ahead;
            if (ClampX) x = Mathf.Clamp(x, MinX, MaxX);

            return new Vector3(x, Target.position.y + Offset.y, transform.position.z);
        }
    }
}
