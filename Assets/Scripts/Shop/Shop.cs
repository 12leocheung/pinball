using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pinball
{
    public class Shop : MonoBehaviour
    {
        class Offer
        {
            public bool isWild;
            public BoardItemDef item;
            public WildcardDef wdef;
            public int price;
            public bool sold;
        }

        class CardView
        {
            public GameObject root;
            public SpriteRenderer bg;
            public SpriteRenderer border;
            public SpriteRenderer icon;
            public SpriteRenderer gem;
            public SpriteRenderer coin;
            public SpriteRenderer soldOverlay;
            public PixelText name;
            public PixelText price;
            public PixelText soldText;
            public PixelText rarity;
            public Offer offer;
        }

        class BagRow
        {
            public GameObject root;
            public SpriteRenderer icon;
            public SpriteRenderer gem;
            public PixelText name;
            public PixelText kind;
            public GameObject empty;
        }

        Transform _root;
        readonly List<CardView> _cards = new List<CardView>();
        readonly List<BagRow> _bagRows = new List<BagRow>();
        readonly List<Offer> _offers = new List<Offer>();
        readonly List<SpriteRenderer> _wildSlots = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _wildIcons = new List<SpriteRenderer>();
        readonly List<CellView> _cells = new List<CellView>();

        class CellView
        {
            public Vector2Int cell;
            public SpriteRenderer sr;
        }

        PixelText _coinText;
        SpriteRenderer _coinIcon;
        GameObject _rerollRoot;
        PixelText _shopHeader;
        PixelText _libTabItems;
        PixelText _libTabWild;
        PixelText _tooltip;
        PixelText _result;
        PixelText _bagHeader;
        PixelText _sellText;
        PixelText _continueText;
        PixelText _rerollText;
        GameObject _sellBox;
        SpriteRenderer _sellBg;
        SpriteRenderer _continueBg;
        SpriteRenderer _rerollBg;
        GameObject _gridRoot;
        SpriteRenderer _ghost;
        SpriteRenderer _hoverHighlight;

        bool _active;
        int _rerollCost = 2;
        int _earned;
        float _inputLock;
        float _wheelCool;

        class Drag
        {
            public bool active;
            public BoardItemDef item;
            public WildcardDef wild;
            public Vector2Int sourceCell = new Vector2Int(-1, -1);
            public int wildIndex = -1;
            public Vector2Int hoverCell = new Vector2Int(-1, -1);
        }

        readonly Drag _drag = new Drag();

        enum PressKind
        {
            None, Continue, Reroll, Sell, Card, Library, LibTabItems, LibTabWilds, Bag, Wild, Placed
        }

        // press arms a drag, a hold or a click; only the release decides which one it
        // was. The shop used to buy on press, which made descriptions unreadable on a
        // phone and turned every tap into a drag that dropped back in the bag.
        const float HoldTime = 0.4f;
        const float LibRowStep = 5.25f;
        PressKind _press = PressKind.None;
        int _pressIndex = -1;
        Vector2Int _pressCell;
        Vector2 _pressPos;
        float _pressAt;
        bool _pressDragged, _pressHeld;

        // sandbox library: the whole catalogue, browsable, nothing random
        readonly List<BoardItemDef> _libItems = new List<BoardItemDef>();
        readonly List<WildcardDef> _libWilds = new List<WildcardDef>();
        bool _libWild;
        int _libRow, _libStartRow, _libShownRow = -1;

        static readonly Rect LibItemsTabRect = R(RightX - 3.1f, 11.4f, 6.2f, 2.2f);
        static readonly Rect LibWildTabRect = R(RightX + 3.1f, 11.4f, 6.2f, 2.2f);

        int LibraryCount { get { return _libWild ? _libWilds.Count : _libItems.Count; } }
        int MaxLibRow { get { return Mathf.Max(0, (LibraryCount + 1) / 2 - 3); } }

        /// <summary>About a dozen screen pixels expressed in world units, so finger
        /// wobble on a small phone screen never reads as a drag.</summary>
        static float DragSlop()
        {
            var d = PixelDisplay.I;
            float w = d != null && d.screenRect != null ? d.screenRect.sizeDelta.x : 1920f;
            return Mathf.Clamp(12f * PixelDisplay.WorldW / Mathf.Max(1f, w), 0.3f, 2.6f);
        }

        static Rect R(float cx, float cy, float w, float h)
        {
            return new Rect(cx - w * 0.5f, cy - h * 0.5f, w, h);
        }

        const float LeftX = -15.7f;
        const float RightX = 15.7f;
        static readonly Rect BoardRect = R(0f, 0f, 14.6f, 22.6f);
        static readonly Rect SellRect = R(15.7f, -9.05f, 12.4f, 2.4f);
        static readonly Rect ContinueRect = R(15.7f, -11.4f, 12.4f, 1.8f);
        static readonly Rect RerollRect = R(-15.7f, -11.4f, 12.4f, 1.8f);
        // the six bag rows: a drop here always returns a tile to the inventory
        static readonly Rect BagRect = R(LeftX, 0.85f, 12.2f, 17.6f);

        public void Build()
        {
            _root = new GameObject("ShopUI").transform;
            _root.SetParent(PixelDisplay.I.worldRoot, false);

            // panels
            PixelBox.Draw(_root, new Vector2(LeftX, 0f), 13.2f, 25.4f, Pal.Deep, Pal.Alpha(Pal.Plum, 0.9f), 40);
            PixelBox.Draw(_root, new Vector2(RightX, 0f), 13.2f, 25.4f, Pal.Deep, Pal.Alpha(Pal.Plum, 0.9f), 40);
            PixelBox.Draw(_root, new Vector2(LeftX, 11.4f), 12.4f, 2.2f, Pal.Deep2, Pal.Alpha(Pal.Mauve, 0.8f), 42);
            PixelBox.Draw(_root, new Vector2(RightX, 11.4f), 12.4f, 2.2f, Pal.Deep2, Pal.Alpha(Pal.Mauve, 0.8f), 42);
            _bagHeader = Ui.Text(_root, "BAG", new Vector2(LeftX, 11.4f), Pal.Cream, 0.13f, TextAlign.Center, 50, true);
            _shopHeader = Ui.Text(_root, "SHOP", new Vector2(RightX, 11.4f), Pal.Cream, 0.13f, TextAlign.Center, 50, true);
            // the sandbox has no stock to sell: its header bar becomes two library tabs
            _libTabItems = Ui.Text(_root, "ITEMS", new Vector2(RightX - 3.1f, 11.4f), Pal.Cream, 0.105f, TextAlign.Center, 50, true);
            _libTabWild = Ui.Text(_root, "WILDCARDS", new Vector2(RightX + 3.1f, 11.4f), Pal.Cream, 0.08f, TextAlign.Center, 50, true);
            _libTabItems.gameObject.SetActive(false);
            _libTabWild.gameObject.SetActive(false);

            // coins live inside the BAG header bar now, clear of the bag rows below
            _coinText = Ui.Text(_root, "0", new Vector2(LeftX + 5.9f, 11.4f), Pal.Gold, 0.09f, TextAlign.Right, 51, true);
            _coinIcon = SpriteBank.Make("coin", _root, 51, new Vector2(LeftX + 3.9f, 11.4f));
            _coinIcon.transform.localScale = Vector3.one * 0.8f;

            // bag rows
            for (int i = 0; i < 6; i++)
            {
                float y = 8.35f - i * 3.0f;
                var row = new BagRow();
                row.root = PixelBox.Draw(_root, new Vector2(LeftX, y), 12.2f, 2.6f, Pal.Alpha(Pal.Deep2, 0.8f), Pal.Alpha(Pal.Plum, 0.95f), 44);
                row.icon = SpriteBank.Make("item_spring", row.root.transform, 46, new Vector2(-4.7f, 0f), Color.white, "icon");
                row.icon.transform.localScale = Vector3.one * 0.62f;
                row.gem = SpriteBank.Make("rar_common", row.root.transform, 47, new Vector2(-6.0f, 1.06f), Color.white, "gem");
                row.name = Ui.Text(row.root.transform, "", new Vector2(-3.3f, 0.36f), Pal.Cream, 0.09f, TextAlign.Left, 47);
                row.kind = Ui.Text(row.root.transform, "", new Vector2(-3.3f, -0.66f), Pal.Alpha(Pal.Sand, 0.85f), 0.075f, TextAlign.Left, 47);
                row.empty = Ui.Text(row.root.transform, "EMPTY", new Vector2(-3.2f, -0.1f), Pal.Alpha(Pal.Mauve, 0.8f), 0.075f, TextAlign.Left, 47).gameObject;
                _bagRows.Add(row);
            }

            _result = Ui.Text(_root, "", new Vector2(LeftX, -8.4f), Pal.Cream, 0.075f, TextAlign.Center, 47);

            // reroll
            var rrBox = PixelBox.Draw(_root, new Vector2(RerollRect.center.x, RerollRect.center.y), RerollRect.width, RerollRect.height, Pal.Deep2, Pal.Alpha(Pal.Mint, 0.9f), 46);
            _rerollRoot = rrBox;
            _rerollBg = rrBox.GetComponent<SpriteRenderer>();
            if (_rerollBg == null) _rerollBg = rrBox.transform.GetChild(0).GetComponent<SpriteRenderer>();
            _rerollText = Ui.Text(rrBox.transform, "REROLL  2", Vector2.zero, Pal.Mint, 0.125f, TextAlign.Center, 50, true);

            // cards
            for (int i = 0; i < 6; i++)
            {
                int col = i % 2;
                int rowIndex = i / 2;
                float x = 12.4f + col * 6.6f;
                float y = 7.55f - rowIndex * 5.25f;
                var card = new CardView();
                card.root = new GameObject("card" + i);
                card.root.transform.SetParent(_root, false);
                card.root.transform.localPosition = new Vector3(x, y, 0f);

                var bg = SpriteBank.Make("ui_solid", card.root.transform, 44, Vector2.zero, Pal.Deep2, "bg");
                bg.transform.localScale = new Vector3(6.1f, 4.9f, 1f);
                card.bg = bg;
                card.border = PixelBox.AddBorder(card.root.transform, 6.1f, 4.9f, Pal.Alpha(Pal.Mauve, 0.9f), 45, 0.1f);

                card.icon = SpriteBank.Make("item_spring", card.root.transform, 47, new Vector2(0f, 0.95f), Color.white, "icon");
                card.icon.transform.localScale = Vector3.one * 0.72f;
                card.gem = SpriteBank.Make("rar_common", card.root.transform, 48, new Vector2(-2.7f, 2.05f), Color.white, "gem");
                card.name = Ui.Text(card.root.transform, "", new Vector2(0f, -0.5f), Pal.Cream, 0.07f, TextAlign.Center, 50);
                card.coin = SpriteBank.Make("coin", card.root.transform, 50, new Vector2(-0.75f, -1.82f), Color.white, "coin");
                card.coin.transform.localScale = Vector3.one * 0.75f;
                // pixel 0.125 = exactly 2 RT texels per font texel: crisp digits (0.11..0.15 are fractional and mush)
                // pixel 0.125 = exactly 2 RT texels per font texel: crisp digits (0.11..0.15 are fractional and mush)
                card.price = Ui.Text(card.root.transform, "0", new Vector2(1.15f, -1.8f), Pal.Gold, 0.125f, TextAlign.Center, 51, true);
                card.rarity = Ui.Text(card.root.transform, "", new Vector2(2.75f, 2.05f), Pal.Alpha(Pal.Sand, 0.9f), 0.06f, TextAlign.Right, 50);

                var sold = SpriteBank.Make("ui_solid", card.root.transform, 52, Vector2.zero, Pal.Alpha(Pal.Ink, 0.72f), "sold");
                sold.transform.localScale = new Vector3(6.1f, 4.9f, 1f);
                card.soldOverlay = sold;
                card.soldText = Ui.Text(card.root.transform, "SOLD", Vector2.zero, Pal.Alpha(Pal.Rose, 0.9f), 0.14f, TextAlign.Center, 53, true);
                _cards.Add(card);
            }

            // tooltip
            var tip = PixelBox.Draw(_root, new Vector2(RightX, -6.7f), 12.4f, 2.1f, Pal.Alpha(Pal.Ink, 0.6f), Pal.Alpha(Pal.Plum, 1f), 44);
            _tooltip = Ui.Text(tip.transform, "", new Vector2(0f, 0.7f), Pal.Alpha(Pal.Sand, 0.95f), 0.068f, TextAlign.Center, 48);

            // sell slot
            _sellBox = PixelBox.Draw(_root, new Vector2(RightX, -9.05f), 12.4f, 2.4f, Pal.Alpha(Pal.Deep2, 0.85f), Pal.Alpha(Pal.Rose, 0.9f), 46);
            _sellBg = _sellBox.transform.GetChild(0).GetComponent<SpriteRenderer>();
            var slotArt = SpriteBank.Make("card_slot", _sellBox.transform, 47, Vector2.zero);
            slotArt.transform.localScale = new Vector3(6.2f, 1.3f, 1f);
            slotArt.color = Pal.Alpha(Pal.Rose, 0.55f);
            _sellText = Ui.Text(_sellBox.transform, "SELL SLOT - DRAG HERE", Vector2.zero, Pal.Alpha(Pal.Pink, 0.95f), 0.08f, TextAlign.Center, 48, true);

            // continue
            var goBox = PixelBox.Draw(_root, new Vector2(ContinueRect.center.x, ContinueRect.center.y), ContinueRect.width, ContinueRect.height, Pal.Deep2, Pal.Alpha(Pal.Gold, 0.95f), 46);
            _continueBg = goBox.transform.GetChild(0).GetComponent<SpriteRenderer>();
            _continueText = Ui.Text(goBox.transform, "NEXT ROUND", Vector2.zero, Pal.Gold, 0.11f, TextAlign.Center, 50, true);

            // wildcard strip above the board
            for (int i = 0; i < 5; i++)
            {
                float x = -5.6f + i * 2.8f;
                var slot = SpriteBank.Make("card_slot", _root, 44, new Vector2(x, 12.35f), Pal.Alpha(Pal.Plum, 0.55f), "wildslot" + i);
                slot.transform.localScale = new Vector3(1.7f, 1.7f, 1f);
                _wildSlots.Add(slot);
                var icon = SpriteBank.Make("wild_moon", _root, 46, new Vector2(x, 12.35f), Color.white, "wildicon" + i);
                icon.transform.localScale = Vector3.one * 0.55f;
                _wildIcons.Add(icon);
            }

            // placement grid (row 0 is never placeable - no cells drawn there)
            _gridRoot = new GameObject("Grid");
            _gridRoot.transform.SetParent(_root, false);
            for (int c = 0; c < BoardGrid.Cols; c++)
                for (int r = 1; r < BoardGrid.Rows; r++)
                {
                    var pos = BoardGrid.Center(c, r);
                    var sr = SpriteBank.Make("grid_cell", _gridRoot.transform, 15, pos);
                    sr.color = Pal.Alpha(Pal.Cream, 0.16f);
                    _cells.Add(new CellView { cell = new Vector2Int(c, r), sr = sr });
                }
            _gridRoot.SetActive(false);

            _ghost = SpriteBank.Make("item_spring", _root, 82, Vector2.zero);
            _ghost.transform.localScale = Vector3.one * 0.75f;
            _ghost.gameObject.SetActive(false);

            _hoverHighlight = SpriteBank.Make("ui_solid", _root, 16, Vector2.zero, Pal.Alpha(Pal.Mint, 0.22f));
            _hoverHighlight.transform.localScale = new Vector3(1.9f, 1.9f, 1f);
            _hoverHighlight.gameObject.SetActive(false);

            _libItems.Clear();
            _libItems.AddRange(Catalog.Items);
            _libWilds.Clear();
            _libWilds.AddRange(Catalog.Wildcards);

            _root.gameObject.SetActive(false);
        }

        public void Show(int earned)
        {
            _active = true;
            _earned = earned;
            _rerollCost = 2;
            _inputLock = 0.25f;
            _root.gameObject.SetActive(true);
            _press = PressKind.None;
            _pressDragged = _pressHeld = false;
            var gm = GameManager.I;
            if (gm != null && gm.Sandbox)
            {
                _libWild = false;
                _libRow = _libStartRow = _libShownRow = 0;
                _wheelCool = 0f;
                _offers.Clear(); // sandbox never rolls stock; stale shop offers must not survive
            }
            else RerollOffers();
            Refresh();
            _drag.active = false;
            _ghost.gameObject.SetActive(false);
            _gridRoot.SetActive(false);
            _hoverHighlight.gameObject.SetActive(false);
        }

        public void Hide()
        {
            _active = false;
            _drag.active = false;
            _press = PressKind.None;
            if (_root != null) _root.gameObject.SetActive(false);
            if (_ghost != null) _ghost.gameObject.SetActive(false);
            if (_gridRoot != null) _gridRoot.SetActive(false);
        }

        public void Lock(float t) { _inputLock = Mathf.Max(_inputLock, t); }

        void RerollOffers()
        {
            _offers.Clear();
            var gm = GameManager.I;
            for (int i = 0; i < 6; i++)
            {
                var o = new Offer();
                bool wantWild = gm.Rng.NextDouble() < 0.3;
                if (wantWild)
                {
                    var w = Catalog.RandomWildcard(gm.Rng);
                    o.isWild = true;
                    o.wdef = w;
                    o.price = gm.Sandbox ? 0 : w.price;
                }
                if (!o.isWild)
                {
                    o.item = Catalog.RandomItem(gm.Rng);
                    o.price = gm.Sandbox ? 0 : o.item.price;
                }
                _offers.Add(o);
            }
        }

        void Refresh()
        {
            var gm = GameManager.I;
            _coinText.Set(Ui.Fmt(gm.Coins));
            _coinText.gameObject.SetActive(!gm.Sandbox);
            _coinIcon.gameObject.SetActive(!gm.Sandbox);
            _rerollRoot.SetActive(!gm.Sandbox);
            _bagHeader.Set("BAG " + gm.Inventory.Count + "/6");
            _shopHeader.gameObject.SetActive(!gm.Sandbox);
            _libTabItems.gameObject.SetActive(gm.Sandbox);
            _libTabWild.gameObject.SetActive(gm.Sandbox);

            if (gm.Sandbox)
            {
                _libTabItems.SetColor(_libWild ? Pal.Alpha(Pal.Mauve, 0.9f) : Pal.Cream);
                _libTabWild.SetColor(_libWild ? Pal.Cream : Pal.Alpha(Pal.Mauve, 0.9f));
                _rerollText.SetColor(Pal.Mint);
                BindLibrary();
            }
            else
            {
                _rerollText.Set("REROLL  " + _rerollCost);
                _rerollText.SetColor(gm.Coins >= _rerollCost ? Pal.Mint : Pal.Mauve);
                for (int i = 0; i < _cards.Count; i++)
                {
                    var card = _cards[i];
                    var o = _offers[i];
                    card.offer = o;
                    card.root.SetActive(true);
                    bool wild = o.isWild;
                    var rarity = wild ? o.wdef.rarity : o.item.rarity;
                    card.icon.sprite = SpriteBank.Get(wild ? o.wdef.sprite : o.item.sprite);
                    card.icon.color = o.sold ? new Color(0.4f, 0.38f, 0.45f, 0.6f) : Color.white;
                    card.name.Set(wild ? Ui.Wrap(o.wdef.name, 12) : Ui.Wrap(o.item.name, 12));
                    card.name.SetColor(o.sold ? Pal.Mauve : Pal.Cream);
                    card.price.Set(o.sold ? "" : o.price.ToString());
                    card.price.SetColor(gm.Coins >= o.price ? Pal.Gold : Pal.Rose);
                    card.gem.sprite = SpriteBank.Get("rar_" + rarity.ToString().ToLowerInvariant());
                    card.rarity.Set(""); // the little corner label is gone
                    card.bg.color = o.sold ? Pal.Ink : Color.Lerp(Pal.Deep2, Pal.RarityColor(rarity), 0.12f);
                    card.soldOverlay.gameObject.SetActive(o.sold);
                    card.soldText.gameObject.SetActive(o.sold);
                    if (card.border != null) card.border.color = o.sold ? Pal.Alpha(Pal.Mauve, 0.4f) : Pal.RarityColor(rarity);
                }
            }

            for (int i = 0; i < _bagRows.Count; i++)
            {
                var row = _bagRows[i];
                bool has = i < gm.Inventory.Count;
                row.empty.SetActive(!has);
                row.icon.gameObject.SetActive(has);
                row.gem.gameObject.SetActive(has);
                row.name.gameObject.SetActive(has);
                row.kind.gameObject.SetActive(has);
                if (!has) continue;
                var it = gm.Inventory[i];
                row.icon.sprite = SpriteBank.Get(it.sprite);
                row.gem.sprite = SpriteBank.Get("rar_" + it.rarity.ToString().ToLowerInvariant());
                row.name.Set(it.name);
                row.kind.Set("DRAG TO PLACE");
            }

            for (int i = 0; i < _wildSlots.Count; i++)
            {
                bool has = i < gm.Wildcards.Count;
                _wildIcons[i].gameObject.SetActive(has);
                _wildSlots[i].color = has ? Pal.Alpha(Pal.RarityColor(gm.Wildcards[i].def.rarity), 0.55f) : Pal.Alpha(Pal.Plum, 0.4f);
                if (!has) continue;
                var w = gm.Wildcards[i];
                _wildIcons[i].sprite = SpriteBank.Get(w.def.sprite);
            }

            _continueText.Set(gm.Sandbox ? "BACK TO TABLE" : "NEXT ROUND");
            _sellText.Set("SELL SLOT - DRAG HERE");
            if (gm.Sandbox)
                _result.Set("SANDBOX - FREE PLAY\nEVERYTHING IS FREE\nFIVE WILDCARD SLOTS\nSEED " + gm.Seed);
            else
                _result.Set("ROUND " + gm.Round + " CLEARED\n+" + _earned + " COINS   SCORE " + Ui.Fmt(gm.Score) +
                            "\nNEXT QUOTA " + Ui.Fmt(gm.QuotaFor(gm.Round + 1)));
        }

        public void Tick()
        {
            if (!_active) return;
            if (Menus.I != null && Menus.I.Paused) return;
            if (_inputLock > 0f) { _inputLock -= Time.unscaledDeltaTime; _press = PressKind.None; return; }
            var m = PixelDisplay.I.MouseWorld;

            if (_drag.active)
            {
                UpdateDrag(m);
                if (InputHub.MouseReleased) EndDrag(m);
                return;
            }

            // sandbox library: scroll the mouse wheel through the catalogue rows
            if (GameManager.I.Sandbox)
            {
                _wheelCool -= Time.unscaledDeltaTime;
                if (Mouse.current != null && _wheelCool <= 0f)
                {
                    float w = Mouse.current.scroll.ReadValue().y;
                    if (w != 0f)
                    {
                        _wheelCool = 0.06f;
                        int newRow = Mathf.Clamp(_libRow + (w > 0f ? -1 : 1), 0, MaxLibRow);
                        if (newRow != _libRow) { _libRow = newRow; Sfx.Play("click"); BindLibrary(); }
                    }
                }
            }

            UpdateHover(m);

            // a press only arms: moving makes it a drag, holding makes it a read,
            // letting go without either makes it a click
            if (InputHub.MousePressed) { BeginPress(m); return; }
            if (_press != PressKind.None) { AdvancePress(m); return; }
            if (InputHub.RerollPressed) DoReroll();
        }

        void BeginPress(Vector2 m)
        {
            var gm = GameManager.I;
            _press = PressKind.None;
            _pressIndex = -1;
            _pressPos = m;
            _pressAt = Time.unscaledTime;
            _pressDragged = false;
            _pressHeld = false;

            if (ContinueRect.Contains(m)) { _press = PressKind.Continue; return; }
            if (RerollRect.Contains(m)) { _press = PressKind.Reroll; return; }
            if (SellRect.Contains(m)) { _press = PressKind.Sell; return; }

            if (gm.Sandbox)
            {
                if (LibItemsTabRect.Contains(m)) { _press = PressKind.LibTabItems; return; }
                if (LibWildTabRect.Contains(m)) { _press = PressKind.LibTabWilds; return; }
            }

            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                if (card.root == null || !card.root.activeSelf) continue;
                var rect = R(card.root.transform.localPosition.x, card.root.transform.localPosition.y, 6.1f, 4.9f);
                if (!rect.Contains(m)) continue;
                if (gm.Sandbox)
                {
                    int lib = _libRow * 2 + i;
                    if (lib >= LibraryCount) return;
                    _press = PressKind.Library;
                    _pressIndex = lib;
                    _libStartRow = _libRow;
                }
                else
                {
                    _press = PressKind.Card;
                    _pressIndex = i;
                }
                return;
            }

            for (int i = 0; i < _bagRows.Count; i++)
            {
                if (i >= gm.Inventory.Count) break;
                var rect = R(_bagRows[i].root.transform.localPosition.x, _bagRows[i].root.transform.localPosition.y, 12.2f, 2.6f);
                if (rect.Contains(m)) { _press = PressKind.Bag; _pressIndex = i; return; }
            }

            for (int i = 0; i < gm.Wildcards.Count; i++)
            {
                var rect = R(_wildSlots[i].transform.localPosition.x, _wildSlots[i].transform.localPosition.y, 1.9f, 2.1f);
                if (rect.Contains(m)) { _press = PressKind.Wild; _pressIndex = i; return; }
            }

            if (BoardRect.Contains(m))
            {
                var cell = BoardGrid.Nearest(m);
                if (gm.Table.ModuleAt(cell) != null) { _press = PressKind.Placed; _pressCell = cell; }
            }
        }

        /// <summary>How far a finger has to travel before a press counts as a drag.
        /// A card needs a deliberate swipe: on a phone a few pixels of wobble while
        /// reading it used to read as a drag, which bought the item before the hold
        /// ever registered. A tile already on the board follows the finger at once.</summary>
        float PressSlop()
        {
            if (_press == PressKind.Placed || _press == PressKind.Wild) return DragSlop();
            return DragSlop() * 2.6f;
        }

        /// <summary>Nearest cell a tile can actually go into. Row 0 hangs over the
        /// flippers and is never placeable, so a drop in the lower band of the board
        /// snaps up into row 1 instead of silently bouncing back to the bag.</summary>
        static Vector2Int DropCell(Vector2 world)
        {
            var cell = BoardGrid.Nearest(world);
            cell.y = Mathf.Clamp(cell.y, 1, BoardGrid.Rows - 1);
            return cell;
        }

        void AdvancePress(Vector2 m)
        {
            if (!_pressDragged && (m - _pressPos).magnitude > PressSlop()) _pressDragged = true;
            if (!_pressHeld && Time.unscaledTime - _pressAt >= HoldTime) _pressHeld = true;

            if (_pressDragged)
            {
                switch (_press)
                {
                    case PressKind.Bag: StartDragItem(_pressIndex); break;
                    case PressKind.Wild: StartDragWildcard(_pressIndex); break;
                    case PressKind.Placed: StartDragPlaced(_pressCell); break;
                    case PressKind.Card:
                        BuyOffer(_pressIndex, true);
                        // a blocked buy (coins, bag, wildcard slot) must not retry
                        // every frame of the drag or the toast is spammed
                        if (!_drag.active) _press = PressKind.None;
                        break;
                    case PressKind.Library:
                        // a swipe across the grid walks the library: sliding up reveals
                        // later rows, the way a touch list is expected to move
                        _libRow = Mathf.Clamp(_libStartRow + Mathf.RoundToInt((m.y - _pressPos.y) / LibRowStep), 0, MaxLibRow);
                        if (_libRow != _libShownRow) BindLibrary();
                        break;
                }
            }

            if (_drag.active)
            {
                UpdateDrag(m);
                if (InputHub.MouseReleased) { _press = PressKind.None; EndDrag(m); }
                return;
            }

            if (!InputHub.MouseReleased) return;
            var kind = _press;
            int index = _pressIndex;
            _press = PressKind.None;
            // holding means "let me read this" - it never also fires the action
            if (!_pressHeld && !_pressDragged) Click(kind, index);
        }

        void Click(PressKind kind, int index)
        {
            var gm = GameManager.I;
            switch (kind)
            {
                case PressKind.Continue:
                    Sfx.Play("click");
                    if (gm.Sandbox) gm.CloseSandboxShop();
                    else { Hide(); gm.ContinueToNextRound(); }
                    break;
                case PressKind.Reroll:
                    if (gm.Sandbox) PageLibrary();
                    else DoReroll();
                    break;
                case PressKind.Card:
                    BuyOffer(index, false);
                    break;
                case PressKind.Library:
                    TakeLibrary(index);
                    break;
                case PressKind.LibTabItems:
                    if (_libWild) { _libWild = false; _libRow = 0; Sfx.Play("click"); Refresh(); }
                    break;
                case PressKind.LibTabWilds:
                    if (!_libWild) { _libWild = true; _libRow = 0; Sfx.Play("click"); Refresh(); }
                    break;
            }
        }

        void StartDragPlaced(Vector2Int cell)
        {
            var gm = GameManager.I;
            var mod = gm.Table.ModuleAt(cell);
            if (mod == null) return;
            var def = mod.def;
            gm.Table.RemoveModule(cell);
            Sfx.Play("click");
            _drag.active = true;
            _drag.item = def;
            _drag.wild = null;
            _drag.sourceCell = cell;
            _drag.wildIndex = -1;
            _ghost.sprite = SpriteBank.Get(def.sprite);
            _ghost.gameObject.SetActive(true);
            _gridRoot.SetActive(true);
            Juice.Float(mod.transform.position, "PICKED UP", Pal.Sand, 1f, 0.9f, 1.6f);
            Refresh();
        }

        void BuyOffer(int index, bool thenDrag)
        {
            if (index < 0 || index >= _offers.Count) return;
            var o = _offers[index];
            var gm = GameManager.I;
            if (o.sold) return;
            if (!gm.Sandbox && gm.Coins < o.price)
            {
                Juice.Float(new Vector2(0f, 10f), "NOT ENOUGH COINS", Pal.Rose, 1.1f, 1.1f, 1.8f);
                Juice.Shake(0.08f);
                return;
            }
            if (!o.isWild && gm.Inventory.Count >= 6)
            {
                Juice.Float(new Vector2(0f, 10f), "BAG IS FULL", Pal.Rose, 1.1f, 1.1f, 1.8f);
                return;
            }
            if (o.isWild && gm.Wildcards.Count >= 5)
            {
                Juice.Float(new Vector2(0f, 10f), "WILDCARD SLOTS FULL",
                            Pal.Rose, 1.05f, 1.1f, 1.8f);
                return;
            }

            if (!gm.Sandbox) gm.Coins -= o.price;
            o.sold = true;
            Sfx.Play("buy");
            Juice.Spark(new Vector2(o.isWild ? 0f : 12f, 6f), Pal.Gold, 8, 8f);
            Juice.Flash(0.06f);

            if (o.isWild)
            {
                gm.Unlock(o.wdef.id);
                gm.AddWildcard(o.wdef);
                Refresh();
                return;
            }

            gm.Unlock(o.item.id);
            gm.Inventory.Add(o.item);
            // a drag that started on the card buys and keeps hold of the tile; a tap
            // just drops it in the bag so the next touch can place it
            if (thenDrag) StartDragItem(gm.Inventory.Count - 1);
            else Refresh();
        }

        /// <summary>Sandbox only: the library is the whole catalogue, free to take.</summary>
        void TakeLibrary(int index)
        {
            var gm = GameManager.I;
            if (index < 0) return;
            if (_libWild)
            {
                if (index >= _libWilds.Count) return;
                if (gm.Wildcards.Count >= 5)
                {
                    Juice.Float(new Vector2(0f, 10f), "WILDCARD SLOTS FULL", Pal.Rose, 1.05f, 1.1f, 1.8f);
                    return;
                }
                gm.Unlock(_libWilds[index].id);
                gm.AddWildcard(_libWilds[index]);
                Sfx.Play("buy");
                Juice.Spark(new Vector2(0f, 6f), Pal.Gold, 8, 8f);
                Juice.Flash(0.06f);
                Refresh();
                return;
            }

            if (index >= _libItems.Count) return;
            if (gm.Inventory.Count >= 6)
            {
                Juice.Float(new Vector2(0f, 10f), "BAG IS FULL", Pal.Rose, 1.1f, 1.1f, 1.8f);
                return;
            }
            gm.Unlock(_libItems[index].id);
            gm.Inventory.Add(_libItems[index]);
            Sfx.Play("buy");
            Juice.Spark(new Vector2(12f, 6f), Pal.Gold, 8, 8f);
            Refresh();
        }

        void PageLibrary()
        {
            if (MaxLibRow <= 0) return;
            _libRow = (_libRow + 3) % (MaxLibRow + 1);
            Sfx.Play("click");
            BindLibrary();
        }

        void BindLibrary()
        {
            _libShownRow = _libRow;
            int count = LibraryCount;
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                int idx = _libRow * 2 + i;
                bool has = idx < count;
                if (card.root.activeSelf != has) card.root.SetActive(has);
                if (!has) continue;
                var def = _libWild ? null : _libItems[idx];
                var wdef = _libWild ? _libWilds[idx] : null;
                var rarity = _libWild ? wdef.rarity : def.rarity;
                card.offer = null;
                card.icon.sprite = SpriteBank.Get(_libWild ? wdef.sprite : def.sprite);
                card.icon.color = Color.white;
                card.name.Set(Ui.Wrap(_libWild ? wdef.name : def.name, 12));
                card.name.SetColor(Pal.Cream);
                card.price.Set("");
                card.gem.sprite = SpriteBank.Get("rar_" + rarity.ToString().ToLowerInvariant());
                card.rarity.Set(""); // no corner type text
                card.bg.color = Color.Lerp(Pal.Deep2, Pal.RarityColor(rarity), 0.12f);
                card.soldOverlay.gameObject.SetActive(false);
                card.soldText.gameObject.SetActive(false);
                if (card.border != null) card.border.color = Pal.RarityColor(rarity);
            }
            int first = _libRow * 2 + 1;
            int last = Mathf.Min(count, _libRow * 2 + 6);
            _rerollText.Set(count == 0 ? "LIBRARY EMPTY" : first + "-" + last + " / " + count);
        }

        void DoReroll()
        {
            var gm = GameManager.I;
            if (gm.Sandbox) { PageLibrary(); return; }
            if (gm.Coins < _rerollCost) return;
            gm.Coins -= _rerollCost;
            _rerollCost += 1;
            RerollOffers();
            Refresh();
            Sfx.Play("click");
            Juice.Shake(0.07f);
        }

        void StartDragItem(int index)
        {
            var gm = GameManager.I;
            if (index < 0 || index >= gm.Inventory.Count) return;
            var def = gm.Inventory[index];
            gm.Inventory.RemoveAt(index);
            Sfx.Play("click");
            _drag.active = true;
            _drag.item = def;
            _drag.wild = null;
            _drag.sourceCell = new Vector2Int(-1, -1);
            _drag.wildIndex = -1;
            _ghost.sprite = SpriteBank.Get(def.sprite);
            _ghost.gameObject.SetActive(true);
            _gridRoot.SetActive(true);
            Refresh();
        }

        void StartDragWildcard(int index)
        {
            var gm = GameManager.I;
            if (index < 0 || index >= gm.Wildcards.Count) return;
            var w = gm.Wildcards[index];
            gm.Wildcards.RemoveAt(index);
            Sfx.Play("click");
            _drag.active = true;
            _drag.wild = w.def;
            _drag.item = null;
            _drag.wildIndex = index;
            _drag.sourceCell = new Vector2Int(-1, -1);
            _ghost.sprite = SpriteBank.Get(w.def.sprite);
            _ghost.gameObject.SetActive(true);
            _gridRoot.SetActive(false);
            Refresh();
        }

        void UpdateDrag(Vector2 m)
        {
            _ghost.transform.position = new Vector3(m.x, m.y, -0.3f);
            _ghost.color = new Color(1f, 1f, 1f, 0.9f);

            bool overSell = SellRect.Contains(m);
            _sellText.Set(overSell ? "RELEASE TO SELL" : "SELL SLOT - DRAG HERE");
            _sellBg.color = overSell ? Pal.Alpha(Pal.Rose, 0.55f) : Pal.Alpha(Pal.Deep2, 0.85f);

            if (_drag.item != null)
            {
                bool overBoard = BoardRect.Contains(m);
                _hoverHighlight.gameObject.SetActive(overBoard);
                if (overBoard)
                {
                    var cell = DropCell(m);
                    _drag.hoverCell = cell;
                    var pos = BoardGrid.Center(cell);
                    _hoverHighlight.transform.position = new Vector3(pos.x, pos.y, 0f);
                    bool free = GameManager.I.Table.IsCellFree(cell);
                    _hoverHighlight.color = free ? Pal.Alpha(Pal.Mint, 0.35f) : Pal.Alpha(Pal.Rose, 0.35f);
                    _ghost.transform.localScale = free ? Vector3.one * 0.75f : Vector3.one * 0.6f;
                    _ghost.color = new Color(1f, free ? 1f : 0.7f, free ? 1f : 0.7f, 0.9f);
                }
                else _ghost.transform.localScale = Vector3.one * 0.75f;

                foreach (var cv in _cells)
                {
                    bool free = GameManager.I.Table.IsCellFree(cv.cell);
                    bool hover = cv.cell == _drag.hoverCell;
                    if (hover) cv.sr.color = Pal.Alpha(free ? Pal.Mint : Pal.Rose, 0.6f);
                    else cv.sr.color = free ? Pal.Alpha(Pal.Cream, 0.16f) : Pal.Alpha(Pal.Rose, 0.1f);
                }
            }
            else
            {
                _hoverHighlight.gameObject.SetActive(false);
            }
        }

        void EndDrag(Vector2 m)
        {
            var gm = GameManager.I;
            _ghost.gameObject.SetActive(false);
            _gridRoot.SetActive(false);
            _hoverHighlight.gameObject.SetActive(false);
            _drag.active = false;
            _sellBg.color = Pal.Alpha(Pal.Deep2, 0.85f);

            if (SellRect.Contains(m))
            {
                Sfx.Play("sell");
                if (_drag.wild != null)
                {
                    int value = gm.SellValue(_drag.wild.price);
                    gm.Coins += value;
                    Juice.Float(SellRect.center, "+" + value + " COINS", Pal.Gold, 1.2f, 1.2f, 2f);
                    Juice.Ring(SellRect.center, Pal.Gold, 0.8f, 3f, 0.4f);
                    _drag.wild = null;
                }
                else if (_drag.item != null)
                {
                    int value = gm.SellValue(_drag.item.price);
                    gm.Coins += value;
                    Juice.Float(SellRect.center, "+" + value + " COINS", Pal.Gold, 1.2f, 1.2f, 2f);
                    Juice.Ring(SellRect.center, Pal.Gold, 0.8f, 3f, 0.4f);
                    _drag.item = null;
                }
                Refresh();
                return;
            }

            if (_drag.wild != null)
            {
                // wildcards only live in the strip: put it back where it was
                int index = Mathf.Clamp(_drag.wildIndex, 0, gm.Wildcards.Count);
                gm.Wildcards.Insert(index, new WildcardInstance { def = _drag.wild });
                _drag.wild = null;
                Refresh();
                return;
            }

            if (_drag.item != null)
            {
                bool droppedInBag = BagRect.Contains(m);
                bool droppedOnBoard = !droppedInBag && BoardRect.Contains(m);
                if (droppedOnBoard)
                {
                    var cell = DropCell(m);
                    if (gm.Table.IsCellFree(cell))
                    {
                        gm.Table.PlaceModule(_drag.item, cell);
                        Juice.Shake(0.12f);
                        _drag.item = null;
                        Refresh();
                        return;
                    }
                }
                // a drop over the bag rows always returns the tile to the inventory -
                // even when it came from the board and its cell is still open, which is
                // what used to snap board tiles straight back out of the bag
                if (gm.Inventory.Count < 6)
                {
                    gm.Inventory.Add(_drag.item);
                }
                else if (_drag.sourceCell.x >= 0 && gm.Table.IsCellFree(_drag.sourceCell))
                {
                    gm.Table.PlaceModule(_drag.item, _drag.sourceCell);
                }
                else
                {
                    Juice.Float(new Vector2(0f, 10f), "BAG IS FULL", Pal.Rose, 1.05f, 1.1f, 1.6f);
                    _drag.item = null; // board slot opened again by the drag start
                    Refresh();
                    return;
                }
                _drag.item = null;
                Refresh();
            }
        }

        void UpdateHover(Vector2 m)
        {
            string tip = "";
            bool sandbox = GameManager.I.Sandbox;
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                if (card.root == null || !card.root.activeSelf) continue;
                var rect = R(card.root.transform.localPosition.x, card.root.transform.localPosition.y, 6.1f, 4.9f);
                if (!rect.Contains(m)) continue;
                if (sandbox)
                {
                    int idx = _libRow * 2 + i;
                    if (idx >= LibraryCount) break;
                    if (_libWild)
                    {
                        var w = _libWilds[idx];
                        tip = w.name + "\n" + Ui.Wrap(w.ConditionText(), 26) + "\nREWARD " + w.RewardText();
                    }
                    else
                    {
                        var it = _libItems[idx];
                        tip = it.name + "\n" + Ui.Wrap(it.desc, 26);
                    }
                    break;
                }
                var o = _offers[i];
                if (o.isWild)
                    tip = o.wdef.name + "\n" + Ui.Wrap(o.wdef.ConditionText(), 26) + "\nREWARD " + o.wdef.RewardText();
                else
                    tip = o.item.name + "\n" + Ui.Wrap(o.item.desc, 26);
                break;
            }
            if (tip == "")
            {
                for (int i = 0; i < _bagRows.Count && i < GameManager.I.Inventory.Count; i++)
                {
                    var rect = R(_bagRows[i].root.transform.localPosition.x, _bagRows[i].root.transform.localPosition.y, 12.2f, 2.6f);
                    if (!rect.Contains(m)) continue;
                    var it = GameManager.I.Inventory[i];
                    tip = it.name + "\n" + Ui.Wrap(it.desc, 26);
                    break;
                }
            }
            if (tip == "")
            {
                for (int i = 0; i < GameManager.I.Wildcards.Count; i++)
                {
                    var rect = R(_wildSlots[i].transform.localPosition.x, _wildSlots[i].transform.localPosition.y, 1.9f, 2.1f);
                    if (!rect.Contains(m)) continue;
                    var w = GameManager.I.Wildcards[i];
                    tip = w.def.name + "\n" + Ui.Wrap(w.def.ConditionText(), 26) + "\nSELL FOR " + GameManager.I.SellValue(w.def.price) + "c";
                    break;
                }
            }
            if (tip == "" && SellRect.Contains(m)) tip = "DRAG A TILE HERE TO SELL IT\nFOR HALF ITS VALUE";
            if (tip == "" && ContinueRect.Contains(m)) tip = sandbox ? "BACK TO THE TABLE" : "NEXT ROUND.\nTHE QUOTA GROWS";
            if (tip == "" && RerollRect.Contains(m)) tip = sandbox ? "PAGE THROUGH THE LIBRARY\nSIX AT A TIME" : "NEW OFFERS\nFOR " + _rerollCost + " COINS";
            if (tip == "" && sandbox) tip = "TAP TO TAKE   HOLD TO READ";
            _tooltip.Set(tip);
        }
    }
}
