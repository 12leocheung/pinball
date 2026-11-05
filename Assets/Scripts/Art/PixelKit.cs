using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Pinball
{
    public static class Pal
    {
        public static readonly Color Ink = Hex("#161125");
        public static readonly Color Deep = Hex("#241c3d");
        public static readonly Color Deep2 = Hex("#2e2550");
        public static readonly Color Plum = Hex("#43356a");
        public static readonly Color Mauve = Hex("#6f5a96");
        public static readonly Color Cream = Hex("#f8eedb");
        public static readonly Color Sand = Hex("#e6d0ac");
        public static readonly Color Pink = Hex("#f5a6ca");
        public static readonly Color Rose = Hex("#e46d93");
        public static readonly Color Red = Hex("#c94a68");
        public static readonly Color Gold = Hex("#f4cb6e");
        public static readonly Color Amber = Hex("#e2934d");
        public static readonly Color Mint = Hex("#a0e6c6");
        public static readonly Color Teal = Hex("#57b9a8");
        public static readonly Color Sky = Hex("#92d6f7");
        public static readonly Color Blue = Hex("#5f92de");
        public static readonly Color Lilac = Hex("#c7a9f3");
        public static readonly Color Violet = Hex("#8d6dd6");
        public static readonly Color White = Hex("#fffdf6");

        public static Color Hex(string s)
        {
            Color c;
            return ColorUtility.TryParseHtmlString(s, out c) ? c : Color.magenta;
        }

        public static Color RarityColor(Rarity r)
        {
            switch (r)
            {
                case Rarity.Uncommon: return Mint;
                case Rarity.Rare: return Sky;
                case Rarity.Legendary: return Gold;
                default: return Cream;
            }
        }

        public static string RarityName(Rarity r)
        {
            switch (r)
            {
                case Rarity.Uncommon: return "UNCOMMON";
                case Rarity.Rare: return "RARE";
                case Rarity.Legendary: return "LEGENDARY";
                default: return "COMMON";
            }
        }

        public static Color Alpha(Color c, float a) { c.a = a; return c; }
    }

    /// <summary>Loads every generated sprite by name from the PixelArtLibrary asset.</summary>
    public static class SpriteBank
    {
        static Dictionary<string, Sprite> _map;
        static Material _mat;
        static Sprite _blank;

        public static void Ensure()
        {
            if (_map != null) return;
            _map = new Dictionary<string, Sprite>();
            var lib = Resources.Load<PixelSpriteLibrary>("PixelArtLibrary");
            if (lib == null)
            {
                Debug.LogError("PixelArtLibrary missing - run Tools/Pinball/1 - Generate Pixel Art");
                return;
            }
            foreach (var e in lib.entries)
                if (e != null && e.sprite != null && !string.IsNullOrEmpty(e.key))
                    _map[e.key] = e.sprite;
        }

        public static Sprite Get(string key)
        {
            Ensure();
            Sprite s;
            if (_map.TryGetValue(key, out s) && s != null) return s;
            Debug.LogWarning("Missing sprite: " + key);
            return BlankSprite();
        }

        public static bool Has(string key)
        {
            Ensure();
            return _map.ContainsKey(key);
        }

        static Sprite BlankSprite()
        {
            if (_blank == null)
            {
                var t = new Texture2D(8, 8, TextureFormat.RGBA32, false);
                var px = new Color32[64];
                for (int i = 0; i < 64; i++) px[i] = new Color32(255, 255, 255, 255);
                t.SetPixels32(px);
                t.Apply();
                _blank = Sprite.Create(t, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
            }
            return _blank;
        }

        public static Material Mat
        {
            get
            {
                if (_mat != null) return _mat;
                var sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (sh == null) sh = Shader.Find("Sprites/Default");
                _mat = new Material(sh);
                return _mat;
            }
        }

        public static SpriteRenderer Make(string key, Transform parent, int order, Vector2 pos, Color? tint = null, string name = null)
        {
            var go = new GameObject(string.IsNullOrEmpty(name) ? key : name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Get(key);
            sr.sharedMaterial = Mat;
            sr.sortingOrder = order;
            if (tint.HasValue) sr.color = tint.Value;
            return sr;
        }

        /// <summary>Flat tinted quad helper. Sprite is 1x1 world units at PPU 8.</summary>
        public static SpriteRenderer Quad(Transform parent, Vector2 pos, float w, float h, Color color, int order)
        {
            var sr = Make("ui_solid", parent, order, pos, color);
            sr.transform.localScale = new Vector3(w, h, 1f);
            return sr;
        }
    }

    public enum TextAlign { Left, Center, Right }

    public static class PixelFont
    {
        static Sprite[] _glyphs;
        static Texture2D _tex;
        static int[] _index;

        static void Ensure()
        {
            if (_glyphs != null) return;
            _tex = SpriteBank.Get("font").texture;
            _glyphs = new Sprite[64];
            _index = new int[128];
            for (int i = 0; i < 128; i++) _index[i] = -1;
            for (int i = 0; i < FontData.Order.Length; i++)
            {
                char c = FontData.Order[i];
                if (c < 128) _index[c] = i;
                int col = i % FontData.Cols;
                int row = i / FontData.Cols;
                var rect = new Rect(col * FontData.Cell, _tex.height - (row + 1) * FontData.Cell, FontData.Cell, FontData.Cell);
                _glyphs[i] = Sprite.Create(_tex, rect, new Vector2(0.5f, 0.5f), 8f);
                _glyphs[i].name = "glyph_" + c;
            }
        }

        public static int IndexOf(char c)
        {
            Ensure();
            if (c < 128 && _index[c] >= 0) return _index[c];
            if (char.IsLower(c))
            {
                char u = char.ToUpperInvariant(c);
                if (u < 128 && _index[u] >= 0) return _index[u];
            }
            return _index[' '];
        }

        public static Sprite Glyph(char c)
        {
            Ensure();
            return _glyphs[IndexOf(c)];
        }

        public const float Advance = 6f; // font pixels per character
        public const float LineHeight = 9f;
    }

    public class PixelText : MonoBehaviour
    {
        public TextAlign align = TextAlign.Left;
        public int order = 60;
        public bool shadow;
        public Color shadowColor = new Color(0.05f, 0.03f, 0.1f, 0.8f);
        public float pixel = 0.125f;

        string _text = "";
        Color _color = Color.white;
        readonly List<SpriteRenderer> _glyphs = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _shadows = new List<SpriteRenderer>();

        public string Text { get { return _text; } }
        public float Pixel { get { return pixel; } }

        public void Set(string text)
        {
            text = text ?? "";
            if (text == _text) return;
            _text = text;
            Rebuild();
        }

        public void Set(string text, Color color)
        {
            SetColor(color);
            Set(text);
        }

        public void SetColor(Color c)
        {
            if (_color == c) return;
            _color = c;
            for (int i = 0; i < _glyphs.Count; i++) if (_glyphs[i].gameObject.activeSelf) _glyphs[i].color = c;
        }

        public void SetPixel(float p)
        {
            if (Mathf.Approximately(pixel, p)) return;
            pixel = p;
            Rebuild();
        }

        public float Width
        {
            get
            {
                int best = 0;
                if (string.IsNullOrEmpty(_text)) return 0f;
                foreach (var line in _text.Split('\n'))
                    best = Mathf.Max(best, line.Length);
                return best <= 0 ? 0f : (best * PixelFont.Advance - 1f) * pixel;
            }
        }

        public float Height
        {
            get
            {
                if (string.IsNullOrEmpty(_text)) return 0f;
                int lines = _text.Split('\n').Length;
                return lines * PixelFont.LineHeight * pixel;
            }
        }

        void Rebuild()
        {
            var lines = string.IsNullOrEmpty(_text) ? new string[0] : _text.Split('\n');
            float scale = pixel * 8f;
            int slot = 0;
            for (int li = 0; li < lines.Length; li++)
            {
                string line = lines[li];
                float w = line.Length <= 0 ? 0f : (line.Length * PixelFont.Advance - 1f) * pixel;
                float x0 = align == TextAlign.Center ? -w * 0.5f : (align == TextAlign.Right ? -w : 0f);
                float y0 = -li * PixelFont.LineHeight * pixel;
                for (int ci = 0; ci < line.Length; ci++)
                {
                    char c = line[ci];
                    if (c == ' ')
                    {
                        HideSlot(slot);
                        HideShadow(slot);
                        slot++;
                        continue;
                    }
                    var sprite = PixelFont.Glyph(c);
                    var sr = Slot(slot);
                    sr.gameObject.SetActive(true);
                    sr.sprite = sprite;
                    sr.color = _color;
                    sr.sortingOrder = order;
                    sr.transform.localPosition = new Vector3(x0 + ci * PixelFont.Advance * pixel, y0, 0f);
                    sr.transform.localScale = new Vector3(scale, scale, 1f);
                    if (shadow)
                    {
                        var sh = ShadowSlot(slot);
                        sh.gameObject.SetActive(true);
                        sh.sprite = sprite;
                        sh.color = shadowColor;
                        sh.sortingOrder = order - 1;
                        sh.transform.localPosition = new Vector3(x0 + ci * PixelFont.Advance * pixel - pixel, y0 - pixel, 0f);
                        sh.transform.localScale = new Vector3(scale, scale, 1f);
                    }
                    else HideShadow(slot);
                    slot++;
                }
            }
            for (int i = slot; i < _glyphs.Count; i++) HideSlot(i);
            for (int i = slot; i < _shadows.Count; i++) HideShadow(i);
        }

        SpriteRenderer Slot(int index)
        {
            while (_glyphs.Count <= index)
            {
                var go = new GameObject("g" + _glyphs.Count);
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sharedMaterial = SpriteBank.Mat;
                _glyphs.Add(sr);
            }
            return _glyphs[index];
        }

        SpriteRenderer ShadowSlot(int index)
        {
            while (_shadows.Count <= index)
            {
                var go = new GameObject("s" + _shadows.Count);
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sharedMaterial = SpriteBank.Mat;
                _shadows.Add(sr);
            }
            return _shadows[index];
        }

        void HideSlot(int index)
        {
            if (index < _glyphs.Count) _glyphs[index].gameObject.SetActive(false);
        }

        void HideShadow(int index)
        {
            if (index < _shadows.Count) _shadows[index].gameObject.SetActive(false);
        }
    }

    /// <summary>Helpers that build flat pixel panels out of a white 1x1 sprite.</summary>
    public static class PixelBox
    {
        public static GameObject Draw(Transform parent, Vector2 center, float w, float h, Color fill, Color border, int order, float borderPx = 1f / 8f, bool bevel = true)
        {
            var root = new GameObject("panel");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(center.x, center.y, 0f);

            var bg = SpriteBank.Make("ui_solid", root.transform, order, Vector2.zero, fill);
            bg.transform.localScale = new Vector3(w, h, 1f);

            if (bevel)
            {
                var b = SpriteBank.Make("ui_solid", root.transform, order + 1, new Vector2(0f, h * 0.5f - borderPx * 0.5f), Pal.Alpha(border, 0.35f));
                b.transform.localScale = new Vector3(w, borderPx, 1f);
                var s = SpriteBank.Make("ui_solid", root.transform, order + 1, new Vector2(0f, -h * 0.5f + borderPx * 0.5f), Pal.Alpha(Pal.Ink, 0.35f));
                s.transform.localScale = new Vector3(w, borderPx, 1f);
            }

            if (borderPx > 0f)
            {
                AddBorder(root.transform, w, h, border, order + 2, borderPx);
            }
            return root;
        }

        public static SpriteRenderer AddBorder(Transform parent, float w, float h, Color color, int order, float t)
        {
            var top = SpriteBank.Make("ui_solid", parent, order, new Vector2(0f, h * 0.5f - t * 0.5f), color);
            top.transform.localScale = new Vector3(w, t, 1f);
            var bottom = SpriteBank.Make("ui_solid", parent, order, new Vector2(0f, -h * 0.5f + t * 0.5f), color);
            bottom.transform.localScale = new Vector3(w, t, 1f);
            var left = SpriteBank.Make("ui_solid", parent, order, new Vector2(-w * 0.5f + t * 0.5f, 0f), color);
            left.transform.localScale = new Vector3(t, h, 1f);
            var right = SpriteBank.Make("ui_solid", parent, order, new Vector2(w * 0.5f - t * 0.5f, 0f), color);
            right.transform.localScale = new Vector3(t, h, 1f);
            return top;
        }
    }

    public class PixelDisplay : MonoBehaviour
    {
        public const int W = 768;
        public const int H = 432;
        public const float RefH = 216f;
        public const float WorldW = 48f;
        public const float WorldH = 27f;
        public static PixelDisplay I;

        public Camera cam;
        public RenderTexture rt;
        public Material crt;
        public RectTransform screenRect;
        public RectTransform cursorRect;
        public Image cursorImage;
        public Vector2 MouseWorld;
        public Vector2 MouseScreen;
        public bool HasMouse;
        public Transform worldRoot;

        float _shake;
        Vector3 _shakeOff;
        float _flash;

        void Awake() { I = this; }

        public void Build()
        {
            SpriteBank.Ensure();
            rt = new RenderTexture(W, H, 16, RenderTextureFormat.ARGB32);
            rt.filterMode = FilterMode.Point;
            rt.wrapMode = TextureWrapMode.Clamp;
            rt.antiAliasing = 1;
            rt.useMipMap = false;
            rt.Create();

            var camGO = new GameObject("PixelCamera");
            camGO.transform.SetParent(transform, false);
            camGO.transform.localPosition = new Vector3(0f, 0f, -10f);
            cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = WorldH * 0.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Pal.Ink;
            cam.targetTexture = rt;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 60f;
            cam.allowHDR = false;
            cam.allowMSAA = false;
            camGO.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            camGO.tag = "MainCamera";

            var world = new GameObject("World");
            world.transform.SetParent(transform, false);
            world.transform.localPosition = new Vector3(0f, 0f, 0f);
            worldRoot = world.transform;

            // overlay canvas showing the render texture through the CRT shader
            var canvasGO = new GameObject("ScreenCanvas", typeof(Canvas));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var imgGO = new GameObject("CRT", typeof(RawImage));
            imgGO.transform.SetParent(canvasGO.transform, false);
            var raw = imgGO.GetComponent<RawImage>();
            raw.texture = rt;
            raw.raycastTarget = false;
            var shader = Shader.Find("Pinball/CRT");
            if (shader != null)
            {
                crt = new Material(shader);
                raw.material = crt;
            }
            screenRect = imgGO.GetComponent<RectTransform>();
            screenRect.anchorMin = new Vector2(0.5f, 0.5f);
            screenRect.anchorMax = new Vector2(0.5f, 0.5f);
            screenRect.pivot = new Vector2(0.5f, 0.5f);
            screenRect.sizeDelta = new Vector2(1920, 1080);

            // The game is drawn by the CRT overlay canvas, but Unity still wants a camera pointed at
            // the display - this one just clears to black behind the canvas and draws nothing.
            var screenCamGO = new GameObject("ScreenCamera");
            screenCamGO.transform.SetParent(transform, false);
            screenCamGO.transform.localPosition = new Vector3(0f, 0f, -20f);
            var screenCam = screenCamGO.AddComponent<Camera>();
            screenCam.clearFlags = CameraClearFlags.SolidColor;
            screenCam.backgroundColor = Color.black;
            screenCam.cullingMask = 0;
            screenCam.depth = -10f;
            screenCam.orthographic = true;
            screenCam.orthographicSize = 1f;
            screenCam.nearClipPlane = 0.1f;
            screenCam.farClipPlane = 5f;
            screenCam.allowHDR = false;
            screenCam.allowMSAA = false;
            screenCam.useOcclusionCulling = false;
            screenCamGO.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();

            var cursorCanvasGO = new GameObject("CursorCanvas", typeof(Canvas));
            var cc = cursorCanvasGO.GetComponent<Canvas>();
            cc.renderMode = RenderMode.ScreenSpaceOverlay;
            cc.sortingOrder = 50;
            var curGO = new GameObject("Cursor", typeof(Image));
            curGO.transform.SetParent(cursorCanvasGO.transform, false);
            cursorImage = curGO.GetComponent<Image>();
            cursorImage.sprite = SpriteBank.Get("cursor");
            cursorImage.raycastTarget = false;
            cursorImage.preserveAspect = true;
            cursorRect = curGO.GetComponent<RectTransform>();
            cursorRect.anchorMin = new Vector2(0.5f, 0.5f);
            cursorRect.anchorMax = new Vector2(0.5f, 0.5f);
            cursorRect.pivot = new Vector2(0.06f, 0.94f);
            Cursor.visible = false;

            Layout();
        }

        void Layout()
        {
            float sw = Screen.width, sh = Screen.height;
            float target = 16f / 9f;
            float w = sh * target, h = sh;
            if (w > sw) { w = sw; h = sw / target; }
            screenRect.sizeDelta = new Vector2(w, h);
            screenRect.anchoredPosition = Vector2.zero;

            // cursor sits on its own canvas: size it from the sprite so the art stays pixel-exact.
            // whole-pixel scale (6x at 2160p, 3x at 1080p) times the cursor size multiplier,
            // rounded again so the art never lands on half pixels at other resolutions.
            const float CursorSizeMul = 1.25f;
            float cpx = Mathf.Max(1f, Mathf.Round(Mathf.Max(3f, Mathf.Round(h / 360f)) * CursorSizeMul));
            var cs = cursorImage != null ? cursorImage.sprite : null;
            if (cs != null)
            {
                var r = cs.rect;
                cursorRect.sizeDelta = new Vector2(r.width, r.height) * cpx;
                // hotspot = the arrow tip itself, one pixel in from the art's top-left corner
                cursorRect.pivot = new Vector2(0.5f / r.width, 1f - 0.5f / r.height);
            }
            else cursorRect.sizeDelta = new Vector2(24f, 36f) * cpx;
        }

        void Update()
        {
            Layout();
            UpdateMouse();
            UpdateShake();
            if (crt != null)
            {
                crt.SetVector("_Texel", new Vector4(1f / W, 1f / H, W, H));
                crt.SetFloat("_NoiseSeed", Time.unscaledTime);
                crt.SetFloat("_Flash", _flash);
                _flash = Mathf.MoveTowards(_flash, 0f, Time.unscaledDeltaTime * 4f);
            }
            if (shakeFlashTimer > 0f) shakeFlashTimer -= Time.unscaledDeltaTime;
        }

        float shakeFlashTimer;

        void UpdateMouse()
        {
            InputHub.TouchTick();
            var m = Mouse.current;
            HasMouse = m != null || InputHub.UsedTouch;

            // a touch is a pointer too: it drives the same world position the menus read.
            // Once a finger has been seen the pointer stays on the touch position, so the
            // frame a finger lifts still reads where it lifted. Without this it fell
            // through to the desktop mouse (0,0 on a phone) and a dragged tile was
            // released off the board, which is why placed items snapped back.
            if (InputHub.HasTouch || InputHub.UsedTouch) MouseScreen = InputHub.TouchPos;
            else if (m != null) MouseScreen = m.position.ReadValue();
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
            Vector2 local = MouseScreen - new Vector2(cx, cy);
            float u = screenRect.sizeDelta.x > 1f ? local.x / screenRect.sizeDelta.x + 0.5f : 0.5f;
            float v = screenRect.sizeDelta.y > 1f ? local.y / screenRect.sizeDelta.y + 0.5f : 0.5f;
            MouseWorld = new Vector2((u - 0.5f) * WorldW, (v - 0.5f) * WorldH);
            cursorRect.anchoredPosition = local;

            // on a touchscreen the finger is the cursor - the drawn arrow would just lag behind it
            if (cursorImage != null && cursorImage.enabled == InputHub.UsedTouch)
                cursorImage.enabled = !InputHub.UsedTouch;
        }

        public void Shake(float amount)
        {
            _shake = Mathf.Clamp(_shake + amount, 0f, 1.1f);
        }

        public void Flash(float amount)
        {
            _flash = Mathf.Clamp(_flash + amount, 0f, 0.9f);
        }

        void UpdateShake()
        {
            _shake = Mathf.MoveTowards(_shake, 0f, Time.unscaledDeltaTime * 3.0f);
            if (cam == null) return;
            float t = Time.unscaledTime * 47f;
            // linear term gives small hits a visible floor; quadratic term keeps big
            // hits (drains, tilt, game over) from overwhelming the frame
            float amp = Mathf.Min(_shake, 1.1f) * 1.05f + Mathf.Min(_shake, 1.1f) * Mathf.Min(_shake, 1.1f) * 0.8f;
            amp = Mathf.Min(amp, 1.3f);
            _shakeOff = new Vector3(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f, 0f) * (amp * 1.6f);
            cam.transform.localPosition = new Vector3(_shakeOff.x, _shakeOff.y, -10f);
        }
    }

    public static class InputHub
    {
        static Keyboard K { get { return Keyboard.current; } }
        static Gamepad P { get { return Gamepad.current; } }
        static Mouse M { get { return Mouse.current; } }

        /// <summary>Set by the attract-mode demo so menus can flick flippers without real input.</summary>
        public static bool ForceLeft;
        public static bool ForceRight;
        /// <summary>Only true while the attract demo is running - a Force* flag left
        /// latched can then never leak into a real game.</summary>
        public static bool ForceEnabled;

        static bool _touchInit;
        public static bool TouchLeft, TouchRight, TouchLaunch, HasTouch;
        /// <summary>Latched once a finger has ever been seen: switches the UI to touch rules.</summary>
        public static bool UsedTouch;
        /// <summary>Edge events and position, so touches can drive the menus as a virtual mouse.</summary>
        public static bool TouchBegin, TouchEnd, TouchPause;
        public static Vector2 TouchPos;
        /// <summary>Last frame a finger was actually down: the pointer stays on the
        /// touch for the release frame instead of snapping to the desktop mouse.</summary>
        static float LastTouchAt = -99f;
        static bool _sawFirstTouch;
        static int _prevTouches;
        static bool _twoFinger;
        static float _twoFingerAt;
        static bool _twoFingerFired;

        // Touch: screen halves = flippers, bottom strip = plunger charge,
        // two fingers held together = pause. Both actions also drive the simulated mouse,
        // so every menu button keeps working on a phone.
        public static void TouchTick()
        {
            if (!_touchInit) { UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable(); _touchInit = true; }
            TouchLeft = TouchRight = TouchLaunch = false;
            TouchBegin = TouchEnd = TouchPause = false;
            var touches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
            HasTouch = touches.Count > 0;
            if (HasTouch)
            {
                UsedTouch = true;
                LastTouchAt = Time.unscaledTime;
                // first touch on mobile: ask for fullscreen right away so portrait
                // screens stop showing a letterboxed window with browser UI around it
                if (!_sawFirstTouch) { _sawFirstTouch = true; if (!Screen.fullScreen) Screen.fullScreen = true; }
            }
            if (_prevTouches > 0 && touches.Count == 0) TouchEnd = true;
            _prevTouches = touches.Count;

            // two fingers must be HELD briefly before the pause menu opens - most
            // two-finger touches are just launch+flip at the same time
            if (touches.Count >= 2 && !_twoFinger) { _twoFinger = true; _twoFingerAt = Time.unscaledTime; _twoFingerFired = false; }
            if (touches.Count < 2) _twoFinger = false;
            if (_twoFinger && !_twoFingerFired && Time.unscaledTime - _twoFingerAt > 0.35f)
            {
                _twoFingerFired = true;
                TouchPause = true;
            }

            for (int i = 0; i < touches.Count; i++)
            {
                var t = touches[i];
                if (t.phase == UnityEngine.InputSystem.TouchPhase.Ended || t.phase == UnityEngine.InputSystem.TouchPhase.Canceled) continue;
                TouchPos = t.screenPosition;
                if (t.phase == UnityEngine.InputSystem.TouchPhase.Began) TouchBegin = true;
                float w = Screen.width, h = Screen.height;
                if (h < 1f || w < 1f) continue;
                if (t.screenPosition.y < h * 0.2f) TouchLaunch = true;
                else if (t.screenPosition.x < w * 0.5f) TouchLeft = true;
                else TouchRight = true;
            }
        }

        public static bool LeftFlipper
        {
            get
            {
                if (ForceEnabled && ForceLeft) return true;
                if (TouchLeft) return true;
                if (K != null && (K.leftArrowKey.isPressed || K.aKey.isPressed)) return true;
                if (P != null && (P.leftTrigger.ReadValue() > 0.35f || P.leftShoulder.isPressed)) return true;
                return false;
            }
        }

        public static bool RightFlipper
        {
            get
            {
                if (ForceEnabled && ForceRight) return true;
                if (TouchRight) return true;
                if (K != null && (K.rightArrowKey.isPressed || K.dKey.isPressed)) return true;
                if (P != null && (P.rightTrigger.ReadValue() > 0.35f || P.rightShoulder.isPressed)) return true;
                return false;
            }
        }

        public static bool LaunchHeld
        {
            get
            {
                if (TouchLaunch) return true;
                if (K != null && K.spaceKey.isPressed) return true;
                if (P != null && P.buttonSouth.isPressed) return true;
                return false;
            }
        }

        public static bool LaunchPressed
        {
            get
            {
                if (K != null && K.spaceKey.wasPressedThisFrame) return true;
                if (P != null && P.buttonSouth.wasPressedThisFrame) return true;
                return false;
            }
        }

        // mouse and touch are the same pointer as far as the UI is concerned
        public static bool MousePressed { get { return TouchBegin || (M != null && M.leftButton.wasPressedThisFrame); } }
        public static bool MouseDown { get { return HasTouch || (M != null && M.leftButton.isPressed); } }
        public static bool MouseReleased { get { return TouchEnd || (M != null && M.leftButton.wasReleasedThisFrame); } }
        public static Vector2 MousePos { get { return HasTouch || UsedTouch ? TouchPos : (M != null ? M.position.ReadValue() : TouchPos); } }

        public static bool StartPressed
        {
            get
            {
                if (K != null && (K.enterKey.wasPressedThisFrame || K.spaceKey.wasPressedThisFrame || K.numpadEnterKey.wasPressedThisFrame)) return true;
                if (P != null && P.buttonSouth.wasPressedThisFrame) return true;
                return TouchBegin || (M != null && M.leftButton.wasPressedThisFrame);
            }
        }

        /// <summary>Sandbox shortcut: end the current ball on purpose.</summary>
        public static bool BreakPressed
        {
            get
            {
                if (K != null && K.bKey.wasPressedThisFrame) return true;
                if (P != null && P.buttonNorth.wasPressedThisFrame) return true;
                return false;
            }
        }

        public static bool RerollPressed
        {
            get
            {
                if (K != null && K.rKey.wasPressedThisFrame) return true;
                if (P != null && P.buttonWest.wasPressedThisFrame) return true;
                return false;
            }
        }

        public static bool PausePressed
        {
            get
            {
                if (K != null && K.escapeKey.wasPressedThisFrame) return true;
                if (P != null && P.startButton.wasPressedThisFrame) return true;
                return TouchPause;
            }
        }

        public static bool NudgeLeftPressed
        {
            get
            {
                if (K != null && K.qKey.wasPressedThisFrame) return true;
                if (P != null && P.dpad.left.wasPressedThisFrame) return true;
                return false;
            }
        }

        public static bool NudgeRightPressed
        {
            get
            {
                if (K != null && K.eKey.wasPressedThisFrame) return true;
                if (P != null && P.dpad.right.wasPressedThisFrame) return true;
                return false;
            }
        }

        public static bool EnterPressed
        {
            get
            {
                if (K != null && (K.enterKey.wasPressedThisFrame || K.numpadEnterKey.wasPressedThisFrame)) return true;
                if (P != null && P.buttonSouth.wasPressedThisFrame) return true;
                return false;
            }
        }
    }

    public static class Phys
    {
        public static PhysicsMaterial2D Ball;
        public static PhysicsMaterial2D Bouncy;
        public static PhysicsMaterial2D Solid;

        public static void Ensure()
        {
            if (Ball != null) return;
            Ball = new PhysicsMaterial2D("ball") { bounciness = 0.22f, friction = 0.05f };
            Bouncy = new PhysicsMaterial2D("bouncy") { bounciness = 0.8f, friction = 0.02f };
            Solid = new PhysicsMaterial2D("solid") { bounciness = 0.35f, friction = 0.05f };
        }
    }

    public class FloatFx : MonoBehaviour
    {
        public PixelText text;
        float _life, _maxLife, _rise;
        Vector3 _start;
        Color _color;

        public void Play(Vector2 pos, string content, Color color, float scale, float life, float rise)
        {
            _start = new Vector3(pos.x, pos.y, -0.1f);
            transform.position = _start;
            _life = 0f;
            _maxLife = life;
            _rise = rise;
            _color = color;
            if (text == null)
            {
                var go = new GameObject("txt");
                go.transform.SetParent(transform, false);
                text = go.AddComponent<PixelText>();
                text.align = TextAlign.Center;
                text.pixel = 0.125f * scale;
                text.order = 85;
                text.shadow = true;
            }
            text.pixel = 0.125f * scale;
            text.order = 85;
            text.Set(content, color);
            gameObject.SetActive(true);
        }

        void Update()
        {
            _life += Time.deltaTime;
            float t = Mathf.Clamp01(_life / _maxLife);
            float pop = t < 0.18f ? Mathf.Lerp(0.45f, 1.18f, t / 0.18f) : Mathf.Lerp(1.18f, 1f, Mathf.Clamp01((t - 0.18f) / 0.22f));
            transform.position = _start + new Vector3(0f, _rise * Mathf.Sqrt(t), 0f);
            transform.localScale = new Vector3(pop, pop, 1f);
            var c = _color;
            c.a = t > 0.65f ? Mathf.Lerp(1f, 0f, (t - 0.65f) / 0.35f) : 1f;
            if (text != null) text.SetColor(c);
            if (t >= 1f) gameObject.SetActive(false);
        }
    }

    public class RingFx : MonoBehaviour
    {
        public SpriteRenderer sr;
        float _life, _max;
        Color _color;
        float _from, _to;

        public void Play(Vector2 pos, Color color, float from, float to, float life)
        {
            if (sr == null)
            {
                sr = gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteBank.Get("ring");
                sr.sharedMaterial = SpriteBank.Mat;
            }
            _color = color;
            _from = from; _to = to; _life = 0f; _max = life;
            transform.position = new Vector3(pos.x, pos.y, -0.05f);
            transform.localScale = Vector3.one * from;
            sr.color = color;
            sr.sortingOrder = 70;
            gameObject.SetActive(true);
        }

        void Update()
        {
            _life += Time.deltaTime;
            float t = Mathf.Clamp01(_life / _max);
            transform.localScale = Vector3.one * Mathf.Lerp(_from, _to, t);
            var c = _color;
            c.a = 1f - t;
            if (sr != null) sr.color = c;
            if (t >= 1f) gameObject.SetActive(false);
        }
    }

    public class SparkFx : MonoBehaviour
    {
        public SpriteRenderer sr;
        Vector2 _vel;
        float _life, _max;
        Color _color;
        float _gravity = 9f;

        public void Play(Vector2 pos, Vector2 vel, Color color, float life, float size, float gravity = 9f)
        {
            _gravity = gravity;
            if (sr == null)
            {
                sr = gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteBank.Get("spark");
                sr.sharedMaterial = SpriteBank.Mat;
            }
            transform.position = new Vector3(pos.x, pos.y, -0.2f);
            transform.localScale = Vector3.one * size;
            _vel = vel;
            _color = color;
            _life = 0f;
            _max = life;
            sr.color = color;
            sr.sortingOrder = 75;
            gameObject.SetActive(true);
        }

        void Update()
        {
            _life += Time.deltaTime;
            float t = Mathf.Clamp01(_life / _max);
            _vel += new Vector2(0f, -_gravity) * Time.deltaTime;
            transform.position += (Vector3)(_vel * Time.deltaTime);
            var c = _color;
            c.a = 1f - t;
            if (sr != null) sr.color = c;
            if (t >= 1f) gameObject.SetActive(false);
        }
    }

    /// <summary>A coin pops up and out of an item, drops, then fades - a little physical flourish per hit.</summary>
    public class CoinFx : MonoBehaviour
    {
        public SpriteRenderer sr;
        Vector2 _vel;
        float _life, _max;
        float _spin;

        public void Play(Vector2 pos)
        {
            if (sr == null)
            {
                sr = gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteBank.Get("coin");
                sr.sharedMaterial = SpriteBank.Mat;
                sr.sortingOrder = 75;
            }
            float x = Random.Range(-0.9f, 0.9f);
            transform.position = new Vector3(pos.x + x * 0.2f, pos.y + 0.35f, -0.2f);
            transform.localScale = Vector3.one * Random.Range(0.55f, 0.7f);
            transform.localRotation = Quaternion.identity;
            _vel = new Vector2(x, Random.Range(3.4f, 4.6f));
            _spin = x * 160f;
            _life = 0f;
            _max = Random.Range(0.8f, 1f);
            sr.color = Color.white;
            gameObject.SetActive(true);
        }

        void Update()
        {
            _life += Time.deltaTime;
            float t = Mathf.Clamp01(_life / _max);
            _vel.y -= 13f * Time.deltaTime;
            transform.position += (Vector3)(_vel * Time.deltaTime);
            transform.Rotate(0f, 0f, _spin * Time.deltaTime);
            var c = sr.color;
            c.a = t < 0.55f ? 1f : 1f - (t - 0.55f) / 0.45f;
            sr.color = c;
            if (t >= 1f) gameObject.SetActive(false);
        }
    }

    public class Juice : MonoBehaviour
    {
        public static Juice I;
        /// <summary>Calm-FX mode: softens screenshake and flashes (accessibility toggle).</summary>
        public static bool Calm;
        readonly List<FloatFx> _floats = new List<FloatFx>();
        readonly List<RingFx> _rings = new List<RingFx>();
        readonly List<SparkFx> _sparks = new List<SparkFx>();
        readonly List<CoinFx> _coins = new List<CoinFx>();
        float _hitstopUntil;
        bool _stopped;

        void Awake()
        {
            I = this;
        }

        void Update()
        {
            if (_stopped && Time.unscaledTime >= _hitstopUntil)
            {
                Time.timeScale = 1f;
                _stopped = false;
            }
        }

        public static void Float(Vector2 pos, string txt, Color color, float scale = 1f, float life = 1.05f, float rise = 2.2f)
        {
            if (I == null) return;
            FloatFx fx = null;
            foreach (var f in I._floats) if (!f.gameObject.activeSelf) { fx = f; break; }
            if (fx == null)
            {
                var go = new GameObject("float");
                go.transform.SetParent(I.transform, false);
                fx = go.AddComponent<FloatFx>();
                I._floats.Add(fx);
            }
            fx.Play(pos, txt, color, scale, life, rise);
        }

        public static void Ring(Vector2 pos, Color color, float from = 0.6f, float to = 2.4f, float life = 0.32f)
        {
            if (I == null) return;
            RingFx fx = null;
            foreach (var f in I._rings) if (!f.gameObject.activeSelf) { fx = f; break; }
            if (fx == null)
            {
                var go = new GameObject("ring");
                go.transform.SetParent(I.transform, false);
                fx = go.AddComponent<RingFx>();
                I._rings.Add(fx);
            }
            fx.Play(pos, color, from, to, life);
        }

        public static void Spark(Vector2 pos, Color color, int count = 6, float speed = 8f, float size = 0.8f)
        {
            if (I == null) return;
            for (int i = 0; i < count; i++)
            {
                SparkFx fx = null;
                foreach (var f in I._sparks) if (!f.gameObject.activeSelf) { fx = f; break; }
                if (fx == null)
                {
                    var go = new GameObject("spark");
                    go.transform.SetParent(I.transform, false);
                    fx = go.AddComponent<SparkFx>();
                    I._sparks.Add(fx);
                }
                float a = Random.Range(0f, Mathf.PI * 2f);
                float sp = speed * Random.Range(0.4f, 1f);
                fx.Play(pos, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * sp, color, Random.Range(0.25f, 0.55f), size * Random.Range(0.6f, 1.2f));
            }
        }

        /// <summary>A coin pops up out of an item and drops away.</summary>
        public static void CoinPop(Vector2 pos)
        {
            if (I == null) return;
            CoinFx fx = null;
            foreach (var f in I._coins) if (!f.gameObject.activeSelf) { fx = f; break; }
            if (fx == null)
            {
                var go = new GameObject("coinpop");
                go.transform.SetParent(I.transform, false);
                fx = go.AddComponent<CoinFx>();
                I._coins.Add(fx);
            }
            fx.Play(pos);
        }

        public static void Trail(Vector2 pos, Color color, float size)
        {
            if (I == null) return;
            SparkFx fx = null;
            foreach (var f in I._sparks) if (!f.gameObject.activeSelf) { fx = f; break; }
            if (fx == null)
            {
                var go = new GameObject("trail");
                go.transform.SetParent(I.transform, false);
                fx = go.AddComponent<SparkFx>();
                I._sparks.Add(fx);
            }
            fx.Play(pos, Vector2.zero, color, 0.22f, size, 0f);
        }

        public static void Shake(float amount)
        {
            if (Calm) amount *= 0.3f;
            if (PixelDisplay.I != null) PixelDisplay.I.Shake(amount);
        }

        public static void Flash(float amount)
        {
            if (Calm) amount *= 0.3f;
            if (PixelDisplay.I != null) PixelDisplay.I.Flash(amount);
        }

        public static void Hitstop(float duration)
        {
            if (I == null) return;
            I._hitstopUntil = Mathf.Max(I._hitstopUntil, Time.unscaledTime + duration);
            if (!I._stopped)
            {
                I._stopped = true;
                Time.timeScale = 0.08f;
            }
        }
    }
}
