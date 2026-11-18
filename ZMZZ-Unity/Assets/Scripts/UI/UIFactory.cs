using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZMZZ.UI
{
    /// <summary>
    /// 代码建 UI 的零件库。所有界面都从这里取控件，样式自然统一。
    /// 以后加新面板直接照抄调用即可，不用再记 RectTransform 的锚点细节。
    ///
    /// 位置约定：anchor 与 pivot 一起决定摆放 —— anchor=(0.5,0.5)、pos=(0,120)
    /// 就是「屏幕中心往上 120 像素」。
    /// </summary>
    public static class UIFactory
    {
        public static RectTransform SetupRect(GameObject go, Transform parent, Vector2 anchor,
                                              Vector2 pivot, Vector2 pos, Vector2 size)
        {
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            if (rect == null) rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return rect;
        }

        public static RectTransform CreateRect(Transform parent, string name, Vector2 anchor,
                                               Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            return SetupRect(go, parent, anchor, pivot, pos, size);
        }

        /// <summary>铺满父节点的容器。</summary>
        public static RectTransform CreateStretchRect(Transform parent, string name, float padding = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
            return rect;
        }

        public static Image CreateImage(Transform parent, string name, Vector2 anchor, Vector2 pivot,
                                        Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            SetupRect(go, parent, anchor, pivot, pos, size);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        public static Image CreateStretchImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text CreateText(Transform parent, string name, string content, int fontSize,
                                      TextAnchor alignment, Color color, Vector2 anchor, Vector2 pivot,
                                      Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            SetupRect(go, parent, anchor, pivot, pos, size);
            var text = go.GetComponent<Text>();
            text.font = UITheme.DefaultFont;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        /// <summary>以屏幕中心为原点的文字，最常用。</summary>
        public static Text CreateLabel(Transform parent, string name, string content, int fontSize,
                                       Color color, Vector2 pos, Vector2 size,
                                       TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            return CreateText(parent, name, content, fontSize, alignment, color,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        }

        public static Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size,
                                          Action onClick, Color? background = null, int fontSize = 28)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            SetupRect(go, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);

            var image = go.GetComponent<Image>();
            image.color = background.HasValue ? background.Value : UITheme.ButtonNormal;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            SetButtonColors(button);
            if (onClick != null) button.onClick.AddListener(() => onClick());

            var text = CreateText(go.transform, "Label", label, fontSize, TextAnchor.MiddleCenter,
                                  UITheme.TextPrimary, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                  Vector2.zero, size);
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            return button;
        }

        static void SetButtonColors(Button button)
        {
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            colors.fadeDuration = 0.06f;
            button.colors = colors;
        }

        /// <summary>横向滑条。视觉是纯色方块，交互用标准 Slider 组件。</summary>
        public static Slider CreateSlider(Transform parent, string name, Vector2 pos, Vector2 size,
                                          float min, float max, float value, Action<float> onChange)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            SetupRect(go, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            var slider = go.GetComponent<Slider>();

            var background = CreateImage(go.transform, "Background", new Vector2(0.5f, 0.5f),
                                         new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 10f),
                                         new Color(0.20f, 0.22f, 0.28f, 1f));
            var backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(1f, 0.5f);
            backgroundRect.sizeDelta = new Vector2(0f, 10f);
            backgroundRect.anchoredPosition = Vector2.zero;

            var fillArea = CreateRect(go.transform, "Fill Area", new Vector2(0.5f, 0.5f),
                                      new Vector2(0.5f, 0.5f), Vector2.zero, size);
            fillArea.anchorMin = new Vector2(0f, 0.25f);
            fillArea.anchorMax = new Vector2(1f, 0.75f);
            fillArea.offsetMin = new Vector2(6f, 0f);
            fillArea.offsetMax = new Vector2(-16f, 0f);

            var fill = CreateImage(fillArea, "Fill", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                   Vector2.zero, Vector2.zero, UITheme.Accent);
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillRect.sizeDelta = Vector2.zero;

            var handleArea = CreateRect(go.transform, "Handle Slide Area", new Vector2(0.5f, 0.5f),
                                        new Vector2(0.5f, 0.5f), Vector2.zero, size);
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(10f, 0f);
            handleArea.offsetMax = new Vector2(-10f, 0f);

            var handle = CreateImage(handleArea, "Handle", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                     Vector2.zero, new Vector2(22f, 34f),
                                     new Color(0.95f, 0.96f, 1f, 1f));
            var handleRect = handle.rectTransform;
            handleRect.anchorMin = new Vector2(0f, 0f);
            handleRect.anchorMax = new Vector2(0f, 1f);
            handleRect.sizeDelta = new Vector2(22f, 0f);

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            if (onChange != null) slider.onValueChanged.AddListener(v => onChange(v));
            return slider;
        }

        public static Toggle CreateToggle(Transform parent, string name, string label, Vector2 pos, Vector2 size,
                                          bool value, Action<bool> onChange)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            SetupRect(go, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);

            var box = CreateImage(go.transform, "Box", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                                  new Vector2(2f, 0f), new Vector2(30f, 30f),
                                  new Color(0.20f, 0.23f, 0.30f, 1f));

            var check = CreateImage(box.transform, "Check", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                    Vector2.zero, new Vector2(16f, 16f), UITheme.Accent);

            CreateText(go.transform, "Label", label, 26, TextAnchor.MiddleLeft, UITheme.TextPrimary,
                       new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(46f, 0f), size);

            var toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = box;
            toggle.graphic = check;
            toggle.isOn = value;
            if (onChange != null) toggle.onValueChanged.AddListener(v => onChange(v));
            return toggle;
        }
    }
}
