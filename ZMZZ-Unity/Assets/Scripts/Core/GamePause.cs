using UnityEngine;

namespace ZMZZ.Core
{
    /// <summary>
    /// 全局暂停。集中在一处，是为了避免「暂停把 Time.timeScale 设成 0，
    /// 结果被命中停顿系统又改回 1」这类互相打架的问题。
    /// </summary>
    public static class GamePause
    {
        public static bool IsPaused { get; private set; }

        public static void Set(bool paused)
        {
            IsPaused = paused;
            Apply();
        }

        /// <summary>把暂停状态重新写进 Time.timeScale（命中停顿结束后也可能调用）。</summary>
        public static void Apply()
        {
            if (IsPaused)
            {
                Time.timeScale = 0f;
                return;
            }

            var hitStop = HitStopManager.Instance;
            Time.timeScale = hitStop != null ? hitStop.BaseTimeScale : 1f;
        }
    }
}
