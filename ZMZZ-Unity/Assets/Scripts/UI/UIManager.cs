using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZMZZ.Core;

namespace ZMZZ.UI
{
    /// <summary>
    /// UI 管理器：整个项目的界面都归它管。
    ///
    /// 职责：
    ///   · 建立并维护四个层级（背景 / 普通 / 弹窗 / 遮罩）
    ///   · 面板的注册、打开、关闭、叠加顺序
    ///   · Esc 逐层返回（先关最上面那个）
    ///   · 场景切换的加载遮罩
    ///
    /// 它是常驻对象（DontDestroyOnLoad），所以换场景不会把界面系统丢掉。
    /// 任何地方都可以直接 UIManager.Instance 拿到它，不存在则自动创建。
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        const int BaseSortingOrder = 10;

        static UIManager _instance;

        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[UIManager]");
                    _instance = go.AddComponent<UIManager>();
                }
                return _instance;
            }
        }

        public static UIManager InstanceOrNull { get { return _instance; } }

        readonly Dictionary<Type, UIPanel> _registered = new Dictionary<Type, UIPanel>();
        readonly List<UIPanel> _open = new List<UIPanel>();
        readonly RectTransform[] _layerRoots = new RectTransform[4];

        Image _loadingOverlay;
        Text _loadingText;

        public int OpenCount { get { Purge(); return _open.Count; } }
        public UIPanel TopOpen { get { Purge(); return _open.Count > 0 ? _open[_open.Count - 1] : null; } }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            BuildLayers();
            EnsureEventSystem();
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            var top = TopOpen;
            if (top != null && top.closeOnEscape) ClosePanel(top);
        }

        // ---------------- 注册与查找 ----------------

        public void Register(UIPanel panel)
        {
            if (panel == null) return;
            _registered[panel.GetType()] = panel;
        }

        public void Unregister(UIPanel panel)
        {
            if (panel == null) return;
            UIPanel current;
            if (_registered.TryGetValue(panel.GetType(), out current) && current == panel)
                _registered.Remove(panel.GetType());
            _open.Remove(panel);
        }

        /// <summary>找一个已存在的面板；找不到返回 null。</summary>
        public T Get<T>() where T : UIPanel
        {
            Purge();

            UIPanel panel;
            if (_registered.TryGetValue(typeof(T), out panel) && panel != null) return panel as T;

            var found = FindObjectOfType<T>();
            if (found != null)
            {
                Register(found);
                return found;
            }
            return null;
        }

        /// <summary>找不到就现建一个（界面是代码搭的，所以随时可以造）。</summary>
        public T GetOrCreate<T>() where T : UIPanel
        {
            var existing = Get<T>();
            if (existing != null) return existing;

            var go = new GameObject(typeof(T).Name);
            return go.AddComponent<T>();
        }

        // ---------------- 开关 ----------------

        public T Open<T>() where T : UIPanel
        {
            var panel = GetOrCreate<T>();
            OpenPanel(panel);
            return panel;
        }

        public void OpenPanel(UIPanel panel)
        {
            if (panel == null) return;
            Purge();
            if (!_open.Contains(panel)) _open.Add(panel);
            panel.InternalOpen();
        }

        public void ClosePanel(UIPanel panel)
        {
            if (panel == null) return;
            _open.Remove(panel);
            panel.InternalClose();
        }

        public void Close<T>() where T : UIPanel
        {
            var panel = Get<T>();
            if (panel != null) ClosePanel(panel);
        }

        public void CloseTop()
        {
            var top = TopOpen;
            if (top != null) ClosePanel(top);
        }

        public void CloseAll()
        {
            Purge();
            while (_open.Count > 0) ClosePanel(_open[_open.Count - 1]);
        }

        public bool IsOpen<T>() where T : UIPanel
        {
            var panel = Get<T>();
            return panel != null && panel.IsOpen;
        }

        // ---------------- 场景切换 ----------------

        /// <summary>带加载遮罩的场景切换。遮罩常驻，所以换场景不会闪。</summary>
        public void LoadScene(string sceneName, string loadingLabel = "载入中…")
        {
            StartCoroutine(LoadSceneRoutine(sceneName, loadingLabel));
        }

        IEnumerator LoadSceneRoutine(string sceneName, string loadingLabel)
        {
            CloseAll();
            GamePause.Set(false);
            ShowLoading(loadingLabel);

            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                Debug.LogError("[ZMZZ] 场景加载失败：请确认 " + sceneName + " 已加进 Build Settings");
                HideLoading();
                yield break;
            }

            while (!operation.isDone) yield return null;

            yield return null;
            HideLoading();
        }

        void ShowLoading(string label)
        {
            if (_loadingOverlay == null)
            {
                var root = _layerRoots[(int)UILayer.Toast];
                _loadingOverlay = UIFactory.CreateStretchImage(root, "LoadingOverlay", new Color(0f, 0f, 0f, 1f));
                _loadingText = UIFactory.CreateLabel(_loadingOverlay.transform, "LoadingLabel", label, 40,
                                                     UITheme.TextPrimary, Vector2.zero, new Vector2(800f, 80f));
            }

            if (_loadingText != null) _loadingText.text = label;
            _loadingOverlay.gameObject.SetActive(true);
            _loadingOverlay.transform.SetAsLastSibling();
        }

        void HideLoading()
        {
            if (_loadingOverlay != null) _loadingOverlay.gameObject.SetActive(false);
        }

        // ---------------- 内部 ----------------

        internal RectTransform CreatePanelRoot(UIPanel panel)
        {
            var go = new GameObject(panel.GetType().Name + "Root", typeof(RectTransform));
            go.transform.SetParent(_layerRoots[(int)panel.layer], false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        void BuildLayers()
        {
            for (int i = 0; i < _layerRoots.Length; i++)
            {
                var layer = (UILayer)i;
                var go = new GameObject(layer + " Layer", typeof(RectTransform), typeof(Canvas),
                                        typeof(CanvasScaler), typeof(GraphicRaycaster));
                go.transform.SetParent(transform, false);

                var canvas = go.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = BaseSortingOrder + i * 100;

                var scaler = go.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;

                _layerRoots[i] = go.GetComponent<RectTransform>();
            }
        }

        void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            var go = new GameObject("[EventSystem]", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(go);
        }

        /// <summary>清掉已经被销毁的面板引用（换场景后必然出现）。</summary>
        void Purge()
        {
            for (int i = _open.Count - 1; i >= 0; i--)
                if (_open[i] == null) _open.RemoveAt(i);

            List<Type> stale = null;
            foreach (var pair in _registered)
            {
                if (pair.Value != null) continue;
                if (stale == null) stale = new List<Type>();
                stale.Add(pair.Key);
            }

            if (stale != null)
                for (int i = 0; i < stale.Count; i++) _registered.Remove(stale[i]);
        }
    }
}
