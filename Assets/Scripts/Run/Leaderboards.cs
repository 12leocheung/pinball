using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pinball
{
    /// <summary>Which of the three boards a score belongs to.</summary>
    public enum LbBoard { Daily, Main, AllTime }

    [Serializable]
    public class LbEntry
    {
        public string name;
        public int score;
        public int round;
        public string mode; // "MAIN" or "DAILY"
        public string day;  // yyyyMMdd, used to keep the daily board to one day
        public long ts;
    }

    [Serializable]
    class LbFile
    {
        public List<LbEntry> entries = new List<LbEntry>();
    }

    /// <summary>
    /// The three top-10 boards, saved in PlayerPrefs:
    ///   Daily   - best scores from today's daily-seed runs
    ///   Main    - best main-mode runs (daily runs do not count)
    ///   AllTime - every non-sandbox run, whichever mode it came from
    /// Score here is the run's best single-round score, the same number the
    /// game-over screen calls BEST SCORE.
    /// </summary>
    public static class Lb
    {
        public const int Cap = 10;
        const string Key = "pb_lb_v1";

        static LbFile _file;

        public static string Today { get { return DateTime.Now.ToString("yyyyMMdd"); } }

        static LbFile File
        {
            get
            {
                if (_file == null) Load();
                return _file;
            }
        }

        static void Load()
        {
            _file = new LbFile();
            var raw = PlayerPrefs.GetString(Key, "");
            if (!string.IsNullOrEmpty(raw))
            {
                try
                {
                    var loaded = JsonUtility.FromJson<LbFile>(raw);
                    if (loaded != null && loaded.entries != null) _file = loaded;
                }
                catch (Exception) { /* a corrupt save just starts empty */ }
            }
            Prune();
        }

        /// <summary>Drops yesterday's daily scores so that board always reads "today".</summary>
        static void Prune()
        {
            var today = Today;
            _file.entries.RemoveAll(e => e == null || e.score <= 0 ||
                (e.mode == "DAILY" && e.day != today));
        }

        public static void Save()
        {
            Prune();
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(_file));
            PlayerPrefs.Save();
        }

        static bool OnBoard(LbBoard board, LbEntry e, string today)
        {
            switch (board)
            {
                case LbBoard.Main: return e.mode == "MAIN";
                case LbBoard.Daily: return e.mode == "DAILY" && e.day == today;
                default: return true; // all-time keeps every mode
            }
        }

        public static List<LbEntry> Top(LbBoard board)
        {
            var today = Today;
            var list = new List<LbEntry>();
            foreach (var e in File.entries)
                if (e != null && OnBoard(board, e, today)) list.Add(e);
            list.Sort((a, b) => b.score != a.score ? b.score.CompareTo(a.score) : a.ts.CompareTo(b.ts));
            if (list.Count > Cap) list.RemoveRange(Cap, list.Count - Cap);
            return list;
        }

        /// <summary>True when a score would land somewhere in this board's top 10.</summary>
        public static bool Qualifies(LbBoard board, int score)
        {
            if (score <= 0) return false;
            var list = Top(board);
            return list.Count < Cap || score > list[list.Count - 1].score;
        }

        /// <summary>1-based position the score would take, or 0 when it would not make the board.</summary>
        public static int RankIfAdded(LbBoard board, int score)
        {
            if (score <= 0) return 0;
            var list = Top(board);
            int rank = 1;
            foreach (var e in list) if (e.score >= score) rank++;
            return rank <= Cap ? rank : 0;
        }

        public static void Submit(LbBoard board, string name, int score, int round, string mode)
        {
            if (score <= 0) return;
            var e = new LbEntry
            {
                name = string.IsNullOrEmpty(name) ? "AAA" : name,
                score = score,
                round = Mathf.Max(1, round),
                mode = mode == "DAILY" ? "DAILY" : "MAIN",
                day = Today,
                ts = DateTime.UtcNow.Ticks
            };
            File.entries.Add(e);
            Save();
        }
    }
}
