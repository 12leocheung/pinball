using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Pinball;

namespace Pinball.EditorTools
{
    /// <summary>Small top-down drawing surface used to author pixel art in code.</summary>
    public class Px
    {
        public readonly int W, H;
        public readonly Color32[] P;

        public Px(int w, int h)
        {
            W = w; H = h; P = new Color32[w * h];
        }

        public bool In(int x, int y) { return x >= 0 && y >= 0 && x < W && y < H; }
        public void Set(int x, int y, Color32 c) { if (In(x, y)) P[y * W + x] = c; }
        public Color32 Get(int x, int y) { return In(x, y) ? P[y * W + x] : new Color32(0, 0, 0, 0); }
        public void Clear(int x, int y) { if (In(x, y)) P[y * W + x] = new Color32(0, 0, 0, 0); }

        public void Fill(Color32 c) { for (int i = 0; i < P.Length; i++) P[i] = c; }

        public void Rect(int x, int y, int w, int h, Color32 c)
        {
            for (int j = 0; j < h; j++) for (int i = 0; i < w; i++) Set(x + i, y + j, c);
        }

        public void ClearRect(int x, int y, int w, int h)
        {
            for (int j = 0; j < h; j++) for (int i = 0; i < w; i++) Clear(x + i, y + j);
        }

        public void BlendRect(int x, int y, int w, int h, Color32 c, float a)
        {
            for (int j = 0; j < h; j++) for (int i = 0; i < w; i++) Blend(x + i, y + j, c, a);
        }

        public void Blend(int x, int y, Color32 c, float a)
        {
            if (!In(x, y)) return;
            var d = P[y * W + x];
            float da = d.a / 255f;
            float na = a + da * (1 - a);
            if (na <= 0.0001f) { P[y * W + x] = new Color32(0, 0, 0, 0); return; }
            P[y * W + x] = new Color32(
                (byte)((c.r * a + d.r * da * (1 - a)) / na),
                (byte)((c.g * a + d.g * da * (1 - a)) / na),
                (byte)((c.b * a + d.b * da * (1 - a)) / na),
                (byte)Mathf.Clamp(na * 255f, 0, 255));
        }

        public void Frame(int x, int y, int w, int h, Color32 c)
        {
            HLine(x, y, w, c); HLine(x, y + h - 1, w, c);
            VLine(x, y, h, c); VLine(x + w - 1, y, h, c);
        }

        public void HLine(int x, int y, int w, Color32 c) { for (int i = 0; i < w; i++) Set(x + i, y, c); }
        public void VLine(int x, int y, int h, Color32 c) { for (int j = 0; j < h; j++) Set(x, y + j, c); }

        public void Disc(float cx, float cy, float r, Color32 c)
        {
            int x0 = Mathf.FloorToInt(cx - r), x1 = Mathf.CeilToInt(cx + r);
            int y0 = Mathf.FloorToInt(cy - r), y1 = Mathf.CeilToInt(cy + r);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    if (dx * dx + dy * dy <= r * r) Set(x, y, c);
                }
        }

        public void ClearDisc(float cx, float cy, float r)
        {
            int x0 = Mathf.FloorToInt(cx - r), x1 = Mathf.CeilToInt(cx + r);
            int y0 = Mathf.FloorToInt(cy - r), y1 = Mathf.CeilToInt(cy + r);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    if (dx * dx + dy * dy <= r * r) Clear(x, y);
                }
        }

        public void Ring(float cx, float cy, float r, float thickness, Color32 c)
        {
            int x0 = Mathf.FloorToInt(cx - r - 1), x1 = Mathf.CeilToInt(cx + r + 1);
            int y0 = Mathf.FloorToInt(cy - r - 1), y1 = Mathf.CeilToInt(cy + r + 1);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d <= r && d >= r - thickness) Set(x, y, c);
                }
        }

        public void Ellipse(float cx, float cy, float rx, float ry, Color32 c)
        {
            int x0 = Mathf.FloorToInt(cx - rx), x1 = Mathf.CeilToInt(cx + rx);
            int y0 = Mathf.FloorToInt(cy - ry), y1 = Mathf.CeilToInt(cy + ry);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f - cx) / Mathf.Max(0.001f, rx);
                    float dy = (y + 0.5f - cy) / Mathf.Max(0.001f, ry);
                    if (dx * dx + dy * dy <= 1f) Set(x, y, c);
                }
        }

        public void Line(int x0, int y0, int x1, int y1, Color32 c, int t = 1)
        {
            int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            while (true)
            {
                if (t <= 1) Set(x0, y0, c);
                else Disc(x0 + 0.5f, y0 + 0.5f, t * 0.5f, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        /// <summary>Triangle filled via scanline.</summary>
        public void Tri(Vector2 a, Vector2 b, Vector2 c2, Color32 c)
        {
            int minX = Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c2.x)));
            int maxX = Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c2.x)));
            int minY = Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c2.y)));
            int maxY = Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c2.y)));
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    if (InsideTri(new Vector2(x + 0.5f, y + 0.5f), a, b, c2)) Set(x, y, c);
                }
        }

        static bool InsideTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
            float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
            float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
            bool neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool pos = (d1 > 0) || (d2 > 0) || (d3 > 0);
            return !(neg && pos);
        }

        public void Arc(float cx, float cy, float r, float a0, float a1, Color32 c, int t = 1)
        {
            int steps = Mathf.Max(10, Mathf.CeilToInt(Mathf.Abs(a1 - a0) * r * 2f));
            Vector2 prev = new Vector2(cx + Mathf.Cos(a0) * r, cy + Mathf.Sin(a0) * r);
            for (int i = 1; i <= steps; i++)
            {
                float a = Mathf.Lerp(a0, a1, i / (float)steps);
                Vector2 cur = new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r);
                Line(Mathf.RoundToInt(prev.x), Mathf.RoundToInt(prev.y), Mathf.RoundToInt(cur.x), Mathf.RoundToInt(cur.y), c, t);
                prev = cur;
            }
        }

        public void Glow(float cx, float cy, float r, Color32 c, float strength)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - r)), x1 = Mathf.Min(W - 1, Mathf.CeilToInt(cx + r));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - r)), y1 = Mathf.Min(H - 1, Mathf.CeilToInt(cy + r));
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x - cx) / r, dy = (y - cy) / r;
                    float d = dx * dx + dy * dy;
                    if (d > 1f) continue;
                    float a = (1f - d) * (1f - d) * strength;
                    Blend(x, y, c, a);
                }
        }

        static readonly int[] Bayer = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

        public void Dither(int x, int y, int w, int h, Color32 c, float amount)
        {
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                {
                    float th = (Bayer[((j & 3) * 4) + (i & 3)] + 0.5f) / 16f;
                    if (th < amount) Blend(x + i, y + j, c, 0.85f);
                }
        }

        /// <summary>Adds a 1px outline around every opaque pixel (expands the canvas by 1 on each side).</summary>
        public Px WithOutline(Color32 ink)
        {
            var o = new Px(W + 2, H + 2);
            for (int y = -1; y <= H; y++)
                for (int x = -1; x <= W; x++)
                {
                    if (Get(x, y).a > 40) { o.Set(x + 1, y + 1, ink); continue; }
                    bool near = false;
                    for (int j = -1; j <= 1 && !near; j++)
                        for (int i = -1; i <= 1; i++)
                            if (Get(x + i, y + j).a > 40) { near = true; break; }
                    if (near) o.Set(x + 1, y + 1, ink);
                }
            var r = new Px(W + 2, H + 2);
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) r.Set(x + 1, y + 1, Get(x, y));
            for (int y = 0; y < r.H; y++) for (int x = 0; x < r.W; x++) { if (r.Get(x, y).a > 40) o.Set(x, y, r.Get(x, y)); }
            return o;
        }

        public Px Scaled(int f)
        {
            var o = new Px(W * f, H * f);
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
                {
                    var c = Get(x, y);
                    if (c.a == 0) continue;
                    o.Rect(x * f, y * f, f, f, c);
                }
            return o;
        }

        public void Blit(Px src, int ox, int oy)
        {
            for (int y = 0; y < src.H; y++)
                for (int x = 0; x < src.W; x++)
                {
                    var c = src.Get(x, y);
                    if (c.a == 0) continue;
                    Blend(ox + x, oy + y, c, c.a / 255f);
                }
        }

        public void BlitOpaque(Px src, int ox, int oy)
        {
            for (int y = 0; y < src.H; y++)
                for (int x = 0; x < src.W; x++)
                {
                    var c = src.Get(x, y);
                    if (c.a > 0) Set(ox + x, oy + y, c);
                }
        }

        public void DrawMask(string[] rows, int ox, int oy, Color32 c)
        {
            for (int y = 0; y < rows.Length; y++)
            {
                string row = rows[y];
                for (int x = 0; x < row.Length; x++)
                    if (row[x] == '#') Set(ox + x, oy + y, c);
            }
        }

        public void DrawMaskCentered(string[] rows, float cx, float cy, Color32 c)
        {
            int w = 0;
            for (int i = 0; i < rows.Length; i++) w = Mathf.Max(w, rows[i].Length);
            int ox = Mathf.RoundToInt(cx - w * 0.5f);
            int oy = Mathf.RoundToInt(cy - rows.Length * 0.5f);
            DrawMask(rows, ox, oy, c);
        }

        public void Text(string s, int x, int y, Color32 c, int scale = 1)
        {
            int cx = x;
            foreach (char ch in s)
            {
                string g = FontData.Get(ch);
                string[] rows = g.Split('|');
                for (int r = 0; r < rows.Length; r++)
                    for (int i = 0; i < rows[r].Length; i++)
                        if (rows[r][i] == '#')
                            Rect(cx + i * scale, y + r * scale, scale, scale, c);
                cx += (FontData.GlyphW + 1) * scale;
            }
        }

        public Texture2D ToTexture()
        {
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            t.wrapMode = TextureWrapMode.Clamp;
            var flipped = new Color32[P.Length];
            for (int y = 0; y < H; y++) Array.Copy(P, y * W, flipped, (H - 1 - y) * W, W);
            t.SetPixels32(flipped);
            t.Apply();
            return t;
        }

        public byte[] ToPng() { var t = ToTexture(); byte[] b = t.EncodeToPNG(); UnityEngine.Object.DestroyImmediate(t); return b; }
    }

    public static class PixelArtGenerator
    {
        public const string ArtDir = "Assets/Art/Pixel";
        const string ResDir = "Assets/Resources";

        // ---- palette -------------------------------------------------------
        public static readonly Color32 Ink = Hex("#161125");
        public static readonly Color32 Deep = Hex("#241c3d");
        public static readonly Color32 Deep2 = Hex("#2e2550");
        public static readonly Color32 Plum = Hex("#43356a");
        public static readonly Color32 Mauve = Hex("#6f5a96");
        public static readonly Color32 Cream = Hex("#f8eedb");
        public static readonly Color32 Sand = Hex("#e6d0ac");
        public static readonly Color32 Pink = Hex("#f5a6ca");
        public static readonly Color32 Rose = Hex("#e46d93");
        public static readonly Color32 Red = Hex("#c94a68");
        public static readonly Color32 Gold = Hex("#f4cb6e");
        public static readonly Color32 Amber = Hex("#e2934d");
        public static readonly Color32 Mint = Hex("#a0e6c6");
        public static readonly Color32 Teal = Hex("#57b9a8");
        public static readonly Color32 Sky = Hex("#92d6f7");
        public static readonly Color32 Blue = Hex("#5f92de");
        public static readonly Color32 Lilac = Hex("#c7a9f3");
        public static readonly Color32 Violet = Hex("#8d6dd6");
        public static readonly Color32 White = Hex("#fffdf6");

        public static Color32 Hex(string s)
        {
            Color c;
            if (!ColorUtility.TryParseHtmlString(s, out c)) return new Color32(255, 0, 255, 255);
            return c;
        }

        public static Color32 Mul(Color32 c, float f)
        {
            return new Color32(
                (byte)Mathf.Clamp(c.r * f, 0, 255),
                (byte)Mathf.Clamp(c.g * f, 0, 255),
                (byte)Mathf.Clamp(c.b * f, 0, 255),
                c.a);
        }

        public static Color32 A(Color32 c, byte a) { return new Color32(c.r, c.g, c.b, a); }

        // ---- generation ----------------------------------------------------

        [MenuItem("Tools/Pinball/1 - Generate Pixel Art", false, 1)]
        public static void Generate()
        {
            var images = new List<KeyValuePair<string, Px>>();

            Action<string, Px> add = (key, img) => images.Add(new KeyValuePair<string, Px>(key, img));

            add("playfield", Playfield());
            add("rail_h", RailH());
            add("rail_v", RailV());
            add("rail_cap", RailCap());
            add("flipper", Flipper(false));
            add("flipper_r", Flipper(true));
            add("ball", Ball());
            add("bumper_a", Bumper(Pink, Rose, Lilac));
            add("bumper_b", Bumper(Mint, Teal, Sky));
            add("peg", Peg());
            add("lane", Lane());
            add("plunger", Plunger());
            add("drain", Drain());
            add("ring", RingSprite());
            add("spark", Spark());
            add("trail", TrailDot());

            add("item_spring", IconSpring());
            add("item_coinpress", IconCoinPress());
            add("item_chime", IconChime());
            add("item_candle", IconCandle());
            add("item_ramp", IconRamp());
            add("item_prism", IconPrism());
            add("item_lodestone", IconLodestone());
            add("item_net", IconNet());
            add("item_rail", IconRail());
            add("item_gear", IconGear());
            add("item_locket", IconLocket());
            add("item_mirror", IconMirror());
            add("item_trampoline", IconTrampoline());
            add("item_idol", IconIdol());
            add("item_toad", IconToad());
            add("item_torch", IconTorch());
            add("item_cannon", IconCannon());
            add("item_honey", IconHoneyJar());
            add("item_starspire", IconStarspire());

            add("wild_moon", Wildcard(MoonEmblem(), Lilac));
            add("wild_sun", Wildcard(SunEmblem(), Gold));
            add("wild_comet", Wildcard(CometEmblem(), Sky));
            add("wild_key", Wildcard(KeyEmblem(), Sand));
            add("wild_eye", Wildcard(EyeEmblem(), Mint));
            add("wild_crown", Wildcard(CrownEmblem(), Gold));
            add("wild_feather", Wildcard(FeatherEmblem(), Pink));
            add("wild_mask", Wildcard(MaskEmblem(), Violet));
            add("wild_flame", Wildcard(FlameEmblem(), Amber));
            add("wild_bell", Wildcard(BellEmblem(), Teal));
            add("wild_acorn", Wildcard(AcornEmblem(), Sand));
            add("wild_shell", Wildcard(ShellEmblem(), Pink));
            add("wild_wind", Wildcard(WindEmblem(), Sky));
            add("wild_vine", Wildcard(VineEmblem(), Mint));
            add("wild_sand", Wildcard(SandEmblem(), Amber));

            // anything in the catalog without bespoke art gets a generated icon, so a new
            // item can never ship with a blank sprite
            var taken = new HashSet<string>();
            foreach (var kv in images) taken.Add(kv.Key);
            foreach (var item in Catalog.Items)
            {
                if (!taken.Add(item.sprite)) continue;
                add(item.sprite, AutoIcon(item));
            }
            foreach (var wc in Catalog.Wildcards)
            {
                if (!taken.Add(wc.sprite)) continue;
                add(wc.sprite, Wildcard(AutoEmblem(wc), AutoAccent(wc)));
            }

            add("boss_collector", BossCollector());
            add("boss_eclipse", BossEclipse());
            add("boss_warden", BossWarden());
            add("boss_undertow", BossUndertow());
            add("boss_hollow", BossHollow());
            add("boss_gardener", BossGardener());

            add("ui_solid", Solid());
            add("ui_grain", Grain());
            add("grid_cell", GridCell());
            add("card_slot", CardSlot());
            add("coin", Coin());
            add("gem", Gem());
            add("heart", Heart());
            add("star", Star());
            add("rar_common", RarityGem(Cream));
            add("rar_uncommon", RarityGem(Mint));
            add("rar_rare", RarityGem(Sky));
            add("rar_legendary", RarityGem(Gold));
            add("cursor", Cursor());
            add("arrow_l", Arrow(false));
            add("arrow_r", Arrow(true));
            add("font", FontAtlas());
            add("flipper_glow", RingSprite());

            Directory.CreateDirectory(ArtDir);
            Directory.CreateDirectory(ResDir);

            var entries = new List<SpriteEntry>();
            foreach (var kv in images)
            {
                SavePng(kv.Key, kv.Value);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var kv in images)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + "/" + kv.Key + ".png");
                if (sprite == null)
                {
                    Debug.LogError("[PixelArt] Failed to import sprite: " + kv.Key);
                    continue;
                }
                entries.Add(new SpriteEntry { key = kv.Key, sprite = sprite });
            }

            string libPath = ResDir + "/PixelArtLibrary.asset";
            AssetDatabase.DeleteAsset(libPath);
            var lib = ScriptableObject.CreateInstance<PixelSpriteLibrary>();
            lib.entries = entries.ToArray();
            AssetDatabase.CreateAsset(lib, libPath);
            AssetDatabase.SaveAssets();

            ContactSheet(images);
            Debug.Log("[PixelArt] Generated " + entries.Count + " sprites into " + ArtDir);
        }

        static void SavePng(string key, Px img)
        {
            string path = ArtDir + "/" + key + ".png";
            File.WriteAllBytes(path, img.ToPng());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.spritePixelsPerUnit = 8;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.maxTextureSize = 2048;
            ti.npotScale = TextureImporterNPOTScale.None;
            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteGenerateFallbackPhysicsShape = false;
            ti.SetTextureSettings(settings);
            ti.SaveAndReimport();
        }

        static void ContactSheet(List<KeyValuePair<string, Px>> images)
        {
            const int cols = 8;
            const int cell = 210;
            const int scale = 4;
            int rows = (images.Count + cols - 1) / cols;
            var sheet = new Px(cols * cell, rows * cell);
            sheet.Fill(Hex("#141020"));
            for (int i = 0; i < images.Count; i++)
            {
                var img = images[i].Value;
                int cx = (i % cols) * cell;
                int cy = (i / cols) * cell;
                sheet.BlendRect(cx + 2, cy + 2, cell - 4, cell - 4, Deep2, 0.6f);
                var up = img.Scaled(scale);
                int ox = cx + (cell - up.W) / 2;
                int oy = cy + 8 + Mathf.Max(0, (cell - 42 - up.H) / 2);
                sheet.Blit(up, ox, oy);
                sheet.Text(images[i].Key.ToUpperInvariant(), cx + 6, cy + cell - 18, Sand, 1);
            }
            string path = "Assets/Art/contact_sheet.png";
            Directory.CreateDirectory("Assets/Art");
            File.WriteAllBytes(path, sheet.ToPng());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        // ---- pinball art ---------------------------------------------------

        static Px Playfield()
        {
            var p = new Px(112, 176);
            p.Fill(Deep);
            for (int ty = 0; ty < 22; ty++)
                for (int tx = 0; tx < 14; tx++)
                    if (((tx + ty) & 1) == 0) p.Rect(tx * 8, ty * 8, 8, 8, Deep2);
            for (int y = 0; y < 176; y += 8) for (int x = 0; x < 112; x += 8) p.Set(x, y, Plum);

            p.Glow(56, 128, 66, Plum, 0.45f);
            p.Glow(56, 10, 54, Violet, 0.20f);
            p.Glow(8, 92, 26, Teal, 0.10f);
            p.Glow(104, 92, 26, Pink, 0.10f);

            // decorative arcs
            p.Arc(56, 132, 44, Mathf.PI * 1.08f, Mathf.PI * 1.92f, Mul(Plum, 1.25f));
            p.Arc(56, 132, 48, Mathf.PI * 1.10f, Mathf.PI * 1.90f, Mul(Deep2, 1.3f));
            p.Arc(56, 118, 30, Mathf.PI * 1.15f, Mathf.PI * 1.85f, Mul(Plum, 1.1f));

            // flipper well
            for (int y = 148; y < 176; y++)
            {
                float t = (y - 148) / 28f;
                p.BlendRect(0, y, 112, 1, Ink, 0.20f + 0.45f * t);
            }
            p.HLine(0, 147, 112, Mul(Plum, 1.3f));

            // shooter lane chevrons
            for (int i = 0; i < 6; i++)
            {
                int y = 156 - i * 9;
                p.Line(99, y, 102, y - 3, Mul(Mauve, 1.1f));
                p.Line(103, y - 3, 106, y, Mul(Mauve, 1.1f));
            }

            // side lane dots
            for (int y = 20; y < 140; y += 8) { p.Set(6, y, Mul(Mauve, 0.9f)); p.Set(105, y, Mul(Mauve, 0.9f)); }

            // stars
            int[,] stars = { { 18, 30 }, { 40, 52 }, { 78, 38 }, { 26, 74 }, { 90, 66 }, { 14, 108 }, { 96, 118 }, { 58, 62 }, { 34, 132 }, { 76, 104 } };
            for (int i = 0; i < stars.GetLength(0); i++)
            {
                int x = stars[i, 0], y = stars[i, 1];
                p.Set(x, y, Sand); p.Set(x - 1, y, A(Sand, 120)); p.Set(x + 1, y, A(Sand, 120));
                p.Set(x, y - 1, A(Sand, 120)); p.Set(x, y + 1, A(Sand, 120));
            }

            // frame
            p.Frame(0, 0, 112, 176, Ink);
            p.Frame(1, 1, 110, 174, Mul(Plum, 0.9f));
            p.Frame(2, 2, 108, 172, Deep2);

            // center guide
            p.VLine(56, 156, 14, Mul(Plum, 1.2f));
            p.Set(56, 154, Mauve);
            return p;
        }

        static Px RailH()
        {
            var p = new Px(8, 4);
            p.HLine(0, 0, 8, Ink);
            p.HLine(0, 1, 8, Cream);
            p.HLine(0, 2, 8, Sand);
            p.HLine(0, 3, 8, Ink);
            return p;
        }

        static Px RailV()
        {
            var p = new Px(4, 8);
            p.VLine(0, 0, 8, Ink);
            p.VLine(1, 0, 8, Cream);
            p.VLine(2, 0, 8, Sand);
            p.VLine(3, 0, 8, Ink);
            return p;
        }

        static Px RailCap()
        {
            var p = new Px(8, 8);
            p.Disc(4, 4, 3.6f, Sand);
            p.Disc(4, 4, 2.4f, Cream);
            return p.WithOutline(Ink);
        }

        static Px Flipper(bool mirror)
        {
            var p = new Px(24, 10);
            // tapered paddle: thicker at the pivot (left), rounded tip (right)
            for (int x = 2; x < 23; x++)
            {
                float t = (x - 2) / 21f;
                float half = Mathf.Lerp(3.4f, 1.6f, t);
                float cy = 5f;
                p.Disc(x + 0.5f, cy, 0.5f, Cream);
                p.VLine(x, Mathf.RoundToInt(cy - half), Mathf.Max(1, Mathf.RoundToInt(half * 2)), Cream);
            }
            p.Disc(3, 5, 2.6f, Cream);
            // shading
            for (int x = 2; x < 23; x++)
            {
                float t = (x - 2) / 21f;
                float half = Mathf.Lerp(3.4f, 1.6f, t);
                int y0 = Mathf.RoundToInt(5f + half * 0.35f);
                int y1 = Mathf.RoundToInt(5f + half);
                p.VLine(x, y0, Mathf.Max(1, y1 - y0), Sand);
            }
            p.Disc(3, 5, 2.4f, Sand);
            p.Disc(2.6f, 3.6f, 0.9f, White);
            var o = p.WithOutline(Ink);
            if (!mirror) return o;
            var m = new Px(o.W, o.H);
            for (int y = 0; y < o.H; y++) for (int x = 0; x < o.W; x++) m.Set(o.W - 1 - x, y, o.Get(x, y));
            return m;
        }

        static Px Ball()
        {
            var p = new Px(14, 14);
            p.Disc(7, 7, 6.4f, Sand);
            p.Disc(7, 7, 5.7f, Cream);
            p.Disc(5.4f, 4.6f, 1.9f, White);
            p.Set(4, 4, White);
            p.Arc(7, 7, 5.4f, Mathf.PI * 0.35f, Mathf.PI * 0.95f, Mul(Sand, 0.92f));
            p.Arc(7, 7, 3.2f, Mathf.PI * 0.05f, Mathf.PI * 0.5f, A(White, 150));
            return p.WithOutline(Ink);
        }

        static Px Bumper(Color32 body, Color32 dark, Color32 accent)
        {
            var p = new Px(22, 22);
            p.Disc(11, 11, 10.2f, dark);
            p.Ring(11, 11, 9.6f, 3.6f, body);
            p.Ring(11, 11, 6.6f, 1.2f, Mul(dark, 1.2f));
            p.Disc(11, 11, 5.4f, Mul(Deep, 1.15f));
            p.Arc(11, 11, 8.9f, Mathf.PI * 1.05f, Mathf.PI * 1.55f, A(White, 210), 1);
            p.Arc(11, 11, 8.9f, Mathf.PI * 1.6f, Mathf.PI * 1.85f, A(White, 120), 1);
            // center gem
            p.Disc(11, 11, 3.4f, accent);
            p.Disc(11, 11, 2.1f, Cream);
            p.Set(10, 9, White); p.Set(11, 9, White); p.Set(10, 10, White);
            p.Arc(11, 11, 5.2f, Mathf.PI * 0.7f, Mathf.PI * 1.4f, A(White, 90));
            return p.WithOutline(Ink);
        }

        static Px Peg()
        {
            var p = new Px(6, 6);
            p.Disc(3, 3, 2.8f, Sand);
            p.Disc(3, 3, 1.9f, Cream);
            p.Set(2, 2, White);
            return p.WithOutline(Ink);
        }

        static Px Lane()
        {
            var p = new Px(8, 64);
            p.Fill(A(Deep2, 210));
            for (int y = 4; y < 64; y += 8)
            {
                p.Line(2, y + 3, 4, y, Mul(Plum, 1.4f));
                p.Line(5, y, 7, y + 3, Mul(Plum, 1.4f));
            }
            p.VLine(0, 0, 64, A(Plum, 180));
            p.VLine(7, 0, 64, A(Plum, 180));
            return p;
        }

        static Px Plunger()
        {
            var p = new Px(16, 20);
            p.Rect(3, 16, 10, 3, Mauve);
            p.HLine(3, 16, 10, Lilac);
            // spring
            for (int i = 0; i < 4; i++)
            {
                int y = 13 - i * 3;
                p.Line(5, y, 11, y - 2, Cream);
                p.Line(11, y - 2, 5, y - 3, Cream);
            }
            p.Disc(8, 5, 4.2f, Rose);
            p.Disc(8, 4, 3.2f, Pink);
            p.Set(6, 3, White); p.Set(7, 3, White);
            return p.WithOutline(Ink);
        }

        static Px Drain()
        {
            var p = new Px(64, 10);
            p.Rect(0, 3, 64, 7, Ink);
            p.HLine(0, 3, 64, Rose);
            for (int i = 0; i < 64; i += 6) { p.Set(i, 4, Red); p.Set(i + 1, 5, Mul(Red, 0.8f)); }
            p.HLine(0, 2, 64, A(Pink, 90));
            return p;
        }

        static Px RingSprite()
        {
            var p = new Px(30, 30);
            p.Ring(15, 15, 14, 1.6f, White);
            p.Ring(15, 15, 11, 0.9f, A(White, 110));
            return p;
        }

        static Px Spark()
        {
            var p = new Px(8, 8);
            p.Set(3, 3, White); p.Set(4, 3, White); p.Set(3, 4, White); p.Set(4, 4, White);
            p.Set(3, 2, A(White, 160)); p.Set(4, 5, A(White, 160)); p.Set(2, 3, A(White, 160)); p.Set(5, 4, A(White, 160));
            return p;
        }

        static Px TrailDot()
        {
            var p = new Px(6, 6);
            p.Disc(3, 3, 2.4f, A(Cream, 90));
            p.Disc(3, 3, 1.2f, A(White, 170));
            return p;
        }

        // ---- shop item icons ----------------------------------------------

        static Px IconBase()
        {
            var p = new Px(22, 22);
            p.Ellipse(11, 18.5f, 8, 2, A(Ink, 90));
            return p;
        }

        static Px IconSpring()
        {
            var p = IconBase();
            p.Rect(4, 3, 14, 4, Gold); p.Frame(4, 3, 14, 4, Ink); p.HLine(5, 4, 12, Cream);
            p.Line(6, 8, 15, 10, Cream, 2);
            p.Line(15, 10, 6, 13, Cream, 2);
            p.Line(6, 13, 15, 15, Cream, 2);
            p.Rect(2, 15, 18, 4, Mauve); p.Frame(2, 15, 18, 4, Ink); p.HLine(3, 16, 16, Lilac);
            p.Set(3, 3, White);
            return p.WithOutline(Ink);
        }

        // ---- content pack II ------------------------------------------------

        static Px IconToad()
        {
            var p = IconBase();
            p.Ellipse(11, 10.5f, 7.2f, 5.6f, Mul(Mint, 0.85f));
            p.Ellipse(11, 11.5f, 6.2f, 4.4f, Mul(Mint, 1.05f));
            p.Disc(11, 7, 3.6f, Cream);
            p.HLine(8, 7, 14, Mauve);
            p.Disc(7.5f, 14, 2.4f, Mul(Mint, 1.1f)); p.Disc(14.5f, 14, 2.4f, Mul(Mint, 1.1f));
            p.Disc(7.5f, 14.5f, 1.2f, Ink); p.Disc(14.5f, 14.5f, 1.2f, Ink);
            p.HLine(3, 6, 5, Mul(Mint, 0.75f)); p.HLine(16, 6, 19, Mul(Mint, 0.75f));
            p.Set(6, 13, White); p.Set(16, 13, White);
            return p.WithOutline(Ink);
        }

        static Px IconTorch()
        {
            var p = IconBase();
            p.Line(3, 15, 19, 15, Ink, 1);
            p.Rect(5, 9, 5, 6, Mul(Sand, 0.95f)); p.Frame(5, 9, 5, 6, Ink);
            p.Rect(12, 9, 5, 6, Mul(Sand, 0.95f)); p.Frame(12, 9, 5, 6, Ink);
            p.VLine(7, 10, 10, Mauve); p.VLine(14, 10, 10, Mauve);
            p.Disc(7.5f, 6.5f, 2.4f, Amber); p.Disc(14.5f, 6.5f, 2.4f, Amber);
            p.Disc(7.5f, 7.5f, 1.4f, Gold); p.Disc(14.5f, 7.5f, 1.4f, Gold);
            p.Set(7, 4, White); p.Set(14, 4, White);
            p.HLine(9, 17, 13, Mauve);
            return p.WithOutline(Ink);
        }

        static Px IconCannon()
        {
            var p = IconBase();
            p.Rect(3, 7, 15, 7, Mauve); p.Frame(3, 7, 15, 7, Ink);
            p.HLine(4, 8, 17, Lilac);
            p.HLine(4, 10, 17, A(Pink, 140));
            p.HLine(4, 12, 17, A(Pink, 90));
            p.Rect(1, 6, 4, 9, Mul(Mauve, 1.2f)); p.Frame(1, 6, 4, 9, Ink);
            p.Disc(15, 4.5f, 3.2f, Sand); p.Ring(15, 4.5f, 3.2f, 1, Ink); p.Disc(15, 4.5f, 1.2f, Ink);
            p.Line(6, 14, 5, 17, Ink, 1); p.Line(16, 14, 18, 17, Ink, 1);
            p.Set(5, 8, White);
            return p.WithOutline(Ink);
        }

        static Px IconHoneyJar()
        {
            var p = IconBase();
            p.Rect(5, 6, 12, 11, Amber); p.Frame(5, 6, 12, 11, Ink);
            p.HLine(6, 7, 16, Mul(Gold, 1.1f));
            p.Rect(7, 3, 8, 3, Gold); p.Frame(7, 3, 8, 3, Ink);
            p.Rect(8, 9, 6, 5, Cream); p.HLine(9, 11, 13, Ink);
            p.Set(7, 14, Amber); p.Set(7, 15, Mul(Amber, 1.15f));
            p.Set(14, 10, White);
            return p.WithOutline(Ink);
        }

        static Px IconStarspire()
        {
            var p = IconBase();
            p.Rect(9, 6, 4, 10, Mul(Violet, 1.2f)); p.Frame(9, 6, 4, 10, Ink);
            p.Rect(10, 6, 2, 10, Lilac);
            p.Rect(5, 16, 12, 3, Sand); p.Frame(5, 16, 12, 3, Ink);
            p.Disc(11, 4, 2.6f, Gold); p.Ring(11, 4, 2.6f, 1, Amber);
            p.Set(11, 3, White);
            p.Arc(11, 11, 7.5f, 3.4f, 5.9f, A(Teal, 160));
            return p.WithOutline(Ink);
        }

        static Px IconCoinPress()
        {
            var p = IconBase();
            p.Rect(2, 12, 18, 6, Teal); p.Frame(2, 12, 18, 6, Ink); p.HLine(3, 13, 16, Mint);
            p.Rect(7, 5, 8, 6, Mul(Teal, 0.8f)); p.Frame(7, 5, 8, 6, Ink);
            p.Disc(11, 7.5f, 4.4f, Gold); p.Ring(11, 7.5f, 4.4f, 1, Amber); p.Ring(11, 7.5f, 2.4f, 1, Amber);
            p.Line(15, 17, 20, 20, Sand, 2);
            p.Set(9, 5, White);
            return p.WithOutline(Ink);
        }

        static Px IconChime()
        {
            var p = IconBase();
            string[] bell =
            {
                "..####..",
                ".######.",
                "########",
                "########",
                "########",
                ".######.",
                "..####..",
                "...#...."
            };
            p.DrawMaskCentered(bell, 11, 9, Mint);
            p.Rect(9, 14, 4, 2, Sand);
            p.HLine(3, 17, 16, Mauve);
            p.Set(6, 7, White);
            return p.WithOutline(Ink);
        }

        static Px IconCandle()
        {
            var p = IconBase();
            p.Rect(7, 9, 8, 9, Cream); p.Frame(7, 9, 8, 9, Ink);
            p.VLine(8, 10, 2, A(White, 200));
            p.Rect(6, 17, 10, 2, Sand); p.Frame(6, 17, 10, 2, Ink);
            // flame
            p.Disc(11, 5.5f, 3.2f, Amber);
            p.Disc(11, 6.5f, 2.2f, Gold);
            p.Disc(11, 7.5f, 1.1f, White);
            p.Set(11, 2, Pink);
            // drips
            p.Set(6, 11, Cream); p.Set(6, 12, Cream); p.Set(16, 10, Cream); p.Set(16, 11, Cream);
            return p.WithOutline(Ink);
        }

        static Px IconRamp()
        {
            var p = IconBase();
            for (int i = 0; i < 5; i++)
            {
                int off = i * 4;
                p.Line(2 + off, 18, 6 + off, 4, i % 2 == 0 ? Mul(Violet, 1.15f) : Plum, 4);
                p.Line(2 + off, 18, 6 + off, 4, A(Sky, 30));
            }
            p.Line(2, 18, 22, 18, Sand, 2);
            p.Line(6, 4, 22, 4, Cream, 1);
            return p.WithOutline(Ink);
        }

        static Px IconPrism()
        {
            var p = IconBase();
            p.Tri(new Vector2(2, 19), new Vector2(11, 3), new Vector2(20, 19), Sky);
            p.Tri(new Vector2(11, 3), new Vector2(20, 19), new Vector2(13, 19), Mul(Sky, 0.75f));
            p.Tri(new Vector2(4, 19), new Vector2(11, 3), new Vector2(9, 19), Lilac);
            p.Line(4, 18, 19, 18, Mint);
            p.Line(9, 8, 14, 16, A(White, 180));
            p.Set(11, 6, White);
            return p.WithOutline(Ink);
        }

        static Px IconLodestone()
        {
            var p = IconBase();
            p.Ring(11, 11, 8.4f, 5.2f, Rose);
            p.ClearRect(4, 14, 14, 8);
            p.Rect(4, 12, 5, 6, Rose); p.Rect(13, 12, 5, 6, Rose);
            p.Frame(4, 12, 5, 6, Ink); p.Frame(13, 12, 5, 6, Ink);
            p.Rect(4, 16, 5, 2, Cream); p.Rect(13, 16, 5, 2, Cream);
            p.Arc(11, 11, 7.2f, Mathf.PI * 1.05f, Mathf.PI * 1.5f, Pink);
            p.Set(7, 8, White);
            return p.WithOutline(Ink);
        }

        static Px IconNet()
        {
            var p = IconBase();
            p.Arc(11, 9, 8, 0, Mathf.PI, Cream, 2);
            p.Line(3, 9, 19, 9, Sand, 1);
            for (int i = 0; i < 6; i++) { p.Line(4 + i * 3, 9, 7 + i * 3, 16, A(Cream, 150)); }
            for (int i = 0; i < 3; i++) p.Arc(11, 9, 8 - i * 2, 0.15f, Mathf.PI - 0.15f, A(Cream, 120));
            p.Disc(11, 8, 2.4f, Sky);
            p.Set(10, 7, White);
            return p.WithOutline(Ink);
        }

        static Px IconRail()
        {
            var p = IconBase();
            p.Line(3, 18, 19, 3, Blue, 4);
            p.Line(4, 17, 20, 2, Mul(Blue, 0.75f), 1);
            p.Line(3, 15, 16, 3, A(White, 120), 1);
            for (int i = 0; i < 5; i++) { int x = 5 + i * 3; int y = 17 - i * 3; p.Set(x, y, Cream); }
            p.Rect(1, 16, 5, 5, Mauve); p.Rect(17, 1, 5, 5, Mauve);
            return p.WithOutline(Ink);
        }

        static Px IconGear()
        {
            var p = IconBase();
            p.Disc(11, 10, 6.4f, Gold);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                p.Disc(11 + Mathf.Cos(a) * 7.2f, 10 + Mathf.Sin(a) * 7.2f, 2.2f, Gold);
            }
            p.ClearDisc(11, 10, 2.6f);
            p.Ring(11, 10, 5.2f, 1.4f, Amber);
            p.Ring(11, 10, 2.9f, 1, Mul(Amber, 0.85f));
            p.Set(8, 6, White); p.Set(9, 6, White);
            return p.WithOutline(Ink);
        }

        static Px IconLocket()
        {
            var p = IconBase();
            p.Disc(11, 12, 7.4f, Gold);
            p.Ring(11, 12, 7.4f, 1.6f, Amber);
            p.Disc(11, 12, 5.2f, Rose);
            string[] st =
            {
                "..#..",
                ".###.",
                "#####",
                ".###.",
                "..#.."
            };
            p.DrawMaskCentered(st, 11, 12, Cream);
            p.Ring(8, 3.5f, 2.6f, 1.2f, Sand);
            p.Set(7, 8, White);
            return p.WithOutline(Ink);
        }

        static Px IconMirror()
        {
            var p = IconBase();
            p.Tri(new Vector2(11, 2), new Vector2(19, 12), new Vector2(11, 19), Sky);
            p.Tri(new Vector2(11, 2), new Vector2(11, 19), new Vector2(3, 12), Mul(Sky, 0.8f));
            p.Line(11, 3, 17, 12, A(White, 200));
            p.Line(8, 8, 11, 13, A(White, 120));
            p.Line(3, 12, 19, 12, A(White, 60));
            p.Set(11, 5, White);
            return p.WithOutline(Ink);
        }

        static Px IconTrampoline()
        {
            var p = IconBase();
            p.Ellipse(11, 8, 9, 4, Rose);
            p.Ellipse(11, 7.4f, 9, 4, Pink);
            p.Ellipse(11, 7.4f, 5.6f, 2.2f, Mul(Pink, 0.85f));
            for (int i = 0; i < 4; i++)
            {
                int x = 4 + i * 4;
                p.VLine(x, 11, 6, Sand);
                p.Set(x, 17, Cream);
            }
            p.Line(2, 6, 20, 6, A(White, 100));
            return p.WithOutline(Ink);
        }

        static Px IconIdol()
        {
            var p = IconBase();
            p.Rect(6, 17, 12, 3, Mul(Gold, 0.8f));
            p.Rect(4, 14, 16, 3, Gold); p.Frame(4, 14, 16, 3, Ink);
            p.Rect(7, 5, 10, 10, Gold); p.Frame(7, 5, 10, 10, Ink);
            p.VLine(8, 6, 8, Cream);
            p.Rect(7, 2, 10, 3, Gold);
            p.Set(8, 1, Gold); p.Set(11, 0, Gold); p.Set(14, 1, Gold);
            p.Rect(9, 8, 2, 2, Pink); p.Rect(13, 8, 2, 2, Pink);
            p.Rect(10, 12, 4, 1, Mul(Amber, 0.8f));
            p.Set(9, 9, White); p.Set(13, 9, White);
            return p.WithOutline(Ink);
        }

        // ---- wildcards -----------------------------------------------------

        static Px Emblem(Px e)
        {
            var o = e.WithOutline(Ink);
            var padded = new Px(18, 18);
            padded.Blit(o, (18 - o.W) / 2, (18 - o.H) / 2);
            return padded;
        }

        static Px MoonEmblem()
        {
            var p = new Px(16, 16);
            p.Disc(8, 8, 7.2f, Lilac);
            p.ClearDisc(11.8f, 6.2f, 6.2f);
            p.Arc(8, 8, 6.6f, 1.05f, 2.6f, Mul(Lilac, 1.35f));
            p.Arc(8, 8, 5.2f, 1.2f, 2.4f, Mul(Lilac, 1.15f));
            p.Set(4, 5, White); p.Set(4, 6, White); p.Set(5, 5, Cream);
            var s = new Px(16, 16);
            s.Set(3, 3, Cream); s.Set(2, 3, A(Cream, 140)); s.Set(3, 2, A(Cream, 140)); s.Set(4, 3, A(Cream, 140)); s.Set(3, 4, A(Cream, 140));
            p.Blit(s, 0, 0);
            return Emblem(p);
        }

        // ---- content pack II emblems ----------------------------------------

        static Px AcornEmblem()
        {
            var p = new Px(16, 16);
            p.Disc(8, 6.5f, 4.2f, Sand);
            p.Disc(8, 10.5f, 3.6f, Mul(Amber, 0.9f));
            p.Line(5, 6, 11, 9, Ink, 1);
            p.Set(8, 2, Mint);
            return Emblem(p);
        }

        static Px ShellEmblem()
        {
            var p = new Px(16, 16);
            p.Disc(8, 9, 6, Pink);
            p.ClearDisc(8, 14.5f, 6.6f);
            p.Arc(8, 9, 4.4f, 0.6f, 2.5f, Ink);
            p.Arc(8, 9, 2.4f, 0.8f, 2.3f, Ink);
            p.Set(4, 7, Cream);
            return Emblem(p);
        }

        static Px WindEmblem()
        {
            var p = new Px(16, 16);
            p.Arc(6, 9, 6.5f, -0.4f, 1.6f, Sky);
            p.Line(6, 9, 14, 9, Sky, 1);
            p.Arc(10, 11, 4.5f, -0.6f, 1.9f, A(Sky, 170));
            p.Line(10, 11, 14, 11, A(Sky, 170), 1);
            p.Set(12, 7, Cream); p.Set(10, 5, Cream);
            return Emblem(p);
        }

        static Px VineEmblem()
        {
            var p = new Px(16, 16);
            p.Arc(8, 8, 6, 3.3f, 5.6f, Mul(Mint, 1.1f));
            p.Arc(8, 8, 6, 0.0f, 2.4f, Mul(Mint, 0.95f));
            p.Arc(8, 8, 6, 2.2f, 3.3f, Mul(Mint, 1.25f));
            p.Disc(12.5f, 5, 1.6f, Violet); p.Disc(4, 10, 1.6f, Violet); p.Disc(9, 13, 1.6f, Violet);
            return Emblem(p);
        }

        static Px SandEmblem()
        {
            var p = new Px(16, 16);
            string[] glass =
            {
                "######",
                "..##..",
                "..##..",
                ".####.",
                "..##..",
                "..##..",
                "######"
            };
            p.DrawMaskCentered(glass, 8, 8, Gold);
            p.Set(8, 13, White);
            return Emblem(p);
        }

        static Px SunEmblem()
        {
            var p = new Px(16, 16);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                p.Line(Mathf.RoundToInt(8 + Mathf.Cos(a) * 5), Mathf.RoundToInt(8 + Mathf.Sin(a) * 5),
                       Mathf.RoundToInt(8 + Mathf.Cos(a) * 7.6f), Mathf.RoundToInt(8 + Mathf.Sin(a) * 7.6f), Gold);
            }
            p.Disc(8, 8, 5.2f, Gold);
            p.Disc(8, 8, 4, Cream);
            p.Disc(8, 8, 2.4f, Amber);
            p.Set(6, 6, White);
            return Emblem(p);
        }

        static Px CometEmblem()
        {
            var p = new Px(16, 16);
            p.Disc(11, 5, 4.4f, Sky);
            p.Disc(11, 5, 2.8f, Cream);
            p.Line(8, 8, 2, 14, Mul(Sky, 0.9f), 2);
            p.Line(9, 7, 4, 14, Lilac, 1);
            p.Line(7, 9, 1, 13, Mul(Sky, 0.6f), 1);
            p.Set(10, 4, White); p.Set(11, 4, White);
            return Emblem(p);
        }

        static Px KeyEmblem()
        {
            var p = new Px(16, 16);
            p.Ring(8, 4.5f, 3.6f, 1.8f, Sand);
            p.VLine(8, 8, 6, Sand);
            p.VLine(7, 8, 7, Sand);
            p.HLine(8, 12, 3, Sand);
            p.HLine(8, 14, 2, Sand);
            p.Set(7, 3, Cream);
            return Emblem(p);
        }

        static Px EyeEmblem()
        {
            var p = new Px(16, 16);
            p.Ellipse(8, 8, 7.4f, 5, Mint);
            p.Ellipse(8, 8, 7.4f, 5, Mint);
            p.ClearDisc(8, 8, 2.4f);
            p.Disc(8, 8, 3.2f, Teal);
            p.Disc(8, 8, 1.8f, Deep);
            p.Set(9, 7, White);
            p.Arc(8, 8, 6.2f, Mathf.PI * 1.1f, Mathf.PI * 1.9f, Mul(Mint, 1.3f));
            return Emblem(p);
        }

        static Px CrownEmblem()
        {
            var p = new Px(16, 16);
            p.Rect(2, 9, 12, 4, Gold);
            p.Tri(new Vector2(2, 9), new Vector2(5, 2), new Vector2(8, 9), Gold);
            p.Tri(new Vector2(5, 9), new Vector2(8, 1), new Vector2(11, 9), Gold);
            p.Tri(new Vector2(8, 9), new Vector2(11, 3), new Vector2(14, 9), Gold);
            p.HLine(2, 12, 12, Amber);
            p.Set(5, 2, Cream); p.Set(8, 1, Cream); p.Set(11, 3, Cream);
            p.Set(7, 10, Rose); p.Set(8, 10, Rose);
            return Emblem(p);
        }

        static Px FeatherEmblem()
        {
            var p = new Px(16, 16);
            for (int i = 0; i < 12; i++)
            {
                int len = Mathf.RoundToInt(Mathf.Sin(i / 12f * Mathf.PI) * 5.6f);
                if (len <= 0) continue;
                p.HLine(3 + i / 2, 2 + i, len, Pink);
            }
            p.Line(6, 2, 3, 14, Cream);
            p.Line(5, 3, 2, 13, Mul(Pink, 0.7f));
            p.Set(6, 2, White);
            return Emblem(p);
        }

        static Px MaskEmblem()
        {
            var p = new Px(16, 16);
            p.Ellipse(8, 8, 5.6f, 6.6f, Violet);
            p.Ellipse(8, 7, 5.6f, 5.4f, Lilac);
            p.ClearDisc(5.4f, 6.4f, 1.7f);
            p.ClearDisc(10.6f, 6.4f, 1.7f);
            p.Arc(8, 10.5f, 2.6f, 0.2f, Mathf.PI - 0.2f, Violet);
            p.Set(5, 4, White); p.Set(6, 3, A(White, 130));
            return Emblem(p);
        }

        static Px FlameEmblem()
        {
            var p = new Px(16, 16);
            p.Tri(new Vector2(8, 1), new Vector2(13.5f, 11), new Vector2(2.5f, 11), Amber);
            p.Disc(8, 11, 5.4f, Amber);
            p.Disc(8, 11, 3.8f, Gold);
            p.Tri(new Vector2(8, 6), new Vector2(10.6f, 11.5f), new Vector2(5.4f, 11.5f), Gold);
            p.Disc(8, 11.5f, 2, Cream);
            p.Set(8, 5, Cream);
            return Emblem(p);
        }

        static Px BellEmblem()
        {
            var p = new Px(16, 16);
            string[] bell =
            {
                "...####...",
                "..######..",
                ".########.",
                ".########.",
                "##########",
                "##########",
                "##########",
                ".########.",
                "..######..",
                "....##...."
            };
            p.DrawMaskCentered(bell, 8, 7, Teal);
            p.HLine(2, 12, 12, Mint);
            p.Disc(8, 14, 1.6f, Sand);
            p.Set(5, 5, White); p.Set(5, 6, White);
            return Emblem(p);
        }

        static Px Wildcard(Px emblem, Color32 accent)
        {
            var p = new Px(24, 32);
            p.Rect(0, 0, 24, 32, Deep);
            p.Frame(0, 0, 24, 32, Ink);
            p.Frame(1, 1, 22, 30, Mul(accent, 0.55f));
            p.Rect(2, 2, 20, 28, Deep2);
            // top glow
            p.Glow(12, 6, 12, accent, 0.35f);
            // corner pips
            p.Set(2, 2, Mul(accent, 1.2f)); p.Set(21, 2, Mul(accent, 1.2f));
            p.Set(2, 29, Mul(accent, 0.8f)); p.Set(21, 29, Mul(accent, 0.8f));
            // emblem
            p.Blit(emblem, 3, 5);
            // bottom band
            p.HLine(3, 25, 18, Mul(accent, 0.5f));
            p.Rect(4, 26, 16, 3, A(Ink, 80));
            for (int i = 0; i < 3; i++) p.Set(8 + i * 3, 27, Mul(accent, 1.1f));
            return p;
        }

        // ---- bosses --------------------------------------------------------

        static Px BossBase(Color32 glow)
        {
            var p = new Px(30, 30);
            p.Glow(15, 15, 15, glow, 0.45f);
            p.Ring(15, 15, 14, 1.4f, Mul(glow, 0.9f));
            return p;
        }

        static Px BossGardener()
        {
            var p = BossBase(Mint);
            p.Ellipse(13, 14, 7.5f, 5.8f, Mul(Teal, 0.95f));
            p.Ellipse(13, 14, 6, 4.6f, Cream);
            p.Disc(13, 14, 3.4f, Ink);
            p.Disc(13, 14, 2.3f, Deep);
            p.Line(19, 17, 25, 11, Mul(Teal, 1.05f), 2);
            p.Line(25, 11, 25, 15, Ink, 2);
            p.Arc(8, 16, 4.2f, 1.6f, 4.2f, Ink);
            p.Line(14, 22, 13, 25, Mul(Mint, 1.15f), 2);
            p.Disc(10.5f, 25.5f, 1.8f, Mint); p.Disc(16.5f, 25.5f, 1.8f, Mint);
            p.Disc(13.5f, 26.5f, 1.4f, Gold);
            return p;
        }

        static Px BossCollector()
        {
            var p = BossBase(Rose);
            p.Ellipse(15, 15, 11, 7.5f, Rose);
            p.Ellipse(15, 15, 9, 6, Cream);
            p.Disc(15, 15, 4.4f, Ink);
            p.Disc(15, 15, 3.2f, Deep);
            p.Set(14, 13, White);
            p.Arc(15, 15, 12, 0, Mathf.PI * 2, A(Pink, 120));
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                p.Line(15 + Mathf.RoundToInt(Mathf.Cos(a) * 6), 15 + Mathf.RoundToInt(Mathf.Sin(a) * 4),
                       15 + Mathf.RoundToInt(Mathf.Cos(a) * 12), 15 + Mathf.RoundToInt(Mathf.Sin(a) * 9), A(Rose, 140));
            }
            return p.WithOutline(Ink);
        }

        static Px BossEclipse()
        {
            var p = BossBase(Plum);
            p.Disc(15, 15, 11, Mul(Plum, 1.1f));
            p.Disc(15, 15, 9, Gold);
            p.Disc(15, 15, 7.6f, Deep);
            p.ClearDisc(19, 13, 8);
            p.Disc(15, 15, 11, Mul(Plum, 1.1f));
            p.Disc(15, 15, 9, Gold);
            p.ClearDisc(19.5f, 13, 8.4f);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6f;
                p.Set(15 + Mathf.RoundToInt(Mathf.Cos(a) * 13), 15 + Mathf.RoundToInt(Mathf.Sin(a) * 13), Gold);
            }
            return p.WithOutline(Ink);
        }

        static Px BossWarden()
        {
            var p = BossBase(Sky);
            p.Rect(5, 4, 20, 22, Mul(Sky, 0.55f));
            p.Frame(5, 4, 20, 22, Sky);
            for (int i = 0; i < 4; i++) p.VLine(8 + i * 4, 5, 20, A(Sky, 170));
            p.HLine(6, 11, 18, A(Sky, 170));
            p.HLine(6, 19, 18, A(Sky, 170));
            p.Rect(10, 17, 4, 4, Pink); p.Rect(16, 7, 4, 4, Pink);
            return p.WithOutline(Ink);
        }

        static Px BossUndertow()
        {
            var p = BossBase(Teal);
            for (int i = 0; i < 4; i++)
            {
                int y = 8 + i * 5;
                p.Arc(15, y, 9, 0.1f, Mathf.PI - 0.1f, i % 2 == 0 ? Teal : Mint, 1);
            }
            p.Disc(15, 15, 3.4f, Mint);
            p.Disc(15, 15, 1.8f, Cream);
            return p.WithOutline(Ink);
        }

        static Px BossHollow()
        {
            var p = BossBase(Violet);
            p.Ring(15, 15, 11, 5, Violet);
            p.Ring(15, 15, 11, 1.4f, Lilac);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                int x = 15 + Mathf.RoundToInt(Mathf.Cos(a) * 8);
                int y = 15 + Mathf.RoundToInt(Mathf.Sin(a) * 8);
                p.Disc(x, y, 1.6f, Pink);
            }
            p.Disc(15, 15, 2, Mul(Pink, 0.8f));
            return p.WithOutline(Ink);
        }

        // ---- UI ------------------------------------------------------------

        static Px Solid()
        {
            var p = new Px(8, 8);
            p.Fill(White);
            return p;
        }

        static Px Grain()
        {
            var p = new Px(8, 8);
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    if ((((x * 7) + (y * 13)) % 11) == 0) p.Set(x, y, A(White, 26));
            return p;
        }

        static Px GridCell()
        {
            var p = new Px(16, 16);
            for (int i = 0; i < 16; i += 1)
            {
                if ((i % 3) != 0) continue;
                p.Set(i, 0, A(Cream, 120));
                p.Set(i, 15, A(Cream, 120));
                p.Set(0, i, A(Cream, 120));
                p.Set(15, i, A(Cream, 120));
            }
            p.Set(0, 0, A(Cream, 40)); p.Set(15, 0, A(Cream, 40));
            p.Set(0, 15, A(Cream, 40)); p.Set(15, 15, A(Cream, 40));
            return p;
        }

        static Px CardSlot()
        {
            var p = new Px(16, 16);
            for (int i = 0; i < 16; i += 2)
            {
                p.Set(i, 0, A(Cream, 130)); p.Set(i + 1, 0, A(Cream, 60));
                p.Set(i, 15, A(Cream, 130)); p.Set(i + 1, 15, A(Cream, 60));
                p.Set(0, i, A(Cream, 130)); p.Set(0, i + 1, A(Cream, 60));
                p.Set(15, i, A(Cream, 130)); p.Set(15, i + 1, A(Cream, 60));
            }
            return p;
        }

        static Px Coin()
        {
            var p = new Px(8, 8);
            p.Disc(4, 4, 3.4f, Gold);
            p.Ring(4, 4, 3.4f, 1, Amber);
            p.VLine(4, 2, 4, Cream);
            p.Set(2, 2, White);
            return p;
        }

        static Px Gem()
        {
            var p = new Px(8, 8);
            p.Tri(new Vector2(4, 0), new Vector2(7, 3), new Vector2(4, 7), Sky);
            p.Tri(new Vector2(4, 0), new Vector2(1, 3), new Vector2(4, 7), Lilac);
            p.Set(4, 2, White);
            return p;
        }

        static Px Heart()
        {
            var p = new Px(8, 8);
            p.Set(1, 1, Pink); p.Set(2, 1, Pink); p.Set(5, 1, Pink); p.Set(6, 1, Pink);
            p.Rect(1, 2, 6, 2, Pink);
            p.Rect(2, 4, 4, 1, Pink);
            p.Rect(3, 5, 2, 1, Pink);
            p.Set(4, 6, Pink);
            p.Set(2, 2, White);
            return p;
        }

        static Px Star()
        {
            var p = new Px(8, 8);
            p.Set(4, 0, Gold);
            p.Set(3, 2, Gold); p.Set(4, 2, Gold); p.Set(5, 2, Gold);
            p.Rect(1, 3, 7, 1, Gold);
            p.Set(2, 4, Gold); p.Set(3, 4, Gold); p.Set(5, 4, Gold); p.Set(6, 4, Gold);
            p.Set(1, 5, Gold); p.Set(6, 5, Gold);
            p.Set(1, 6, Amber); p.Set(7, 6, Amber);
            p.Set(2, 1, A(White, 160));
            return p;
        }

        static Px RarityGem(Color32 c)
        {
            var p = new Px(8, 8);
            p.Tri(new Vector2(4, 0.5f), new Vector2(7.5f, 4), new Vector2(4, 7.5f), c);
            p.Tri(new Vector2(4, 0.5f), new Vector2(0.5f, 4), new Vector2(4, 7.5f), Mul(c, 0.75f));
            p.Set(4, 2, White);
            return p.WithOutline(Ink);
        }

        static Px Cursor()
        {
            // classic arrow pointer: tip top-left, vertical left edge, 45 degree right edge,
            // head bottom cut back into a notch, 2px tail running down-right. Px space is
            // top-down and ToTexture flips for Unity, so this is drawn in reading order.
            string[] mask =
            {
                "#.........",
                "##........",
                "###.......",
                "####......",
                "#####.....",
                "######....",
                "#######...",
                "########..",
                "#########.",
                "##########",
                "######....",
                "####.##...",
                "###...##..",
                "##....##..",
                "#.....##..",
                ".......#.."
            };

            var p = new Px(10, mask.Length);
            p.DrawMask(mask, 0, 0, Cream);
            return p.WithOutline(Ink);
        }

        static Px Arrow(bool right)
        {
            var p = new Px(8, 8);
            string[] m = { "...#....", "..##....", ".###....", "####....", ".###....", "..##....", "...#....", "........" };
            if (right)
            {
                string[] r = { "....#...", "....##..", "....###.", "....####", "....###.", "....##..", "....#...", "........" };
                m = r;
            }
            p.DrawMask(m, 0, 0, Cream);
            return p.WithOutline(Ink);
        }

        // ---- generated art (content pack III) -------------------------------

        static int Hash(string s)
        {
            int h = 216613626;
            for (int i = 0; i < s.Length; i++) h = unchecked((h ^ s[i]) * 16777619);
            return h & 0x7fffffff;
        }

        static Color32 Pick(string id, int salt, params Color32[] pool)
        {
            return pool[Hash(id + "#" + salt) % pool.Length];
        }

        /// <summary>Deterministic icon so every catalog entry has readable art even
        /// when nobody hand-drew it: the item kind picks the silhouette, the id picks
        /// the colour, and the rarity gem sits in the corner.</summary>
        static Px AutoIcon(BoardItemDef d)
        {
            int h = Hash(d.id);
            Color32 a, b;
            switch (d.kind)
            {
                case ItemKind.Bumper: a = Pick(d.id, 1, Rose, Pink, Amber, Gold, Red); break;
                case ItemKind.Coin: a = Pick(d.id, 2, Gold, Amber, Sand); break;
                case ItemKind.Mult: a = Pick(d.id, 3, Violet, Lilac, Sky); break;
                case ItemKind.Trampoline: a = Pick(d.id, 4, Teal, Mint, Sky); break;
                case ItemKind.Magnet: a = Pick(d.id, 5, Sky, Lilac, Teal); break;
                case ItemKind.Hold: a = Pick(d.id, 6, Pink, Rose, Lilac); break;
                case ItemKind.Lantern: a = Pick(d.id, 7, Amber, Gold, Mint); break;
                default: a = Pick(d.id, 8, Sand, Mauve, Cream); break;
            }
            b = Pick(d.id, 9, Cream, White, Gold, Mul(a, 1.35f));

            var p = IconBase();
            switch (d.kind)
            {
                case ItemKind.Coin:
                    for (int i = 0; i < 3; i++)
                    {
                        float y = 15.5f - i * 2.6f;
                        p.Ellipse(11, y, 8f, 2.8f, Mul(a, 0.75f + 0.1f * i));
                    }
                    p.Ellipse(11, 10.3f, 8f, 2.8f, a);
                    p.Ellipse(11, 9.9f, 6f, 2f, b);
                    p.Set(9, 9, White);
                    break;

                case ItemKind.Mult:
                    for (int i = 0; i < 8; i++)
                    {
                        float ang = i * Mathf.PI / 4f;
                        int x0 = Mathf.RoundToInt(11 + Mathf.Cos(ang) * 5f);
                        int y0 = Mathf.RoundToInt(11 + Mathf.Sin(ang) * 5f);
                        int x1 = Mathf.RoundToInt(11 + Mathf.Cos(ang) * 8.4f);
                        int y1 = Mathf.RoundToInt(11 + Mathf.Sin(ang) * 8.4f);
                        p.Line(x0, y0, x1, y1, i % 2 == 0 ? a : Mul(a, 0.8f));
                    }
                    p.Disc(11, 11, 5.2f, a);
                    p.Disc(11, 11, 3.2f, b);
                    p.Set(9, 9, White);
                    break;

                case ItemKind.Trampoline:
                    p.Rect(2, 14, 18, 4, Mul(a, 0.85f));
                    p.Frame(2, 14, 18, 4, Ink);
                    p.HLine(3, 15, 16, b);
                    for (int i = 0; i < 3; i++)
                    {
                        float y = 12.5f - i * 2.2f;
                        p.Line(5, Mathf.RoundToInt(y), 17, Mathf.RoundToInt(y - 1.2f), Mul(a, 1.1f), 2);
                        p.Line(17, Mathf.RoundToInt(y - 1.2f), 5, Mathf.RoundToInt(y - 2.2f), Mul(a, 1.1f), 2);
                    }
                    p.Disc(11, 4.2f, 2.8f, b);
                    p.Ring(11, 4.2f, 2.8f, 1, Ink);
                    p.Set(10, 3, White);
                    break;

                case ItemKind.Magnet:
                    for (int i = 0; i < 5; i++)
                    {
                        float r = 7f - i * 0.9f;
                        p.Arc(11, 9f, r, Mathf.PI * 0.06f, Mathf.PI * 0.94f, i == 0 || i == 4 ? Ink : a, 2);
                    }
                    p.Rect(3, 8, 4, 7, Mul(a, 0.9f)); p.Frame(3, 8, 4, 7, Ink);
                    p.Rect(15, 8, 4, 7, Mul(a, 0.9f)); p.Frame(15, 8, 4, 7, Ink);
                    p.HLine(4, 13, 2, b); p.HLine(16, 13, 2, b);
                    p.Set(5, 7, White);
                    break;

                case ItemKind.Hold:
                    p.Tri(new Vector2(3, 7), new Vector2(19, 7), new Vector2(15, 17), Mul(a, 0.8f));
                    p.Tri(new Vector2(3, 7), new Vector2(15, 17), new Vector2(7, 17), a);
                    p.HLine(3, 7, 16, Cream);
                    p.Line(6, 10, 16, 10, A(Mul(a, 0.55f), 200), 1);
                    p.Line(6, 13, 14, 13, A(Mul(a, 0.55f), 200), 1);
                    p.Line(9, 8, 12, 15, A(Mul(a, 0.55f), 200), 1);
                    p.Disc(11, 12, 2f, b);
                    p.Set(10, 11, White);
                    break;

                case ItemKind.Wall:
                    p.Rect(3, 5, 16, 12, a);
                    p.Frame(3, 5, 16, 12, Ink);
                    p.HLine(4, 6, 14, Mul(a, 1.3f));
                    p.VLine(4, 6, 10, Mul(a, 1.2f));
                    p.HLine(4, 15, 14, Mul(a, 0.65f));
                    p.VLine(17, 6, 10, Mul(a, 0.65f));
                    for (int i = 0; i < 4; i++)
                        p.Set(5 + (i % 2) * 11, 8 + (i / 2) * 6, b);
                    break;

                case ItemKind.Lantern:
                    p.VLine(11, 1, 3, Ink);
                    p.Rect(4, 4, 14, 3, Mul(a, 0.7f)); p.Frame(4, 4, 14, 3, Ink);
                    p.Rect(5, 7, 12, 8, Mul(a, 0.85f)); p.Frame(5, 7, 12, 8, Ink);
                    p.Rect(7, 8, 8, 6, b);
                    p.Disc(11, 11, 2.4f, Amber);
                    p.Disc(11, 11, 1.2f, Cream);
                    p.Rect(3, 15, 16, 2, Sand); p.Frame(3, 15, 16, 2, Ink);
                    p.Set(8, 9, White);
                    break;

                default: // Bumper
                    p.Disc(11, 11, 7.4f, Mul(a, 0.8f));
                    p.Disc(11, 11, 6f, a);
                    p.Disc(11, 8.6f, 2.8f, b);
                    p.Ring(11, 11, 7.4f, 1.2f, Ink);
                    for (int i = 0; i < 6; i++)
                    {
                        float ang = i * Mathf.PI / 3f + h % 7 * 0.1f;
                        p.Set(Mathf.RoundToInt(11 + Mathf.Cos(ang) * 4.4f),
                              Mathf.RoundToInt(11 + Mathf.Sin(ang) * 4.4f), Mul(b, 0.9f));
                    }
                    p.Set(8, 6, White);
                    break;
            }

            p.Disc(17.6f, 16.6f, 2.4f, Pal.RarityColor(d.rarity));
            p.Ring(17.6f, 16.6f, 2.4f, 1, Ink);
            return p.WithOutline(Ink);
        }

        /// <summary>Card accent for a generated wildcard emblem.</summary>
        static Color32 AutoAccent(WildcardDef w)
        {
            return Pick(w.id, 11, Pal.RarityColor(w.rarity), Teal, Gold, Lilac, Rose, Mint);
        }

        /// <summary>Rune emblem for a wildcard with no hand-drawn one.</summary>
        static Px AutoEmblem(WildcardDef w)
        {
            var p = new Px(16, 16);
            int h = Hash(w.id);
            Color32 c = Pal.RarityColor(w.rarity);
            switch (h % 5)
            {
                case 0: // diamond
                    p.Tri(new Vector2(8, 1), new Vector2(14.5f, 8), new Vector2(8, 15), c);
                    p.Tri(new Vector2(8, 1), new Vector2(1.5f, 8), new Vector2(8, 15), Mul(c, 0.7f));
                    p.Set(7, 5, White);
                    break;
                case 1: // ring
                    p.Ring(8, 8, 6.6f, 2.2f, c);
                    p.Ring(8, 8, 3.2f, 1f, Mul(c, 1.3f));
                    p.Set(5, 4, White);
                    break;
                case 2: // crown
                    p.Rect(2, 9, 12, 4, c);
                    p.Tri(new Vector2(2, 9), new Vector2(4.5f, 3), new Vector2(7, 9), c);
                    p.Tri(new Vector2(5, 9), new Vector2(8, 1.5f), new Vector2(11, 9), c);
                    p.Tri(new Vector2(9, 9), new Vector2(11.5f, 3), new Vector2(14, 9), c);
                    p.HLine(2, 12, 12, Mul(c, 0.65f));
                    break;
                case 3: // cross-key
                    p.Rect(6, 1, 4, 14, c);
                    p.Rect(1, 5, 14, 4, c);
                    p.Rect(7, 6, 2, 12, Mul(c, 1.25f));
                    p.Set(7, 6, White);
                    break;
                default: // crescent
                    p.Disc(8, 8, 7f, c);
                    p.ClearDisc(11.5f, 5.5f, 6f);
                    p.Arc(8, 8, 6.4f, 1.05f, 2.6f, Mul(c, 1.35f));
                    p.Set(6, 6, White);
                    break;
            }
            int pips = 1 + h % 3;
            for (int i = 0; i < pips; i++)
                p.Set(4 + i * 4, 14, Mul(c, 1.2f));
            return Emblem(p);
        }

        /// <summary>One-shot: regenerate the art once after the next domain reload, then
        /// leave a marker so it never runs again.</summary>
        [InitializeOnLoadMethod]
        static void AutoRegenerateOnce()
        {
            string marker = System.IO.Path.Combine("Temp", "artgen-pending");
            if (!System.IO.File.Exists(marker)) return;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                try { Generate(); }
                finally { System.IO.File.Delete(marker); }
            };
        }

        static Px FontAtlas()
        {
            var p = new Px(FontData.Cols * FontData.Cell, 4 * FontData.Cell);
            for (int i = 0; i < FontData.Order.Length; i++)
            {
                char ch = FontData.Order[i];
                string[] rows = FontData.Get(ch).Split('|');
                int cx = (i % FontData.Cols) * FontData.Cell;
                int cy = (i / FontData.Cols) * FontData.Cell;
                for (int y = 0; y < rows.Length; y++)
                    for (int x = 0; x < rows[y].Length; x++)
                        if (rows[y][x] == '#') p.Set(cx + 1 + x, cy + 1 + y, White);
            }
            return p;
        }
    }
}
