using UnityEditor;
using UnityEngine;

namespace Pinball.EditorTools
{
    /// <summary>Play-mode test drivers so automated checks can drive the game loop without fake input.</summary>
    public static class DebugDrivers
    {
        [MenuItem("Tools/Pinball/DEBUG - Start Run From Menu")]
        public static void StartRun()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter play mode first");
                return;
            }
            var menus = Object.FindObjectOfType<Menus>();
            if (menus != null) menus.StartRunFromMenu();
        }

        [MenuItem("Tools/Pinball/DEBUG - Toggle Pause")]
        public static void TogglePause()
        {
            if (!Application.isPlaying) return;
            var menus = Object.FindObjectOfType<Menus>();
            if (menus != null) menus.TogglePause();
        }

        [MenuItem("Tools/Pinball/DEBUG - Launch Ball")]
        public static void LaunchBall()
        {
            if (!Application.isPlaying) return;
            var t = Object.FindObjectOfType<PinballTable>();
            if (t == null || !t.AwaitingLaunch) { Debug.LogWarning("driver: no ball to launch"); return; }
            t.Charge = 1f;
            t.DoLaunch();
            var save = t.GetType().GetField("_ballSave", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Debug.Log("driver: launched, ball-save window = " + (save != null ? (float)save.GetValue(t) : -1f) + "s");
        }

        [MenuItem("Tools/Pinball/DEBUG - Force Tilt")]
        public static void ForceTilt()
        {
            if (!Application.isPlaying) return;
            var t = Object.FindObjectOfType<PinballTable>();
            if (t == null) { Debug.LogWarning("driver: no table"); return; }
            var cooldown = t.GetType().GetField("_nudgeCooldown", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            for (int i = 0; i < 4; i++)
            {
                if (cooldown != null) cooldown.SetValue(t, 0f);
                t.Nudge(1);
            }
            Debug.Log("driver: Tilted=" + PinballTable.Tilted);
        }

        const System.Reflection.BindingFlags BP = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;

        [MenuItem("Tools/Pinball/DEBUG - Launch Then Drain (Save Test)")]
        public static void LaunchThenDrain()
        {
            if (!Application.isPlaying) return;
            var t = Object.FindObjectOfType<PinballTable>();
            var gm = GameManager.I;
            if (t == null || gm == null) { Debug.LogWarning("driver: missing table/manager"); return; }
            if (!t.AwaitingLaunch) { Debug.LogWarning("driver: ball not awaiting launch"); return; }
            t.Charge = 1f;
            t.DoLaunch();
            int before = gm.BallsLeft;
            var b = t.BallRef;
            b.transform.position = new Vector3(0f, -11.6f, 0f);
            if (b.rb != null) { b.rb.position = b.transform.position; b.rb.linearVelocity = Vector2.zero; }
            Debug.Log("driver: launch+drain done, balls before=" + before + " (drain will resolve on next frame)");
        }

        [MenuItem("Tools/Pinball/DEBUG - Inspect State")]
        public static void Inspect()
        {
            if (!Application.isPlaying) return;
            var gm = GameManager.I;
            var t = Object.FindObjectOfType<PinballTable>();
            if (gm == null || t == null) { Debug.LogWarning("driver: no manager/table"); return; }
            var save = t.GetType().GetField("_ballSave", BP);
            var used = t.GetType().GetField("_saveUsed", BP);
            var b = t.BallRef;
            Debug.Log("driver: balls=" + gm.BallsLeft + " phase=" + gm.Phase + " awaiting=" + t.AwaitingLaunch +
                      " ballSave=" + (float)save.GetValue(t) + " saveUsed=" + (bool)used.GetValue(t) +
                      " score=" + gm.Score + " ballInPlay=" + (b != null && b.InPlay));
        }

        [MenuItem("Tools/Pinball/DEBUG - Drain Ball (Test Save)")]
        public static void DrainBall()
        {
            if (!Application.isPlaying) return;
            var gm = GameManager.I;
            if (gm == null) { Debug.LogWarning("driver: no manager"); return; }
            Debug.Log("driver: balls before drain = " + gm.BallsLeft);
            var t = Object.FindObjectOfType<PinballTable>();
            var ball = t != null ? t.BallRef : null;
            if (ball != null && ball.InPlay)
            {
                ball.transform.position = new Vector3(0f, -11.6f, 0f);
                if (ball.rb != null) { ball.rb.position = ball.transform.position; ball.rb.linearVelocity = Vector2.zero; }
            }
            else Debug.LogWarning("driver: ball not in play");
        }

        [MenuItem("Tools/Pinball/DEBUG - Combo x6")]
        public static void ForceCombo()
        {
            if (!Application.isPlaying) return;
            var gm = GameManager.I;
            if (gm == null) { Debug.LogWarning("driver: no manager"); return; }
            gm.Combo = 6;
            Debug.Log("driver: combo bonus = " + gm.ComboBonus());
        }

        [MenuItem("Tools/Pinball/DEBUG - Check Placement Rules")]
        public static void CheckPlacement()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter play mode first"); return; }
            var gm = GameManager.I;
            var t = Object.FindObjectOfType<PinballTable>();
            if (gm == null || t == null) { Debug.LogWarning("driver: no manager/table"); return; }
            var sb = new System.Text.StringBuilder();
            for (int r = 0; r < BoardGrid.Rows; r++)
            {
                for (int c = 0; c < BoardGrid.Cols; c++)
                    sb.Append(t.IsCellFree(new Vector2Int(c, r)) ? "Y" : "-");
                if (r < BoardGrid.Rows - 1) sb.Append("  |  ");
            }
            Debug.Log("driver: placement grid, bottom row first ('Y' = free, '-' = blocked):\n       " + sb);
        }

        [MenuItem("Tools/Pinball/DEBUG - Shake Test")]
        public static void ShakeTest()
        {
            if (!Application.isPlaying) return;
            var pd = typeof(PixelDisplay).GetProperty("I", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var inst = pd != null ? pd.GetGetMethod(true).Invoke(null, null) as MonoBehaviour : null;
            Juice.Shake(0.4f);
            Debug.Log("driver: shake 0.4 applied (watch camera offset)");
        }

        [MenuItem("Tools/Pinball/DEBUG - Force Game Over (Lose Screen)")]
        public static void ForceGameOver()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter play mode first"); return; }
            var gm = GameManager.I;
            if (gm == null) { Debug.LogWarning("driver: no manager"); return; }
            if (gm.Phase == Phase.Menu)
            {
                var menus = Object.FindObjectOfType<Menus>();
                if (menus != null) menus.StartRunFromMenu();
            }
            gm.Score = 0; // below any quota -> guaranteed loss
            int balls = gm.BallsLeft;
            for (int i = 0; i < balls; i++) gm.OnBallDrained();
            Debug.Log("driver: forced game over, phase=" + gm.Phase + " balls was=" + balls);
        }

        [MenuItem("Tools/Pinball/DEBUG - Force Shop With New Content")]
        public static void ForceShop()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter play mode first"); return; }
            var gm = GameManager.I;
            if (gm == null) { Debug.LogWarning("No GameManager found"); return; }
            var menus = Object.FindObjectOfType<Menus>();
            if (gm.Phase == Phase.Menu && menus != null) menus.StartRunFromMenu();

            // attach the three NEW wildcards so their icons render in the strip
            var newWilds = new[] { "acorn", "shell", "sand" };
            for (int i = 0; i < newWilds.Length; i++)
                if (Catalog.WildById(newWilds[i]) is WildcardDef wdef && wdef != null) gm.AddWildcard(wdef);

            gm.Coins += 40;
            gm.Phase = Phase.Shop;
            if (gm.HudUi != null) gm.HudUi.SetVisible(false);
            gm.ShopUi.Show(9);

            // dump the rotated-in stock so we can confirm pools include new content
            var field = typeof(Shop).GetField("_offers", BP);
            var offers = field != null ? field.GetValue(gm.ShopUi) as System.Collections.IEnumerable : null;
            if (offers == null) { Debug.LogWarning("driver: could not read offers"); return; }
            int n = 0;
            foreach (var obj in offers)
            {
                var type = obj.GetType();
                bool isWild = (bool)type.GetField("isWild").GetValue(obj);
                string id = isWild
                    ? (type.GetField("wdef").GetValue(obj) as WildcardDef)?.id ?? "?"
                    : (type.GetField("item").GetValue(obj) as BoardItemDef)?.id ?? "?";
                Debug.Log("driver: offer " + n + " = " + (isWild ? "wild:" : "item:") + id);
                n++;
            }
            Debug.Log("driver: shop forced, coins=" + gm.Coins + ", " + n + " offers rotating");
        }
    }
}
