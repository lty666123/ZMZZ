using UnityEngine;

namespace ZMZZ.UI
{
    /// <summary>
    /// UI 的配色与字体。全部界面都从这里取，改一处全项目生效。
    /// 原型期用纯色，上正式美术时把这里换成 Sprite 引用即可。
    /// </summary>
    public static class UITheme
    {
        public static readonly Color MenuBackground = new Color(0.06f, 0.07f, 0.10f, 1f);
        public static readonly Color Deco = new Color(0.10f, 0.12f, 0.17f, 1f);
        public static readonly Color DecoLight = new Color(0.14f, 0.17f, 0.24f, 1f);
        public static readonly Color PanelBackground = new Color(0.10f, 0.11f, 0.15f, 0.98f);
        public static readonly Color Dim = new Color(0f, 0f, 0f, 0.72f);

        public static readonly Color TextPrimary = new Color(0.93f, 0.95f, 1f, 1f);
        public static readonly Color TextSecondary = new Color(0.64f, 0.69f, 0.80f, 1f);
        public static readonly Color Accent = new Color(0.96f, 0.78f, 0.29f, 1f);

        public static readonly Color ButtonNormal = new Color(0.17f, 0.20f, 0.28f, 1f);
        public static readonly Color ButtonAccent = new Color(0.28f, 0.44f, 0.74f, 1f);
        public static readonly Color ButtonDanger = new Color(0.44f, 0.22f, 0.24f, 1f);

        static readonly string[] FontCandidates =
        {
            "Microsoft YaHei", "Microsoft YaHei UI", "SimHei", "SimSun",
            "Noto Sans CJK SC", "Source Han Sans SC", "Arial Unicode MS",
        };

        static Font _font;

        /// <summary>
        /// 默认字体。优先挑系统里带中文的字体，否则中文会显示成方块。
        /// </summary>
        public static Font DefaultFont
        {
            get
            {
                if (_font != null) return _font;

                string[] installed = Font.GetOSInstalledFontNames();
                if (installed != null)
                {
                    for (int i = 0; i < FontCandidates.Length; i++)
                    {
                        if (System.Array.IndexOf(installed, FontCandidates[i]) < 0) continue;
                        _font = Font.CreateDynamicFontFromOSFont(FontCandidates[i], 32);
                        if (_font != null) return _font;
                    }
                }

                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 32);
                return _font;
            }
        }
    }
}
