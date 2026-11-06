using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pinball
{
    /// <summary>
    /// The three top-10 boards (daily, main mode, all time) and the 3-letter
    /// name prompt shown when a run lands on one of them. The all-time board
    /// tags every score with the mode it came from.
    /// </summary>
    public class LeaderboardUi : MonoBehaviour
    {
        public static LeaderboardUi I;

        /// <summary>True while a Board panel or the name prompt is up, so the
        /// menus and the game-over screen stop handling clicks behind it.</summary>
        public static bool Busy { get { return I != null && (I._boardsOpen || I._nameOpen); } }

        class Brd
        {
            public GameObject root;
            public Rect rect;
            public PixelText label;
        }

        // ---------------- boards panel
        GameObject _boards;
        bool _boardsOpen;
        LbBoard _tab = LbBoard.Main;
        Brd _tabDaily, _tabMain, _tabAll, _back;
        PixelText _subtitle, _empty;
        readonly List<PixelText> _ranks = new List<PixelText>();
        readonly List<PixelText> _names = new List<PixelText>();
        readonly List<PixelText> _scores = new List<PixelText>();
        readonly List<PixelText> _modes = new List<PixelText>();
        PixelText _modeHead;
        float _block;

        // ---------------- name entry
        const string Charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        GameObject _namePanel;
        bool _nameOpen;
        PixelText _nameTitle, _nameBoards, _nameMode;
        readonly char[] _chars = { 'A', 'A', 'A' };
        readonly PixelText[] _slotText = new PixelText[3];
        readonly GameObject[] _slotBox = new GameObject[3];
        int _slot;
        Brd _prev, _next, _save, _skip;
        readonly List<LbBoard> _pendingBoards = new List<LbBoard>();
        int _pendingScore, _pendingRound;
        bool _pendingDaily;
        bool _charsHooked;

        void Awake() { I = this; }

        // ================================================================ build

        public void Build(Transform root)
        {
            BuildBoards(root);
            BuildNamePrompt(root);
            _boards.SetActive(false);
            _namePanel.SetActive(false);
        }

        void BuildBoards(Transform root)
        {
            _boards = PixelBox.Draw(root, Vector2.zero, 28f, 22.6f, Pal.Deep, Pal.Alpha(Pal.Plum, 1f), 109);
            // dim the menu behind the panel so its title cannot peek around the edges;
            // child of the panel, so it is hidden again when the panel closes
            SpriteBank.Quad(_boards.transform, Vector2.zero, 64f, 44f, Pal.Alpha(Pal.Ink, 0.88f), 105);
            Ui.Text(_boards.transform, "LEADERBOARDS", new Vector2(0f, 9.9f), Pal.Cream, 0.26f, TextAlign.Center, 112, true);
            _subtitle = Ui.Text(_boards.transform, "", new Vector2(0f, 8.35f), Pal.Gold, 0.08f, TextAlign.Center, 112);

            _tabDaily = Mk(_boards.transform, "DAILY", new Vector2(-9.2f, 7.0f), 7.8f, 1.5f, Pal.Sky, 112);
            _tabMain = Mk(_boards.transform, "MAIN", new Vector2(0f, 7.0f), 7.8f, 1.5f, Pal.Gold, 112);
            _tabAll = Mk(_boards.transform, "ALL TIME", new Vector2(9.2f, 7.0f), 7.8f, 1.5f, Pal.Lilac, 112);

            Ui.Text(_boards.transform, "RANK", new Vector2(-12.4f, 5.6f), Pal.Alpha(Pal.Mauve, 0.95f), 0.065f, TextAlign.Left, 112);
            Ui.Text(_boards.transform, "NAME", new Vector2(-9.4f, 5.6f), Pal.Alpha(Pal.Mauve, 0.95f), 0.065f, TextAlign.Left, 112);
            Ui.Text(_boards.transform, "SCORE", new Vector2(8.4f, 5.6f), Pal.Alpha(Pal.Mauve, 0.95f), 0.065f, TextAlign.Right, 112);
            _modeHead = Ui.Text(_boards.transform, "MODE", new Vector2(12.8f, 5.6f), Pal.Alpha(Pal.Mauve, 0.95f), 0.065f, TextAlign.Right, 112);

            for (int i = 0; i < Lb.Cap; i++)
            {
                float y = 4.4f - i * 1.05f;
                _ranks.Add(Ui.Text(_boards.transform, "", new Vector2(-12.4f, y), Pal.Alpha(Pal.Sand, 0.9f), 0.085f, TextAlign.Left, 112));
                _names.Add(Ui.Text(_boards.transform, "", new Vector2(-9.4f, y), Pal.Cream, 0.085f, TextAlign.Left, 112));
                _scores.Add(Ui.Text(_boards.transform, "", new Vector2(8.4f, y), Pal.Gold, 0.085f, TextAlign.Right, 112));
                _modes.Add(Ui.Text(_boards.transform, "", new Vector2(12.8f, y), Pal.Alpha(Pal.Sand, 0.9f), 0.075f, TextAlign.Right, 112));
            }

            _empty = Ui.Text(_boards.transform, "", new Vector2(0f, 0.4f), Pal.Alpha(Pal.Mauve, 0.95f), 0.1f, TextAlign.Center, 112);
            Ui.Text(_boards.transform, "SCORES ARE SAVED ON THIS DEVICE", new Vector2(0f, -6.6f), Pal.Alpha(Pal.Mauve, 0.85f), 0.065f, TextAlign.Center, 112);
            _back = Mk(_boards.transform, "BACK", new Vector2(0f, -8.2f), 6.4f, 1.5f, Pal.Rose, 112);
        }

        void BuildNamePrompt(Transform root)
        {
            _namePanel = new GameObject("NamePrompt");
            _namePanel.transform.SetParent(root, false);
            SpriteBank.Quad(_namePanel.transform, Vector2.zero, 64f, 44f, Pal.Alpha(Pal.Ink, 0.9f), 118);
            var box = PixelBox.Draw(_namePanel.transform, Vector2.zero, 29f, 17f, Pal.Deep, Pal.Alpha(Pal.Plum, 1f), 119);

            _nameTitle = Ui.Text(box.transform, "NEW HIGH SCORE!", new Vector2(0f, 6.4f), Pal.Gold, 0.25f, TextAlign.Center, 123, true);
            _nameBoards = Ui.Text(box.transform, "", new Vector2(0f, 4.7f), Pal.Cream, 0.09f, TextAlign.Center, 123);
            _nameMode = Ui.Text(box.transform, "", new Vector2(0f, 3.7f), Pal.Lilac, 0.08f, TextAlign.Center, 123);

            for (int i = 0; i < 3; i++)
            {
                _slotBox[i] = PixelBox.Draw(box.transform, new Vector2(-3.6f + i * 3.6f, 0.6f), 3.0f, 3.6f, Pal.Deep2, Pal.Alpha(Pal.Gold, 0.9f), 120);
                _slotText[i] = Ui.Text(_slotBox[i].transform, "A", Vector2.zero, Pal.Cream, 0.42f, TextAlign.Center, 123, true);
            }

            _prev = Mk(box.transform, "<", new Vector2(-8.0f, 0.6f), 2.6f, 3.6f, Pal.Sky, 120);
            _next = Mk(box.transform, ">", new Vector2(8.0f, 0.6f), 2.6f, 3.6f, Pal.Sky, 120);

            Ui.Text(box.transform, "TYPE A NAME OR USE < >", new Vector2(0f, -1.9f), Pal.Alpha(Pal.Sand, 0.95f), 0.075f, TextAlign.Center, 123);
            _save = Mk(box.transform, "SAVE", new Vector2(-3.5f, -4.1f), 6.2f, 1.7f, Pal.Gold, 120);
            _skip = Mk(box.transform, "SKIP", new Vector2(3.5f, -4.1f), 6.2f, 1.7f, Pal.Rose, 120);
        }

        Brd Mk(Transform parent, string label, Vector2 pos, float w, float h, Color accent, int order)
        {
            var b = new Brd();
            b.root = PixelBox.Draw(parent, pos, w, h, Pal.Deep2, accent, order);
            b.rect = new Rect(pos.x - w * 0.5f, pos.y - h * 0.5f, w, h);
            b.label = Ui.Text(b.root.transform, label, Vector2.zero, Pal.Cream, order >= 120 ? 0.2f : 0.115f, TextAlign.Center, order + 3, true);
            return b;
        }

        // ================================================================ boards

        public void OpenBoards(LbBoard tab)
        {
            _tab = tab;
            _boardsOpen = true;
            _block = 0.2f;
            _boards.SetActive(true);
            RefreshBoards();
            Sfx.Play("click");
        }

        public void CloseBoards(bool silent = false)
        {
            _boardsOpen = false;
            if (_boards != null) _boards.SetActive(false);
            if (!silent) Sfx.Play("click");
        }

        void RefreshBoards()
        {
            var list = Lb.Top(_tab);
            _subtitle.Set(_tab == LbBoard.Daily ? "TODAY'S DAILY RUNS"
                        : _tab == LbBoard.Main ? "BEST MAIN-MODE RUNS"
                        : "EVERY RUN, EVERY MODE");
            _modeHead.gameObject.SetActive(_tab == LbBoard.AllTime);
            SetTabColor(_tabDaily, _tab == LbBoard.Daily);
            SetTabColor(_tabMain, _tab == LbBoard.Main);
            SetTabColor(_tabAll, _tab == LbBoard.AllTime);

            for (int i = 0; i < Lb.Cap; i++)
            {
                bool has = i < list.Count;
                SetRowVisible(i, has);
                if (!has) continue;
                var e = list[i];
                _ranks[i].Set((i + 1) + ".");
                _names[i].Set(e.name);
                _scores[i].Set(Ui.Fmt(e.score));
                bool showMode = _tab == LbBoard.AllTime;
                _modes[i].Set(showMode ? e.mode : "");
                _modes[i].SetColor(e.mode == "DAILY" ? Pal.Sky : Pal.Gold);
            }
            _empty.Set(list.Count == 0 ? "NO SCORES YET - SET THE FIRST ONE!" : "");
        }

        void SetTabColor(Brd b, bool active)
        {
            if (b == null || b.label == null) return;
            b.label.SetColor(active ? Pal.White : Pal.Alpha(Pal.Sand, 0.5f));
            if (b.root != null) b.root.transform.localScale = active ? new Vector3(1f, 1.06f, 1f) : Vector3.one;
        }

        void SetRowVisible(int i, bool on)
        {
            _ranks[i].gameObject.SetActive(on);
            _names[i].gameObject.SetActive(on);
            _scores[i].gameObject.SetActive(on);
            _modes[i].gameObject.SetActive(on);
        }

        void TickBoards()
        {
            if (_block > 0f) _block -= Time.unscaledDeltaTime;
            var m = PixelDisplay.I != null ? PixelDisplay.I.MouseWorld : Vector2.zero;
            Hover(_tabDaily, _tabDaily.rect.Contains(m));
            Hover(_tabMain, _tabMain.rect.Contains(m));
            Hover(_tabAll, _tabAll.rect.Contains(m));
            Hover(_back, _back.rect.Contains(m));

            if (InputHub.PausePressed) { CloseBoards(); return; }
            if (!InputHub.MousePressed || _block > 0f) return;

            if (_tabDaily.rect.Contains(m)) { _tab = LbBoard.Daily; RefreshBoards(); Sfx.Play("click"); }
            else if (_tabMain.rect.Contains(m)) { _tab = LbBoard.Main; RefreshBoards(); Sfx.Play("click"); }
            else if (_tabAll.rect.Contains(m)) { _tab = LbBoard.AllTime; RefreshBoards(); Sfx.Play("click"); }
            else if (_back.rect.Contains(m)) CloseBoards();
        }

        // ================================================================ name prompt

        /// <summary>Called when a run ends: offers name entry when the score
        /// lands on one or more boards.</summary>
        public static void OfferScore(int score, int round, bool daily)
        {
            if (I == null || score <= 0) return;
            I._pendingBoards.Clear();
            if (daily && Lb.Qualifies(LbBoard.Daily, score)) I._pendingBoards.Add(LbBoard.Daily);
            if (!daily && Lb.Qualifies(LbBoard.Main, score)) I._pendingBoards.Add(LbBoard.Main);
            if (Lb.Qualifies(LbBoard.AllTime, score)) I._pendingBoards.Add(LbBoard.AllTime);
            // every finished run gets asked for a name; the boards keep their own top 10,
            // so a weak score falls off later instead of being silently dropped here
            if (I._pendingBoards.Count == 0) I._pendingBoards.Add(daily ? LbBoard.Daily : LbBoard.Main);

            I._pendingScore = score;
            I._pendingRound = round;
            I._pendingDaily = daily;
            I._slot = 0;
            I.LoadSavedName();
            I._nameOpen = true;
            I._block = 0.25f;
            I._namePanel.SetActive(true);
            I.RefreshPrompt();
            I.HookChars(true);
            Sfx.Play("click");
        }

        void LoadSavedName()
        {
            string saved = PlayerPrefs.GetString("pb_name", "");
            for (int i = 0; i < _chars.Length; i++)
            {
                char c = i < saved.Length ? char.ToUpperInvariant(saved[i]) : 'A';
                _chars[i] = Charset.IndexOf(c) >= 0 ? c : 'A';
            }
        }

        void RefreshPrompt()
        {
            int bestRank = 99;
            foreach (var b in _pendingBoards) bestRank = Mathf.Min(bestRank, Lb.RankIfAdded(b, _pendingScore));
            _nameTitle.Set(bestRank == 1 ? "NEW #1 SCORE!" : "NEW HIGH SCORE!");

            var parts = new List<string>();
            foreach (var b in _pendingBoards)
            {
                if (b == LbBoard.Daily) parts.Add("DAILY");
                else if (b == LbBoard.Main) parts.Add("MAIN");
                else parts.Add("ALL TIME");
            }
            _nameBoards.Set("TOP 10 - " + string.Join(" + ", parts.ToArray()));

            string mode = _pendingDaily ? "DAILY" : "MAIN";
            int allRank = Lb.RankIfAdded(LbBoard.AllTime, _pendingScore);
            _nameMode.Set(allRank > 0
                ? "ALL TIME #" + allRank + " - " + mode + " MODE"
                : "MODE " + mode);

            for (int i = 0; i < 3; i++)
            {
                bool sel = i == _slot;
                _slotText[i].Set(_chars[i].ToString());
                _slotText[i].SetColor(sel ? Pal.White : Pal.Alpha(Pal.Cream, 0.7f));
                _slotBox[i].transform.localScale = sel ? new Vector3(1.06f, 1.06f, 1f) : Vector3.one;
            }
        }

        void TickName()
        {
            if (_block > 0f) _block -= Time.unscaledDeltaTime;
            var m = PixelDisplay.I != null ? PixelDisplay.I.MouseWorld : Vector2.zero;
            var kb = Keyboard.current;

            // keyboard: arrows pick a slot/letter, enter saves, esc skips
            if (kb != null)
            {
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) { Save(); return; }
                if (kb.escapeKey.wasPressedThisFrame) { CloseName(); return; }
                if (kb.backspaceKey.wasPressedThisFrame) { if (_slot > 0) _slot--; RefreshPrompt(); }
                if (kb.leftArrowKey.wasPressedThisFrame) { if (_slot > 0) _slot--; RefreshPrompt(); }
                if (kb.rightArrowKey.wasPressedThisFrame) { if (_slot < 2) _slot++; RefreshPrompt(); }
            }

            Hover(_prev, _prev.rect.Contains(m));
            Hover(_next, _next.rect.Contains(m));
            Hover(_save, _save.rect.Contains(m));
            Hover(_skip, _skip.rect.Contains(m));
            if (!InputHub.MousePressed || _block > 0f) return;

            for (int i = 0; i < 3; i++)
                if (new Rect(-3.6f + i * 3.6f - 1.5f, 0.6f - 1.8f, 3.0f, 3.6f).Contains(m))
                {
                    _slot = i;
                    RefreshPrompt();
                    Sfx.Play("click");
                    return;
                }

            if (_prev.rect.Contains(m)) { Cycle(-1); }
            else if (_next.rect.Contains(m)) { Cycle(1); }
            else if (_save.rect.Contains(m)) Save();
            else if (_skip.rect.Contains(m)) CloseName();
        }

        void OnChar(char c)
        {
            if (!_nameOpen) return;
            char u = char.ToUpperInvariant(c);
            if (Charset.IndexOf(u) < 0) return;
            _chars[_slot] = u;
            if (_slot < 2) _slot++;
            RefreshPrompt();
            Sfx.Play("click");
        }

        void Cycle(int dir)
        {
            int idx = Charset.IndexOf(_chars[_slot]);
            idx = (idx + dir + Charset.Length) % Charset.Length;
            _chars[_slot] = Charset[idx];
            RefreshPrompt();
            Sfx.Play("click");
        }

        void HookChars(bool on)
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (on && !_charsHooked) { kb.onTextInput += OnChar; _charsHooked = true; }
            else if (!on && _charsHooked) { kb.onTextInput -= OnChar; _charsHooked = false; }
        }

        void Save() { SubmitName(new string(_chars)); }

        /// <summary>Puts the pending run on every board it qualified for and
        /// opens the most specific of them.</summary>
        public void SubmitName(string name)
        {
            if (string.IsNullOrEmpty(name)) name = "AAA";
            // exactly one row per run: the mode decides which boards it shows on,
            // so writing one row per pending board used to create duplicates
            string mode = _pendingDaily ? "DAILY" : "MAIN";
            Lb.Submit(LbBoard.AllTime, name, _pendingScore, _pendingRound, mode);
            PlayerPrefs.SetString("pb_name", name);
            PlayerPrefs.Save();
            Sfx.Play("save");

            LbBoard open = _pendingBoards.Contains(LbBoard.Daily) ? LbBoard.Daily
                         : _pendingBoards.Contains(LbBoard.Main) ? LbBoard.Main
                         : LbBoard.AllTime;
            CloseName(true);
            OpenBoards(open);
        }

        public void CloseName(bool silent = false)
        {
            _nameOpen = false;
            HookChars(false);
            if (_namePanel != null) _namePanel.SetActive(false);
            if (!silent) Sfx.Play("click");
        }

        /// <summary>Closes both surfaces, e.g. when the run restarts.</summary>
        public void CloseAll()
        {
            CloseBoards(true);
            CloseName(true);
        }

        // ================================================================ input

        void Update()
        {
            if (_nameOpen) { TickName(); return; }
            if (_boardsOpen) { TickBoards(); return; }
        }

        static void Hover(Brd b, bool over)
        {
            if (b == null || b.root == null) return;
            b.root.transform.localScale = over ? new Vector3(1.04f, 1.09f, 1f) : Vector3.one;
            if (b.label != null) b.label.SetColor(over ? Pal.White : Pal.Cream);
        }
    }
}
