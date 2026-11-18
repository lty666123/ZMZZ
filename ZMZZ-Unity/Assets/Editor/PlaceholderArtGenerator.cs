using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 一键生成占位素材（纯代码绘制的小像素图，用来写逻辑，不用等美术）。
/// 菜单：ZMZZ / 生成占位素材
/// 想改颜色或造型，直接改下面的调色板和各个 Draw 方法。
/// </summary>
public static class PlaceholderArtGenerator
{
    const string RootFolder = "Assets/Art";
    const string OutFolder  = "Assets/Art/Placeholders";
    const int PPU = 32;    // 每 32 像素 = 1 个世界单位，所以 32×32 的图正好 1×1 格

    // ---------- 调色板 ----------
    static readonly Color32 Clear       = new Color32(0, 0, 0, 0);
    static readonly Color32 Skin        = new Color32(245, 210, 170, 255);
    static readonly Color32 Blue        = new Color32(70, 130, 220, 255);
    static readonly Color32 BlueDark    = new Color32(40, 80, 150, 255);
    static readonly Color32 Red         = new Color32(210, 70, 70, 255);
    static readonly Color32 Dark        = new Color32(35, 35, 45, 255);
    static readonly Color32 White       = new Color32(245, 245, 250, 255);
    static readonly Color32 Ground      = new Color32(62, 66, 78, 255);
    static readonly Color32 GroundTop   = new Color32(96, 104, 120, 255);
    static readonly Color32 Wood        = new Color32(150, 100, 60, 255);
    static readonly Color32 WoodDark    = new Color32(105, 68, 40, 255);
    static readonly Color32 WoodLight   = new Color32(190, 140, 90, 255);
    static readonly Color32 Gold        = new Color32(235, 190, 60, 255);
    static readonly Color32 GoldLight   = new Color32(255, 235, 140, 255);
    static readonly Color32 Purple      = new Color32(120, 70, 180, 255);
    static readonly Color32 PurpleLight = new Color32(190, 140, 255, 255);
    static readonly Color32 Steel       = new Color32(200, 208, 220, 255);

    [MenuItem("ZMZZ/生成占位素材")]
    public static void Generate()
    {
        EnsureFolder();

        Save(DrawPlayer(),     "ph_player");     // 主角
        Save(DrawEnemy(),      "ph_enemy");      // 敌人
        Save(DrawGroundTile(), "ph_ground");     // 地面砖
        Save(DrawPlatform(),   "ph_platform");   // 平台
        Save(DrawCoin(),       "ph_coin");       // 金币/道具
        Save(DrawPortal(),     "ph_portal");     // 传送门（以后给锚点用）
        Save(DrawChest(),      "ph_chest");      // 宝箱
        Save(DrawSword(),      "ph_sword");      // 武器/攻击特效

        AssetDatabase.Refresh();
        Debug.Log("[占位素材] 已生成到 " + OutFolder + "，共 8 张");
    }

    /// <summary>给 -executeMethod 用的无参入口。</summary>
    public static void GenerateFromBatch()
    {
        Generate();
    }

    // ---------------- 造型 ----------------

    static Canvas DrawPlayer()
    {
        var c = new Canvas(32, 32);
        c.Circle(16, 24, 6, Skin);          // 头
        c.Rect(13, 25, 2, 2, Dark);         // 左眼
        c.Rect(18, 25, 2, 2, Dark);         // 右眼
        c.Rect(10, 9, 12, 11, Blue);        // 身体
        c.Rect(10, 14, 12, 2, BlueDark);    // 腰带
        c.Rect(11, 3, 4, 6, BlueDark);      // 左腿
        c.Rect(17, 3, 4, 6, BlueDark);      // 右腿
        return c;
    }

    static Canvas DrawEnemy()
    {
        var c = new Canvas(32, 32);
        c.Circle(16, 14, 12, Red);          // 身体
        c.Rect(0, 0, 32, 5, Clear);         // 用透明色"擦掉"底部，让它看起来是趴着的
        c.Circle(12, 16, 3, White);         // 左眼
        c.Circle(21, 16, 3, White);
        c.Rect(12, 16, 2, 2, Dark);         // 瞳孔
        c.Rect(20, 16, 2, 2, Dark);
        return c;
    }

    static Canvas DrawGroundTile()
    {
        var c = new Canvas(32, 32);
        c.Rect(0, 0, 32, 32, Ground);
        c.Rect(0, 28, 32, 4, GroundTop);    // 顶面亮一点，方便看清地面边缘
        c.Rect(0, 15, 32, 1, Dark);         // 砖缝
        c.Rect(15, 0, 1, 15, Dark);
        c.Rect(7, 16, 1, 12, Dark);
        c.Rect(24, 16, 1, 12, Dark);
        return c;
    }

    static Canvas DrawPlatform()
    {
        var c = new Canvas(32, 12);
        c.Rect(0, 0, 32, 12, Wood);
        c.Rect(0, 9, 32, 3, WoodLight);     // 顶面
        return c;
    }

    static Canvas DrawCoin()
    {
        var c = new Canvas(16, 16);
        c.Circle(8, 8, 7, Gold);
        c.Circle(8, 9, 3, GoldLight);       // 高光
        return c;
    }

    static Canvas DrawPortal()
    {
        var c = new Canvas(32, 48);
        c.Circle(16, 34, 15, Purple);       // 拱形门框
        c.Rect(1, 0, 30, 34, Purple);
        c.Circle(16, 34, 11, PurpleLight);  // 内部发光
        c.Rect(5, 2, 22, 34, PurpleLight);
        return c;
    }

    static Canvas DrawChest()
    {
        var c = new Canvas(32, 24);
        c.Rect(2, 0, 28, 14, Wood);         // 箱体
        c.Rect(2, 14, 28, 8, WoodDark);     // 盖子
        c.Rect(2, 20, 28, 2, WoodLight);
        c.Rect(14, 9, 4, 7, Gold);          // 锁
        return c;
    }

    static Canvas DrawSword()
    {
        var c = new Canvas(32, 8);
        c.Rect(2, 2, 4, 4, WoodDark);       // 剑柄
        c.Rect(6, 0, 2, 8, Gold);           // 护手
        c.Rect(8, 2, 20, 4, Steel);         // 剑身
        return c;
    }

    // ---------------- 底层：一块可画画的像素画布 ----------------

    class Canvas
    {
        public readonly int W;
        public readonly int H;
        readonly Color32[] pixels;

        public Canvas(int w, int h)
        {
            W = w;
            H = h;
            pixels = new Color32[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Clear;
        }

        /// <summary>实心矩形。注意原点在左下角（和 Unity 贴图坐标一致）。</summary>
        public void Rect(int x, int y, int w, int h, Color32 color)
        {
            for (int j = y; j < y + h; j++)
                for (int i = x; i < x + w; i++)
                    Set(i, j, color);
        }

        /// <summary>实心圆。</summary>
        public void Circle(int cx, int cy, int radius, Color32 color)
        {
            int r2 = radius * radius;
            for (int j = cy - radius; j <= cy + radius; j++)
                for (int i = cx - radius; i <= cx + radius; i++)
                {
                    int dx = i - cx;
                    int dy = j - cy;
                    if (dx * dx + dy * dy <= r2) Set(i, j, color);
                }
        }

        void Set(int x, int y, Color32 color)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;   // 越界自动忽略，画的时候不用管边界
            pixels[y * W + x] = color;
        }

        public Texture2D ToTexture()
        {
            var texture = new Texture2D(W, H, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }
    }

    // ---------------- 存盘 + 配好导入设置 ----------------

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(RootFolder)) AssetDatabase.CreateFolder("Assets", "Art");
        if (!AssetDatabase.IsValidFolder(OutFolder)) AssetDatabase.CreateFolder(RootFolder, "Placeholders");
    }

    static void Save(Canvas canvas, string fileName)
    {
        string path = OutFolder + "/" + fileName + ".png";

        var texture = canvas.ToTexture();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PPU;
            importer.filterMode = FilterMode.Point;      // 像素风必须用 Point，不然会糊
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        Debug.Log("[占位素材] " + fileName + ".png  " + canvas.W + "×" + canvas.H);
    }
}
