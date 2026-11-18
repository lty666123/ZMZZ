using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZMZZ.Combat;
using ZMZZ.Core;
using ZMZZ.Enemy;
using ZMZZ.Player;

namespace ZMZZ.UI
{
    /// <summary>
    /// 战斗 HUD。现在它也是 UIManager 管的一个面板（Normal 层），
    /// 所以暂停菜单、设置这类弹窗会自然地盖在它上面。
    /// 只显示战斗中必须一眼看到的信息：自己的血与体力、Boss 的血与韧性、弹反窗口提示。
    /// </summary>
    public class HudController : UIPanel
    {
        public PlayerCombat Player;
        public BossController Boss;
        public Health PlayerHealth;
        public Stamina PlayerStamina;
        public Health BossHealth;
        public Stagger BossStagger;

        [Header("配色")]
        public Color PlayerHealthColor = new Color(0.30f, 0.82f, 0.45f, 1f);
        public Color PlayerStaminaColor = new Color(0.95f, 0.76f, 0.28f, 1f);
        public Color BossHealthColor = new Color(0.83f, 0.29f, 0.26f, 1f);
        public Color BossStaggerColor = new Color(0.35f, 0.75f, 0.95f, 1f);
        public Color BossBrokenColor = new Color(1f, 0.45f, 0.85f, 1f);
        public Color BackColor = new Color(0.05f, 0.05f, 0.07f, 0.75f);

        Image _playerHpFill;
        Image _playerStaminaFill;
        Image _bossHpFill;
        Image _bossStaggerFill;
        Image _parryWindowBadge;
        Text _stateText;
        Text _bossActionText;
        Text _breakText;
        Text _outcomeText;
        Transform _canvasTransform;

        enum Outcome { None, Victory, Defeat }
        Outcome _outcome = Outcome.None;

        protected override void Awake()
        {
            layer = UILayer.Normal;
            base.Awake();
        }

        protected override void Update()
        {
            RefreshBars();
            RefreshTexts();
            HandleOutcome();
        }

        void RefreshBars()
        {
            if (PlayerHealth != null && _playerHpFill != null)
                _playerHpFill.fillAmount = PlayerHealth.Normalized;

            if (PlayerStamina != null && _playerStaminaFill != null)
                _playerStaminaFill.fillAmount = PlayerStamina.Normalized;

            if (BossHealth != null && _bossHpFill != null)
                _bossHpFill.fillAmount = BossHealth.Normalized;

            if (BossStagger != null && _bossStaggerFill != null)
            {
                _bossStaggerFill.fillAmount = BossStagger.Normalized;
                _bossStaggerFill.color = BossStagger.IsBroken ? BossBrokenColor : BossStaggerColor;
            }

            if (_parryWindowBadge != null)
            {
                bool visible = Player != null && Player.InParryWindow;
                _parryWindowBadge.enabled = visible;
                if (visible)
                {
                    var c = _parryWindowBadge.color;
                    c.a = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 30f);
                    _parryWindowBadge.color = c;
                }
            }

            if (_breakText != null)
                _breakText.enabled = BossStagger != null && BossStagger.IsBroken;
        }

        void RefreshTexts()
        {
            if (_stateText != null)
            {
                string status = Player != null ? Player.StateLabel : "-";
                _stateText.text = "状态：" + status;
            }

            if (_bossActionText != null)
                _bossActionText.text = Boss != null ? "Boss：" + Boss.ActionLabel : "";
        }

        void HandleOutcome()
        {
            if (_outcome == Outcome.None)
            {
                if (PlayerHealth != null && PlayerHealth.IsDead) _outcome = Outcome.Defeat;
                else if (BossHealth != null && BossHealth.IsDead) _outcome = Outcome.Victory;

                if (_outcome != Outcome.None && _outcomeText != null)
                {
                    _outcomeText.enabled = true;
                    _outcomeText.text = _outcome == Outcome.Victory
                        ? "VICTORY\n按 R 重来一局"
                        : "YOU DIED\n按 R 重来一局";
                    _outcomeText.color = _outcome == Outcome.Victory
                        ? new Color(0.55f, 1f, 0.65f, 1f)
                        : new Color(1f, 0.45f, 0.45f, 1f);
                }
            }

            if (_outcome != Outcome.None && Input.GetKeyDown(KeyCode.R))
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        protected override void BuildUI(RectTransform root)
        {
            _canvasTransform = root;

            _playerHpFill = CreateBar(_canvasTransform, "PlayerHp",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(40f, -40f), new Vector2(420f, 26f), PlayerHealthColor);

            _playerStaminaFill = CreateBar(_canvasTransform, "PlayerStamina",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(40f, -74f), new Vector2(420f, 14f), PlayerStaminaColor);

            _parryWindowBadge = CreateImage(_canvasTransform, "ParryBadge",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(474f, -40f), new Vector2(104f, 26f),
                new Color(1f, 0.85f, 0.2f, 0.9f));
            _parryWindowBadge.enabled = false;

            CreateText(_canvasTransform, "ParryBadgeLabel", "弹反窗口",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(474f, -40f), new Vector2(104f, 26f), 17, TextAnchor.MiddleCenter,
                new Color(0.08f, 0.08f, 0.1f, 1f));

            _bossHpFill = CreateBar(_canvasTransform, "BossHp",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 96f), new Vector2(900f, 26f), BossHealthColor);

            _bossStaggerFill = CreateBar(_canvasTransform, "BossStagger",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 64f), new Vector2(900f, 12f), BossStaggerColor);

            _breakText = CreateText(_canvasTransform, "BreakText", "破 防 !",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 140f), new Vector2(400f, 44f), 34, TextAnchor.MiddleCenter,
                new Color(1f, 0.5f, 0.9f, 1f));
            _breakText.enabled = false;

            CreateText(_canvasTransform, "BossLabel", "BOSS",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-380f, 140f), new Vector2(200f, 30f), 22, TextAnchor.MiddleLeft,
                new Color(0.9f, 0.85f, 0.8f, 0.9f));

            _stateText = CreateText(_canvasTransform, "StateText", "",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-40f, -40f), new Vector2(460f, 30f), 20, TextAnchor.MiddleRight,
                new Color(0.85f, 0.9f, 1f, 0.9f));

            _bossActionText = CreateText(_canvasTransform, "BossActionText", "",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-40f, -74f), new Vector2(520f, 30f), 20, TextAnchor.MiddleRight,
                new Color(1f, 0.8f, 0.7f, 0.9f));

            CreateText(_canvasTransform, "Hints",
                "移动 A/D　跳跃 空格　攻击 J（三连段）　闪避 K　格挡 L（按住）\n" +
                "弹反 = 举盾瞬间被打中　·　黄色前摇可格挡　红色前摇只能闪避\n" +
                "调试：T 切换慢动作　R 重开　数值在 Resources/CombatTuning.asset",
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(40f, 30f), new Vector2(1000f, 90f), 18, TextAnchor.LowerLeft,
                new Color(0.8f, 0.85f, 0.9f, 0.75f));

            _outcomeText = CreateText(_canvasTransform, "Outcome", "",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 200f), 60, TextAnchor.MiddleCenter,
                Color.white);
            _outcomeText.enabled = false;
        }

        Image CreateImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
                          Vector2 pivot, Vector2 anchoredPos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        Image CreateBar(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
                        Vector2 pivot, Vector2 anchoredPos, Vector2 size, Color fillColor)
        {
            var back = CreateImage(parent, name + "Back", anchorMin, anchorMax, pivot, anchoredPos, size, BackColor);

            var fillGo = new GameObject(name + "Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(back.transform, false);
            var rect = fillGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = fillGo.GetComponent<Image>();
            image.sprite = PrimitiveSprite.White;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = 0;
            image.fillAmount = 1f;
            image.color = fillColor;
            return image;
        }

        Text CreateText(Transform parent, string name, string content, Vector2 anchorMin, Vector2 anchorMax,
                        Vector2 pivot, Vector2 anchoredPos, Vector2 size, int fontSize,
                        TextAnchor alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var text = go.GetComponent<Text>();
            text.font = UITheme.DefaultFont;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
