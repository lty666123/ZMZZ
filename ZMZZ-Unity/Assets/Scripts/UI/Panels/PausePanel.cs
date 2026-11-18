using UnityEngine;
using ZMZZ.Core;

namespace ZMZZ.UI.Panels
{
    /// <summary>
    /// 暂停菜单。打开时把游戏时间冻结，并且因为它是 Popup 层，
    /// 从它里面再打开设置面板时，暂停菜单会留在下面（Esc 一层层退回来）。
    /// </summary>
    public class PausePanel : UIPanel
    {
        protected override void Awake()
        {
            layer = UILayer.Popup;
            pauseGame = true;
            closeOnEscape = true;
            base.Awake();
        }

        protected override void BuildUI(RectTransform root)
        {
            UIFactory.CreateStretchImage(root, "Dim", new Color(0f, 0f, 0f, 0.78f));

            var panel = UIFactory.CreateImage(root, "Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                              Vector2.zero, new Vector2(780f, 640f), UITheme.PanelBackground);

            UIFactory.CreateLabel(panel.transform, "Title", "暂　停", 52, UITheme.TextPrimary,
                                  new Vector2(0f, 230f), new Vector2(600f, 70f));

            UIFactory.CreateButton(panel.transform, "ResumeButton", "继续游戏", new Vector2(0f, 120f),
                                   new Vector2(420f, 78f), Hide, UITheme.ButtonAccent);
            UIFactory.CreateButton(panel.transform, "SettingsButton", "设置", new Vector2(0f, 20f),
                                   new Vector2(420f, 78f), () => UIManager.Instance.Open<SettingsPanel>());
            UIFactory.CreateButton(panel.transform, "MainMenuButton", "返回主菜单", new Vector2(0f, -80f),
                                   new Vector2(420f, 78f), BackToMainMenu);
            UIFactory.CreateButton(panel.transform, "QuitButton", "退出游戏", new Vector2(0f, -180f),
                                   new Vector2(420f, 78f), AppQuit.Quit, UITheme.ButtonDanger);

            UIFactory.CreateLabel(panel.transform, "Hint", "按 Esc 继续游戏", 22, UITheme.TextSecondary,
                                  new Vector2(0f, -260f), new Vector2(600f, 40f));
        }

        protected override void Update()
        {
            if (IsOpen) return;

            // 已有别的面板开着时，Esc 交给 UIManager 逐层关闭，这里不要抢
            var ui = UIManager.InstanceOrNull;
            if (ui == null || ui.OpenCount > 0) return;

            if (Input.GetKeyDown(KeyCode.Escape)) Show();
        }

        void BackToMainMenu()
        {
            UIManager.Instance.LoadScene(SceneNames.MainMenu, "返回主菜单…");
        }
    }
}
