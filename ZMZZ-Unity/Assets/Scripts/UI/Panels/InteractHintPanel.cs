using UnityEngine;
using UnityEngine.UI;

namespace ZMZZ.UI.Panels
{
    /// <summary>
    /// 屏幕下方的交互提示条，比如「按 E 激活锚点：东门」。
    ///
    /// 它放在 Toast 层，所以永远盖在其它界面之上，而且 closeOnEscape = false，
    /// 按 Esc 不会把它关掉。
    ///
    /// 这个文件同时也是「怎么写一个最简单的 UIPanel」的示例：
    /// Awake 里定层级 → BuildUI 里搭控件 → 对外暴露一个 SetText。
    /// </summary>
    public class InteractHintPanel : UIPanel
    {
        Text _text;
        string _pendingText = string.Empty;

        protected override void Awake()
        {
            layer = UILayer.Toast;
            closeOnEscape = false;
            base.Awake();
        }

        protected override void BuildUI(RectTransform root)
        {
            var background = UIFactory.CreateImage(root, "HintBackground",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 240f), new Vector2(640f, 58f),
                new Color(0f, 0f, 0f, 0.72f));

            _text = UIFactory.CreateText(background.transform, "HintText", _pendingText, 26,
                TextAnchor.MiddleCenter, UITheme.TextPrimary,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(640f, 58f));
        }

        protected override void OnBuilt()
        {
            if (_text != null) _text.text = _pendingText;
        }

        /// <summary>可以在一开始就先调它，界面还没搭出来也没关系，搭好后会自动补上。</summary>
        public void SetText(string text)
        {
            _pendingText = text;
            if (_text != null) _text.text = text;
        }
    }

    /// <summary>给非 UI 代码（比如锚点）用的简易入口，免得到处写 UIManager 样板代码。</summary>
    public static class InteractHint
    {
        static string _lastText;

        public static void Show(string text)
        {
            var ui = UIManager.Instance;
            var panel = ui.GetOrCreate<InteractHintPanel>();

            // 锚点每帧都会调 Show，这里挡一道，避免每帧都重设文字
            if (panel.IsOpen && _lastText == text) return;

            _lastText = text;
            panel.SetText(text);
            if (!panel.IsOpen) panel.Show();
        }

        public static void Hide()
        {
            _lastText = null;

            var ui = UIManager.InstanceOrNull;
            if (ui == null) return;

            var panel = ui.Get<InteractHintPanel>();
            if (panel != null && panel.IsOpen) panel.Hide();
        }
    }
}
