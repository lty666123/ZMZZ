using UnityEngine;

namespace ZMZZ.Core
{
    /// <summary>
    /// 运行时生成 1x1 白色方块 Sprite，供原型期的所有视觉与 UI 使用。
    /// 换正式美术时，只需把 SpriteRenderer.sprite 换成真图，代码不用动。
    /// </summary>
    public static class PrimitiveSprite
    {
        static Sprite _white;

        public static Sprite White
        {
            get
            {
                if (_white == null)
                {
                    var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                    tex.name = "PrimitiveWhite";
                    var pixels = new Color32[16];
                    for (int i = 0; i < pixels.Length; i++)
                        pixels[i] = new Color32(255, 255, 255, 255);
                    tex.SetPixels32(pixels);
                    tex.Apply();
                    tex.filterMode = FilterMode.Bilinear;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    _white = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
                    _white.name = "PrimitiveWhiteSprite";
                }
                return _white;
            }
        }

        /// <summary>创建一个纯色显示用的子物体（无碰撞）。</summary>
        public static SpriteRenderer CreateVisual(Transform parent, string name, Vector2 size, Color color,
                                                  int sortingOrder = 0, Sprite sprite = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite != null ? sprite : White;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return sr;
        }
    }
}
