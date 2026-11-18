using UnityEngine;

namespace ZMZZ.Core
{
    /// <summary>
    /// 图层与物理查询的集中定义。所有 LayerMask 都从这里取，避免各处硬编码字符串。
    /// 图层由编辑器工具 PrototypeSceneBuilder 自动创建。
    /// </summary>
    public static class GameLayers
    {
        public const string PlayerName = "Player";
        public const string EnemyName = "Enemy";
        public const string GroundName = "Ground";

        static int _player = -1;
        static int _enemy = -1;
        static int _ground = -1;

        public static int Player
        {
            get { if (_player < 0) _player = LayerMask.NameToLayer(PlayerName); return _player < 0 ? 0 : _player; }
        }

        public static int Enemy
        {
            get { if (_enemy < 0) _enemy = LayerMask.NameToLayer(EnemyName); return _enemy < 0 ? 0 : _enemy; }
        }

        public static int Ground
        {
            get { if (_ground < 0) _ground = LayerMask.NameToLayer(GroundName); return _ground < 0 ? 0 : _ground; }
        }

        public static LayerMask PlayerMask { get { return 1 << Player; } }
        public static LayerMask EnemyMask { get { return 1 << Enemy; } }
        public static LayerMask GroundMask { get { return 1 << Ground; } }

        /// <summary>把某个物体连同所有子物体设为指定图层。</summary>
        public static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; i++)
                SetLayerRecursively(go.transform.GetChild(i).gameObject, layer);
        }
    }
}
