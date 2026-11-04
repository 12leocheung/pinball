using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Pinball.EditorTools
{
    /// <summary>
    /// Scene-view starter board editor (dev tool).
    ///
    /// Open with Tools/Pinball/Starter Board Editor. It builds a real copy of the
    /// play table in the scene (the same PinballTable.Build the game runs), so what
    /// you see is the actual board. Editing is shop-style: pick BUMPER / PIN /
    /// SMALL PIN in the panel, a ghost follows the cursor and snaps to the field,
    /// green means valid / red means rejected, release to place. Click an existing
    /// fixture to pick it up and move it, right-click to erase.
    ///
    /// PinballTable.Build lays the field out from Assets/Resources/StarterBoard.asset,
    /// so every run plays exactly the layout saved here. An empty asset keeps the
    /// table's built-in default field.
    /// </summary>
    public class StarterBoardEditor
    {
        const string RootName = "StarterBoardPreview";
        const string AssetPath = "Assets/Resources/StarterBoard.asset";

        // fixture catalogue (art keys come from the PixelArtLibrary)
        class FixtureInfo
        {
            public string kind, label, spriteKey, note;
            public float grab;      // cursor distance that grabs/picks this fixture
            public float radius;    // collider radius, for spacing rules
            public float ghostScale;
            public Vector2 highlightSize;
        }

        static readonly FixtureInfo[] Palette =
        {
            new FixtureInfo { kind = "bumper", label = "Bumper", spriteKey = "bumper_b",
                note = "60 pts - reserves the grid cells around it", grab = 1.15f, radius = 1.08f, ghostScale = 0.9f, highlightSize = new Vector2(2.6f, 2.6f) },
            new FixtureInfo { kind = "pin", label = "Pin", spriteKey = "peg",
                note = "5 pts peg - anywhere on the field", grab = 0.5f, radius = 0.36f, ghostScale = 1f, highlightSize = new Vector2(1.1f, 1.1f) },
            new FixtureInfo { kind = "smallpin", label = "Small pin", spriteKey = "peg",
                note = "compact peg for tighter spots", grab = 0.4f, radius = 0.25f, ghostScale = 0.7f, highlightSize = new Vector2(0.8f, 0.8f) },
        };

        static FixtureInfo InfoFor(string kind)
        {
            foreach (var f in Palette) if (f.kind == kind) return f;
            return Palette[0];
        }

        static PinballTable _preview;
        static StarterBoard _board;
        static bool _active;
        static bool _eraseMode;
        static string _dragKind;                 // null = not dragging
        static int _moveIndex = -1;              // >=0 = moving the asset entry at this index
        static bool _justPicked;                 // true between palette pickup and the release of that same click
        static Vector2 _moveOldPos;
        static string _status = "";
        static bool _statusOk = true;
        static Vector2 _scroll;
        static Rect _panelRect;

        // shop-style world visuals (the exact sprites the in-game shop uses)
        GameObject _gridRoot;
        SpriteRenderer _ghost;
        SpriteRenderer _hoverHighlight;
        SpriteRenderer _eraseHighlight;
        readonly List<PinballTablePreviewCell> _cells = new List<PinballTablePreviewCell>();

        readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
        Texture2D _px;

        static readonly StarterBoardEditor I = new StarterBoardEditor();

        [MenuItem("Tools/Pinball/Starter Board Editor")]
        public static void Open() { Begin(false); }

        [MenuItem("Tools/Pinball/Stop Editing Starter Board")]
        public static void Close() { End(); }

        [InitializeOnLoadMethod]
        static void Auto()
        {
            EditorApplication.playModeStateChanged -= OnPlayState;
            EditorApplication.playModeStateChanged += OnPlayState;
            EditorApplication.delayCall += () =>
            {
                var stale = FindPreview(); // survives a domain reload; rebuild for fresh art/materials
                if (stale != null) Object.DestroyImmediate(stale.gameObject);
                if (!EditorApplication.isPlayingOrWillChangePlaymode) Begin(true);
            };
        }

        static void OnPlayState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode && _active) End();
        }

        static PinballTable FindPreview()
        {
            // hidden from the hierarchy, so Find() can't be used
            foreach (var t in Resources.FindObjectsOfTypeAll<PinballTable>())
                if (t != null && t.gameObject != null && t.gameObject.name == RootName && !EditorApplication.isPlaying)
                    return t;
            return null;
        }

        static void Begin(bool quiet)
        {
            if (_active) return;
            if (EditorApplication.isPlaying)
            {
                _status = "Stop play mode first - the board preview needs edit mode.";
                _statusOk = false;
                return;
            }

            EnsureBoardAsset();
            EnsureSeeded();
            I.EnsurePreview();
            SceneView.duringSceneGui -= I.OnSceneGui;
            SceneView.duringSceneGui += I.OnSceneGui;
            _active = true;
            _dragKind = null;
            _moveIndex = -1;
            _eraseMode = false;
            _status = "Pick BUMPER / PIN / SMALL PIN in the panel, drag onto the playfield.";
            _statusOk = true;
            var sv = SceneView.lastActiveSceneView;
            if (sv != null) sv.LookAt(Vector3.zero, sv.rotation, 13.5f);
            if (!quiet) Debug.Log("Starter board editor: drag a fixture from the panel onto the playfield. Click a placed one to move it; right-click erases. PinballTable.Build reads this layout.");
        }

        /// <summary>Verification hook: forces a fresh preview build from the saved asset.</summary>
        public static void EditorTestRefresh()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Starter board preview requires edit mode."); return; }
            if (!_active) { Begin(true); return; }
            var stale = FindPreview();
            if (stale != null) Object.DestroyImmediate(stale.gameObject);
            _preview = null;
            I._cells.Clear();
            I.EnsurePreview();
        }

        static void End()
        {
            SceneView.duringSceneGui -= I.OnSceneGui;
            _active = false;
            _dragKind = null;
            _moveIndex = -1;
            var root = FindPreview();
            if (root != null) Object.DestroyImmediate(root.gameObject);
            I._cells.Clear();
            I._gridRoot = null;
            I._ghost = null;
            I._hoverHighlight = null;
            I._eraseHighlight = null;
            _preview = null;
            if (_board != null) AssetDatabase.SaveAssets();
            SceneView.RepaintAll();
        }

        // ---------------------------------------------------------------- assets

        static void EnsureBoardAsset()
        {
            _board = StarterBoard.Instance;
            if (_board != null) return;
            var existing = AssetDatabase.LoadAssetAtPath<StarterBoard>(AssetPath);
            if (existing != null) { _board = existing; return; }
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            _board = ScriptableObject.CreateInstance<StarterBoard>();
            AssetDatabase.CreateAsset(_board, AssetPath);
        }

        /// <summary>First open: seed the asset with the table's built-in layout so there is something real to edit.</summary>
        static void EnsureSeeded()
        {
            if (_board == null || _board.HasFixtures) return;
            Undo.RecordObject(_board, "Starter Board - Seed Defaults");
            _board.fixtures.AddRange(StarterBoard.DefaultFixtures());
            EditorUtility.SetDirty(_board);
            SetStatus("Seeded the table's default layout - edit away.", true);
        }

        // ------------------------------------------------------------- preview

        void EnsurePreview()
        {
            var existing = FindPreview();
            if (existing != null) { _preview = existing; return; }

            var root = new GameObject(RootName);
            root.hideFlags = HideFlags.HideAndDontSave;
            _preview = root.AddComponent<PinballTable>();
            _preview.Build(); // the same Build the game runs - fixture layout from the asset

            // shop-style drag visuals, same sprites as Shop.cs
            _gridRoot = new GameObject("Grid");
            _gridRoot.transform.SetParent(_preview.BoardRoot, false);
            for (int r = 0; r < BoardGrid.Rows; r++)
            {
                for (int c = 0; c < BoardGrid.Cols; c++)
                {
                    var pos = BoardGrid.Center(c, r);
                    var sr = SpriteBank.Make("grid_cell", _gridRoot.transform, 15, pos);
                    _cells.Add(new PinballTablePreviewCell { cell = new Vector2Int(c, r), sr = sr });
                }
            }
            _gridRoot.SetActive(false);

            _ghost = SpriteBank.Make("peg", _preview.BoardRoot, 82, Vector2.zero);
            _ghost.gameObject.SetActive(false);

            _hoverHighlight = SpriteBank.Make("ui_solid", _preview.BoardRoot, 16, Vector2.zero, Pal.Alpha(Pal.Mint, 0.22f));
            _hoverHighlight.gameObject.SetActive(false);

            _eraseHighlight = SpriteBank.Make("ui_solid", _preview.BoardRoot, 16, Vector2.zero, Pal.Alpha(Pal.Rose, 0.45f));
            _eraseHighlight.gameObject.SetActive(false);
        }

        static void HideRecursive(GameObject go)
        {
            go.hideFlags = HideFlags.HideAndDontSave;
            foreach (Transform child in go.transform) HideRecursive(child.gameObject);
        }

        void Rebuild()
        {
            var root = _preview != null ? _preview.gameObject : (FindPreview() != null ? FindPreview().gameObject : null);
            if (root != null) Object.DestroyImmediate(root.gameObject);
            _preview = null;
            _cells.Clear();
            _gridRoot = null;
            _ghost = null;
            _hoverHighlight = null;
            _eraseHighlight = null;
            EnsurePreview();
        }

        // -------------------------------------------------------------- scene gui

        void OnSceneGui(SceneView sv)
        {
            if (!_active || _preview == null || _board == null) return;
            var e = Event.current;
            if (e == null) return;

            _panelRect = new Rect(10f, 10f, 296f, 388f);
            Handles.BeginGUI();
            try
            {
                DrawPanel(e);
                HandleSceneEvents(sv, e);
                UpdateVisuals(e);
            }
            finally
            {
                Handles.EndGUI();
            }
        }

        // ----------------------------------------------------------- shop events

        void HandleSceneEvents(SceneView sv, Event e)
        {
            if (e.type == EventType.Used) return; // consumed by the panel GUI - the drag just started may live on

            switch (e.type)
            {
                case EventType.MouseDrag:
                    if (_dragKind != null) { e.Use(); sv.Repaint(); }
                    break;

                case EventType.MouseUp:
                    if (e.button == 0 && _dragKind != null)
                    {
                        if (_justPicked && _panelRect.Contains(e.mousePosition))
                        {
                            // this up only ends the pickup click on the panel - the drag keeps going
                            _justPicked = false;
                            e.Use();
                            sv.Repaint();
                            break;
                        }
                        Commit(e.mousePosition);
                        e.Use();
                        sv.Repaint();
                    }
                    break;

                case EventType.MouseDown:
                    if (e.button == 0 && _dragKind != null)
                    {
                        // the placing click: consume it so the scene view doesn't rubber-band select
                        if (!_panelRect.Contains(e.mousePosition)) { _justPicked = false; e.Use(); }
                    }
                    else if (e.button == 0 && _dragKind == null)
                    {
                        if (_eraseMode) { if (EraseNearest(e.mousePosition)) { e.Use(); sv.Repaint(); } }
                        else if (TryPickup(e.mousePosition)) { e.Use(); sv.Repaint(); }
                    }
                    else if (e.button == 1 && _dragKind == null)
                    {
                        if (EraseNearest(e.mousePosition)) { e.Use(); sv.Repaint(); }
                    }
                    break;

                case EventType.KeyDown:
                    if (e.keyCode == KeyCode.Escape && _dragKind != null)
                    {
                        _dragKind = null;
                        _moveIndex = -1;
                        _justPicked = false;
                        SetStatus("Drag cancelled.", true);
                        e.Use();
                        sv.Repaint();
                    }
                    break;

                case EventType.MouseMove:
                    sv.Repaint();
                    break;
            }
        }

        bool TryPickup(Vector2 mouse)
        {
            if (_panelRect.Contains(mouse)) return false;
            var w = PointOnBoard(mouse);
            if (Mathf.Abs(w.x) > 100f) return false;
            int idx;
            var f = _board.Nearest(w, 1.3f, out idx);
            if (f == null) return false;
            if (Vector2.Distance(f.Pos, w) > InfoFor(f.kind).grab) return false;
            _dragKind = f.kind;
            _moveIndex = idx;
            _moveOldPos = f.Pos;
            SetStatus("Moving " + InfoFor(f.kind).label.ToLower() + " - release to commit, Escape to cancel.", true);
            return true;
        }

        void Commit(Vector2 mouse)
        {
            var kind = _dragKind;
            var moving = _moveIndex >= 0;
            var index = _moveIndex;
            _dragKind = null;
            _moveIndex = -1;
            _justPicked = false;

            if (_panelRect.Contains(mouse))
            {
                SetStatus("Drop cancelled - released back on the panel.", true);
                return;
            }

            var w = PointOnBoard(mouse);
            if (Mathf.Abs(w.x) > 100f)
            {
                SetStatus("Drop cancelled - release over the playfield.", true);
                return;
            }

            var pos = Snap(w);
            string why;
            if (!CanPlace(kind, pos, moving ? index : -1, out why))
            {
                SetStatus(InfoFor(kind).label + " rejected at (" + pos.x + ", " + pos.y + ") - " + why, false);
                return; // nothing changed, no rebuild needed
            }

            if (moving)
            {
                Undo.RecordObject(_board, "Starter Board - Move " + kind);
                _board.SetFixture(index, kind, pos);
                SetStatus(InfoFor(kind).label + " moved to (" + pos.x + ", " + pos.y + ").", true);
            }
            else
            {
                Undo.RecordObject(_board, "Starter Board - Place " + kind);
                _board.AddFixture(kind, pos);
                SetStatus(InfoFor(kind).label + " placed at (" + pos.x + ", " + pos.y + ").", true);
            }
            EditorUtility.SetDirty(_board);
            Rebuild();
        }

        bool EraseNearest(Vector2 mouse)
        {
            if (_panelRect.Contains(mouse)) return false;
            var w = PointOnBoard(mouse);
            if (Mathf.Abs(w.x) > 100f) return false;
            int idx;
            var f = _board.Nearest(w, 1.3f, out idx);
            if (f == null) return false;
            if (Vector2.Distance(f.Pos, w) > InfoFor(f.kind).grab) return false;
            Undo.RecordObject(_board, "Starter Board - Erase " + f.kind);
            _board.RemoveAt(idx);
            EditorUtility.SetDirty(_board);
            SetStatus("Removed " + InfoFor(f.kind).label.ToLower() + " at (" + f.x + ", " + f.y + ").", true);
            Rebuild();
            return true;
        }

        // -------------------------------------------------------------- rules

        /// <summary>
        /// Same table rules the game layout respects: off the walls and shooter lane,
        /// above the flippers, bumpers clear of each other, no two fixtures overlapping.
        /// </summary>
        bool CanPlace(string kind, Vector2 pos, int excludeIndex, out string why)
        {
            bool isBumper = kind == "bumper";
            bool isSmall = kind == "smallpin";
            float minY = -6.5f;
            float minX, maxX, maxY;
            if (isBumper) { minX = -5.8f; maxX = 3.6f; maxY = 9.7f; }
            else { minX = -6.3f; maxX = 3.9f; maxY = 10.4f; }
            if (pos.x < minX || pos.x > maxX) { why = "stays clear of the side walls and the shooter lane"; return false; }
            if (pos.y < minY || pos.y > maxY) { why = "stays between the flippers and the top wall"; return false; }

            for (int i = 0; i < _board.fixtures.Count; i++)
            {
                if (i == excludeIndex) continue;
                var f = _board.fixtures[i];
                float d = Vector2.Distance(f.Pos, pos);
                bool otherBumper = f.kind == "bumper";
                // bumpers may sit in a tight row: only reject true overlaps (1.08 + 1.08 shares)
                // plus a sliver of margin, since the ball (radius 0.9) flows around a row anyway
                if (isBumper && otherBumper && d < 2.25f) { why = "too close to another bumper"; return false; }
                float minGap = isBumper && otherBumper ? 2.25f
                    : isBumper || otherBumper ? 1.08f + (isBumper ? 1.08f : (isSmall ? 0.25f : 0.36f)) + 0.2f
                    : InfoFor(kind).radius + InfoFor(f.kind).radius + 0.35f;
                if (d < minGap)
                {
                    why = isBumper || otherBumper
                        ? "too close to a " + (otherBumper ? "bumper" : "pin")
                        : "too close to another pin";
                    return false;
                }
            }

            // no placement may wall the ball in: there must always be a route from
            // the lane mouth to the drain (this rejects e.g. 5 pins in a row)
            if (!RouteToDrain(_board.fixtures, excludeIndex, pos, kind))
            {
                why = "seals the ball out of the playfield - keep a gap the ball can roll through";
                return false;
            }

            why = null;
            return true;
        }

        /// <summary>
        /// Builds a coarse 0.1-unit occupancy map of the table (fixtures at collider
        /// size, outer frame, shooter lane wall, one-way gate) and flood-fills from
        /// the lane mouth region to the drain slot between the flippers. Returns
        /// false when the layout leaves no route, i.e. the ball is walled in.
        /// </summary>
        static bool RouteToDrain(List<Pinball.StarterBoard.Fixture> fixtures, int excludeIndex, Vector2 extraPos, string extraKind)
        {
            const float BallR = 0.9f;     // ball collider radius
            const float Step = 0.1f;
            const int Cols = 140;         // -7..7 x -11..11 at 0.1 resolution
            const int Rows = 220;

            var pts = new List<Vector2>(fixtures.Count + 1);
            var radii = new List<float>(fixtures.Count + 1);
            for (int i = 0; i < fixtures.Count; i++)
            {
                if (i == excludeIndex) continue;
                pts.Add(fixtures[i].Pos);
                radii.Add(InfoFor(fixtures[i].kind).radius);
            }
            if (extraKind != null)
            {
                pts.Add(extraPos);
                radii.Add(InfoFor(extraKind).radius);
            }

            var blocked = new bool[Cols * Rows];
            for (int j = 0; j < Rows; j++)
            {
                float y = -11f + (j + 0.5f) * Step;
                for (int i = 0; i < Cols; i++)
                {
                    float x = -7f + (i + 0.5f) * Step;
                    bool b;
                    if (x < -6.0f || x > 6.0f || y > 10.0f)
                        b = true;                                         // outer frame
                    else if (x > 3.9f && x < 5.7f && y < 8.9f)
                        b = true;                                         // shooter lane wall (ends at y=8.0)
                    else if (x > 3.73f && x < 5.87f && y > 7.9f)
                        b = true;                                         // one-way lane gate
                    else
                    {
                        b = false;
                        float clearance = 0f;
                        for (int k = 0; k < pts.Count; k++)
                        {
                            var d = new Vector2(x - pts[k].x, y - pts[k].y);
                            clearance = radii[k] + BallR + 0.04f;
                            if (d.sqrMagnitude < clearance * clearance) { b = true; break; }
                        }
                    }
                    blocked[j * Cols + i] = b;
                }
            }

            // start: just past the lane gate after the kicker throws the ball clear
            // target: the slot between the flipper tips, straight into the drain
            var seen = new bool[Cols * Rows];
            var q = new Queue<int>();
            for (int j = 0; j < Rows; j++)
            {
                float y = -11f + (j + 0.5f) * Step;
                if (y < 8.9f || y > 10.0f) continue;
                for (int i = 0; i < Cols; i++)
                {
                    float x = -7f + (i + 0.5f) * Step;
                    if (x < 2.4f || x > 3.7f) continue;
                    int idx = j * Cols + i;
                    if (!blocked[idx]) { seen[idx] = true; q.Enqueue(idx); }
                }
            }

            while (q.Count > 0)
            {
                int cur = q.Dequeue();
                int i = cur % Cols, j = cur / Cols;
                float x = -7f + (i + 0.5f) * Step;
                float y = -11f + (j + 0.5f) * Step;
                if (Mathf.Abs(x) < 1.2f && y > -10.5f && y < -9.4f) return true;
                if (i > 0 && !seen[cur - 1] && !blocked[cur - 1]) { seen[cur - 1] = true; q.Enqueue(cur - 1); }
                if (i < Cols - 1 && !seen[cur + 1] && !blocked[cur + 1]) { seen[cur + 1] = true; q.Enqueue(cur + 1); }
                if (j > 0 && !seen[cur - Cols] && !blocked[cur - Cols]) { seen[cur - Cols] = true; q.Enqueue(cur - Cols); }
                if (j < Rows - 1 && !seen[cur + Cols] && !blocked[cur + Cols]) { seen[cur + Cols] = true; q.Enqueue(cur + Cols); }
            }
            return false;
        }

        // ------------------------------------------------ mouse -> board mapping

        /// <summary>Cursor position onto the board's z=0 plane via the scene camera's affine frame.</summary>
        static Vector2 PointOnBoard(Vector2 gui)
        {
            var g0 = HandleUtility.WorldToGUIPoint(new Vector3(0f, 0f, 0f));
            var gx = HandleUtility.WorldToGUIPoint(new Vector3(1f, 0f, 0f));
            var gy = HandleUtility.WorldToGUIPoint(new Vector3(0f, 1f, 0f));
            var a = gx - g0;
            var b = gy - g0;
            var d = gui - g0;
            float det = a.x * b.y - a.y * b.x;
            if (Mathf.Abs(det) < 1e-8f) return new Vector2(9999f, 9999f);
            float x = (b.y * d.x - b.x * d.y) / det;
            float y = (a.x * d.y - a.y * d.x) / det;
            return new Vector2(x, y);
        }

        static Vector2 Snap(Vector2 w)
        {
            return new Vector2(Mathf.Round(w.x * 10f) / 10f, Mathf.Round(w.y * 10f) / 10f);
        }

        // ------------------------------------------------------------ drag views

        void UpdateVisuals(Event e)
        {
            bool dragging = _dragKind != null;
            bool erasing = _dragKind == null && _eraseMode;

            if (_gridRoot != null) _gridRoot.SetActive(dragging);
            if (_ghost != null) _ghost.gameObject.SetActive(dragging);
            if (_hoverHighlight != null) _hoverHighlight.gameObject.SetActive(dragging);
            if (_eraseHighlight != null) _eraseHighlight.gameObject.SetActive(erasing);
            if (!dragging && !erasing) return;

            var mp = e.mousePosition;
            var w = PointOnBoard(mp);
            var pos = Snap(w);
            var info = dragging ? InfoFor(_dragKind) : Palette[0];

            if (dragging)
            {
                var onField = Mathf.Abs(w.x) <= 100f;
                bool valid = false;
                if (onField) CanPlace(_dragKind, pos, _moveIndex, out _);

                if (_ghost != null)
                {
                    var spr = IconSprite(info.spriteKey);
                    if (spr != null) _ghost.sprite = spr;
                    _ghost.transform.localPosition = new Vector3(pos.x, pos.y, -0.3f);
                    _ghost.transform.localScale = new Vector3(info.ghostScale, info.ghostScale, 1f)
                        * (valid || !onField ? 1f : 0.85f);
                    _ghost.color = !onField ? new Color(1f, 1f, 1f, 0.35f)
                        : valid ? new Color(1f, 1f, 1f, 0.95f) : new Color(1f, 0.7f, 0.7f, 0.9f);
                }
                if (_hoverHighlight != null)
                {
                    _hoverHighlight.transform.localPosition = new Vector3(pos.x, pos.y, -0.05f);
                    _hoverHighlight.transform.localScale = new Vector3(info.highlightSize.x, info.highlightSize.y, 1f)
                        * (valid || !onField ? 1f : 0.85f);
                    _hoverHighlight.color = !onField ? Pal.Alpha(Pal.Mint, 0.22f)
                        : valid ? Pal.Alpha(Pal.Mint, 0.35f) : Pal.Alpha(Pal.Rose, 0.35f);
                }
                if (_gridRoot != null)
                {
                    // shop-style cell feedback: bumpers reserve the cells around them,
                    // previewed live with the ghost's future position
                    foreach (var cv in _cells)
                    {
                        bool reserved = _preview.IsCellFree(cv.cell) == false;
                        if (_dragKind == "bumper" && _moveIndex >= 0
                            && Vector2.Distance(BoardGrid.Center(cv.cell), _moveOldPos) < 1.85f)
                            reserved = false; // this bumper's old reservation frees up
                        if (!reserved && _dragKind == "bumper" && onField
                            && Vector2.Distance(BoardGrid.Center(cv.cell), pos) < 1.85f)
                            reserved = true;  // and its new spot reserves these
                        cv.sr.color = reserved ? Pal.Alpha(Pal.Rose, 0.25f) : Pal.Alpha(Pal.Cream, 0.16f);
                    }
                }
            }
            else if (erasing)
            {
                int idx;
                var f = _board.Nearest(w, 1.3f, out idx);
                bool over = f != null && Mathf.Abs(w.x) <= 100f && Vector2.Distance(f.Pos, w) <= InfoFor(f.kind).grab;
                if (over && _eraseHighlight != null)
                {
                    var scale = InfoFor(f.kind).highlightSize + new Vector2(0.2f, 0.2f);
                    _eraseHighlight.transform.localPosition = new Vector3(f.x, f.y, -0.05f);
                    _eraseHighlight.transform.localScale = new Vector3(scale.x, scale.y, 1f);
                }
            }
        }

        // ---------------------------------------------------------------- panel

        Texture2D Px
        {
            get
            {
                if (_px == null)
                {
                    _px = new Texture2D(1, 1);
                    _px.SetPixel(0, 0, Color.white);
                    _px.Apply();
                }
                return _px;
            }
        }

        void DrawRect(Rect r, Color c)
        {
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Px, ScaleMode.StretchToFill);
            GUI.color = prev;
        }

        void DrawBorder(Rect r, Color c, float t)
        {
            DrawRect(new Rect(r.x, r.y, r.width, t), c);
            DrawRect(new Rect(r.x, r.yMax - t, r.width, t), c);
            DrawRect(new Rect(r.x, r.y, t, r.height), c);
            DrawRect(new Rect(r.xMax - t, r.y, t, r.height), c);
        }

        Sprite IconSprite(string key)
        {
            Sprite spr;
            if (_sprites.TryGetValue(key, out spr) && spr != null) return spr;
            var lib = Resources.Load<PixelSpriteLibrary>("PixelArtLibrary");
            if (lib != null && lib.entries != null)
                foreach (var s in lib.entries)
                {
                    if (s == null || s.sprite == null || s.key != key) continue;
                    spr = s.sprite;
                    break;
                }
            _sprites[key] = spr;
            return spr;
        }

        void DrawPanel(Event e)
        {
            var r = _panelRect;
            DrawRect(r, new Color(0.09f, 0.07f, 0.14f, 0.95f));
            DrawBorder(r, new Color(0.5f, 0.9f, 0.75f, 0.8f), 1.5f);

            var x = r.x + 10f;
            float y = r.y + 10f;

            GUI.Label(new Rect(x, y, 200f, 18f), "STARTER BOARD", EditorStyles.boldLabel);
            if (GUI.Button(new Rect(r.xMax - 60f, y, 50f, 18f), "Focus"))
                SceneView.lastActiveSceneView?.LookAt(Vector3.zero, SceneView.lastActiveSceneView.rotation, 13.5f);
            y += 22f;
            GUI.Label(new Rect(x, y, 260f, 16f),
                _board.HasFixtures ? "fixtures: " + _board.fixtures.Count + "  (the run plays this field)"
                    : "no fixtures saved - the table keeps its default field",
                EditorStyles.miniLabel);
            y += 18f;

            if (GUI.Button(new Rect(r.x + 10f, y, 48f, 20f), "Save"))
            {
                EditorUtility.SetDirty(_board);
                AssetDatabase.SaveAssets();
                Debug.Log("Starter board asset saved to " + AssetPath);
                SetStatus("Starter board asset saved.", true);
            }
            if (GUI.Button(new Rect(r.x + 62f, y, 64f, 20f), "Defaults"))
            {
                Undo.RecordObject(_board, "Starter Board - Reset Defaults");
                _board.Clear();
                _board.fixtures.AddRange(StarterBoard.DefaultFixtures());
                EditorUtility.SetDirty(_board);
                Rebuild();
                SetStatus("Reset to the table's default layout.", true);
            }
            if (GUI.Button(new Rect(r.x + 130f, y, 62f, 20f), "Validate")) Validate();
            var eraseStyle = new GUIStyle(GUI.skin.button);
            if (_eraseMode) eraseStyle.normal.textColor = new Color(1.0f, 0.45f, 0.55f);
            _eraseMode = GUI.Toggle(new Rect(r.x + 196f, y, 92f, 20f), _eraseMode, "ERASE MODE", eraseStyle);
            y += 24f;

            GUI.Label(new Rect(x, y, 260f, 16f), "FIXTURES  (pick up, drag onto the playfield)", EditorStyles.miniLabel);
            y += 18f;

            float contentH = Palette.Length * 34f + 6f;
            _scroll = GUI.BeginScrollView(new Rect(r.x + 4f, y, 288f, contentH), _scroll, new Rect(0f, 0f, 272f, contentH));
            float sy = 0f;
            foreach (var f in Palette)
            {
                var slot = new Rect(4f, sy, 268f, 28f);
                DrawSlot(slot, f, e);
                sy += 34f;
            }
            GUI.EndScrollView(true);
            y += contentH + 6f;

            // hover info: shop-style hover line
            var infoText = "";
            if (_dragKind != null)
                infoText = (_moveIndex >= 0 ? "moving: " : "dragging: ") + InfoFor(_dragKind).label + "  |  " + InfoFor(_dragKind).note;
            else
            {
                var w = PointOnBoard(e.mousePosition);
                if (!_panelRect.Contains(e.mousePosition) && Mathf.Abs(w.x) <= 100f)
                {
                    int idx;
                    var f = _board.Nearest(w, 1.3f, out idx);
                    if (f != null && Vector2.Distance(f.Pos, w) <= InfoFor(f.kind).grab)
                        infoText = InfoFor(f.kind).label + " @ (" + f.x + ", " + f.y + ") - drag to move, right-click to erase";
                    else
                        infoText = "empty field @ (" + w.x.ToString("0.0") + ", " + w.y.ToString("0.0") + ")";
                }
            }
            GUI.Label(new Rect(x, y, 272f, 40f), infoText, EditorStyles.wordWrappedMiniLabel);
            y += 42f;

            var statusStyle = new GUIStyle(EditorStyles.miniLabel);
            statusStyle.normal.textColor = _statusOk ? new Color(0.6f, 0.95f, 0.8f) : new Color(1f, 0.55f, 0.62f);
            GUI.Label(new Rect(x, y, 272f, 34f), _status, statusStyle);
        }

        void DrawSlot(Rect slot, FixtureInfo f, Event e)
        {
            var tint = f.kind == "bumper" ? Pal.Pink : Pal.Mint;
            DrawRect(slot, new Color(tint.r, tint.g, tint.b, 0.2f));
            DrawBorder(slot, tint, 1f);
            GUI.Label(new Rect(slot.x + 30f, slot.y + 2f, slot.width - 92f, 14f), f.label, EditorStyles.miniLabel);
            GUI.Label(new Rect(slot.x + 30f, slot.y + 14f, slot.width - 92f, 14f), f.note, EditorStyles.miniLabel);

            var spr = IconSprite(f.spriteKey);
            var ir = new Rect(slot.x + 2f, slot.y + 2f, 24f, 24f);
            if (spr != null)
            {
                var prev = GUI.color;
                GUI.color = Color.Lerp(Color.white, tint, 0.25f);
                GUI.DrawTextureWithTexCoords(ir, spr.texture, SpriteUV(spr));
                GUI.color = prev;
            }
            else
            {
                DrawRect(ir, tint);
                GUI.Label(ir, "?", EditorStyles.miniLabel);
            }

            var hover = slot.Contains(e.mousePosition);
            if (hover) DrawRect(slot, new Color(1f, 1f, 1f, 0.08f));

            if (e.type == EventType.MouseDown && e.button == 0 && hover && _dragKind == null)
            {
                _dragKind = f.kind;
                _moveIndex = -1;
                _justPicked = true;
                if (_eraseMode) _eraseMode = false;
                SetStatus("Picked up " + f.label + " - now click a spot on the playfield to place it.", true);
                e.Use();
                SceneView.RepaintAll();
            }
        }

        Rect SpriteUV(Sprite spr)
        {
            var tr = spr.textureRect;
            var t = spr.texture;
            return new Rect(tr.x / t.width, tr.y / t.height, tr.width / t.width, tr.height / t.height);
        }

        // ------------------------------------------------------------- utilities

        static void SetStatus(string msg, bool ok)
        {
            _status = msg;
            _statusOk = ok;
            SceneView.RepaintAll();
        }

        void Validate()
        {
            var sb = new System.Text.StringBuilder();
            int issues = 0;
            int bumpers = 0, pins = 0;
            for (int i = 0; i < _board.fixtures.Count; i++)
            {
                var f = _board.fixtures[i];
                if (f.kind != "bumper" && f.kind != "pin" && f.kind != "smallpin")
                {
                    sb.AppendLine("unknown fixture kind '" + f.kind + "' at (" + f.x + ", " + f.y + ")");
                    issues++;
                    continue;
                }
                if (f.kind == "bumper") bumpers++; else pins++;

                string why;
                if (!CanPlace(f.kind, f.Pos, i, out why))
                {
                    sb.AppendLine(InfoFor(f.kind).label + " at (" + f.x + ", " + f.y + "): " + why);
                    issues++;
                }
            }
            if (bumpers == 0) sb.AppendLine("no bumpers - the field will have none in play");
            if (issues == 0)
                SetStatus("Validation OK: " + bumpers + " bumper(s), " + pins + " pin(s). " +
                    (bumpers == 0 ? "(note: no bumpers)" : ""), true);
            else
                SetStatus("Validation: " + issues + " issue(s) -\n" + sb.ToString(), false);
        }
    }

    // shop-style cell record for editor-only preview
    class PinballTablePreviewCell
    {
        public Vector2Int cell;
        public SpriteRenderer sr;
    }
}
