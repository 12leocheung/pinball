using System.Collections.Generic;
using UnityEngine;

namespace Pinball
{
    public static class BoardGrid
    {
        public const int Cols = 5;
        public const int Rows = 8;
        public const float CellSize = 2f;

        public static Vector2 Center(int c, int r)
        {
            // field spans BoardLeft(-7)..LaneX(4.8); 5 cells of 2 leave 1.8, split
            // evenly so the squares sit equal against both the left wall and the lane
            return new Vector2(-5.1f + c * CellSize, -7.5f + r * CellSize);
        }

        public static Vector2 Center(Vector2Int cell) { return Center(cell.x, cell.y); }

        public static bool InRange(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < Cols && cell.y >= 0 && cell.y < Rows;
        }

        public static Vector2Int Nearest(Vector2 world)
        {
            int c = Mathf.RoundToInt((world.x + 5.1f) / CellSize);
            int r = Mathf.RoundToInt((world.y + 7.5f) / CellSize);
            return new Vector2Int(Mathf.Clamp(c, 0, Cols - 1), Mathf.Clamp(r, 0, Rows - 1));
        }
    }

    public class Ball : MonoBehaviour
    {
        public Rigidbody2D rb;
        public bool InPlay;
        public float maxSpeed = 32f;
        float _trail;
        bool _launched;

        /// <summary>True only after the ball leaves the plunger - lanterns gate on this so
        /// a parked (kinematic) ball doesn't tick nearby scoring items.</summary>
        public bool Launched { get { return _launched && rb != null && rb.bodyType == RigidbodyType2D.Dynamic; } }

        public void Init()
        {
            Phys.Ensure();
            gameObject.transform.localScale = Vector3.one;
            rb = gameObject.GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.mass = 0.45f;
            rb.gravityScale = 1.6f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.linearDamping = 0.02f;
            rb.angularDamping = 0.05f;

            var col = gameObject.GetComponent<CircleCollider2D>();
            if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.9f;
            col.sharedMaterial = Phys.Ball;

            var sr = gameObject.GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteBank.Get("ball");
            sr.sharedMaterial = SpriteBank.Mat;
            sr.sortingOrder = 20;
        }

        public void Reset(Vector2 pos)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.position = pos;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            transform.position = pos;
            transform.rotation = Quaternion.identity;
            _launched = false;
            InPlay = true;
        }

        public void Launch(Vector2 velocity)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = velocity;
            _launched = true;
        }

        public void Push(Vector2 impulse)
        {
            if (rb.bodyType != RigidbodyType2D.Dynamic) return;
            rb.linearVelocity = rb.linearVelocity + impulse;
        }

        public void Capture()
        {
            InPlay = false;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        void Update()
        {
            if (rb.bodyType != RigidbodyType2D.Dynamic) return;
            var v = rb.linearVelocity;
            float sp = v.magnitude;
            if (sp > maxSpeed) rb.linearVelocity = v * (maxSpeed / sp);

            _trail -= Time.deltaTime;
            if (_launched && sp > 5f && _trail <= 0f)
            {
                _trail = 0.045f;
                Juice.Trail(transform.position, Pal.Alpha(Pal.Cream, 0.5f), 0.85f);
            }


        }
    }

    public class Flipper : MonoBehaviour
    {
        public bool isLeft;
        public float restAngle;
        public float activeAngle;
        public float speed = 1500f;
        public float power = 1f;
        Rigidbody2D _rb;
        SpriteRenderer _sr;
        float _angle;
        float _flash;
        bool _wasPressed;

        public void Init(bool left, float length)
        {
            isLeft = left;
            Phys.Ensure();
            if (left) { restAngle = -22f; activeAngle = 34f; }
            else { restAngle = 202f; activeAngle = 146f; }
            _angle = restAngle;

            _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.useFullKinematicContacts = true;

            var cap = gameObject.AddComponent<CapsuleCollider2D>();
            cap.direction = CapsuleDirection2D.Horizontal;
            cap.size = new Vector2(length, 1.0f);
            cap.offset = new Vector2(length * 0.5f, 0f);
            cap.sharedMaterial = Phys.Solid;

            var spriteGO = new GameObject("art");
            spriteGO.transform.SetParent(transform, false);
            spriteGO.transform.localPosition = new Vector3(length * 0.5f, 0f, 0f);
            _sr = spriteGO.AddComponent<SpriteRenderer>();
            _sr.sprite = SpriteBank.Get("flipper");
            _sr.sharedMaterial = SpriteBank.Mat;
            _sr.sortingOrder = 18;
            _sr.transform.localScale = Vector3.one;

            transform.localRotation = Quaternion.Euler(0f, 0f, _angle);
        }

        void FixedUpdate()
        {
            bool pressed = isLeft ? InputHub.LeftFlipper : InputHub.RightFlipper;
            if (PinballTable.Tilted) pressed = false;
            if (pressed && !_wasPressed) Sfx.Play("flip");
            _wasPressed = pressed;
            float target = pressed ? activeAngle : restAngle;
            float step = speed * (pressed ? 1f : 0.55f) * power * Time.fixedDeltaTime;
            _angle = Mathf.MoveTowards(_angle, target, step);
            _rb.MoveRotation(_angle);
            if (_sr != null)
            {
                _flash = Mathf.MoveTowards(_flash, pressed ? 1f : 0f, Time.fixedDeltaTime * 8f);
                _sr.color = Color.Lerp(Color.white, Pal.Alpha(Pal.Pink, 0.95f), _flash * 0.5f);
            }
        }
    }

    public class Bumper : MonoBehaviour
    {
        public int value = 100;
        public float bounce = 8f;
        public Color tint = Pal.Pink;
        public bool isCorner;
        SpriteRenderer _sr;
        float _flash;
        Vector3 _base;

        public void Init(int val, Color color, bool cornerOnly)
        {
            Phys.Ensure();
            value = val;
            tint = color;
            isCorner = cornerOnly;
            _base = transform.localScale;

            var col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 1.08f;
            col.sharedMaterial = Phys.Bouncy;

            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = SpriteBank.Get(cornerOnly ? "bumper_a" : "bumper_b");
            _sr.sharedMaterial = SpriteBank.Mat;
            _sr.sortingOrder = 14;
            _sr.transform.localScale = Vector3.one * 0.9f;
            tint = color;
        }

        void OnCollisionEnter2D(Collision2D c)
        {
            var ball = c.collider.GetComponent<Ball>();
            if (ball == null) return;
            Vector2 n = ((Vector2)ball.transform.position - (Vector2)transform.position).normalized;
            ball.Push(n * bounce);
            _flash = 1f;
            if (GameManager.I != null) GameManager.I.OnBumperHit(value, transform.position, tint);
        }

        void Update()
        {
            if (_sr == null) return;
            _flash = Mathf.MoveTowards(_flash, 0f, Time.deltaTime * 4.5f);
            float s = 0.9f + _flash * 0.14f;
            _sr.transform.localScale = new Vector3(s, s, 1f);
            _sr.color = Color.Lerp(Color.white, Color.Lerp(Color.white, tint, 0.35f), _flash);
        }
    }

    public class BoardModule : MonoBehaviour
    {
        public BoardItemDef def;
        public Vector2Int cell;
        public SpriteRenderer icon;
        public bool full;
        float _flash;
        float _lanternTimer;
        int _coinsPaid;
        float _pulse;
        float _squash;
        Vector2 _squashDir = Vector2.up;
        Color tint = Color.white;

        // coiled cannon: swallows the ball for a short charge, then launches it
        float _cannonTimer;
        float _cannonCd;
        Ball _cannonBall;

        public void Init(BoardItemDef d, Vector2Int c, Vector2 worldPos)
        {
            def = d;
            cell = c;
            tint = Pal.RarityColor(d.rarity);
            transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
            Phys.Ensure();

            // only the item art sits on the board - a faint rarity shadow keeps it readable
            var shadowGO = new GameObject("shadow");
            shadowGO.transform.SetParent(transform, false);
            var shadow = shadowGO.AddComponent<SpriteRenderer>();
            shadow.sprite = SpriteBank.Get(d.sprite);
            shadow.sharedMaterial = SpriteBank.Mat;
            shadow.sortingOrder = 11;
            shadow.color = Pal.Alpha(Pal.Ink, 0.55f);
            shadowGO.transform.localScale = Vector3.one * 0.82f;
            shadowGO.transform.localPosition = new Vector3(0.1f, -0.1f, 0f);

            var iconGO = new GameObject("icon");
            iconGO.transform.SetParent(transform, false);
            icon = iconGO.AddComponent<SpriteRenderer>();
            icon.sprite = SpriteBank.Get(d.sprite);
            icon.sharedMaterial = SpriteBank.Mat;
            icon.sortingOrder = 13;
            iconGO.transform.localScale = Vector3.one * 0.82f;

            bool solid = d.kind == ItemKind.Wall || d.kind == ItemKind.Magnet;
            if (solid)
            {
                var box = gameObject.AddComponent<BoxCollider2D>();
                box.size = new Vector2(1.44f, 1.44f);
                box.sharedMaterial = Phys.Solid;
            }
            else
            {
                var circle = gameObject.AddComponent<CircleCollider2D>();
                circle.radius = 0.72f;
                circle.sharedMaterial = Phys.Bouncy;
            }
        }

        void OnCollisionEnter2D(Collision2D c)
        {
            var ball = c.collider.GetComponent<Ball>();
            if (ball == null || GameManager.I == null) return;
            if (def.kind == ItemKind.Wall) return;

            Vector2 n = ((Vector2)ball.transform.position - (Vector2)transform.position).normalized;
            if (n.sqrMagnitude < 0.01f) n = Vector2.up;
            _squashDir = n;

            switch (def.kind)
            {
                case ItemKind.Bumper:
                    ball.Push(n * def.bounce);
                    GameManager.I.OnModuleHit(this, def.value);
                    break;
                case ItemKind.Trampoline:
                    if (def.id == "cannon")
                    {
                        if (_cannonBall == null && !full && _cannonCd <= 0f)
                        {
                            // coil variant: capture the ball in the barrel, fire after a beat
                            _cannonBall = ball;
                            _cannonTimer = 0.5f;
                            _cannonCd = 0.2f;
                            ball.rb.linearVelocity = Vector2.zero;
                            ball.rb.bodyType = RigidbodyType2D.Kinematic;
                            ball.rb.position = transform.position;
                            ball.transform.position = transform.position;
                            _pulse = 1f;
                            Juice.Ring(transform.position, Pal.RarityColor(def.rarity), 0.5f, 2.4f, 0.45f);
                        }
                        // while the cannon is holding the ball in, leave it untouched
                        return;
                    }
                    ball.Push(n * def.bounce + Vector2.up * 2f);
                    GameManager.I.OnModuleHit(this, def.value);
                    break;
                case ItemKind.Coin:
                    if (_coinsPaid < 3)
                    {
                        _coinsPaid++;
                        GameManager.I.AddCoins(1, transform.position, true);
                        // little physical flourish: a coin pops out of the press
                        Juice.CoinPop(transform.position);
                    }
                    ball.Push(n * def.bounce);
                    GameManager.I.OnModuleHit(this, def.value);
                    break;
                case ItemKind.Mult:
                    ball.Push(n * def.bounce);
                    GameManager.I.AddMultiplier(def.mult, transform.position);
                    GameManager.I.OnModuleHit(this, def.value);
                    break;
                case ItemKind.Magnet:
                    ball.Push(n * def.bounce);
                    GameManager.I.OnModuleHit(this, def.value);
                    break;
                case ItemKind.Lantern:
                    ball.Push(n * 4f);
                    GameManager.I.OnModuleHit(this, def.value);
                    break;
                case ItemKind.Hold:
                    if (!full && GameManager.I.CanHoldBall())
                    {
                        full = true;
                        ball.Capture();
                        GameManager.I.OnHoldCatch(this);
                        icon.color = Pal.Alpha(Pal.Cream, 0.5f);
                    }
                    else
                    {
                        ball.Push(n * def.bounce);
                    }
                    break;
            }
        }

        public void ResetRoundState()
        {
            _coinsPaid = 0;
            _lanternTimer = 0f;
            if (!full) return;
        }

        /// <summary>Cancels a pending cannon charge (round restart) without ejecting anything.</summary>
        public void CancelCannon()
        {
            _cannonTimer = 0f;
            _cannonBall = null;
        }

        public void ClearHold()
        {
            full = false;
            if (icon != null) icon.color = Color.white;
        }

        void Update()
        {
            if (def != null && def.id == "cannon") TickCannon();
            if (_cannonCd > 0f) _cannonCd -= Time.deltaTime;
            if (icon == null) return;
            _flash = Mathf.MoveTowards(_flash, 0f, Time.deltaTime * 5f);
            _squash = Mathf.MoveTowards(_squash, 0f, Time.deltaTime * 7f);
            // jelly squash: compress along the hit direction, bulge across it
            float sq = 0.18f * _squash;
            bool hitFromAboveOrBelow = Mathf.Abs(_squashDir.y) >= Mathf.Abs(_squashDir.x);
            float sy = hitFromAboveOrBelow ? 1f - sq : 1f + sq;
            float sx = hitFromAboveOrBelow ? 1f + sq : 1f - sq;
            float s = 0.82f + _flash * 0.14f;
            icon.transform.localScale = new Vector3(s * sx, s * sy, 1f);
            icon.color = _flash > 0.01f ? Color.Lerp(Color.white, tint, _flash * 0.75f) : Color.white;
            if (full) icon.color = Pal.Alpha(Pal.Cream, 0.6f);

            if (def.kind == ItemKind.Lantern && GameManager.I != null)
            {
                var b = GameManager.I.Table != null ? GameManager.I.Table.BallRef : null;
                // while the ball is still inside the shooter lane it hasn't really
                // "come near" the item - otherwise launching past a lantern
                // starts it scoring before the ball is even in play
                if (b != null && b.Launched && b.transform.position.x <= PinballTable.LaneX - 0.4f &&
                    Vector2.Distance(b.transform.position, transform.position) < 4.2f)
                {
                    _pulse += Time.deltaTime;
                    icon.color = Color.Lerp(Pal.White, Pal.Gold, Mathf.PingPong(_pulse * 3f, 1f));
                    _lanternTimer -= Time.deltaTime;
                    if (_lanternTimer <= 0f)
                    {
                        _lanternTimer = 0.6f;
                        GameManager.I.OnModuleHit(this, def.value);
                    }
                }
            }
        }

        void TickCannon()
        {
            if (_cannonTimer <= 0f) return;
            _cannonTimer -= Time.deltaTime;
            if (_cannonTimer > 0f || _cannonBall == null) return;

            var b = _cannonBall;
            _cannonBall = null;
            _pulse = 1f;
            if (b.InPlay)
            {
                b.rb.bodyType = RigidbodyType2D.Dynamic;
                // spawn the ball clear of the barrel, or the fresh contact overlap
                // re-triggers the capture on the next frame and the ball never escapes
                var outPos = (Vector2)transform.position + new Vector2(0f, 1.7f);
                b.rb.position = outPos;
                b.transform.position = outPos;
                b.rb.linearVelocity = new Vector2(Random.Range(-2.2f, 2.2f), 23.5f);
                _cannonCd = 0.6f;
                Juice.Ring(transform.position, Pal.Cream, 0.6f, 3.4f, 0.5f);
                Juice.Spark(transform.position + Vector3.up * 0.4f, Pal.Cream, 6, 6f);
                GameManager.I.OnModuleHit(this, def.value);
                Sfx.Play("launch", 0.04f, 0.75f);
            }
        }

        void OnDestroy()
        {
            if (_cannonBall == null) return;
            var b = _cannonBall;
            _cannonBall = null;
            if (b.InPlay && b.rb.bodyType == RigidbodyType2D.Kinematic)
            {
                // module torn down mid-charge (shop rebuild): drop the ball gently
                b.rb.bodyType = RigidbodyType2D.Dynamic;
                b.rb.linearVelocity = Vector2.up * 4f;
            }
        }

        public void Pulse()
        {
            _flash = 1f;
            _squash = 1f;
        }
    }

    public class PinballTable : MonoBehaviour
    {
        public Ball BallRef;
        public Flipper leftFlipper;
        public Flipper rightFlipper;
        public readonly List<Bumper> Bumpers = new List<Bumper>();
        public readonly Dictionary<Vector2Int, BoardModule> Modules = new Dictionary<Vector2Int, BoardModule>();
        public readonly HashSet<Vector2Int> Reserved = new HashSet<Vector2Int>();

        public bool AwaitingLaunch;
        public bool Charging;
        public float Charge;

        /// <summary>True after 3 nudges inside the tilt window; flippers go dead until a new ball.</summary>
        public static bool Tilted;
        bool _saveUsed;
        float _ballSave;
        float _nudgeCooldown;
        float _nudgeWindow;
        int _nudgeCount;

        public Transform BoardRoot;
        Transform _wallsRoot;
        GameObject _plunger;
        GameObject _ballGo;
        BoxCollider2D _gate;
        SpriteRenderer _gateSr;
        BossDef _boss;
        float _plungerBaseY;
        bool _kicked;
        float _leftPinSince;
        float _rightPinSince;
        float _laneCalm;

        const float BoardLeft = -7f;
        const float BoardRight = 7f;
        const float BoardTop = 11f;
        const float BoardBottom = -11f;
        /// <summary>Right edge of the playfield / left edge of the shooter lane.
        /// Modules gate their scoring on this so a ball sliding up the launch
        /// column never both-tick items on its way past.</summary>
        public const float LaneX = 4.8f;
        const float LaneCenter = 5.9f;
        const float DrainY = -11.05f;
        const float StuckLoseTime = 3.5f;
        float _stuckTime;

        public void Build()
        {
            Phys.Ensure();
            BoardRoot = new GameObject("Board").transform;
            BoardRoot.SetParent(transform, false);

            SpriteBank.Make("playfield", BoardRoot, 0, Vector2.zero);

            _wallsRoot = new GameObject("Walls").transform;
            _wallsRoot.SetParent(BoardRoot, false);
            BuildWalls();

            // launch lane art
            var lane = SpriteBank.Make("lane", BoardRoot, 2, new Vector2(LaneCenter, -1.5f), Pal.Alpha(Pal.White, 0.55f));
            lane.transform.localScale = new Vector3(2.4f, 19f, 1f);

            // the pin field is laid out from the starter board asset when the Starter
            // Board editor has saved fixtures; otherwise the table keeps its default field
            var starterBoard = StarterBoard.Instance;
            var field = starterBoard != null && starterBoard.HasFixtures ? starterBoard.fixtures : StarterBoard.DefaultFixtures();
            int bumperId = 0;
            foreach (var f in field)
            {
                if (f.kind == "bumper")
                {
                    var bg = new GameObject("Bumper" + bumperId);
                    bg.transform.SetParent(BoardRoot, false);
                    bg.transform.position = new Vector3(f.x, f.y, 0f);
                    var b = bg.AddComponent<Bumper>();
                    // alternate the two bumper looks so mixes keep their classic pairing
                    bool mint = bumperId % 2 == 1;
                    b.Init(60, mint ? Pal.Mint : Pal.Pink, mint);
                    Bumpers.Add(b);
                    bumperId++;
                    continue;
                }
                AddPeg(new Vector2(f.x, f.y), f.kind == "smallpin" ? 0.7f : 1f);
            }

            // flippers (pair centered on the playfield: width -7..LaneX, so both
            // pivots sit an even 1.5 from their own wall)
            var lf = new GameObject("FlipperL");
            lf.transform.SetParent(BoardRoot, false);
            lf.transform.position = new Vector3(-5.5f, -9.35f, 0f);
            leftFlipper = lf.AddComponent<Flipper>();
            leftFlipper.Init(true, 3.0f);

            var rf = new GameObject("FlipperR");
            rf.transform.SetParent(BoardRoot, false);
            rf.transform.position = new Vector3(3.3f, -9.35f, 0f);
            rightFlipper = rf.AddComponent<Flipper>();
            rightFlipper.Init(false, 3.0f);

            // drain
            var drain = SpriteBank.Make("drain", BoardRoot, 3, new Vector2(-1.1f, -11.2f));
            drain.transform.localScale = new Vector3(7f, 1f, 1f);

            // plunger
            var pl = SpriteBank.Make("plunger", BoardRoot, 3, new Vector2(LaneCenter, -9.9f));
            _plunger = pl.gameObject;
            _plungerBaseY = -9.9f;

            _ballGo = new GameObject("Ball");
            _ballGo.transform.SetParent(BoardRoot, false);
            BallRef = _ballGo.AddComponent<Ball>();
            BallRef.Init();

            // reserved cells near the bumpers: a module must stay clear of the bumper
            // art (collider 1.08) plus most of its own footprint, 1.85 handles both
            for (int c = 0; c < BoardGrid.Cols; c++)
                for (int r = 0; r < BoardGrid.Rows; r++)
                {
                    var p = BoardGrid.Center(c, r);
                    foreach (var b in Bumpers)
                        // 1.6 keeps truly overlapping cells locked (the bumper collider is
                    // 1.08 and most item art is ~1.4 wide) while freeing the borderline
                    // seats just beside a bumper that used to be greyed out
                    if (Vector2.Distance(p, b.transform.position) < 1.6f) Reserved.Add(new Vector2Int(c, r));
                }

            // the cells straight above the flipper gap stay empty so the ball can always drain
            Reserved.Add(new Vector2Int(1, 0));
            Reserved.Add(new Vector2Int(2, 0));
            Reserved.Add(new Vector2Int(3, 0));
        }

        void AddPeg(Vector2 pos, float scale = 1f)
        {
            var go = new GameObject("Peg");
            go.transform.SetParent(BoardRoot, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            if (scale != 1f) go.transform.localScale = Vector3.one * scale;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.36f;
            col.sharedMaterial = Phys.Bouncy;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteBank.Get("peg");
            sr.sharedMaterial = SpriteBank.Mat;
            sr.sortingOrder = 8;
            go.AddComponent<Peg>();
        }

        void BuildWalls()
        {
            // outer frame
            AddWall(new Vector2(BoardLeft, BoardBottom), new Vector2(BoardLeft, BoardTop));
            AddWall(new Vector2(BoardLeft, BoardTop), new Vector2(BoardRight, BoardTop));
            AddWall(new Vector2(BoardRight, BoardTop), new Vector2(BoardRight, -10.9f));
            // inlane guides run into the flipper pivots and mirror each other exactly
            // about the playfield centre, so nothing can wedge behind a flipper
            AddWall(new Vector2(BoardLeft, -8.55f), new Vector2(-5.45f, -9.45f));
            AddWall(new Vector2(LaneX, -8.55f), new Vector2(3.25f, -9.45f));
            // shooter lane channel (open at the top so the kicker can throw the ball into play)
            AddWall(new Vector2(LaneX, -10.9f), new Vector2(LaneX, 8.0f));
            AddWall(new Vector2(LaneX, -10.9f), new Vector2(BoardRight, -10.9f));
            BuildLaneGate();
        }

        /// One-way gate on the lane mouth: open while the ball waits on the plunger so it can be
        /// launched, then closed once the kicker has thrown the ball clear, so a ball in play can
        /// never slip back down the shooter lane and loop forever.
        void BuildLaneGate()
        {
            var go = new GameObject("LaneGate");
            go.transform.SetParent(BoardRoot, false);
            go.transform.localPosition = new Vector3(LaneX, 9.45f, 0f);

            _gate = go.AddComponent<BoxCollider2D>();
            _gate.size = new Vector2(0.3f, 3.0f);
            _gate.sharedMaterial = Phys.Solid;
            _gate.enabled = false;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteBank.Get("rail_v");
            sr.sharedMaterial = SpriteBank.Mat;
            sr.sortingOrder = 7;
            sr.color = Pal.Alpha(Pal.Sky, 0.85f);
            sr.transform.localScale = new Vector3(1f, 3f, 1f);
            _gateSr = sr;
            sr.enabled = false;
        }

        void SetLaneGate(bool closed)
        {
            if (_gate != null) _gate.enabled = closed;
            if (_gateSr != null) _gateSr.enabled = closed;
        }

        void AddWall(Vector2 a, Vector2 b)
        {
            var go = new GameObject("wall");
            Vector2 mid = (a + b) * 0.5f;
            float len = Vector2.Distance(a, b);
            float ang = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            go.transform.SetParent(_wallsRoot, false);
            go.transform.localPosition = new Vector3(mid.x, mid.y, 0f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, ang);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteBank.Get("rail_h");
            sr.sharedMaterial = SpriteBank.Mat;
            sr.sortingOrder = 6;
            sr.color = Color.Lerp(Pal.Sand, Pal.Plum, 0.35f);
            go.transform.localScale = new Vector3(len, 1f, 1f);

            var ec = _wallsRoot.gameObject.AddComponent<EdgeCollider2D>();
            ec.points = new[] { a, b };
            ec.edgeRadius = 0.06f;
            ec.sharedMaterial = Phys.Solid;
            ec.offset = Vector2.zero;
        }

        public void ApplyBoss(BossDef boss)
        {
            _boss = boss;
            Phys.Ensure();
            BallRef.rb.gravityScale = 1.6f * (boss != null ? boss.gravityMul : 1f);
            float power = boss != null ? boss.flipperMul : 1f;
            leftFlipper.power = power;
            rightFlipper.power = power;
        }

        public void BeginRound()
        {
            foreach (var m in Modules.Values) m.ResetRoundState();
            foreach (var m in Modules.Values) m.CancelCannon();
            _saveUsed = false;
            Tilted = false;
            ClearBall();
        }

        public void ClearBall()
        {
            if (BallRef != null) BallRef.InPlay = false;
            if (BallRef != null && BallRef.rb != null)
            {
                BallRef.rb.bodyType = RigidbodyType2D.Kinematic;
                BallRef.rb.linearVelocity = Vector2.zero;
            }
            AwaitingLaunch = false;
            Charging = false;
            Charge = 0f;
            _laneCalm = 0f;
            SetLaneGate(false);
            if (_plunger != null) _plunger.transform.localPosition = new Vector3(LaneCenter, _plungerBaseY, 0f);
        }

        public void PrepareBall()
        {
            BallRef.Reset(new Vector2(LaneCenter, -9.4f));
            AwaitingLaunch = true;
            Charging = false;
            Charge = 0f;
            _kicked = false;
            _stuckTime = 0f;
            _laneCalm = 0f;
            _leftPinSince = _rightPinSince = 0f;
            Tilted = false;
            _nudgeCount = 0;
            _nudgeWindow = 0f;
            _nudgeCooldown = 0f;
            SetLaneGate(false);
            if (_plunger != null) _plunger.transform.localPosition = new Vector3(LaneCenter, _plungerBaseY, 0f);
        }

        public void Tick()
        {
            // ball-save grace window: a fresh launch that drains straight back down
            // respawns on the plunger instead of costing a ball (once per round)
            if (_ballSave > 0f) _ballSave -= Time.deltaTime;

            if (AwaitingLaunch)
            {
                bool held = InputHub.LaunchHeld;
                if (held)
                {
                    Charging = true;
                    Charge = Mathf.Clamp01(Charge + Time.deltaTime / 0.95f);
                }
                else if (Charging)
                {
                    DoLaunch();
                }
                if (_plunger != null)
                    _plunger.transform.localPosition = new Vector3(LaneCenter, _plungerBaseY - Charge * 0.7f, 0f);
            }

            if (BallRef != null && BallRef.InPlay && BallRef.rb.bodyType == RigidbodyType2D.Dynamic)
            {
                Vector2 bp = BallRef.transform.position;
                if (bp.y < DrainY)
                {
                    if (_ballSave > 0f)
                    {
                        _ballSave = 0f;
                        _saveUsed = true;
                        Juice.Float(bp, "BALL SAVED", Pal.Mint, 1.2f, 1.3f, 2.2f);
                        Juice.Ring(bp, Pal.Mint, 0.6f, 3.2f, 0.5f);
                        Sfx.Play("save");
                        PrepareBall();
                        return;
                    }
                    BallRef.InPlay = false;
                    BallRef.Capture();
                    if (GameManager.I != null) GameManager.I.OnBallDrained();
                }
                else if (!AwaitingLaunch && bp.x > LaneX + 0.2f)
                {
                    // the ball came back down the shooter lane: a slow one near the
                    // bottom hands the plunger straight back, a parked one anywhere
                    // in the lane gets re-plunged so it can never just sit and die
                    _laneCalm = BallRef.rb.linearVelocity.magnitude < 2.2f ? _laneCalm + Time.fixedDeltaTime : 0f;
                    if (_laneCalm > 0.3f)
                    {
                        _laneCalm = 0f;
                        _kicked = false;
                        SetLaneGate(false);
                        if (bp.y < -8.6f)
                        {
                            AwaitingLaunch = true;
                            Charging = false;
                            Charge = 0f;
                        }
                        else PrepareBall();
                    }
                }
                else if (TryFreePinnedBall(bp)) { /* pinned ball re-placed on the flipper */ }
                else if (BallRef.rb.linearVelocity.sqrMagnitude < 0.25f)
                {
                    // a wedged ball is lost, the same as draining one
                    _stuckTime += Time.fixedDeltaTime;
                    if (_stuckTime > StuckLoseTime)
                    {
                        _stuckTime = 0f;
                        Juice.Float(bp + Vector2.up * 0.8f, "STUCK - BALL LOST", Pal.Rose, 1.1f, 1.1f, 2.2f);
                        Juice.Shake(0.3f);
                        BallRef.InPlay = false;
                        BallRef.Capture();
                        if (GameManager.I != null) GameManager.I.OnBallDrained();
                    }
                }
                else _stuckTime = 0f;
            }

            // nudge + tilt
            if (_nudgeCooldown > 0f) _nudgeCooldown -= Time.deltaTime;
            if (_nudgeWindow > 0f) _nudgeWindow -= Time.deltaTime;
            else _nudgeCount = 0;
            if (InputHub.NudgeLeftPressed) Nudge(-1);
            else if (InputHub.NudgeRightPressed) Nudge(1);
        }

        /// <summary>The flipper's kinematic capsule can squash a slow ball into the inlane
        /// guide edge and the solver never depenetrates it - the ball parks with zero
        /// velocity inside the pivot pocket forever. Once a ball has sat there briefly,
        /// re-place it on top of the bar so it can roll off the tip naturally.</summary>
        bool TryFreePinnedBall(Vector2 bp)
        {
            bool left = TrackPin(bp, leftFlipper, ref _leftPinSince);
            bool right = TrackPin(bp, rightFlipper, ref _rightPinSince);
            if (!(left || right)) return false;
            var f = left ? leftFlipper : rightFlipper;
            Vector2 pivot = f.transform.position;
            Vector2 axis = (Vector2)f.transform.right;      // toward the tip
            Vector2 upN = new Vector2(-axis.y, axis.x);     // the bar's upper face
            Vector2 dest = pivot + axis * 1.35f + upN * 1.45f;
            BallRef.transform.position = dest;
            BallRef.rb.position = dest;
            BallRef.rb.linearVelocity = axis * 1.4f;
            BallRef.rb.angularVelocity = 0f;
            Juice.Spark(dest, Pal.Cream, 5, 7f, 0.6f);
            _stuckTime = 0f;
            _leftPinSince = _rightPinSince = 0f;
            return true;
        }

        bool TrackPin(Vector2 bp, Flipper f, ref float since)
        {
            if (f == null) return false;
            float dist2 = (bp - (Vector2)f.transform.position).sqrMagnitude;
            if (dist2 < 2.25f && BallRef.rb.linearVelocity.magnitude < 1.6f)
            {
                since += Time.fixedDeltaTime;
                return since > 0.85f; // still parked after the grace time: fire the free
            }
            since = 0f;
            return false;
        }

        public void Nudge(int dir)
        {
            if (_nudgeCooldown > 0f || Tilted) return;
            _nudgeCooldown = 0.35f;
            if (_nudgeWindow <= 0f) _nudgeWindow = 2.5f;
            _nudgeCount++;
            if (BallRef != null && BallRef.InPlay && BallRef.rb.bodyType == RigidbodyType2D.Dynamic)
                BallRef.Push(new Vector2(dir * 4.4f, 2.6f));
            Juice.Shake(0.24f);
            Sfx.Play("click");
            if (_nudgeCount >= 3)
            {
                Tilted = true;
                if (GameManager.I != null) GameManager.I.ShowBanner("TILT!", Pal.Rose, 1.6f);
                Juice.Shake(0.55f);
                Sfx.Play("lose");
            }
        }

        public void DoLaunch()
        {
            float c = Mathf.Max(Charge, 0.1f);
            Charging = false;
            AwaitingLaunch = false;
            // the lowest charge still clears the kicker: the kicker needs ~24.3
            // to climb from the plunger to the lane mouth, so anything weaker
            // just rolled back down and sat dead in the column
            float speed = Mathf.Lerp(24.5f, 30.5f, c);
            BallRef.Launch(new Vector2(0f, speed));
            if (!_saveUsed) _ballSave = 4.5f;
            Juice.Spark(BallRef.transform.position, Pal.Cream, 5, 5f, 0.7f);
            if (GameManager.I != null) GameManager.I.OnBallLaunched(c);
            Charge = 0f;
        }

        void FixedUpdate()
        {
            if (BallRef == null || !BallRef.InPlay) return;
            if (BallRef.rb.bodyType != RigidbodyType2D.Dynamic) return;

            // lane kicker: throws the ball out of the shooter lane into the playfield
            Vector2 bp = BallRef.transform.position;
            if (!_kicked && bp.x > LaneX + 0.1f && bp.y > 9.2f)
            {
                _kicked = true;
                BallRef.rb.linearVelocity = new Vector2(-13.5f, Mathf.Max(4f, BallRef.rb.linearVelocity.y * 0.25f));
                Juice.Spark(bp, Pal.Sky, 10, 9f);
                Juice.Ring(bp, Pal.Sky, 0.5f, 2.2f, 0.3f);
                Juice.Shake(0.07f);
            }
            else if (_kicked)
            {
                // shut the gate once the ball is fully clear of the lane mouth
                if (bp.x < LaneX - 1.2f)
                {
                    SetLaneGate(true);
                    _kicked = false;
                }
                else if (bp.y < 7.0f) _kicked = false;
            }

            // magnets
            foreach (var m in Modules.Values)
            {
                if (m.def.kind != ItemKind.Magnet) continue;
                Vector2 to = (Vector2)m.transform.position - (Vector2)BallRef.transform.position;
                float d = to.magnitude;
                if (d > 0.1f && d < 7f)
                {
                    float f = Mathf.Lerp(22f, 2f, d / 7f);
                    BallRef.rb.AddForce(to.normalized * f);
                }
            }
        }

        public bool IsCellFree(Vector2Int cell)
        {
            // the bottom row hangs over the flippers - scoring items placed there sit
            // on the flipper and create endless bounce loops against the wall, so the
            // whole row is never placeable
            if (cell.y == 0) return false;
            return BoardGrid.InRange(cell) && !Modules.ContainsKey(cell) && !Reserved.Contains(cell);
        }

        public BoardModule PlaceModule(BoardItemDef def, Vector2Int cell)
        {
            if (!IsCellFree(cell)) return null;
            var go = new GameObject("Module_" + def.id);
            go.transform.SetParent(BoardRoot, false);
            var m = go.AddComponent<BoardModule>();
            m.Init(def, cell, BoardGrid.Center(cell));
            Modules[cell] = m;
            Juice.Ring(BoardGrid.Center(cell), Pal.RarityColor(def.rarity), 0.7f, 2.6f, 0.35f);
            Juice.Spark(BoardGrid.Center(cell), Pal.RarityColor(def.rarity), 8, 7f);
            return m;
        }

        public BoardModule RemoveModule(Vector2Int cell)
        {
            BoardModule m;
            if (!Modules.TryGetValue(cell, out m)) return null;
            Modules.Remove(cell);
            if (m != null) Destroy(m.gameObject);
            return m;
        }

        public void ClearModules()
        {
            foreach (var m in Modules.Values) if (m != null) Destroy(m.gameObject);
            Modules.Clear();
        }

        /// <summary>Frees every hold module so a sandbox can keep catching balls.</summary>
        public void ClearHolds()
        {
            foreach (var m in Modules.Values)
                if (m != null && m.def.kind == ItemKind.Hold) m.ClearHold();
        }

        public BoardModule ModuleAt(Vector2Int cell)
        {
            BoardModule m;
            return Modules.TryGetValue(cell, out m) ? m : null;
        }
    }

    public class Peg : MonoBehaviour
    {
        float _flash;
        SpriteRenderer _sr;

        void Start()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        void OnCollisionEnter2D(Collision2D c)
        {
            var ball = c.collider.GetComponent<Ball>();
            if (ball == null) return;
            _flash = 1f;
            if (GameManager.I != null) GameManager.I.OnPegHit(transform.position);
        }

        void Update()
        {
            if (_sr == null) return;
            _flash = Mathf.MoveTowards(_flash, 0f, Time.deltaTime * 6f);
            _sr.color = Color.Lerp(Color.white, Pal.Gold, _flash);
        }
    }
}
