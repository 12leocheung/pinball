using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pinball
{
    /// <summary>Two-layer menu system: the main menu (title, seed picker, controls) and the in-game pause menu.</summary>
    public class Menus : MonoBehaviour
    {
        public static Menus I;

        public bool Paused { get; private set; }
        public bool SeedEditing { get; private set; }
        public int SeedValue { get; private set; }

        GameObject _main;
        GameObject _pause;
        GameObject _controls;
        Transform _titleGroup;
        PixelText _seedLabel;
        PixelText _best;
        string _seedBuffer;
        bool _cursorOn;
        float _block;
        bool _controlsOpen;
        float _spark;

        // ---------------- unlocked items browser
        GameObject _items;
        GameObject _itemsGrid;
        PixelText _itemsCount;
        PixelText _itemsPageTxt;
        bool _itemsOpen;
        int _itemsPage;
        float _itemsWheelCool;
        class ItemRow
        {
            public GameObject root;
            public PixelText name;
            public PixelText desc;
            public PixelText stat;
        }
        readonly List<ItemRow> _itemRows = new List<ItemRow>();
        const int ItemsPerPage = 6;

        class Btn
        {
            public GameObject root;
            public Rect rect;
            public PixelText label;
        }

        Btn _play, _seed, _rnd, _mControls, _daily, _sandbox, _mItems, _mBoards;
        Btn _resume, _restart, _pControls, _soundBtn, _musicBtn, _fxBtn, _fullBtn, _quit;
        Btn _back, _itemsBack, _itemsPrev, _itemsNext;

        public void Build()
        {
            I = this;
            SeedValue = Random.Range(10000000, 100000000);
            var root = PixelDisplay.I.worldRoot;

            // ---------------- main menu
            _main = new GameObject("MainMenu");
            _main.transform.SetParent(root, false);
            var dim = SpriteBank.Quad(_main.transform, Vector2.zero, 64f, 44f, Pal.Alpha(Pal.Ink, 0.78f), 98);
            dim.name = "dim";

            _titleGroup = new GameObject("Title").transform;
            _titleGroup.SetParent(_main.transform, false);
            Ui.Text(_titleGroup, "PINBOUND", new Vector2(0f, 9.2f), Pal.Cream, 0.56f, TextAlign.Center, 100, true);
            Ui.Text(_titleGroup, "A PINBALL ROGUELIKE", new Vector2(0f, 5.6f), Pal.Lilac, 0.22f, TextAlign.Center, 100, true);
            SpriteBank.Make("rar_common", _titleGroup, 100, new Vector2(-6.2f, 5.6f)).transform.localScale = Vector3.one * 0.9f;
            SpriteBank.Make("rar_legendary", _titleGroup, 100, new Vector2(6.2f, 5.6f)).transform.localScale = Vector3.one * 0.9f;

            _best = Ui.Text(_main.transform, "", new Vector2(0f, -11.7f), Pal.Alpha(Pal.Sand, 0.9f), 0.13f, TextAlign.Center, 100, true);
            Ui.Text(_main.transform, "SAME SEED GIVES THE SAME SHOPS AND DROPS", new Vector2(0f, -12.6f), Pal.Alpha(Pal.Mauve, 0.95f), 0.08f, TextAlign.Center, 100);

            _play = MkBtn(_main.transform, "PLAY", new Vector2(0f, 0.4f), 9.4f, 2.0f, Pal.Gold);
            _rnd = MkBtn(_main.transform, "RND", new Vector2(-6.8f, -1.9f), 3.6f, 1.6f, Pal.Sky);
            _seed = MkBtn(_main.transform, "SEED", new Vector2(2.1f, -1.9f), 10.8f, 1.6f, Pal.Mint);
            _seedLabel = _seed.label;
            _sandbox = MkBtn(_main.transform, "SANDBOX", new Vector2(0f, -4.2f), 9.4f, 2.0f, Pal.Teal);
            _mItems = MkBtn(_main.transform, "ITEMS", new Vector2(0f, -6.5f), 9.4f, 1.6f, Pal.Gold);
            _mControls = MkBtn(_main.transform, "CONTROLS", new Vector2(0f, -8.3f), 9.4f, 1.6f, Pal.Lilac);
            // wide enough for the CLEARED variant of the label
            _daily = MkBtn(_main.transform, "DAILY RUN", new Vector2(0f, -10.1f), 13.2f, 1.6f, Pal.Red);
            // in the main stack, in the gap under the subtitle, above PLAY
            _mBoards = MkBtn(_main.transform, "LEADERBOARDS", new Vector2(0f, 2.7f), 9.4f, 1.6f, Pal.Lilac);

            // ---------------- pause
            _pause = new GameObject("PauseMenu");
            _pause.transform.SetParent(root, false);
            var dim2 = SpriteBank.Quad(_pause.transform, Vector2.zero, 64f, 44f, Pal.Alpha(Pal.Ink, 0.82f), 99);
            dim2.name = "dim";
            Ui.Text(_pause.transform, "PAUSED", new Vector2(0f, 4.9f), Pal.Rose, 0.3f, TextAlign.Center, 101, true);
            _resume = MkBtn(_pause.transform, "RESUME", new Vector2(0f, 2.9f), 9.4f, 1.6f, Pal.Gold);
            _restart = MkBtn(_pause.transform, "RESTART RUN", new Vector2(0f, 1.1f), 9.4f, 1.6f, Pal.Sky);
            _pControls = MkBtn(_pause.transform, "CONTROLS", new Vector2(0f, -0.7f), 9.4f, 1.6f, Pal.Lilac);
            _soundBtn = MkBtn(_pause.transform, "SOUND ON", new Vector2(0f, -2.5f), 9.4f, 1.6f, Pal.Mint);
            _musicBtn = MkBtn(_pause.transform, "MUSIC ON", new Vector2(0f, -4.3f), 9.4f, 1.6f, Pal.Teal);
            _fxBtn = MkBtn(_pause.transform, "FLASH ON", new Vector2(0f, -6.1f), 9.4f, 1.6f, Pal.Gold);
            _fullBtn = MkBtn(_pause.transform, "FULLSCREEN (F)", new Vector2(0f, -7.9f), 9.4f, 1.6f, Pal.Sky);
            _quit = MkBtn(_pause.transform, "MAIN MENU", new Vector2(0f, -9.7f), 9.4f, 1.6f, Pal.Rose);

            // ---------------- controls
            _controls = new GameObject("ControlsPanel");
            _controls.transform.SetParent(root, false);
            var dim3 = SpriteBank.Quad(_controls.transform, Vector2.zero, 64f, 44f, Pal.Alpha(Pal.Ink, 0.8f), 108);
            dim3.name = "dim";
            PixelBox.Draw(_controls.transform, Vector2.zero, 27f, 19.5f, Pal.Deep, Pal.Alpha(Pal.Plum, 1f), 109);
            Ui.Text(_controls.transform, "CONTROLS", new Vector2(0f, 7.9f), Pal.Cream, 0.26f, TextAlign.Center, 112, true);

            Row(_controls.transform, 5.6f, "ARROWS", "FLIPPERS. A OR D WORK TOO");
            Row(_controls.transform, 3.6f, "SPACE", "HOLD TO CHARGE THE LAUNCH");
            Row(_controls.transform, 1.6f, "Q / E", "NUDGE THE BOARD. THREE = TILT");
            Row(_controls.transform, -0.4f, "TOUCH", "SIDES FLIP. HOLD LAUNCH TO SHOOT");
            Row(_controls.transform, -2.4f, "TOUCH", "TAP CARDS TO BUY. DRAG ONTO THE BOARD");
            Row(_controls.transform, -4.4f, "R", "REROLL THE SHOP OFFERS");
            Row(_controls.transform, -6.4f, "PAUSE", "ESC OR HOLD TWO FINGERS. F = FULLSCreen");

            Ui.Text(_controls.transform, "SAME SEED GIVES THE SAME SHOPS AND DROPS", new Vector2(0f, -8.2f), Pal.Gold, 0.085f, TextAlign.Center, 112);
            _back = MkBtn(_controls.transform, "BACK", new Vector2(0f, -9.8f), 6.4f, 1.6f, Pal.Rose);

            // ---------------- unlocked items browser (from the main menu)
            _items = PixelBox.Draw(root, Vector2.zero, 27.6f, 22.6f, Pal.Deep, Pal.Alpha(Pal.Plum, 1f), 109);
            Ui.Text(_items.transform, "UNLOCKED", new Vector2(0f, 9.7f), Pal.Cream, 0.26f, TextAlign.Center, 112, true);
            _itemsCount = Ui.Text(_items.transform, "", new Vector2(0f, 8.2f), Pal.Alpha(Pal.Sand, 0.9f), 0.08f, TextAlign.Center, 112);
            _itemsGrid = new GameObject("ItemsGrid");
            _itemsGrid.transform.SetParent(_items.transform, false);
            // the items panel draws at 109, so its buttons need to sit above it
            _itemsPrev = MkBtn(_items.transform, "< PREV", new Vector2(-9.4f, -9.0f), 4.6f, 1.5f, Pal.Sky, 112);
            _itemsNext = MkBtn(_items.transform, "NEXT >", new Vector2(9.4f, -9.0f), 4.6f, 1.5f, Pal.Sky, 112);
            _itemsPageTxt = Ui.Text(_items.transform, "", new Vector2(0f, -9.0f), Pal.Alpha(Pal.Sand, 0.9f), 0.09f, TextAlign.Center, 112);
            _itemsBack = MkBtn(_items.transform, "BACK", new Vector2(0f, -11.1f), 6.4f, 1.5f, Pal.Rose, 112);
            _items.SetActive(false);

            HideAll();
        }

        static void Row(Transform parent, float y, string cap, string desc)
        {
            PixelBox.Draw(parent, new Vector2(-10.4f, y), 4.9f, 1.4f, Pal.Deep2, Pal.Alpha(Pal.Mauve, 1f), 110);
            Ui.Text(parent, cap, new Vector2(-10.4f, y), Pal.Cream, 0.082f, TextAlign.Center, 112);
            Ui.Text(parent, desc, new Vector2(-7.2f, y), Pal.Alpha(Pal.Sand, 0.95f), 0.085f, TextAlign.Left, 112);
        }

        static Btn MkBtn(Transform parent, string label, Vector2 pos, float w, float h, Color accent, int order = 101)
        {
            var b = new Btn();
            b.root = PixelBox.Draw(parent, pos, w, h, Pal.Deep2, accent, order);
            b.rect = new Rect(pos.x - w * 0.5f, pos.y - h * 0.5f, w, h);
            b.label = Ui.Text(b.root.transform, label, Vector2.zero, Pal.Cream, 0.115f, TextAlign.Center, order + 3, true);
            return b;
        }

        void Update()
        {
            if (I == null || _main == null) return;

            // F toggles fullscreen anywhere - web players expect this
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.fKey.wasPressedThisFrame) ToggleFullscreen();

            if (_titleGroup != null && _main.activeSelf)
            {
                _titleGroup.localPosition = new Vector3(0f, Mathf.Sin(Time.unscaledTime * 1.7f) * 0.3f, 0f);
                _spark -= Time.unscaledDeltaTime;
                if (_spark <= 0f)
                {
                    _spark = Random.Range(0.35f, 0.8f);
                    var c = new[] { Pal.Pink, Pal.Sky, Pal.Lilac, Pal.Gold }[Random.Range(0, 4)];
                    Juice.Spark(new Vector2(Random.Range(-14f, 14f), Random.Range(-9f, 9f)), c, 2, 3.5f, 0.55f);
                }
            }

            if (_block > 0f) _block -= Time.unscaledDeltaTime;
            bool mouse = InputHub.MousePressed && _block <= 0f;
            Vector2 m = PixelDisplay.I != null ? PixelDisplay.I.MouseWorld : Vector2.zero;
            var gm = GameManager.I;
            if (gm == null) return;

            // the leaderboard panel and its name prompt take all input while open
            if (LeaderboardUi.Busy) return;

            if (SeedEditing)
            {
                TickSeedEditing();
                if (InputHub.PausePressed || InputHub.EnterPressed)
                {
                    EndSeedEdit();
                    _block = 0.15f;
                }
                return;
            }

            if (_controlsOpen)
            {
                if ((mouse && _back.rect.Contains(m)) || InputHub.PausePressed || InputHub.EnterPressed)
                {
                    _block = 0.2f;
                    CloseControls();
                }
                return;
            }

            if (_itemsOpen)
            {
                TickItems(mouse, m);
                return;
            }

            if (InputHub.PausePressed)
            {
                if (Paused) { TogglePause(); return; }
                if (gm.Phase == Phase.Play || gm.Phase == Phase.Shop) { TogglePause(); return; }
                if (gm.Phase == Phase.GameOver) { _block = 0.2f; gm.EnterMenu(); return; }
            }

            if (Paused)
            {
                Hover(_resume, _resume.rect.Contains(m));
                Hover(_restart, _restart.rect.Contains(m));
                Hover(_pControls, _pControls.rect.Contains(m));
                Hover(_soundBtn, _soundBtn.rect.Contains(m));
                Hover(_musicBtn, _musicBtn.rect.Contains(m));
                Hover(_fxBtn, _fxBtn.rect.Contains(m));
                Hover(_fullBtn, _fullBtn.rect.Contains(m));
                Hover(_quit, _quit.rect.Contains(m));
                if (!mouse) return;
                if (_resume.rect.Contains(m)) TogglePause();
                else if (_restart.rect.Contains(m)) { bool wasSandbox = gm.Sandbox; TogglePause(); if (wasSandbox) gm.EndSandbox(); else gm.StartRun(gm.Seed); }
                else if (_pControls.rect.Contains(m)) { _block = 0.2f; OpenControls(); }
                else if (_soundBtn.rect.Contains(m)) { Sfx.Muted = !Sfx.Muted; RefreshSound(); Sfx.Play("click"); }
                else if (_musicBtn.rect.Contains(m)) { Music.Muted = !Music.Muted; RefreshSound(); Sfx.Play("click"); }
                else if (_fxBtn.rect.Contains(m)) { gm.ToggleCalm(); RefreshSound(); Sfx.Play("click"); }
                else if (_fullBtn.rect.Contains(m)) { ToggleFullscreen(); Sfx.Play("click"); }
                else if (_quit.rect.Contains(m)) { TogglePause(); gm.EnterMenu(); }
                return;
            }

            if (_main.activeSelf && gm.Phase == Phase.Menu)
            {
                Hover(_play, _play.rect.Contains(m));
                Hover(_rnd, _rnd.rect.Contains(m));
                Hover(_seed, _seed.rect.Contains(m));
                Hover(_mControls, _mControls.rect.Contains(m));
                Hover(_mItems, _mItems.rect.Contains(m));
                Hover(_daily, _daily.rect.Contains(m));
                Hover(_sandbox, _sandbox.rect.Contains(m));
                Hover(_mBoards, _mBoards.rect.Contains(m));
                if (!mouse && !InputHub.EnterPressed) return;
                if (mouse && _seed.rect.Contains(m))
                {
                    // no keyboard on a phone: tapping the seed rolls a fresh one instead
                    if (InputHub.UsedTouch)
                    {
                        SeedValue = Random.Range(10000000, 100000000);
                        RefreshSeedLabel();
                        Sfx.Play("click");
                    }
                    else StartSeedEdit();
                }
                else if (mouse && _rnd.rect.Contains(m))
                {
                    SeedValue = Random.Range(10000000, 100000000);
                    RefreshSeedLabel();
                    Sfx.Play("click");
                }
                else if (mouse && _mControls.rect.Contains(m)) { _block = 0.2f; OpenControls(); }
                else if (mouse && _mItems.rect.Contains(m)) { _block = 0.2f; OpenItems(); }
                else if (mouse && _mBoards.rect.Contains(m)) { _block = 0.2f; LeaderboardUi.I.OpenBoards(LbBoard.Main); }
                else if (mouse && _sandbox.rect.Contains(m)) StartSandboxFromMenu();
                else if (mouse && _daily.rect.Contains(m)) StartDaily();
                else if ((mouse && _play.rect.Contains(m)) || InputHub.EnterPressed) StartRunFromMenu();
            }
        }

        public static void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
            if (Screen.fullScreen)
                Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, true);
            else
                Screen.SetResolution(1280, 720, false);
        }

        public void TogglePause()
        {
            Paused = !Paused;
            Time.timeScale = Paused ? 0f : 1f;
            if (_restart != null && _restart.label != null && GameManager.I != null)
                _restart.label.Set(GameManager.I.Sandbox ? "END SESSION" : "RESTART RUN");
            _pause.SetActive(Paused);
            _block = 0.18f;
            Sfx.Play("click");
        }

        /// <summary>Swallow mouse input for a moment so a click that reveals the menu
        /// can't also fire the main-menu button underneath it in the same frame.</summary>
        public void BlockClicks(float seconds)
        {
            _block = Mathf.Max(_block, seconds);
        }

        public void HideAll()
        {
            Paused = false;
            Time.timeScale = 1f;
            _main.SetActive(false);
            _pause.SetActive(false);
            if (SeedEditing) EndSeedEdit();
            CloseControls(true);
            CloseItems(true);
            if (LeaderboardUi.I != null) LeaderboardUi.I.CloseAll();
        }

        public void ShowMain()
        {
            _main.SetActive(true);
            string b = "";
            if (GameManager.BestEver > 0) b = "BEST RUN " + Ui.Fmt(GameManager.BestEver);
            if (GameManager.BestRound > 1) b += (b.Length == 0 ? "BEST ROUND " : "   -   ROUND ") + GameManager.BestRound;
            _best.Set(b.Length > 0 ? b : "SURVIVE THE QUOTA");
            bool done = PlayerPrefs.GetInt("pb_daily", 0) == DailySeed();
            _daily.label.Set(done ? "DAILY RUN - CLEAR" : "DAILY RUN");
            _daily.label.SetColor(done ? Pal.Mint : Pal.Cream);
            // sandbox is a plain mode now: no credits, no lock
            _sandbox.label.Set("SANDBOX");
            _sandbox.label.SetColor(Pal.Cream);
            RefreshSeedLabel();
            RefreshSound();
        }

        void StartSandboxFromMenu()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            _block = 0.3f;
            Sfx.Play("buy");
            gm.StartSandbox();
        }

        public void StartRunFromMenu()
        {
            _block = 0.3f;
            Sfx.Play("buy");
            GameManager.I.StartRun(SeedValue);
        }

        int DailySeed()
        {
            var n = System.DateTime.Now;
            return n.Year * 10000 + n.Month * 100 + n.Day;
        }

        void StartDaily()
        {
            PlayerPrefs.SetInt("pb_daily", DailySeed());
            PlayerPrefs.Save();
            _block = 0.3f;
            Sfx.Play("buy");
            GameManager.I.StartRun(DailySeed(), true);
        }

        void OpenControls()
        {
            _controlsOpen = true;
            _controls.SetActive(true);
            Sfx.Play("click");
        }

        // ---------------- unlocked items browser ----------------

        List<object> UnlockedDefs()
        {
            var list = new List<object>();
            var gm = GameManager.I;
            if (gm == null || gm.UnlockedIds.Count == 0) return list;
            foreach (var it in Catalog.Items) if (gm.UnlockedIds.Contains(it.id)) list.Add(it);
            foreach (var w in Catalog.Wildcards) if (gm.UnlockedIds.Contains(w.id)) list.Add(w);
            return list;
        }

        void OpenItems()
        {
            _itemsOpen = true;
            _items.SetActive(true);
            _itemsPage = 0;
            RefreshItemsPage();
            Sfx.Play("click");
        }

        void CloseItems(bool silent = false)
        {
            _itemsOpen = false;
            if (_items != null) _items.SetActive(false);
            if (!silent) Sfx.Play("click");
        }

        void TickItems(bool mouse, Vector2 m)
        {
            var list = UnlockedDefs();
            int pages = Mathf.Max(1, Mathf.CeilToInt(list.Count / (float)ItemsPerPage));

            // mouse wheel pages through the list, same rhythm as the shop library
            _itemsWheelCool -= Time.unscaledDeltaTime;
            if (Mouse.current != null && _itemsWheelCool <= 0f)
            {
                float w = Mouse.current.scroll.ReadValue().y;
                if (w != 0f)
                {
                    _itemsWheelCool = 0.06f;
                    _itemsPage = Mathf.Clamp(_itemsPage + (w > 0f ? -1 : 1), 0, pages - 1);
                    RefreshItemsPage();
                }
            }

            Hover(_itemsPrev, _itemsPrev.rect.Contains(m));
            Hover(_itemsNext, _itemsNext.rect.Contains(m));
            Hover(_itemsBack, _itemsBack.rect.Contains(m));
            if (!mouse) return;
            if (_itemsBack.rect.Contains(m)) { _block = 0.2f; CloseItems(); }
            else if (_itemsPrev.rect.Contains(m)) { _itemsPage = Mathf.Clamp(_itemsPage - 1, 0, pages - 1); RefreshItemsPage(); }
            else if (_itemsNext.rect.Contains(m)) { _itemsPage = Mathf.Clamp(_itemsPage + 1, 0, pages - 1); RefreshItemsPage(); }
        }

        static string StatLine(BoardItemDef d)
        {
            switch (d.kind)
            {
                case ItemKind.Bumper: return "+" + d.value + " PTS ON EVERY HIT";
                case ItemKind.Trampoline: return d.id == "cannon"
                    ? "GRABS THE BALL, THEN FIRES IT UP"
                    : "+" + d.value + " PTS. THROWS THE BALL UP";
                case ItemKind.Coin: return "+1 COIN PER HIT. 3 PER ROUND";
                case ItemKind.Mult: return "+" + d.mult + " MULT ON EVERY HIT";
                case ItemKind.Magnet: return "+" + d.value + " PTS. PULLS THE BALL";
                case ItemKind.Lantern: return "+" + d.value + " PTS WHILE THE BALL IS NEAR";
                case ItemKind.Hold: return "CATCHES A BALL FOR THE NEXT ROUND";
                case ItemKind.Wall: return "SOLID. BINDS THE BALL";
                default: return "";
            }
        }

        void RefreshItemsPage()
        {
            var list = UnlockedDefs();
            int pages = Mathf.Max(1, Mathf.CeilToInt(list.Count / (float)ItemsPerPage));
            _itemsPage = Mathf.Clamp(_itemsPage, 0, pages - 1);
            _itemsCount.Set("YOU OWN " + list.Count + " OF " + (Catalog.Items.Length + Catalog.Wildcards.Length));
            _itemsPageTxt.Set("PAGE " + (_itemsPage + 1) + "/" + pages);

            foreach (var r in _itemRows)
                if (r.root != null) Destroy(r.root);
            _itemRows.Clear();

            int first = _itemsPage * ItemsPerPage;
            for (int i = 0; i < ItemsPerPage; i++)
            {
                int idx = first + i;
                if (idx >= list.Count) break;
                float y = 6.5f - i * 2.55f;
                var row = new ItemRow();
                row.root = PixelBox.Draw(_itemsGrid.transform, new Vector2(0f, y), 25.4f, 2.25f, Pal.Alpha(Pal.Deep2, 0.85f), Pal.Alpha(Pal.Plum, 0.8f), 111);
                row.root.name = "ItemRow" + i;

                SpriteRenderer icon;
                SpriteRenderer frame;
                string nm, ds, st;
                Color tint;
                var item = list[idx] as BoardItemDef;
                if (item != null)
                {
                    icon = SpriteBank.Make(item.sprite, row.root.transform, 113, new Vector2(-11.4f, 0f));
                    tint = Pal.RarityColor(item.rarity);
                    nm = item.name;
                    ds = item.desc;
                    st = StatLine(item);
                }
                else
                {
                    var wild = (WildcardDef)list[idx];
                    icon = SpriteBank.Make(wild.sprite, row.root.transform, 113, new Vector2(-11.4f, 0f));
                    tint = Pal.RarityColor(wild.rarity);
                    nm = wild.name;
                    ds = wild.ConditionText();
                    st = "GIVES " + wild.RewardText();
                }
                icon.transform.localScale = Vector3.one * 0.72f;
                frame = SpriteBank.Make("ui_solid", row.root.transform, 112, new Vector2(-12.6f, 0f), tint, "stripe");
                frame.transform.localScale = new Vector3(0.06f, 2.1f, 1f);
                row.name = Ui.Text(row.root.transform, nm, new Vector2(-10.2f, 0.62f), tint, 0.085f, TextAlign.Left, 113);
                row.desc = Ui.Text(row.root.transform, ds, new Vector2(-10.2f, -0.35f), Pal.Alpha(Pal.Sand, 0.92f), 0.062f, TextAlign.Left, 113);
                row.stat = Ui.Text(row.root.transform, st, new Vector2(-10.2f, -1.02f), Pal.Gold, 0.06f, TextAlign.Left, 113);
                _itemRows.Add(row);
            }
        }

        void CloseControls(bool silent = false)
        {
            _controlsOpen = false;
            if (_controls != null) _controls.SetActive(false);
            if (!silent) Sfx.Play("click");
        }

        void StartSeedEdit()
        {
            SeedEditing = true;
            _seedBuffer = "";
            _cursorOn = true;
            var kb = Keyboard.current;
            if (kb != null) kb.onTextInput += OnSeedChar;
            Sfx.Play("click");
            RefreshSeedLabel();
        }

        void OnSeedChar(char c)
        {
            if (char.IsDigit(c) && _seedBuffer.Length < 9) _seedBuffer += c;
        }

        void TickSeedEditing()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.backspaceKey.wasPressedThisFrame && _seedBuffer.Length > 0)
                _seedBuffer = _seedBuffer.Substring(0, _seedBuffer.Length - 1);
            _cursorOn = Mathf.Repeat(Time.unscaledTime * 2.5f, 1f) < 0.55f;
            RefreshSeedLabel();
        }

        void EndSeedEdit()
        {
            var kb = Keyboard.current;
            if (kb != null) kb.onTextInput -= OnSeedChar;
            SeedEditing = false;
            int v;
            if (int.TryParse(_seedBuffer, out v) && v > 0) SeedValue = v;
            _seedBuffer = "";
            RefreshSeedLabel();
            Sfx.Play("click");
        }

        void RefreshSeedLabel()
        {
            if (_seedLabel == null) return;
            if (SeedEditing) _seedLabel.Set("SEED " + _seedBuffer + (_cursorOn ? "_" : ""), Pal.White);
            else _seedLabel.Set("SEED " + SeedValue, Pal.Cream);
        }

        void RefreshSound()
        {
            if (_soundBtn != null && _soundBtn.label != null)
                _soundBtn.label.Set(Sfx.Muted ? "SOUND OFF" : "SOUND ON");
            if (_musicBtn != null && _musicBtn.label != null)
                _musicBtn.label.Set(Music.Muted ? "MUSIC OFF" : "MUSIC ON");
            if (_fxBtn != null && _fxBtn.label != null)
                _fxBtn.label.Set(Juice.Calm ? "FLASH OFF" : "FLASH ON");
        }

        static void Hover(Btn b, bool over)
        {
            if (b == null || b.root == null) return;
            b.root.transform.localScale = over ? new Vector3(1.04f, 1.09f, 1f) : Vector3.one;
            if (b.label != null) b.label.SetColor(over ? Pal.White : Pal.Cream);
        }
    }
}
