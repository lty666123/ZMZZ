using UnityEngine;
using ZMZZ.Core;

namespace ZMZZ.UI
{
    /// <summary>
    /// 所有界面的基类。子类只需要实现 BuildUI —— 在里面用 UIFactory 搭控件，
    /// 生命周期（创建、显示、隐藏、Esc 返回、暂停）全部由 UIManager 负责。
    ///
    /// 新增一个界面的标准做法（照抄即可）：
    ///   1. 继承 UIPanel，实现 BuildUI
    ///   2. Awake 里设 layer，然后 base.Awake()
    ///   3. 打开：UIManager.Instance.Open&lt;你的面板&gt;()
    /// </summary>
    public abstract class UIPanel : MonoBehaviour
    {
        [Tooltip("留空则用类名")]
        public string panelId = "";

        public UILayer layer = UILayer.Normal;

        [Tooltip("场景加载后自动打开")]
        public bool openOnStart = false;

        [Tooltip("按 Esc 能关掉它")]
        public bool closeOnEscape = true;

        [Tooltip("打开时暂停游戏（暂停菜单用）")]
        public bool pauseGame = false;

        public bool IsOpen { get; private set; }
        public RectTransform Root { get; private set; }
        public string PanelId { get { return string.IsNullOrEmpty(panelId) ? GetType().Name : panelId; } }

        bool _built;

        protected virtual void Awake()
        {
            UIManager.Instance.Register(this);
        }

        protected virtual void OnDestroy()
        {
            var manager = UIManager.InstanceOrNull;
            if (manager != null) manager.Unregister(this);
        }

        protected virtual void Start()
        {
            if (openOnStart) Show();
        }

        public void Show()
        {
            UIManager.Instance.OpenPanel(this);
        }

        public void Hide()
        {
            UIManager.Instance.ClosePanel(this);
        }

        public void Toggle()
        {
            if (IsOpen) Hide();
            else Show();
        }

        /// <summary>第一次显示前把界面搭出来（只搭一次，之后复用）。</summary>
        public void EnsureBuilt()
        {
            if (_built) return;
            _built = true;
            Root = UIManager.Instance.CreatePanelRoot(this);
            Root.gameObject.SetActive(false);
            BuildUI(Root);
            OnBuilt();
        }

        internal void InternalOpen()
        {
            EnsureBuilt();
            if (IsOpen)
            {
                Root.gameObject.SetActive(true);
                return;
            }

            IsOpen = true;
            Root.gameObject.SetActive(true);
            if (pauseGame) GamePause.Set(true);
            OnOpen();
        }

        internal void InternalClose()
        {
            if (Root != null) Root.gameObject.SetActive(false);
            if (!IsOpen) return;

            IsOpen = false;
            if (pauseGame) GamePause.Set(false);
            OnClose();
        }

        /// <summary>在这里用 UIFactory 搭界面。root 是一个铺满屏幕的容器。</summary>
        protected abstract void BuildUI(RectTransform root);

        protected virtual void OnBuilt() { }
        protected virtual void OnOpen() { }
        protected virtual void OnClose() { }
        protected virtual void Update() { }
    }
}
