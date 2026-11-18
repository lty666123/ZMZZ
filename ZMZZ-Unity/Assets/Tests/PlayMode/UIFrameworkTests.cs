using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using ZMZZ.Core;
using ZMZZ.UI;
using ZMZZ.UI.Panels;

namespace ZMZZ.Tests
{
    /// <summary>
    /// UI 管理器的行为验证：面板能不能开关、层级栈对不对、暂停界面有没有真的冻住时间。
    /// </summary>
    public class UIFrameworkTests
    {
        static void CleanupUi()
        {
            var ui = UIManager.InstanceOrNull;
            if (ui != null) ui.CloseAll();
            GamePause.Set(false);
        }

        [UnityTest]
        public IEnumerator UIManager_AutoCreatesAndOpensPanel()
        {
            CleanupUi();
            var ui = UIManager.Instance;
            Assert.IsNotNull(ui, "UIManager 应该能自己创建出来");
            Assert.AreEqual(0, ui.OpenCount);

            var settings = ui.Open<SettingsPanel>();
            Assert.IsNotNull(settings);
            Assert.IsTrue(settings.IsOpen, "打开之后面板应该处于开启状态");
            Assert.AreEqual(1, ui.OpenCount);
            Assert.IsNotNull(settings.Root, "面板应该被搭出界面根节点");
            Assert.Greater(settings.Root.childCount, 0, "设置面板里应该有控件");

            ui.Close<SettingsPanel>();
            Assert.IsFalse(settings.IsOpen);
            Assert.AreEqual(0, ui.OpenCount);

            Object.DestroyImmediate(settings.gameObject);
            yield return null;
            CleanupUi();
        }

        [UnityTest]
        public IEnumerator UIManager_KeepsStackOrder_AndClosesTopFirst()
        {
            CleanupUi();
            var ui = UIManager.Instance;

            var settings = ui.Open<SettingsPanel>();
            var pause = ui.Open<PausePanel>();
            Assert.AreEqual(2, ui.OpenCount);
            Assert.AreSame(pause, ui.TopOpen, "最后打开的应该在最上面");

            ui.CloseTop();
            Assert.IsFalse(pause.IsOpen);
            Assert.AreSame(settings, ui.TopOpen, "关掉最上层之后应该回到下一层");

            ui.CloseAll();
            Assert.AreEqual(0, ui.OpenCount);

            Object.DestroyImmediate(settings.gameObject);
            Object.DestroyImmediate(pause.gameObject);
            yield return null;
            CleanupUi();
        }

        [UnityTest]
        public IEnumerator PausePanel_FreezesAndRestoresTime()
        {
            CleanupUi();
            var ui = UIManager.Instance;

            var pause = ui.Open<PausePanel>();
            Assert.IsTrue(GamePause.IsPaused, "暂停面板打开时游戏应该暂停");
            Assert.AreEqual(0f, Time.timeScale, 0.001f, "暂停时时间流速必须是 0");

            pause.Hide();
            Assert.IsFalse(GamePause.IsPaused);
            Assert.Greater(Time.timeScale, 0f, "关闭暂停后时间必须恢复流动");

            Object.DestroyImmediate(pause.gameObject);
            yield return null;
            CleanupUi();
        }

        [UnityTest]
        public IEnumerator MainMenu_HasThreeOptions_AndSettingsOpens()
        {
            CleanupUi();

            var load = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(
                SceneNames.MainMenu, UnityEngine.SceneManagement.LoadSceneMode.Additive);
            Assert.IsNotNull(load, "MainMenu 场景不在 Build Settings 里");
            while (!load.isDone) yield return null;
            yield return null;

            var menu = Object.FindObjectOfType<MainMenuPanel>();
            Assert.IsNotNull(menu, "入口场景里应该有主菜单面板");
            Assert.IsTrue(menu.IsOpen, "主菜单应该自动打开");

            var buttons = menu.Root.GetComponentsInChildren<Button>(true);
            var names = new List<string>();
            for (int i = 0; i < buttons.Length; i++) names.Add(buttons[i].name);

            Assert.Contains("StartButton", names, "缺少「开始游戏」按钮，实际有：" + string.Join(",", names));
            Assert.Contains("SettingsButton", names, "缺少「设置」按钮");
            Assert.Contains("QuitButton", names, "缺少「退出游戏」按钮");

            // 点「设置」应该弹出设置面板
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].name != "SettingsButton") continue;
                buttons[i].onClick.Invoke();
                break;
            }

            yield return null;
            Assert.IsTrue(UIManager.Instance.IsOpen<SettingsPanel>(), "点设置之后应该弹出设置面板");

            UIManager.Instance.CloseAll();
            var unload = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(SceneNames.MainMenu);
            while (unload != null && !unload.isDone) yield return null;
            yield return null;
            CleanupUi();
        }
    }
}
