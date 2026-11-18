using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using ZMZZ.CameraSystem;
using ZMZZ.Combat;
using ZMZZ.Core;
using ZMZZ.Enemy;
using ZMZZ.Player;
using ZMZZ.UI;
using ZMZZ.UI.Panels;

namespace ZMZZ.EditorTools
{
    /// <summary>
    /// 用代码搭出战斗原型场景。
    /// 好处是：场景完全可重建、可版本管理、可批量执行（-executeMethod PrototypeSceneBuilder.BuildFromBatch），
    /// 而且不需要手工在编辑器里点几十次。
    ///
    /// 菜单：ZMZZ / 构建战斗原型场景
    /// </summary>
    public static class PrototypeSceneBuilder
    {
        const string SceneFolder = "Assets/Scenes";
        const string ScenePath = SceneFolder + "/CombatPrototype.unity";
        const string MainMenuScenePath = SceneFolder + "/MainMenu.unity";
        const string ArtFolder = "Assets/Art";
        const string SpritePath = ArtFolder + "/WhiteSquare.png";
        const string ResourcesFolder = "Assets/Resources";
        const string TuningPath = ResourcesFolder + "/CombatTuning.asset";

        static readonly Color GroundColor = new Color(0.17f, 0.18f, 0.22f, 1f);
        static readonly Color PlayerColor = new Color(0.35f, 0.62f, 0.95f, 1f);
        static readonly Color BossColor = new Color(0.78f, 0.30f, 0.32f, 1f);

        [MenuItem("ZMZZ/构建战斗原型场景")]
        public static void BuildFromMenu()
        {
            Build();
        }

        /// <summary>给 -executeMethod 用的无参入口。</summary>
        public static void BuildFromBatch()
        {
            Build();
        }

        static void Build()
        {
            EnsureFolders();
            EnsureLayers();
            var sprite = EnsureWhiteSprite();
            var tuning = EnsureTuning();

            BuildGameScene(tuning, sprite);
            BuildMainMenuScene();

            // 主菜单必须排在场景列表第一个，打包后才会从入口界面开始
            EnsureInBuildSettings(ScenePath);
            EnsureInBuildSettings(MainMenuScenePath);

            AssetDatabase.SaveAssets();
            Debug.Log("[ZMZZ] 场景已生成：" + MainMenuScenePath + "（入口）与 " + ScenePath + "（战斗）");
        }

        static void BuildGameScene(CombatTuning tuning, Sprite sprite)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildGlobalLight();
            BuildArena(tuning, sprite);
            var player = BuildPlayer(tuning, sprite);
            var boss = BuildBoss(tuning, sprite);
            BuildCamera(tuning, player.Root.transform);
            BuildHud(player.Combat, player.Health, player.Stamina, boss.Controller, boss.Health, boss.Stagger);
            BuildPausePanel();
            BuildSystems();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        static void BuildMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildMenuCamera();

            var go = new GameObject("MainMenuPanel");
            var menu = go.AddComponent<MainMenuPanel>();
            menu.openOnStart = true;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        }

        static void BuildMenuCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0f, 0f, -10f);

            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UITheme.MenuBackground;
            camera.nearClipPlane = -50f;
            go.AddComponent<AudioListener>();
        }

        static void BuildPausePanel()
        {
            var go = new GameObject("PausePanel");
            var pause = go.AddComponent<PausePanel>();
            pause.openOnStart = false;
            pause.pauseGame = true;
            pause.closeOnEscape = true;
        }

        // ---------------- 资源准备 ----------------

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(SceneFolder)) AssetDatabase.CreateFolder("Assets", "Scenes");
            if (!AssetDatabase.IsValidFolder(ArtFolder)) AssetDatabase.CreateFolder("Assets", "Art");
            if (!AssetDatabase.IsValidFolder(ResourcesFolder)) AssetDatabase.CreateFolder("Assets", "Resources");
        }

        static void EnsureLayers()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("[ZMZZ] 读不到 TagManager.asset，跳过图层检查");
                return;
            }

            var tagManager = new SerializedObject(assets[0]);
            var layers = tagManager.FindProperty("layers");
            EnsureLayer(layers, GameLayers.PlayerName);
            EnsureLayer(layers, GameLayers.EnemyName);
            EnsureLayer(layers, GameLayers.GroundName);
            tagManager.ApplyModifiedProperties();
        }

        static void EnsureLayer(SerializedProperty layers, string layerName)
        {
            for (int i = 0; i < layers.arraySize; i++)
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName) return;

            // 0~7 是 Unity 内置图层，只能用 8 以上
            for (int i = 8; i < layers.arraySize; i++)
            {
                var element = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(element.stringValue))
                {
                    element.stringValue = layerName;
                    Debug.Log("[ZMZZ] 已创建图层 " + layerName + "（索引 " + i + "）");
                    return;
                }
            }
            Debug.LogWarning("[ZMZZ] 没有空图层可以放 " + layerName);
        }

        static Sprite EnsureWhiteSprite()
        {
            if (!File.Exists(SpritePath))
            {
                var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                var pixels = new Color32[16 * 16];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color32(255, 255, 255, 255);
                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceUpdate);
            }

            var importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    changed = true;
                }
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    changed = true;
                }
                if (!Mathf.Approximately(importer.spritePixelsPerUnit, 16f))
                {
                    importer.spritePixelsPerUnit = 16f;   // 16px = 1 个单位
                    changed = true;
                }
                if (importer.filterMode != FilterMode.Point)
                {
                    importer.filterMode = FilterMode.Point;
                    changed = true;
                }
                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    changed = true;
                }
                if (changed) importer.SaveAndReimport();
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (sprite == null) Debug.LogWarning("[ZMZZ] 占位图导入失败：" + SpritePath);
            return sprite;
        }

        static CombatTuning EnsureTuning()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<CombatTuning>(TuningPath);
            if (tuning == null)
            {
                tuning = ScriptableObject.CreateInstance<CombatTuning>();
                AssetDatabase.CreateAsset(tuning, TuningPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[ZMZZ] 已创建战斗数值资产：" + TuningPath);
            }
            return tuning;
        }

        static void EnsureInBuildSettings(string path)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ---------------- 场景内容 ----------------

        static void BuildGlobalLight()
        {
            // URP 2D 下必须有一盏全局光，否则 Sprite-Lit 材质全是黑的
            var go = new GameObject("Global Light 2D");
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.color = Color.white;
            light.intensity = 1f;
        }

        static void BuildArena(CombatTuning tuning, Sprite sprite)
        {
            float halfWidth = tuning.arenaHalfWidth;

            var ground = new GameObject("Ground");
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            var groundCollider = ground.AddComponent<BoxCollider2D>();
            groundCollider.size = new Vector2((halfWidth + 3f) * 2f, 1f);
            var groundVisual = CreateSprite(ground.transform, "Visual", groundCollider.size, GroundColor, sprite, -20);
            groundVisual.transform.localPosition = Vector3.zero;
            GameLayers.SetLayerRecursively(ground, GameLayers.Ground);

            CreateWall("WallLeft", -halfWidth - 1f, sprite);
            CreateWall("WallRight", halfWidth + 1f, sprite);
        }

        static void CreateWall(string name, float x, Sprite sprite)
        {
            var wall = new GameObject(name);
            wall.transform.position = new Vector3(x, 4f, 0f);
            var collider = wall.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1f, 10f);
            var visual = CreateSprite(wall.transform, "Visual", collider.size, GroundColor, sprite, -20);
            visual.transform.localPosition = Vector3.zero;
            GameLayers.SetLayerRecursively(wall, GameLayers.Ground);
        }

        struct PlayerRig
        {
            public GameObject Root;
            public PlayerCombat Combat;
            public Health Health;
            public Stamina Stamina;
            public PlayerInputReader Input;
            public PlayerMotor Motor;
        }

        static PlayerRig BuildPlayer(CombatTuning tuning, Sprite sprite)
        {
            var rig = new PlayerRig();
            var root = new GameObject("Player");
            root.transform.position = tuning.playerSpawn;
            root.layer = GameLayers.Player;
            rig.Root = root;

            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = tuning.gravityScale;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var solidCollider = root.AddComponent<BoxCollider2D>();
            solidCollider.size = new Vector2(0.62f, 1.78f);

            var hurtCollider = root.AddComponent<BoxCollider2D>();
            hurtCollider.isTrigger = true;
            hurtCollider.size = new Vector2(0.8f, 1.9f);

            var visual = CreateSprite(root.transform, "Visual", new Vector2(0.62f, 1.78f), PlayerColor, sprite, 10);
            visual.transform.localPosition = Vector3.zero;
            var flash = visual.gameObject.AddComponent<ColorFlash>();
            flash.target = visual;
            flash.baseColor = PlayerColor;

            var guard = CreateSprite(root.transform, "GuardVisual", new Vector2(0.34f, 1.5f),
                                     new Color(0.4f, 0.62f, 1f, 0.45f), sprite, 12);
            guard.transform.localPosition = new Vector3(0.62f, 0.1f, 0f);
            guard.enabled = false;

            var groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(root.transform, false);
            groundCheck.transform.localPosition = new Vector3(0f, -0.9f, 0f);

            var hitbox = BuildHitbox(root.transform, "Hitbox", Team.Player, GameLayers.EnemyMask, sprite);
            var runner = BuildAttackRunner(root.transform, hitbox, body);

            rig.Input = root.AddComponent<PlayerInputReader>();
            rig.Motor = root.AddComponent<PlayerMotor>();
            rig.Motor.Body = body;
            rig.Motor.GroundCheck = groundCheck.transform;

            rig.Health = root.AddComponent<Health>();
            rig.Health.maxHealth = tuning.playerMaxHealth;

            rig.Stamina = root.AddComponent<Stamina>();
            rig.Stamina.max = tuning.playerMaxStamina;

            var hurtbox = root.AddComponent<Hurtbox>();
            hurtbox.team = Team.Player;
            hurtbox.owner = root.transform;
            hurtbox.knockbackScale = 1f;
            hurtbox.health = rig.Health;
            hurtbox.body = body;

            rig.Combat = root.AddComponent<PlayerCombat>();
            rig.Combat.Input = rig.Input;
            rig.Combat.Motor = rig.Motor;
            rig.Combat.Runner = runner;
            rig.Combat.Health = rig.Health;
            rig.Combat.Stamina = rig.Stamina;
            rig.Combat.Flash = flash;
            rig.Combat.GuardVisual = guard;

            GameLayers.SetLayerRecursively(root, GameLayers.Player);
            return rig;
        }

        struct BossRig
        {
            public GameObject Root;
            public BossController Controller;
            public Health Health;
            public Stagger Stagger;
        }

        static BossRig BuildBoss(CombatTuning tuning, Sprite sprite)
        {
            var rig = new BossRig();
            var root = new GameObject("Boss");
            root.transform.position = tuning.bossSpawn;
            root.layer = GameLayers.Enemy;
            rig.Root = root;

            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = tuning.gravityScale;
            body.freezeRotation = true;
            body.mass = 10f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var solidCollider = root.AddComponent<BoxCollider2D>();
            solidCollider.size = new Vector2(1.8f, 2.9f);

            var hurtCollider = root.AddComponent<BoxCollider2D>();
            hurtCollider.isTrigger = true;
            hurtCollider.size = new Vector2(2.2f, 3.0f);

            var visual = CreateSprite(root.transform, "Visual", new Vector2(2.2f, 3.0f), BossColor, sprite, 8);
            visual.transform.localPosition = Vector3.zero;
            var flash = visual.gameObject.AddComponent<ColorFlash>();
            flash.target = visual;
            flash.baseColor = BossColor;

            var hitbox = BuildHitbox(root.transform, "Hitbox", Team.Enemy, GameLayers.PlayerMask, sprite);
            var runner = BuildAttackRunner(root.transform, hitbox, body);

            rig.Health = root.AddComponent<Health>();
            rig.Health.maxHealth = tuning.bossMaxHealth;

            rig.Stagger = root.AddComponent<Stagger>();
            rig.Stagger.maxStagger = tuning.bossMaxStagger;
            rig.Stagger.breakThreshold = tuning.bossStaggerBreakThreshold;
            rig.Stagger.regenPerSecond = tuning.bossStaggerRegenPerSecond;
            rig.Stagger.regenDelayFrames = tuning.bossStaggerRegenDelayFrames;
            rig.Stagger.brokenDamageMultiplier = tuning.bossBrokenDamageMultiplier;
            rig.Stagger.brokenFrames = tuning.bossBrokenFrames;

            var hurtbox = root.AddComponent<Hurtbox>();
            hurtbox.team = Team.Enemy;
            hurtbox.owner = root.transform;
            hurtbox.knockbackScale = tuning.bossKnockbackScale;
            hurtbox.health = rig.Health;
            hurtbox.stagger = rig.Stagger;
            hurtbox.body = body;

            rig.Controller = root.AddComponent<BossController>();
            rig.Controller.Body = body;
            rig.Controller.Runner = runner;
            rig.Controller.Health = rig.Health;
            rig.Controller.Stagger = rig.Stagger;
            rig.Controller.Flash = flash;

            GameLayers.SetLayerRecursively(root, GameLayers.Enemy);
            return rig;
        }

        static Hitbox BuildHitbox(Transform parent, string name, Team team, LayerMask mask, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var hitbox = go.AddComponent<Hitbox>();
            hitbox.team = team;
            hitbox.owner = parent;
            hitbox.targetMask = mask;
            hitbox.showVisual = true;

            var visual = CreateSprite(go.transform, "Visual", Vector2.one, new Color(1f, 1f, 1f, 0.3f), sprite, 40);
            visual.transform.localPosition = Vector3.zero;
            visual.enabled = false;
            hitbox.visual = visual;
            return hitbox;
        }

        static AttackRunner BuildAttackRunner(Transform parent, Hitbox hitbox, Rigidbody2D body)
        {
            var go = new GameObject("AttackRunner");
            go.transform.SetParent(parent, false);
            var runner = go.AddComponent<AttackRunner>();
            runner.hitbox = hitbox;
            runner.body = body;
            return runner;
        }

        static void BuildCamera(CombatTuning tuning, Transform target)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(target.position.x, target.position.y + 1.4f, -10f);

            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.07f, 0.10f, 1f);
            camera.nearClipPlane = -50f;
            go.AddComponent<AudioListener>();

            var shake = go.AddComponent<CameraShake>();
            var follow = go.AddComponent<CameraFollow>();
            follow.Target = target;
            follow.Shake = shake;
            follow.ClampX = true;
            follow.MinX = -tuning.arenaHalfWidth + 8f;
            follow.MaxX = tuning.arenaHalfWidth - 8f;
        }

        static void BuildHud(PlayerCombat player, Health playerHealth, Stamina playerStamina,
                             BossController boss, Health bossHealth, Stagger bossStagger)
        {
            var go = new GameObject("HUD");
            var hud = go.AddComponent<HudController>();
            hud.openOnStart = true;
            hud.Player = player;
            hud.PlayerHealth = playerHealth;
            hud.PlayerStamina = playerStamina;
            hud.Boss = boss;
            hud.BossHealth = bossHealth;
            hud.BossStagger = bossStagger;
        }

        static void BuildSystems()
        {
            var go = new GameObject("Systems");
            go.AddComponent<PrototypeBootstrap>();
            go.AddComponent<HitStopManager>();
        }

        static SpriteRenderer CreateSprite(Transform parent, string name, Vector2 size, Color color,
                                           Sprite sprite, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }
    }
}
