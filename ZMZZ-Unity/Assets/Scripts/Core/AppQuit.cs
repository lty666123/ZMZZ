using UnityEngine;

namespace ZMZZ.Core
{
    /// <summary>退出游戏。编辑器里退出播放模式，打包后调 Application.Quit。</summary>
    public static class AppQuit
    {
        public static void Quit()
        {
            Debug.Log("[ZMZZ] 退出游戏");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
