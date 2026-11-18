using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZMZZ.UI.Panels
{
    /// <summary>
    /// 设置面板：主音量、显示模式、垂直同步、分辨率。设置存在 PlayerPrefs 里，重启后仍然生效。
    /// 目前项目还没有音频资源，所以「主音量」先接在 AudioListener.volume 上，等接入音乐音效后自动生效。
    /// </summary>
    public class SettingsPanel : UIPanel
    {
        const string KeyVolume = "settings.volume";
        const string KeyFullscreen = "settings.fullscreen";
        const string KeyVSync = "settings.vsync";
        const string KeyResolution = "settings.resolutionIndex";

        static readonly Vector2Int[] ResolutionPresets =
        {
            new Vector2Int(1280, 720),
            new Vector2Int(1600, 900),
            new Vector2Int(1920, 1080),
            new Vector2Int(2560, 1440),
            new Vector2Int(3840, 2160),
        };

        readonly List<Vector2Int> _available = new List<Vector2Int>();

        Slider _volumeSlider;
        Toggle _fullscreenToggle;
        Toggle _vsyncToggle;
        Text _resolutionLabel;
        int _resolutionIndex;
        bool _applying;

        protected override void Awake()
        {
            layer = UILayer.Popup;
            base.Awake();
        }

        protected override void BuildUI(RectTransform root)
        {
            UIFactory.CreateStretchImage(root, "Dim", UITheme.Dim);

            var panel = UIFactory.CreateImage(root, "Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                              Vector2.zero, new Vector2(880f, 620f), UITheme.PanelBackground);
            var container = panel.rectTransform;

            UIFactory.CreateLabel(container, "Title", "设置", 48, UITheme.TextPrimary,
                                  new Vector2(0f, 240f), new Vector2(700f, 64f));

            UIFactory.CreateLabel(container, "VolumeLabel", "主音量", 28, UITheme.TextSecondary,
                                  new Vector2(-280f, 150f), new Vector2(240f, 40f), TextAnchor.MiddleLeft);
            _volumeSlider = UIFactory.CreateSlider(container, "VolumeSlider", new Vector2(110f, 150f),
                                                   new Vector2(400f, 40f), 0f, 1f, 1f, OnVolumeChanged);

            _fullscreenToggle = UIFactory.CreateToggle(container, "FullscreenToggle", "全屏显示",
                                                       new Vector2(-130f, 70f), new Vector2(620f, 40f),
                                                       true, OnFullscreenChanged);

            _vsyncToggle = UIFactory.CreateToggle(container, "VSyncToggle", "垂直同步",
                                                  new Vector2(-130f, 0f), new Vector2(620f, 40f),
                                                  true, OnVSyncChanged);

            UIFactory.CreateLabel(container, "ResolutionLabel", "分辨率", 28, UITheme.TextSecondary,
                                  new Vector2(-280f, -70f), new Vector2(240f, 40f), TextAnchor.MiddleLeft);
            _resolutionLabel = UIFactory.CreateLabel(container, "ResolutionValue", "-", 28, UITheme.TextPrimary,
                                                     new Vector2(-60f, -70f), new Vector2(200f, 40f),
                                                     TextAnchor.MiddleLeft);
            UIFactory.CreateButton(container, "ResolutionPrev", "<", new Vector2(140f, -70f), new Vector2(70f, 56f),
                                   () => CycleResolution(-1), null, 30);
            UIFactory.CreateButton(container, "ResolutionNext", ">", new Vector2(232f, -70f), new Vector2(70f, 56f),
                                   () => CycleResolution(1), null, 30);

            UIFactory.CreateButton(container, "DefaultButton", "恢复默认", new Vector2(-140f, -230f),
                                   new Vector2(240f, 68f), ResetToDefaults);
            UIFactory.CreateButton(container, "CloseButton", "返回", new Vector2(140f, -230f),
                                   new Vector2(240f, 68f), Hide, UITheme.ButtonAccent);
        }

        protected override void OnBuilt()
        {
            BuildAvailableResolutions();
        }

        protected override void OnOpen()
        {
            LoadFromPrefs();
        }

        protected override void OnClose()
        {
            SaveToPrefs();
        }

        // ---------------- 逻辑 ----------------

        void BuildAvailableResolutions()
        {
            _available.Clear();
            var native = Screen.currentResolution;

            for (int i = 0; i < ResolutionPresets.Length; i++)
            {
                var preset = ResolutionPresets[i];
                if (preset.x <= native.width && preset.y <= native.height) _available.Add(preset);
            }

            // 屏幕比最小预设还小的话，至少留一个「当前分辨率」
            if (_available.Count == 0) _available.Add(new Vector2Int(Screen.width, Screen.height));
        }

        void LoadFromPrefs()
        {
            _applying = true;

            float volume = PlayerPrefs.GetFloat(KeyVolume, 1f);
            int defaultResolution = _available.Count - 1;
            _resolutionIndex = Mathf.Clamp(PlayerPrefs.GetInt(KeyResolution, defaultResolution), 0, _available.Count - 1);
            bool fullscreen = PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) == 1;
            bool vsync = PlayerPrefs.GetInt(KeyVSync, 1) == 1;

            if (_volumeSlider != null) _volumeSlider.value = volume;
            if (_fullscreenToggle != null) _fullscreenToggle.isOn = fullscreen;
            if (_vsyncToggle != null) _vsyncToggle.isOn = vsync;

            UpdateResolutionLabel();
            _applying = false;
            ApplyAll();
        }

        void SaveToPrefs()
        {
            PlayerPrefs.SetFloat(KeyVolume, _volumeSlider != null ? _volumeSlider.value : 1f);
            PlayerPrefs.SetInt(KeyFullscreen, _fullscreenToggle != null && _fullscreenToggle.isOn ? 1 : 0);
            PlayerPrefs.SetInt(KeyVSync, _vsyncToggle != null && _vsyncToggle.isOn ? 1 : 0);
            PlayerPrefs.SetInt(KeyResolution, _resolutionIndex);
            PlayerPrefs.Save();
        }

        void ApplyAll()
        {
            AudioListener.volume = _volumeSlider != null ? _volumeSlider.value : 1f;
            QualitySettings.vSyncCount = (_vsyncToggle != null && _vsyncToggle.isOn) ? 1 : 0;

            if (_available.Count == 0) return;
            var preset = _available[Mathf.Clamp(_resolutionIndex, 0, _available.Count - 1)];
            bool fullscreen = _fullscreenToggle != null && _fullscreenToggle.isOn;

            if (Screen.fullScreen != fullscreen) Screen.fullScreen = fullscreen;
            if (Screen.width != preset.x || Screen.height != preset.y)
                Screen.SetResolution(preset.x, preset.y, fullscreen);
        }

        void OnVolumeChanged(float value)
        {
            if (_applying) return;
            AudioListener.volume = value;
        }

        void OnFullscreenChanged(bool value)
        {
            if (_applying) return;
            Screen.fullScreen = value;
        }

        void OnVSyncChanged(bool value)
        {
            if (_applying) return;
            QualitySettings.vSyncCount = value ? 1 : 0;
        }

        void CycleResolution(int delta)
        {
            if (_available.Count == 0) return;
            _resolutionIndex = (_resolutionIndex + delta + _available.Count) % _available.Count;
            UpdateResolutionLabel();
            ApplyAll();
        }

        void UpdateResolutionLabel()
        {
            if (_resolutionLabel == null || _available.Count == 0) return;
            var preset = _available[Mathf.Clamp(_resolutionIndex, 0, _available.Count - 1)];
            _resolutionLabel.text = preset.x + " × " + preset.y;
        }

        void ResetToDefaults()
        {
            _applying = true;
            if (_volumeSlider != null) _volumeSlider.value = 1f;
            if (_fullscreenToggle != null) _fullscreenToggle.isOn = true;
            if (_vsyncToggle != null) _vsyncToggle.isOn = true;
            _resolutionIndex = _available.Count > 0 ? _available.Count - 1 : 0;
            UpdateResolutionLabel();
            _applying = false;
            ApplyAll();
        }
    }
}
