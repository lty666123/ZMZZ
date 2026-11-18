using UnityEngine;
using ZMZZ.Core;

namespace ZMZZ.UI.Panels
{
    /// <summary>
    /// 入口界面：开始游戏 / 设置 / 退出游戏。
    /// 背景的「城市剪影」是用方块拼的，上正式美术时整块换掉即可。
    /// </summary>
    public class MainMenuPanel : UIPanel
    {
        const string VersionLabel = "原型版本 0.1 · M1 战斗原型";

        protected override void Awake()
        {
            layer = UILayer.Normal;
            base.Awake();
        }

        protected override void BuildUI(RectTransform root)
        {
            UIFactory.CreateStretchImage(root, "Background", UITheme.MenuBackground);
            BuildSkyline(root);

            UIFactory.CreateLabel(root, "Title", "ZMZZ", 120, UITheme.TextPrimary,
                                  new Vector2(0f, 230f), new Vector2(1400f, 160f));
            UIFactory.CreateLabel(root, "Subtitle", "工程代号 · 2D 横版动作 RPG 原型", 30, UITheme.TextSecondary,
                                  new Vector2(0f, 130f), new Vector2(1400f, 50f));

            UIFactory.CreateButton(root, "StartButton", "开始游戏", new Vector2(0f, 10f), new Vector2(420f, 86f),
                                   OnStartClicked, UITheme.ButtonAccent);
            UIFactory.CreateButton(root, "SettingsButton", "设置", new Vector2(0f, -96f), new Vector2(420f, 86f),
                                   OnSettingsClicked);
            UIFactory.CreateButton(root, "QuitButton", "退出游戏", new Vector2(0f, -202f), new Vector2(420f, 86f),
                                   OnQuitClicked, UITheme.ButtonDanger);

            UIFactory.CreateLabel(root, "Version", VersionLabel, 22, UITheme.TextSecondary,
                                  new Vector2(0f, -330f), new Vector2(1000f, 40f));
            UIFactory.CreateText(root, "Hint",
                                 "游戏内操作：A/D 移动　空格 跳跃　J 攻击　K 闪避　L 格挡　Esc 暂停　T 慢动作",
                                 22, TextAnchor.LowerCenter, UITheme.TextSecondary,
                                 new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                                 new Vector2(0f, 30f), new Vector2(1700f, 40f));
        }

        static void BuildSkyline(RectTransform root)
        {
            var container = UIFactory.CreateStretchRect(root, "Skyline");

            float[] heights = { 130f, 220f, 95f, 270f, 160f, 250f, 110f, 200f, 150f, 230f, 120f };
            const float width = 92f;
            const float gap = 8f;

            float total = heights.Length * width + (heights.Length - 1) * gap;
            float x = -total * 0.5f;
            var anchor = new Vector2(0.5f, 0f);
            var pivot = new Vector2(0f, 0f);

            for (int i = 0; i < heights.Length; i++)
            {
                var color = (i % 2 == 0) ? UITheme.Deco : UITheme.DecoLight;
                UIFactory.CreateImage(container, "Block" + i, anchor, pivot,
                                      new Vector2(x, 0f), new Vector2(width, heights[i]), color);
                x += width + gap;
            }

            UIFactory.CreateImage(container, "GroundLine", anchor, new Vector2(0.5f, 0f),
                                  new Vector2(0f, 0f), new Vector2(2600f, 6f),
                                  new Color(0.18f, 0.21f, 0.28f, 1f));
        }

        void OnStartClicked()
        {
            UIManager.Instance.LoadScene(SceneNames.Game, "载入战斗中…");
        }

        void OnSettingsClicked()
        {
            UIManager.Instance.Open<SettingsPanel>();
        }

        void OnQuitClicked()
        {
            AppQuit.Quit();
        }
    }
}
