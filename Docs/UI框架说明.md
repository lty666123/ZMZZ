# UI 框架说明

> 目的：让「加一个新界面」变成一件机械的事 —— 照抄模板就行，不用重新想架构。
> 位置：`Assets/Scripts/UI/`

---

## 1. 五个组成部分

| 文件 | 职责 |
| --- | --- |
| `UIManager.cs` | 总管。建层级、注册面板、开关面板、Esc 逐层返回、切场景加载遮罩。常驻对象（换场景不丢） |
| `UIPanel.cs` | 所有界面的基类。子类只实现 `BuildUI()`，生命周期由管理器负责 |
| `UIFactory.cs` | 代码建控件的零件库：文字、按钮、滑条、开关、容器 |
| `UITheme.cs` | 配色与字体。改一处全项目生效 |
| `UILayer.cs` | 四个层级：Background / Normal / Popup / Toast |

任何地方都能直接拿到管理器，不存在会自动创建：

```csharp
UIManager.Instance.Open<SettingsPanel>();
```

---

## 2. 当前的界面

| 界面 | 类 | 层级 | 说明 |
| --- | --- | --- | --- |
| 入口界面 | `MainMenuPanel` | Normal | 开始游戏 / 设置 / 退出游戏 |
| 战斗 HUD | `HudController` | Normal | 血条、体力、Boss 韧性、破防与弹反提示 |
| 暂停菜单 | `PausePanel` | Popup | Esc 唤起，打开时冻结游戏时间 |
| 设置 | `SettingsPanel` | Popup | 主音量、全屏、垂直同步、分辨率，存 PlayerPrefs |

场景流：`MainMenu`（入口）→ 点开始游戏 → `CombatPrototype`（战斗）→ Esc → 返回主菜单。
场景名不要硬编码，用 `Core/SceneNames.cs`。

---

## 3. 新增一个界面（照抄这段）

```csharp
using UnityEngine;
using ZMZZ.Core;

namespace ZMZZ.UI.Panels
{
    public class InventoryPanel : UIPanel
    {
        protected override void Awake()
        {
            layer = UILayer.Popup;   // 先定层级，再交给基类注册
            base.Awake();
        }

        protected override void BuildUI(RectTransform root)
        {
            // root 是一个铺满屏幕的容器，所有控件都挂在它下面
            UIFactory.CreateStretchImage(root, "Dim", UITheme.Dim);

            var panel = UIFactory.CreateImage(root, "Panel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 640f), UITheme.PanelBackground);

            UIFactory.CreateLabel(panel.transform, "Title", "背包", 46, UITheme.TextPrimary,
                                  new Vector2(0f, 250f), new Vector2(600f, 60f));

            UIFactory.CreateButton(panel.transform, "CloseButton", "关闭",
                                   new Vector2(0f, -250f), new Vector2(240f, 68f), Hide, UITheme.ButtonAccent);
        }

        protected override void OnOpen()  { /* 每次打开时刷新数据 */ }
        protected override void OnClose() { /* 保存、清理 */ }
    }
}
```

打开与关闭：

```csharp
UIManager.Instance.Open<InventoryPanel>();   // 没有就自动创建
UIManager.Instance.Close<InventoryPanel>();
UIManager.Instance.Toggle<InventoryPanel>(); // 用 Get 判断后开关
UIManager.Instance.CloseTop();               // 关掉最上面那个
```

可用的生命周期钩子：`OnBuilt`（界面搭好时）、`OnOpen`、`OnClose`、`Update`。

---

## 4. 约定与坑

1. **层级先想清楚**。弹窗一律 `Popup`，常驻 HUD 用 `Normal`，加载遮罩这类盖住一切的用 `Toast`。
2. **Esc 是逐层返回**：`UIManager` 会先关最上面那个 `closeOnEscape = true` 的面板。所以「暂停 → 设置」按 Esc 先回暂停，再按才回到游戏。需要拦截 Esc 的自定义逻辑，就在自己的 `Update` 里判断 `UIManager.InstanceOrNull.OpenCount == 0` 再处理，别抢。
3. **面板想要暂停游戏就设 `pauseGame = true`**，不要自己去改 `Time.timeScale` —— 统一走 `Core/GamePause.cs`，否则会和命中停顿系统打架（那边也在改时间流速）。
4. **两种放置方式都支持**：需要在场景里预先挂引用（比如 HUD 要连玩家和 Boss）的，就摆在场景里并勾 `openOnStart`；纯界面就直接 `Open<T>()` 懒创建。
5. **中文字体**已经处理好了：`UITheme.DefaultFont` 会自动从系统里挑带中文的字体（微软雅黑等）。新增文字不要自己写 `Resources.GetBuiltinResource<Font>`，否则中文会变方块。
6. **按钮需要 EventSystem**，`UIManager` 已经自动创建，新建场景不用管。

---

## 5. 相关命令

重新生成两个场景（改过场景结构代码之后）：

```
pwsh -NoProfile -ExecutionPolicy Bypass -File "E:\unity-codex\2D ZMZZ\Tools\run-unity.ps1" -Mode method -ExecuteMethod "ZMZZ.EditorTools.PrototypeSceneBuilder.BuildFromBatch"
```

跑自动化测试（需要先关掉 Unity 编辑器）：

```
pwsh -NoProfile -ExecutionPolicy Bypass -File "E:\unity-codex\2D ZMZZ\Tools\run-unity.ps1" -Mode test
```
