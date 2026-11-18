using System;
using System.Collections.Generic;
using UnityEngine;
using ZMZZ.Player;
using ZMZZ.UI.Panels;

namespace ZMZZ.World
{
    /// <summary>
    /// 传送锚点。
    ///
    /// 第一块（本文件）：走到锚点旁边按 E 激活，激活状态会被记住。
    /// 第二块（下一步）：按 M 打开地图，选择已激活的锚点传送过去。
    ///
    /// 怎么用：在场景里建一个空物体，挂上这个脚本，位置放在你想当锚点的地方，
    /// 然后给它一个子物体当外观（方块），把子物体的 SpriteRenderer 拖到 visual 上。
    /// </summary>
    public class AnchorPoint : MonoBehaviour
    {
        [Header("身份（地图和传送都靠它来认人）")]
        [Tooltip("全游戏唯一，别重复。建议写成「区域_位置」，例如 area01_gate")]
        public string anchorId = "anchor_01";

        [Tooltip("地图上显示给玩家看的名字")]
        public string displayName = "未命名锚点";

        [Tooltip("所在场景名。留空就自动用当前场景的名字")]
        public string sceneName = "";

        [Header("交互（这两个是最值得你上手调的）")]
        [Tooltip("玩家走到多近才能激活，单位是格")]
        public float interactRadius = 2.2f;
        public KeyCode activateKey = KeyCode.E;

        [Header("外观（原型期就是换个颜色）")]
        public SpriteRenderer visual;
        public Color idleColor = new Color(0.62f, 0.66f, 0.76f, 1f);
        public Color activatedColor = new Color(0.35f, 0.9f, 0.6f, 1f);

        Transform _player;
        bool _wasInRange;

        /// <summary>
        /// 注意这里：激活状态不存在锚点自己身上，而是去问全局登记表。
        /// 这样传送走再回来，状态还在。
        /// </summary>
        public bool IsActivated { get { return AnchorRegistry.IsActivated(anchorId); } }

        public string SceneName
        {
            get { return string.IsNullOrEmpty(sceneName) ? gameObject.scene.name : sceneName; }
        }

        void Awake()
        {
            AnchorRegistry.Register(this);
            UpdateVisual();
        }

        void OnDestroy()
        {
            AnchorRegistry.Unregister(this);
        }

        void Update()
        {
            var player = FindPlayer();
            if (player == null) return;

            bool inRange = Vector2.Distance(player.position, transform.position) <= interactRadius;

            if (!inRange)
            {
                if (_wasInRange) InteractHint.Hide();
                _wasInRange = false;
                return;
            }

            _wasInRange = true;

            if (IsActivated)
            {
                InteractHint.Show("锚点已激活：" + displayName);
                return;
            }

            InteractHint.Show("按 " + activateKey + " 激活锚点：" + displayName);
            if (Input.GetKeyDown(activateKey)) Activate();
        }

        public void Activate()
        {
            if (IsActivated) return;

            AnchorRegistry.Activate(this);
            UpdateVisual();

            // 想加激活特效、音效、镜头抖动，都加在这后面
            Debug.Log("[ZMZZ] 锚点已激活：" + anchorId);
        }

        void UpdateVisual()
        {
            if (visual != null) visual.color = IsActivated ? activatedColor : idleColor;
        }

        Transform FindPlayer()
        {
            if (_player != null) return _player;
            var motor = FindObjectOfType<PlayerMotor>();
            if (motor != null) _player = motor.transform;
            return _player;
        }

        void OnDrawGizmos()
        {
            // 在 Scene 视图里能看见交互范围，方便你把半径调到合适大小
            Gizmos.color = Application.isPlaying && IsActivated
                ? activatedColor
                : new Color(0.45f, 0.70f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, interactRadius);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.4f);
        }
    }

    /// <summary>
    /// 全局锚点登记表：记住哪些锚点被激活过。
    ///
    /// 现在用 PlayerPrefs 存一行「逗号分隔的 ID」，够用且简单。
    /// 等做正式存档系统时，把 Load/Save 两个方法换成读写存档文件即可，外面的接口不用动。
    /// </summary>
    public static class AnchorRegistry
    {
        const string PrefsKey = "zmzz.activatedAnchors";

        static readonly HashSet<string> ActivatedIds = new HashSet<string>();
        static readonly List<AnchorPoint> AnchorsInScene = new List<AnchorPoint>();
        static bool _loaded;

        /// <summary>某个锚点刚被激活时触发。地图面板可以靠它实时刷新。</summary>
        public static event Action<string> Activated;

        /// <summary>当前场景里有哪些锚点（地图面板要用全量的，那是第二块的事）。</summary>
        public static IReadOnlyList<AnchorPoint> CurrentSceneAnchors { get { return AnchorsInScene; } }

        public static void Register(AnchorPoint anchor)
        {
            EnsureLoaded();
            if (anchor == null || AnchorsInScene.Contains(anchor)) return;
            AnchorsInScene.Add(anchor);
        }

        public static void Unregister(AnchorPoint anchor)
        {
            AnchorsInScene.Remove(anchor);
        }

        public static bool IsActivated(string anchorId)
        {
            EnsureLoaded();
            return !string.IsNullOrEmpty(anchorId) && ActivatedIds.Contains(anchorId);
        }

        public static void Activate(AnchorPoint anchor)
        {
            EnsureLoaded();
            if (anchor == null || string.IsNullOrEmpty(anchor.anchorId)) return;
            if (!ActivatedIds.Add(anchor.anchorId)) return;

            Save();
            var handler = Activated;
            if (handler != null) handler(anchor.anchorId);
        }

        /// <summary>调试用：清掉记录，重新体验一次「第一次激活」的流程。</summary>
        public static void ClearAll()
        {
            ActivatedIds.Clear();
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.Save();
        }

        static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            string raw = PlayerPrefs.GetString(PrefsKey, string.Empty);
            if (string.IsNullOrEmpty(raw)) return;

            string[] ids = raw.Split(',');
            for (int i = 0; i < ids.Length; i++)
                if (!string.IsNullOrEmpty(ids[i])) ActivatedIds.Add(ids[i]);
        }

        static void Save()
        {
            PlayerPrefs.SetString(PrefsKey, string.Join(",", ActivatedIds));
            PlayerPrefs.Save();
        }
    }
}
