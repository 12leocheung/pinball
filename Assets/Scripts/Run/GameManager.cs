using System.Collections.Generic;
using UnityEngine;

namespace Pinball
{
    public enum Phase { Menu, Play, Shop, GameOver }

    public class RunStats
    {
        // BumperHits and ModulesHit only bank hits from the current ball;
        // FullCharges / conditionsDone / BestBallAlive keep spanning the round.
        public int BumperHits;
        public int FullCharges;
        public int HoldCatches;
        public int ConditionsThisRound;
        public readonly HashSet<string> ModulesHit = new HashSet<string>();
        public readonly HashSet<string> RoundModulesHit = new HashSet<string>();
        public float BallAlive;
        public float BestBallAlive;
        public int SingleBallScore;
        public int BestSingleBallScore;
        public int TopHalfScore;

        public void ResetBall()
        {
            BallAlive = 0f;
            SingleBallScore = 0;
            TopHalfScore = 0;
            BumperHits = 0;
            ModulesHit.Clear();
        }
    }

    public static class Ui
    {
        public static PixelText Text(Transform parent, string s, Vector2 pos, Color color, float pixel = 0.125f, TextAlign align = TextAlign.Left, int order = 60, bool shadow = false)
        {
            var go = new GameObject("text");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            var t = go.AddComponent<PixelText>();
            t.align = align;
            t.pixel = pixel;
            t.order = order;
            t.shadow = shadow;
            t.Set(s, color);
            return t;
        }

        public static string Wrap(string s, int maxChars)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var words = s.Split(' ');
            var sb = new System.Text.StringBuilder();
            int line = 0;
            for (int i = 0; i < words.Length; i++)
            {
                string w = words[i];
                if (line > 0 && line + 1 + w.Length > maxChars)
                {
                    sb.Append('\n');
                    line = 0;
                }
                else if (line > 0)
                {
                    sb.Append(' ');
                    line++;
                }
                sb.Append(w);
                line += w.Length;
            }
            return sb.ToString();
        }

        public static string Fmt(int n)
        {
            return n.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>m:ss for the sandbox session clock.</summary>
        public static string Clock(float seconds)
        {
            int s = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return (s / 60) + ":" + (s % 60).ToString("00");
        }
    }

    public class UiBar
    {
        public SpriteRenderer fill;
        public float width;
        public float height;
        public Vector2 center;

        public UiBar(Transform parent, Vector2 pos, float w, float h, Color bg, Color fillColor, int order)
        {
            center = pos;
            width = w;
            height = h;
            var back = SpriteBank.Quad(parent, pos, w, h, bg, order);
            back.name = "bar_bg";
            fill = SpriteBank.Quad(parent, pos, w, h, fillColor, order + 1);
            fill.name = "bar_fill";
        }

        public void Set(float t)
        {
            t = Mathf.Clamp01(t);
            float w = Mathf.Max(0.0001f, width * t);
            fill.transform.localScale = new Vector3(w, height * 0.72f, 1f);
            fill.transform.localPosition = new Vector3(center.x - width * 0.5f + w * 0.5f, center.y, 0f);
        }

        public void SetColor(Color c)
        {
            fill.color = c;
        }
    }

    public class Hud : MonoBehaviour
    {
        Transform _root;
        PixelText _round;
        PixelText _score;
        PixelText _quota;
        PixelText _quotaLabel;
        PixelText _ballsLabel;
        PixelText _coins;
        PixelText _mult;
        PixelText _combo;
        PixelText _balls;
        PixelText _charge;
        PixelText _bossName;
        PixelText _bossDesc;
        SpriteRenderer _bossIcon;
        GameObject _bossGroup;
        UiBar _scoreBar;
        UiBar _chargeBar;
        UiBar _wildBar;
        PixelText _wildBarLabel;
        GameObject _wildBox;
        readonly List<GameObject> _ballIcons = new List<GameObject>();
        readonly List<WildRow> _rows = new List<WildRow>();
        PixelText _hint;
        PixelText _seedText;
        bool _touchHint;
        GameObject _sandboxBar;
        GameObject _shopBtn;
        GameObject _breakBtn;
        GameObject _endBtn;
        GameObject _pauseBtn;
        GameObject _launchBtn;
        // top centre band: above the table (which ends at y 11) and clear of the
        // launch charge bar at the bottom, so nothing overlaps on a phone either
        static readonly Rect SandboxShopRect = new Rect(-7.4f, 11.25f, 4.4f, 1.7f);
        static readonly Rect SandboxBreakRect = new Rect(-2.2f, 11.25f, 4.4f, 1.7f);
        static readonly Rect SandboxEndRect = new Rect(3f, 11.25f, 4.4f, 1.7f);
        static readonly Rect PauseRect = new Rect(21.2f, 10.65f, 2.4f, 1.9f);

        class WildRow
        {
            public GameObject root;
            public SpriteRenderer icon;
            public SpriteRenderer frame;
            public PixelText name;
            public PixelText cond;
            public PixelText reward;
            public UiBar bar;
            public PixelText progress;
        }

        public void Build(Transform parent)
        {
            _root = new GameObject("Hud").transform;
            _root.SetParent(parent, false);

            // ---- left panel
            PixelBox.Draw(_root, new Vector2(-15.7f, 0f), 12.6f, 25.0f, Pal.Deep, Pal.Alpha(Pal.Plum, 0.9f), 40);
            PixelBox.Draw(_root, new Vector2(-15.7f, 11.3f), 11.9f, 2.0f, Pal.Deep2, Pal.Alpha(Pal.Mauve, 0.8f), 42);
            Ui.Text(_root, "SCORE", new Vector2(-21.2f, 8.9f), Pal.Alpha(Pal.Sand, 0.95f), 0.115f, TextAlign.Left, 60);
            _score = Ui.Text(_root, "0", new Vector2(-10.0f, 6.5f), Pal.Cream, 0.3f, TextAlign.Right, 62, true);

            _quotaLabel = Ui.Text(_root, "QUOTA", new Vector2(-21.2f, 4.2f), Pal.Alpha(Pal.Sand, 0.95f), 0.115f, TextAlign.Left, 60);
            _quota = Ui.Text(_root, "0", new Vector2(-10.0f, 4.2f), Pal.Alpha(Pal.Gold, 1f), 0.15f, TextAlign.Right, 61);
            _scoreBar = new UiBar(_root, new Vector2(-15.7f, 3.1f), 11.2f, 0.5f, Pal.Alpha(Pal.Ink, 0.75f), Pal.Sky, 60);

            _balls = Ui.Text(_root, "BALLS", new Vector2(-21.2f, 1.4f), Pal.Alpha(Pal.Sand, 0.95f), 0.115f, TextAlign.Left, 60);
            _ballsLabel = _balls;
            for (int i = 0; i < 7; i++)
            {
                var sr = SpriteBank.Make("heart", _root, 61, new Vector2(-16.9f + i * 1.15f, 1.4f), Pal.Pink, "ball" + i);
                sr.transform.localScale = Vector3.one;
                _ballIcons.Add(sr.gameObject);
            }

            var coinIcon = SpriteBank.Make("coin", _root, 61, new Vector2(-20.9f, -0.7f), Color.white, "coinIcon");
            coinIcon.transform.localScale = Vector3.one * 1.1f;
            _coins = Ui.Text(_root, "0", new Vector2(-19.7f, -0.7f), Pal.Gold, 0.17f, TextAlign.Left, 61, true);

            _mult = Ui.Text(_root, "MULT x1", new Vector2(-21.2f, -2.5f), Pal.Lilac, 0.15f, TextAlign.Left, 61);
            _combo = Ui.Text(_root, "", new Vector2(-21.2f, -3.5f), Pal.Gold, 0.115f, TextAlign.Left, 61, true);
            _combo.Set("");

            // boss card
            _bossGroup = PixelBox.Draw(_root, new Vector2(-15.7f, -6.7f), 11.9f, 4.8f, Pal.Alpha(Pal.Ink, 0.55f), Pal.Alpha(Pal.Red, 0.9f), 44);
            _bossIcon = SpriteBank.Make("boss_collector", _bossGroup.transform, 46, new Vector2(-4.1f, 0.1f));
            _bossIcon.transform.localScale = Vector3.one * 0.95f;
            _bossName = Ui.Text(_bossGroup.transform, "BOSS", new Vector2(-2.2f, 1.1f), Pal.Alpha(Pal.Rose, 1f), 0.095f, TextAlign.Left, 47);
            _bossDesc = Ui.Text(_bossGroup.transform, "", new Vector2(-2.2f, -0.45f), Pal.Alpha(Pal.Sand, 0.9f), 0.072f, TextAlign.Left, 47);
            _bossGroup.SetActive(false);

            _hint = Ui.Text(_root, "SPACE: CHARGE LAUNCH\nARROWS: FLIP  Q/E: NUDGE", new Vector2(-21.2f, -10.6f), Pal.Alpha(Pal.Mauve, 1f), 0.08f, TextAlign.Left, 60);
            _seedText = Ui.Text(_root, "", new Vector2(-21.2f, -12.05f), Pal.Alpha(Pal.Mauve, 0.85f), 0.072f, TextAlign.Left, 60);

            _round = Ui.Text(_root, "ROUND 1", new Vector2(-15.7f, 11.3f), Pal.Cream, 0.15f, TextAlign.Center, 50, true);

            // ---- right panel
            PixelBox.Draw(_root, new Vector2(15.7f, 0f), 12.6f, 25.0f, Pal.Deep, Pal.Alpha(Pal.Plum, 0.9f), 40);
            PixelBox.Draw(_root, new Vector2(15.7f, 11.3f), 11.9f, 2.0f, Pal.Deep2, Pal.Alpha(Pal.Mauve, 0.8f), 42);
            Ui.Text(_root, "WILDCARDS", new Vector2(15.7f, 11.3f), Pal.Cream, 0.14f, TextAlign.Center, 50, true);

            // condition bar block (top of the right panel)
            _wildBox = PixelBox.Draw(_root, new Vector2(15.7f, 9.35f), 11.9f, 1.75f, Pal.Alpha(Pal.Ink, 0.5f), Pal.Alpha(Pal.Plum, 1f), 45);
            _wildBarLabel = Ui.Text(_wildBox.transform, "", new Vector2(0f, 0.4f), Pal.Alpha(Pal.Cream, 0.95f), 0.078f, TextAlign.Center, 50);
            _wildBar = new UiBar(_wildBox.transform, new Vector2(0f, -0.48f), 11.1f, 0.36f, Pal.Alpha(Pal.Ink, 0.85f), Pal.Lilac, 47);

            for (int i = 0; i < 5; i++)
            {
                var row = new WildRow();
                float y = 6.95f - i * 4.25f;
                row.root = PixelBox.Draw(_root, new Vector2(15.7f, y), 11.9f, 3.95f, Pal.Alpha(Pal.Deep2, 0.85f), Pal.Alpha(Pal.Plum, 1f), 44);
                row.frame = SpriteBank.Make("ui_solid", row.root.transform, 45, new Vector2(-6.16f, 0f), Pal.Cream, "stripe");
                row.frame.transform.localScale = new Vector3(0.07f, 4.2f, 1f);
                row.icon = SpriteBank.Make("wild_moon", row.root.transform, 46, new Vector2(-4.4f, 0.12f), Color.white, "icon");
                row.icon.transform.localScale = Vector3.one * 0.78f;
                row.name = Ui.Text(row.root.transform, "WILDCARD", new Vector2(-2.85f, 1.35f), Pal.Cream, 0.098f, TextAlign.Left, 50);
                row.cond = Ui.Text(row.root.transform, "", new Vector2(-2.85f, 0.32f), Pal.Alpha(Pal.Sand, 0.9f), 0.072f, TextAlign.Left, 50);
                row.reward = Ui.Text(row.root.transform, "", new Vector2(-2.85f, -1.2f), Pal.Gold, 0.07f, TextAlign.Left, 50);
                row.bar = new UiBar(row.root.transform, new Vector2(1.4f, -1.6f), 8.4f, 0.3f, Pal.Alpha(Pal.Ink, 0.8f), Pal.Teal, 47);
                row.progress = Ui.Text(row.root.transform, "", new Vector2(5.7f, -1.2f), Pal.Alpha(Pal.Cream, 0.95f), 0.07f, TextAlign.Right, 51);
                row.root.SetActive(false);
                _rows.Add(row);
            }

            // ---- launch charge bar
            var chargeBox = PixelBox.Draw(_root, new Vector2(0f, -11.7f), 12.6f, 1.3f, Pal.Alpha(Pal.Deep, 0.92f), Pal.Alpha(Pal.Plum, 1f), 45);
            _chargeBar = new UiBar(chargeBox.transform, new Vector2(0f, -0.24f), 11.4f, 0.36f, Pal.Alpha(Pal.Ink, 0.8f), Pal.Pink, 47);
            _charge = Ui.Text(chargeBox.transform, "HOLD SPACE TO CHARGE", new Vector2(0f, 0.34f), Pal.Alpha(Pal.Sand, 0.95f), 0.082f, TextAlign.Center, 51);

            // ---- sandbox controls (only shown during a sandbox session)
            _sandboxBar = new GameObject("SandboxBar");
            _sandboxBar.transform.SetParent(_root, false);
            _shopBtn = PixelBox.Draw(_sandboxBar.transform, new Vector2(-5.2f, 12.1f), 4.4f, 1.7f, Pal.Deep2, Pal.Alpha(Pal.Teal, 0.95f), 46);
            Ui.Text(_shopBtn.transform, "LIBRARY", Vector2.zero, Pal.Teal, 0.082f, TextAlign.Center, 51, true);
            _breakBtn = PixelBox.Draw(_sandboxBar.transform, new Vector2(0f, 12.1f), 4.4f, 1.7f, Pal.Deep2, Pal.Alpha(Pal.Gold, 0.95f), 46);
            Ui.Text(_breakBtn.transform, "BREAK BALL", Vector2.zero, Pal.Gold, 0.07f, TextAlign.Center, 51, true);
            _endBtn = PixelBox.Draw(_sandboxBar.transform, new Vector2(5.2f, 12.1f), 4.4f, 1.7f, Pal.Deep2, Pal.Alpha(Pal.Rose, 0.95f), 46);
            Ui.Text(_endBtn.transform, "END", Vector2.zero, Pal.Rose, 0.09f, TextAlign.Center, 51, true);
            _sandboxBar.SetActive(false);

            // ---- touch launch button: a fixed spot to press for the plunger,
            // sitting beside the charge bar so the finger never covers the fill
            _launchBtn = PixelBox.Draw(_root, new Vector2(-7.85f, -11.7f), 3.0f, 1.5f, Pal.Deep2, Pal.Alpha(Pal.Pink, 0.95f), 52);
            Ui.Text(_launchBtn.transform, "LAUNCH", Vector2.zero, Pal.White, 0.075f, TextAlign.Center, 54, true);
            _launchBtn.SetActive(false);

            // ---- touch pause button (hidden until a finger is actually used)
            _pauseBtn = PixelBox.Draw(_root, new Vector2(22.4f, 11.6f), 2.4f, 1.9f, Pal.Alpha(Pal.Deep2, 0.92f), Pal.Alpha(Pal.Cream, 0.85f), 52);
            Ui.Text(_pauseBtn.transform, "II", Vector2.zero, Pal.Cream, 0.1f, TextAlign.Center, 54, true);
            _pauseBtn.SetActive(false);
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || _root == null || !_root.gameObject.activeSelf) return;
            if (Menus.I != null && Menus.I.Paused) return;
            if (PixelDisplay.I == null) return;
            var m = PixelDisplay.I.MouseWorld;

            bool sandbox = _sandboxBar.activeSelf;
            bool overShop = sandbox && SandboxShopRect.Contains(m);
            bool overBreak = sandbox && SandboxBreakRect.Contains(m);
            bool overEnd = sandbox && SandboxEndRect.Contains(m);
            bool overPause = _pauseBtn != null && _pauseBtn.activeSelf && PauseRect.Contains(m);

            _shopBtn.transform.localScale = overShop ? new Vector3(1.1f, 1.12f, 1f) : Vector3.one;
            _breakBtn.transform.localScale = overBreak ? new Vector3(1.1f, 1.12f, 1f) : Vector3.one;
            _endBtn.transform.localScale = overEnd ? new Vector3(1.1f, 1.12f, 1f) : Vector3.one;
            _pauseBtn.transform.localScale = overPause ? new Vector3(1.1f, 1.12f, 1f) : Vector3.one;

            if (!InputHub.MousePressed) return;
            if (overPause)
            {
                if (Menus.I != null) Menus.I.TogglePause();
            }
            else if (overShop) gm.OpenSandboxShop();
            else if (overBreak) gm.BreakBall();
            else if (overEnd) gm.EndSandbox();
            // the launch button is read straight from the bottom-strip touch zone
            // (TouchLaunch already maps it), it only exists to show where to press
        }

        public void Refresh()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            bool sb = gm.Sandbox;
            _round.Set(sb ? "SANDBOX" : "ROUND " + gm.Round);
            _seedText.Set("SEED " + gm.Seed);
            if (InputHub.UsedTouch != _touchHint)
            {
                _touchHint = InputHub.UsedTouch;
                _hint.Set(_touchHint
                    ? "TAP SIDES: FLIP\nHOLD LAUNCH TO CHARGE"
                    : "SPACE: CHARGE LAUNCH\nARROWS: FLIP  Q/E: NUDGE");
            }
            // shrink long scores so a million+ run still fits inside the panel
            string scoreTxt = Ui.Fmt(gm.Score);
            _score.SetPixel(Mathf.Min(0.3f, 10.9f / (6f * Mathf.Max(1, scoreTxt.Length))));
            _score.Set(scoreTxt);
            if (sb)
            {
                _quotaLabel.Set("FREE PLAY");
                _quota.Set("");
                _scoreBar.Set(1f);
                _scoreBar.SetColor(Pal.Teal);
            }
            else
            {
                _quotaLabel.Set("QUOTA");
                _quota.Set(Ui.Fmt(Mathf.CeilToInt(gm.Quota)));
                _quota.SetColor(Pal.Alpha(Pal.Gold, 1f));
                _scoreBar.Set(gm.Quota <= 0f ? 0f : gm.Score / gm.Quota);
                _scoreBar.SetColor(gm.Score >= gm.Quota ? Pal.Mint : Pal.Sky);
            }
            _coins.Set(Ui.Fmt(gm.Coins));
            _mult.Set("MULT x" + gm.Mult.ToString("0.##"));
            bool comboOn = gm.Combo >= 2;
            if (_combo.gameObject.activeSelf != comboOn) _combo.gameObject.SetActive(comboOn);
            if (comboOn)
            {
                int pct = Mathf.RoundToInt((gm.ComboBonus() - 1f) * 100f);
                _combo.Set("COMBO x" + gm.Combo + "  +" + pct + "%");
            }
            _ballsLabel.Set(sb ? "BALLS  INF" : "BALLS");

            for (int i = 0; i < _ballIcons.Count; i++)
                _ballIcons[i].SetActive(!sb && i < gm.BallsLeft);

            bool sandboxUi = sb && gm.Phase == Phase.Play;
            if (_sandboxBar.activeSelf != sandboxUi) _sandboxBar.SetActive(sandboxUi);
            bool pauseUi = InputHub.UsedTouch && gm.Phase == Phase.Play;
            if (_pauseBtn.activeSelf != pauseUi) _pauseBtn.SetActive(pauseUi);
            if (_launchBtn.activeSelf != pauseUi) _launchBtn.SetActive(pauseUi);

            if (gm.Boss != null)
            {
                if (!_bossGroup.activeSelf) _bossGroup.SetActive(true);
                _bossName.Set(gm.Boss.name);
                _bossDesc.Set(Ui.Wrap(gm.Boss.desc, 18));
                _bossIcon.sprite = SpriteBank.Get(gm.Boss.sprite);
                _bossIcon.color = gm.Boss.tint;
            }
            else if (_bossGroup.activeSelf) _bossGroup.SetActive(false);

            // wildcard rows
            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                bool show = i < gm.Wildcards.Count;
                if (row.root.activeSelf != show) row.root.SetActive(show);
                if (!show) continue;
                var w = gm.Wildcards[i];
                row.icon.sprite = SpriteBank.Get(w.def.sprite);
                row.icon.color = w.disabled ? new Color(0.45f, 0.42f, 0.5f, 0.7f) : Color.white;
                row.frame.color = w.disabled ? Pal.Alpha(Pal.Mauve, 0.6f) : Pal.RarityColor(w.def.rarity);
                row.name.Set(w.def.name);
                row.name.SetColor(w.completedThisRound ? Pal.Mint : (w.disabled ? Pal.Mauve : Pal.Cream));
                row.cond.Set(Ui.Wrap(w.disabled ? "DISABLED BY BOSS" : w.def.ConditionText(), 19));
                row.cond.SetColor(w.disabled ? Pal.Rose : Pal.Alpha(Pal.Sand, 0.85f));
                row.reward.Set(w.def.RewardText());
                row.bar.Set(w.def.target <= 0f ? 0f : w.progress / w.def.target);
                row.bar.SetColor(w.completedThisRound ? Pal.Mint : (w.disabled ? Pal.Mauve : Pal.RarityColor(w.def.rarity)));
                row.progress.Set(w.completedThisRound ? "DONE" : (Mathf.FloorToInt(w.progress) + "/" + Mathf.RoundToInt(w.def.target)));
            }

            // top bar: show the closest incomplete wildcard
            WildcardInstance active = null;
            float best = -1f;
            foreach (var w in gm.Wildcards)
            {
                if (w.completedThisRound || w.disabled) continue;
                float t = w.def.target <= 0f ? 0f : w.progress / w.def.target;
                if (t > best) { best = t; active = w; }
            }
            if (active != null)
            {
                if (!_wildBox.activeSelf) _wildBox.SetActive(true);
                _wildBarLabel.Set(active.def.name + "   " + Mathf.FloorToInt(active.progress) + "/" + Mathf.RoundToInt(active.def.target));
                _wildBar.Set(best);
                _wildBar.SetColor(Pal.RarityColor(active.def.rarity));
            }
            else if (gm.Wildcards.Count == 0)
            {
                if (!_wildBox.activeSelf) _wildBox.SetActive(true);
                _wildBarLabel.Set("NO WILDCARDS YET");
                _wildBar.Set(0f);
            }
            else if (_wildBox.activeSelf) _wildBox.SetActive(false);

            float charge = gm.Table != null ? gm.Table.Charge : 0f;
            _chargeBar.Set(charge);
            if (gm.Phase == Phase.Play)
            {
                var t = gm.Table;
                if (t != null && t.AwaitingLaunch)
                    _charge.Set(t.Charging ? "CHARGING " + Mathf.RoundToInt(charge * 100f) + "%" : "HOLD SPACE TO CHARGE");
                else
                    _charge.Set("GOOD LUCK");
            }
        }

        public void SetVisible(bool visible)
        {
            if (_root != null) _root.gameObject.SetActive(visible);
        }
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager I;

        public PinballTable Table;
        public Hud HudUi;
        public Shop ShopUi;
        public Menus MenusUi;

        public Phase Phase = Phase.Play;
        public int Round = 1;
        public int Score;
        public int Coins;
        public int BallsLeft;
        public int BallsPerRound = 3;
        public float Mult = 1f;
        public float Quota;
        public BossDef Boss;
        public int Seed;
        public bool DemoMode;
        public static int BestEver;
        public static int BestRound;
        public readonly List<WildcardInstance> Wildcards = new List<WildcardInstance>();
        public readonly List<BoardItemDef> Inventory = new List<BoardItemDef>();
        public RunStats Stats = new RunStats();
        public System.Random Rng = new System.Random();
        public bool HoldSaved;
        public bool HoldFilledThisRound;
        public int BestScore;
        public int Combo;
        public bool Sandbox;
        const float ComboWindow = 1.6f;
        float _comboTimer;
        bool _quotaCeleb;

        PixelText _banner;
        float _bannerTimer;
        float _bannerMax;
        Color _bannerColor;
        GameObject _gameOver;
        PixelText _goTitle;
        PixelText _goBody;
        PixelText _goNewBest;
        GameObject _goAgainRoot;
        PixelText _goAgainLabel;
        Rect _goAgainRect;
        GameObject _goMenuRoot;
        PixelText _goMenuLabel;
        Rect _goMenuRect;
        bool _newBest;
        bool _bestCelebrated;
        float _runClock;

        /// <summary>Every owned/bought item and wildcard, saved as a ';'-list so the
        /// menu can show exactly what each thing does.</summary>
        public readonly HashSet<string> UnlockedIds = new HashSet<string>();

        public void Unlock(string id)
        {
            if (string.IsNullOrEmpty(id) || UnlockedIds.Contains(id)) return;
            UnlockedIds.Add(id);
            var sb = new System.Text.StringBuilder();
            foreach (var u in UnlockedIds) { if (sb.Length > 0) sb.Append(';'); sb.Append(u); }
            PlayerPrefs.SetString("pb_unlocked", sb.ToString());
            PlayerPrefs.Save();
        }

        void LoadUnlocked()
        {
            UnlockedIds.Clear();
            var raw = PlayerPrefs.GetString("pb_unlocked", "");
            if (string.IsNullOrEmpty(raw)) return;
            foreach (var id in raw.Split(';'))
                if (!string.IsNullOrEmpty(id)) UnlockedIds.Add(id);
        }

        public void Boot()
        {
            I = this;
            BestEver = PlayerPrefs.GetInt("pb_best", 0);
            BestRound = PlayerPrefs.GetInt("pb_round", 0);
            Juice.Calm = PlayerPrefs.GetInt("pb_calm", 0) == 1;
            LoadUnlocked();

            var tableGO = new GameObject("Table");
            tableGO.transform.SetParent(transform, false);
            Table = tableGO.AddComponent<PinballTable>();
            Table.Build();

            HudUi = gameObject.AddComponent<Hud>();
            HudUi.Build(PixelDisplay.I.worldRoot);

            ShopUi = gameObject.AddComponent<Shop>();
            ShopUi.Build();

            BuildOverlays();

            Sfx.Boot(transform);
            Music.Boot(transform, 0);
            MenusUi = gameObject.AddComponent<Menus>();
            MenusUi.Build();

            EnterMenu();
        }

        float _demoLaunch;
        float _demoFlip = 0.6f;
        float _demoFlipHold;

        public void EnterMenu()
        {
            InputHub.ForceLeft = false;
            InputHub.ForceRight = false;
            InputHub.ForceEnabled = true;
            DemoMode = true;
            Sandbox = false;
            Phase = Phase.Menu;
            Round = 1;
            Quota = 0f;
            Score = 0;
            Mult = 1f;
            HoldSaved = false;
            Stats = new RunStats();
            Table.ClearModules();
            Table.ApplyBoss(null);
            Table.BeginRound();
            Table.PrepareBall();
            _demoLaunch = 1.4f;
            _demoFlip = 0.6f;
            _demoFlipHold = 0f;
            _gameOver.SetActive(false);
            HudUi.SetVisible(false);
            ShopUi.Hide();
            Time.timeScale = 1f;
            if (Menus.I != null) Menus.I.ShowMain();
        }

        void TickDemo()
        {
            Table.Tick();
            _demoLaunch -= Time.deltaTime;
            if (Table.AwaitingLaunch && _demoLaunch <= 0f)
            {
                Table.Charge = 1f;
                Table.DoLaunch();
                _demoLaunch = 2.6f + Random.value * 2.4f;
            }
            _demoFlip -= Time.deltaTime;
            if (_demoFlip <= 0f)
            {
                _demoFlip = 0.5f + Random.value * 0.9f;
                InputHub.ForceLeft = Random.value < 0.55f;
                InputHub.ForceRight = !InputHub.ForceLeft && Random.value < 0.5f;
                if (InputHub.ForceLeft || InputHub.ForceRight) _demoFlipHold = 0.22f;
            }
            if (_demoFlipHold > 0f)
            {
                _demoFlipHold -= Time.deltaTime;
                if (_demoFlipHold <= 0f)
                {
                    InputHub.ForceLeft = false;
                    InputHub.ForceRight = false;
                }
            }
        }

        void BuildOverlays()
        {
            _banner = Ui.Text(PixelDisplay.I.worldRoot, "", new Vector2(0f, 2.6f), Pal.Cream, 0.22f, TextAlign.Center, 90, true);
            _banner.Set("");

            var dim = PixelBox.Draw(PixelDisplay.I.worldRoot, new Vector2(0f, 0f), 48f, 27f, Pal.Alpha(Pal.Ink, 0.92f), Pal.Alpha(Pal.Ink, 0f), 95, 0f, false);
            _gameOver = dim;
            _goTitle = Ui.Text(dim.transform, "QUOTA FAILED", new Vector2(0f, 3.2f), Pal.Rose, 0.3f, TextAlign.Center, 96, true);
            _goNewBest = Ui.Text(dim.transform, "NEW BEST!", new Vector2(0f, 1.7f), Pal.Gold, 0.19f, TextAlign.Center, 96, true);
            _goNewBest.gameObject.SetActive(false);
            // body grows downward from its anchor (line height 9*0.115 = 1.035/line); 5 lines end at y=-4.44,
            // so it clears the RUN AGAIN box (top -4.95). Keep it at 5 lines max or the buttons cover it.
            _goBody = Ui.Text(dim.transform, "", new Vector2(0f, -0.3f), Pal.Cream, 0.115f, TextAlign.Center, 96);

            _goAgainRoot = PixelBox.Draw(dim.transform, new Vector2(0f, -6.5f), 9.4f, 1.9f, Pal.Deep2, Pal.Gold, 97);
            _goAgainRect = new Rect(-4.7f, -7.45f, 9.4f, 1.9f);
            _goAgainLabel = Ui.Text(_goAgainRoot.transform, "RUN AGAIN", Vector2.zero, Pal.Gold, 0.115f, TextAlign.Center, 99, true);
            _goMenuRoot = PixelBox.Draw(dim.transform, new Vector2(0f, -8.7f), 9.4f, 1.9f, Pal.Deep2, Pal.Rose, 97);
            _goMenuRect = new Rect(-4.7f, -9.65f, 9.4f, 1.9f);
            _goMenuLabel = Ui.Text(_goMenuRoot.transform, "MAIN MENU", Vector2.zero, Pal.Rose, 0.115f, TextAlign.Center, 99, true);

            dim.SetActive(false);
        }

        public int QuotaFor(int round)
        {
            float q = 2500f * Mathf.Pow(2.2f, round - 1);
            if (round % 5 == 0) q *= 1.35f;
            return Mathf.RoundToInt(q / 10f) * 10;
        }

        public void StartRun() { StartRun(0); }

        public void StartRun(int seed)
        {
            _newBest = false;
            _bestCelebrated = false;
            Seed = seed > 0 ? seed : Random.Range(10000000, 100000000);
            Rng = new System.Random(Seed);
            Music.SetVariant(seed);
            DemoMode = false;
            Sandbox = false;
            InputHub.ForceEnabled = false;
            // the menu demo may still be holding a forced flipper down the frame you
            // pressed start - TickDemo never runs again to release it, so clear it here
            InputHub.ForceLeft = false;
            InputHub.ForceRight = false;
            Time.timeScale = 1f;
            Round = 1;
            Score = 0;
            Coins = 3;
            BestScore = 0;
            Mult = 1f;
            BallsPerRound = 3;
            HoldSaved = false;
            Wildcards.Clear();
            Inventory.Clear();
            Table.ClearModules();
            if (Menus.I != null) Menus.I.HideAll();
            _gameOver.SetActive(false);
            BeginRound();
            if (ShopUi != null) ShopUi.Lock(0.25f);
        }

        public void BeginRound()
        {
            Quota = Sandbox ? 0f : QuotaFor(Round);
            // every boss round rolls its own opponent, so round 5 is never
            // always the same boss
            Boss = Sandbox || Round % 5 != 0 ? null : Catalog.RandomBoss(Rng);
            Table.ApplyBoss(Boss);
            Score = 0;
            Mult = 1f;
            HoldFilledThisRound = false;
            _quotaCeleb = false;
            Stats = new RunStats();
            foreach (var w in Wildcards)
            {
                w.progress = 0f;
                w.completedThisRound = false;
                w.disabled = Boss != null && Boss.disablesRarity && w.def.rarity == Boss.disabledRarity;
            }

            int balls = BallsPerRound + (HoldSaved ? 1 : 0);
            HoldSaved = false;
            if (Boss != null) balls -= Boss.ballPenalty;
            BallsLeft = Mathf.Max(1, balls);

            Table.BeginRound();
            Phase = Phase.Play;
            HudUi.SetVisible(true);
            ShopUi.Hide();
            SpawnBall();

            if (Boss != null) ShowBanner(Boss.name, Boss.tint, 2.2f);
            else ShowBanner("ROUND " + Round + "   -   QUOTA " + Ui.Fmt(Mathf.CeilToInt(Quota)), Pal.Cream, 1.8f);

            if (HudUi != null) HudUi.Refresh();
        }

        void SpawnBall()
        {
            Stats.ResetBall();
            Table.PrepareBall();
        }

        void Update()
        {
            if (PixelDisplay.I == null) return;
            UpdateBanner();

            if (Menus.I != null && Menus.I.Paused && Phase != Phase.Menu) return;

            switch (Phase)
            {
                case Phase.Menu:
                    TickDemo();
                    break;
                case Phase.Play:
                    if (Sandbox && InputHub.BreakPressed) BreakBall();
                    Table.Tick();
                    TrackBall();
                    UpdateWildcards(Time.deltaTime);
                    if (_comboTimer > 0f)
                    {
                        _comboTimer -= Time.deltaTime;
                        if (_comboTimer <= 0f) Combo = 0;
                    }
                    break;
                case Phase.Shop:
                    ShopUi.Tick();
                    break;
                case Phase.GameOver:
                    GameOverTick();
                    break;
            }

            if (HudUi != null) HudUi.Refresh();
        }

        void UpdateBanner()
        {
            if (_bannerTimer <= 0f) return;
            _bannerTimer -= Time.deltaTime;
            float t = 1f - _bannerTimer / _bannerMax;
            float pop = t < 0.15f ? Mathf.Lerp(0.5f, 1.15f, t / 0.15f) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((t - 0.15f) / 0.2f));
            _banner.transform.localScale = new Vector3(pop, pop, 1f);
            var c = _bannerColor;
            c.a = t > 0.7f ? Mathf.Lerp(1f, 0f, (t - 0.7f) / 0.3f) : 1f;
            _banner.SetColor(c);
            if (_bannerTimer <= 0f) _banner.Set("");
        }

        public void ShowBanner(string text, Color color, float time)
        {
            _bannerColor = color;
            _banner.Set(text, color);
            _bannerTimer = time;
            _bannerMax = Mathf.Max(0.01f, time);
            _banner.transform.localScale = Vector3.one * 0.6f;
        }

        void TrackBall()
        {
            if (Table == null || Table.BallRef == null) return;
            if (!Table.BallRef.InPlay) return;
            if (Table.BallRef.rb.bodyType != RigidbodyType2D.Dynamic) return;
            Stats.BallAlive += Time.deltaTime;
            Stats.BestBallAlive = Mathf.Max(Stats.BestBallAlive, Stats.BallAlive);
        }

        float ProgressFor(WildcardDef d)
        {
            switch (d.cond)
            {
                case ConditionKind.BumperHits: return Stats.BumperHits;
                case ConditionKind.SingleBallScore: return Mathf.Max(Stats.SingleBallScore, Stats.BestSingleBallScore);
                // comet stays round-spanning; every other module-hit wildcard
                // only counts hits made with the current ball
                case ConditionKind.ModuleHits: return d.id == "comet" ? Stats.RoundModulesHit.Count : Stats.ModulesHit.Count;
                case ConditionKind.FullCharges: return Stats.FullCharges;
                case ConditionKind.AliveTime: return Stats.BestBallAlive;
                case ConditionKind.NoBottomScore: return Stats.TopHalfScore;
                case ConditionKind.ConditionsDone: return Stats.ConditionsThisRound;
                case ConditionKind.HoldCatches: return Stats.HoldCatches;
                case ConditionKind.RoundScore: return Score;
                default: return 0f;
            }
        }

        void UpdateWildcards(float dt)
        {
            float mul = Boss != null ? Boss.progressMul : 1f;
            foreach (var w in Wildcards)
            {
                if (w.disabled || w.completedThisRound) continue;
                w.progress = ProgressFor(w.def) * mul;
                if (w.progress >= w.def.target)
                {
                    CompleteWildcard(w);
                }
            }
        }

        void CompleteWildcard(WildcardInstance w)
        {
            w.completedThisRound = true;
            Stats.ConditionsThisRound++;
            Sfx.Play("wild");
            Juice.Flash(0.18f);
            Juice.Shake(0.3f);
            ShowBanner(w.def.name + "  COMPLETE", Pal.RarityColor(w.def.rarity), 1.8f);
            Juice.Float(new Vector2(0f, 6.4f), "+" + w.def.RewardText(), Pal.Gold, 1.6f, 1.6f, 2.4f);

            switch (w.def.reward)
            {
                case RewardKind.Mult:
                    Mult += w.def.rewardValue;
                    break;
                case RewardKind.Coins:
                    AddCoins(Mathf.RoundToInt(w.def.rewardValue), new Vector2(0f, 4f), true);
                    break;
                case RewardKind.Ball:
                    BallsLeft += Mathf.RoundToInt(w.def.rewardValue);
                    break;
                case RewardKind.Score:
                    Score += Mathf.RoundToInt(w.def.rewardValue);
                    break;
            }
        }

        int Award(int basePoints, Vector2 pos, Color color, bool bumper = false, float scale = 1f)
        {
            // only a launched (dynamic) ball scores - shields the round start from
            // collisions the physics system queues while the old ball is being parked
            if (Table == null || Table.BallRef == null || Table.BallRef.rb == null ||
                Table.BallRef.rb.bodyType != RigidbodyType2D.Dynamic)
                return 0;
            float bossMul = bumper && Boss != null ? Boss.bumperMul : 1f;
            int pts = Mathf.Max(1, Mathf.RoundToInt(basePoints * Mult * bossMul * scale * ComboBonus()));
            Score += pts;
            Stats.SingleBallScore += pts;
            Stats.BestSingleBallScore = Mathf.Max(Stats.BestSingleBallScore, Stats.SingleBallScore);
            if (pos.y > 0f) Stats.TopHalfScore += pts;
            if (Score > BestScore) BestScore = Score;
            Juice.Float(pos + Random.insideUnitCircle * 0.25f, "+" + Ui.Fmt(pts), color, 1.05f, 1f, 2.3f);

            // first time a round crosses the quota: celebrate on the spot
            if (!_quotaCeleb && Quota > 0f && Score >= Quota)
            {
                _quotaCeleb = true;
                ShowBanner("QUOTA MET!", Pal.Mint, 1.6f);
                Sfx.Play("win");
                Juice.Flash(0.25f);
                Juice.Shake(0.3f);
                Juice.Ring(pos, Pal.Mint, 0.7f, 3.6f, 0.55f);
            }
            return pts;
        }

        public float ComboBonus()
        {
            return 1f + Mathf.Min(Combo * 0.05f, 0.5f);
        }

        void BumpCombo(Vector2 pos)
        {
            Combo++;
            _comboTimer = ComboWindow;
            if (Combo >= 5 && Combo % 5 == 0)
                Juice.Float(pos + Vector2.up * 1.5f, "COMBO x" + Combo, Pal.Lilac, 1.25f, 1.1f, 1.7f);
        }

        public void OnBumperHit(int value, Vector2 pos, Color tint)
        {
            Sfx.Play("bumper");
            Stats.BumperHits++;
            BumpCombo(transform.position);
            int pts = Award(value, pos, tint, true);
            Juice.Ring(pos, tint, 0.8f, 2.6f, 0.3f);
            Juice.Spark(pos, tint, 6, 9f, 0.8f);
            Juice.Shake(0.2f);
            if (pts > 400) Juice.Hitstop(0.035f);
        }

        public void OnPegHit(Vector2 pos)
        {
            Sfx.Play("peg");
            BumpCombo(pos);
            Award(6, pos, Pal.Sand, false, 0.8f);
            Juice.Spark(pos, Pal.Sand, 3, 5f, 0.55f);
        }

        public void OnModuleHit(BoardModule module, int value)
        {
            Stats.ModulesHit.Add(module.def.id);
            Stats.RoundModulesHit.Add(module.def.id);
            module.Pulse();
            Sfx.Play("module");
            BumpCombo(module.transform.position);
            int pts = Award(value, module.transform.position, Pal.RarityColor(module.def.rarity));
            Juice.Ring(module.transform.position, Pal.RarityColor(module.def.rarity), 0.9f, 2.8f, 0.34f);
            Juice.Spark(module.transform.position, Pal.RarityColor(module.def.rarity), 7, 9f, 0.8f);
            Juice.Shake(0.27f);
            if (pts > 250) Juice.Hitstop(0.045f);
        }

        public void AddCoins(int amount, Vector2 pos, bool floaty)
        {
            if (amount == 0) return;
            Sfx.Play("coin");
            Coins += amount;
            if (floaty) Juice.Float(pos + Vector2.up * 0.6f, "+" + amount + " COIN", Pal.Gold, 1.05f, 1.2f, 2.4f);
            Juice.Spark(pos, Pal.Gold, 4, 6f, 0.7f);
        }

        public void AddMultiplier(float amount, Vector2 pos)
        {
            Mult += amount;
            Juice.Float(pos + Vector2.up * 0.8f, "+" + amount.ToString("0.##") + " MULT", Pal.Lilac, 1.2f, 1.2f, 2.4f);
            Juice.Flash(0.08f);
        }

        public bool CanHoldBall()
        {
            return !HoldFilledThisRound;
        }

        public void OnHoldCatch(BoardModule module)
        {
            HoldFilledThisRound = true;
            HoldSaved = true;
            Stats.HoldCatches++;
            Sfx.Play("save");
            ShowBanner("BALL SAVED FOR NEXT ROUND", Pal.Mint, 1.8f);
            Juice.Ring(module.transform.position, Pal.Mint, 0.8f, 3.4f, 0.5f);
            Juice.Spark(module.transform.position, Pal.Mint, 14, 11f);
            Juice.Flash(0.22f);
            ConsumeBall();
        }

        public void OnBallLaunched(float charge)
        {
            Sfx.Play("launch", 0.04f, 0.75f);
            if (charge >= 0.92f) Stats.FullCharges++;
            Juice.Shake(0.06f);
        }

        public void OnBallDrained()
        {
            Sfx.Play("drain");
            Stats.ResetBall();
            Combo = 0;
            _comboTimer = 0f;
            Juice.Shake(0.4f);
            Juice.Flash(0.1f);
            if (DemoMode)
            {
                Table.PrepareBall();
                _demoLaunch = 1.1f;
                return;
            }
            if (Sandbox)
            {
                Table.ClearHolds();
                HoldFilledThisRound = false;
                SpawnBall();
                return;
            }
            ConsumeBall();
        }

        void GameOverTick()
        {
            // keyboard / gamepad only: StartPressed also counts a mouse click, which
            // would swallow the MAIN MENU click below and restart the run instead
            if (InputHub.EnterPressed || InputHub.LaunchPressed)
            {
                StartRun(0);
                return;
            }
            if (_goAgainRoot == null || PixelDisplay.I == null) return;
            var m = PixelDisplay.I.MouseWorld;
            bool hovA = _goAgainRect.Contains(m);
            bool hovM = _goMenuRect.Contains(m);
            _goAgainRoot.transform.localScale = hovA ? new Vector3(1.05f, 1.1f, 1f) : Vector3.one;
            _goMenuRoot.transform.localScale = hovM ? new Vector3(1.05f, 1.1f, 1f) : Vector3.one;
            _goAgainLabel.SetColor(hovA ? Pal.White : Pal.Gold);
            _goMenuLabel.SetColor(hovM ? Pal.White : Pal.Rose);
            if (InputHub.MousePressed)
            {
                if (hovA) StartRun(0);
                else if (hovM)
                {
                    // block for a beat or the same click lands on the menu's own
                    // buttons (the lose-screen MAIN MENU box sits over DAILY RUN)
                    if (Menus.I != null) Menus.I.BlockClicks(0.3f);
                    EnterMenu();
                }
            }
        }

        void ConsumeBall()
        {
            if (Sandbox)
            {
                Table.ClearHolds();
                HoldFilledThisRound = false;
                SpawnBall();
                return;
            }
            BallsLeft--;
            if (BallsLeft > 0)
            {
                SpawnBall();
            }
            else
            {
                EndRound();
            }
        }

        void EndRound()
        {
            Table.ClearBall();
            bool won = Score >= Quota;
            bool passedBest = Score > BestEver && BestEver > 0;
            _newBest = _newBest || passedBest;
            if (Score > BestEver) BestEver = Score;
            int roundReached = won ? Round + 1 : Round;
            if (roundReached > BestRound) BestRound = roundReached;
            PlayerPrefs.SetInt("pb_best", BestEver);
            PlayerPrefs.SetInt("pb_round", BestRound);
            PlayerPrefs.Save();
            if (won)
            {
                int earned = 3 + Mathf.FloorToInt(2f * Score / Mathf.Max(1f, Quota));
                if (Boss != null) earned += 2;
                earned = Mathf.Min(earned, 12);
                Coins += earned;
                ShowBanner(Boss != null ? "BOSS CLEARED!" : "QUOTA MET!", Pal.Mint, 2f);
                Sfx.Play("win");
                if (passedBest && !_bestCelebrated)
                {
                    _bestCelebrated = true;
                    Juice.Float(new Vector2(0f, 4.2f), "NEW BEST!", Pal.Gold, 1.4f, 1.6f, 2.2f);
                    Juice.Flash(0.2f);
                }
                Phase = Phase.Shop;
                HudUi.SetVisible(false);
                ShopUi.Show(earned);
                Juice.Flash(0.25f);
            }
            else
            {
                Phase = Phase.GameOver;
                HudUi.SetVisible(true);
                _goTitle.Set("QUOTA FAILED");
                _goNewBest.gameObject.SetActive(_newBest);
                _goBody.Set("ROUND " + Round + "\nSCORE " + Ui.Fmt(Score) + "  /  NEEDED " + Ui.Fmt(Mathf.CeilToInt(Quota)) +
                            "\n\nBEST SCORE " + Ui.Fmt(BestEver) + "  -  BEST ROUND " + BestRound +
                            "\nSEED " + Seed);
                _gameOver.SetActive(true);
                Sfx.Play("lose");
                Juice.Flash(0.5f);
                Juice.Shake(0.6f);
            }
        }

        public void ContinueToNextRound()
        {
            Round++;
            BeginRound();
        }

        public void AddWildcard(WildcardDef def)
        {
            // five slots everywhere (sandbox included), duplicates are fine
            if (Wildcards.Count >= 5) return;
            Wildcards.Add(new WildcardInstance { def = def, progress = 0f });
            Juice.Float(new Vector2(0f, 3f), def.name, Pal.RarityColor(def.rarity), 1.3f, 1.4f, 2.2f);
        }

        // ---------------- sandbox ----------------

        /// <summary>Starts a free-play practice session: no timer, no credits,
        /// just a table to build on.</summary>
        public void StartSandbox()
        {
            _newBest = false;
            _bestCelebrated = false;
            Seed = Random.Range(10000000, 100000000);
            Rng = new System.Random(Seed);
            Music.SetVariant(Seed);
            Sandbox = true;
            DemoMode = false;
            InputHub.ForceEnabled = false;
            InputHub.ForceLeft = false;
            InputHub.ForceRight = false;
            Time.timeScale = 1f;
            Round = 1;
            Score = 0;
            Coins = 0;
            BestScore = 0;
            Mult = 1f;
            BallsPerRound = 3;
            HoldSaved = false;
            Wildcards.Clear();
            Inventory.Clear();
            Table.ClearModules();
            if (Menus.I != null) Menus.I.HideAll();
            _gameOver.SetActive(false);
            BeginRound();
            ShowBanner("SANDBOX - FREE PLAY", Pal.Teal, 2f);
            if (ShopUi != null) ShopUi.Lock(0.25f);
            if (HudUi != null) HudUi.Refresh();
        }

        public void EndSandbox()
        {
            if (!Sandbox) return;
            Sandbox = false;
            if (ShopUi != null) ShopUi.Hide();
            Table.ClearBall();
            EnterMenu();
            ShowBanner("SANDBOX OVER", Pal.Teal, 2f);
        }

        /// <summary>Sandbox only: the player decides when the current ball is done.</summary>
        public void BreakBall()
        {
            if (!Sandbox || Phase != Phase.Play) return;
            OnBallDrained();
            ShowBanner("NEW BALL", Pal.Teal, 0.9f);
        }

        public void OpenSandboxShop()
        {
            if (!Sandbox || Phase != Phase.Play) return;
            Phase = Phase.Shop;
            HudUi.SetVisible(false);
            ShopUi.Show(0);
            ShopUi.Lock(0.2f);
        }

        public void CloseSandboxShop()
        {
            if (!Sandbox) return;
            ShopUi.Hide();
            Phase = Phase.Play;
            HudUi.SetVisible(true);
            Table.ClearHolds();
            HoldFilledThisRound = false;
            SpawnBall();
            HudUi.Refresh();
            ShowBanner("BACK TO THE TABLE", Pal.Teal, 1.2f);
        }

        public int SellValue(int price)
        {
            return Mathf.Max(1, Mathf.FloorToInt(price * 0.5f));
        }

        public void ToggleCalm()
        {
            Juice.Calm = !Juice.Calm;
            PlayerPrefs.SetInt("pb_calm", Juice.Calm ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
