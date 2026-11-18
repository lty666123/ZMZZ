using UnityEngine;
using ZMZZ.Core;

namespace ZMZZ.Combat
{
    /// <summary>命中特效：原型期用一个会放大淡出的方块代替粒子。冻结在 hitstop 里，所以用缩放时间。</summary>
    public class HitSpark : MonoBehaviour
    {
        SpriteRenderer _renderer;
        float _life;
        float _age;
        float _startScale;
        float _endScale;
        Color _color;

        public static void Spawn(Vector2 position, Color color, float startScale, float endScale, float life)
        {
            var go = new GameObject("HitSpark");
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PrimitiveSprite.White;
            sr.color = color;
            sr.sortingOrder = 50;

            var spark = go.AddComponent<HitSpark>();
            spark._renderer = sr;
            spark._color = color;
            spark._life = Mathf.Max(0.01f, life);
            spark._startScale = startScale;
            spark._endScale = endScale;
            go.transform.localScale = new Vector3(startScale, startScale, 1f);
        }

        void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / _life);
            float scale = Mathf.Lerp(_startScale, _endScale, t);
            transform.localScale = new Vector3(scale, scale, 1f);
            transform.Rotate(0f, 0f, 540f * Time.deltaTime);
            var c = _color;
            c.a = _color.a * (1f - t);
            _renderer.color = c;
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
