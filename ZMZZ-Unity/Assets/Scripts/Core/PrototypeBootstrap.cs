using UnityEngine;

namespace ZMZZ.Core
{
    /// <summary>
    /// 原型期的一次性设置。放在场景里一个空物体上即可。
    /// 逻辑帧锁定 60Hz —— 动作游戏的帧数据必须建立在固定步长之上，否则手感会飘。
    /// </summary>
    public class PrototypeBootstrap : MonoBehaviour
    {
        public bool lockTo60Fps = true;

        void Awake()
        {
            Time.fixedDeltaTime = 1f / 60f;
            Time.maximumDeltaTime = 0.1f;
            Physics2D.queriesHitTriggers = true;

            // 玩家与敌人不做物理碰撞（否则会互相顶开），攻击判定全部走 Hitbox 的主动查询
            Physics2D.IgnoreLayerCollision(GameLayers.Player, GameLayers.Enemy, true);

            if (lockTo60Fps)
            {
                QualitySettings.vSyncCount = 1;
                Application.targetFrameRate = 60;
            }
        }
    }
}
