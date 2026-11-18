using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ZMZZ.Enemy;
using ZMZZ.Player;

namespace ZMZZ.Tests
{
    /// <summary>
    /// 场景冒烟测试：把自动生成的战斗场景加载起来跑两秒，
    /// 确认没有空引用、没有异常、双方都活着 —— 也就是「能真的玩」。
    /// </summary>
    public class CombatPrototypeSceneTests
    {
        [UnityTest]
        public IEnumerator CombatPrototype_LoadsAndRunsWithoutErrors()
        {
            var errors = new List<string>();
            Application.LogCallback handler = (condition, stackTrace, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                    errors.Add(type + ": " + condition);
            };
            Application.logMessageReceived += handler;

            var load = SceneManager.LoadSceneAsync("CombatPrototype", LoadSceneMode.Additive);
            Assert.IsNotNull(load, "CombatPrototype 场景不在 Build Settings 里");
            while (!load.isDone) yield return null;

            yield return null;

            var player = Object.FindObjectOfType<PlayerCombat>();
            var boss = Object.FindObjectOfType<BossController>();
            Assert.IsNotNull(player, "场景里找不到玩家");
            Assert.IsNotNull(boss, "场景里找不到 Boss");

            // 让 Boss 的连招真的跑起来。
            // 注意：无头模式下渲染帧远快于逻辑帧，所以这里按「游戏时间」等待而不是按帧数。
            float deadline = Time.time + 2f;
            int guard = 0;
            while (Time.time < deadline && guard++ < 20000) yield return null;

            Application.logMessageReceived -= handler;

            Assert.IsTrue(player.Health.Current > 0f || boss.Health.IsDead,
                "玩家不应该在没有任何输入的情况下被秒杀");
            Assert.IsEmpty(errors, "运行期出现异常：\n" + string.Join("\n", errors));

            // 测试隔离：把加进来的场景卸掉，别影响后面的用例
            Time.timeScale = 1f;
            var unload = SceneManager.UnloadSceneAsync("CombatPrototype");
            while (unload != null && !unload.isDone) yield return null;
        }
    }
}
