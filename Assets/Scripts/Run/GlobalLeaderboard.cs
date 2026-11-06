using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Pinball
{
    /// <summary>
    /// The worldwide boards, backed by a Firebase Realtime Database over its REST
    /// API (no SDK, so it works in a WebGL build with nothing but CORS).
    ///
    /// The database URL is read at runtime from window.PINBOUND_FIREBASE_URL in
    /// index.html, so pointing the game at a different database is a one-line page
    /// edit rather than a rebuild. With no URL configured the game keeps using the
    /// on-device boards and every screen still works.
    /// </summary>
    public static class GlobalBoard
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern string Pinbound_FirebaseUrl();
#endif

        const string PrefUrl = "pb_global_url";
        const int FetchLimit = 100;

        static string _url;
        static bool _urlChecked;
        static bool _online, _busy;
        static int _version;
        static string _error = "";

        /// <summary>Every score pulled from the database, unsorted.</summary>
        public static readonly List<LbEntry> All = new List<LbEntry>();

        public static bool Online { get { return _online; } }
        public static string Error { get { return _error; } }
        /// <summary>Bumped after every successful fetch, so the UI can repaint once.</summary>
        public static int Version { get { return _version; } }

        public static string Url
        {
            get
            {
                if (!_urlChecked) { _urlChecked = true; ResolveUrl(); }
                return _url;
            }
        }

        public static bool Configured { get { return !string.IsNullOrEmpty(Url); } }

        static void ResolveUrl()
        {
            string url = "";
#if UNITY_WEBGL && !UNITY_EDITOR
            try { url = Pinbound_FirebaseUrl(); } catch (Exception) { url = ""; }
#endif
            if (string.IsNullOrEmpty(url)) url = PlayerPrefs.GetString(PrefUrl, "");
            _url = (url ?? "").Trim().TrimEnd('/');
        }

        // ------------------------------------------------------------- read

        public static void Refresh(MonoBehaviour host)
        {
            if (host == null || host.gameObject == null || !Configured || _busy) return;
            host.StartCoroutine(Fetch());
        }

        public static void Refresh(MonoBehaviour host, int score, LbBoard board)
        {
            if (host == null || host.gameObject == null || !Configured || _busy) return;
            host.StartCoroutine(Fetch(score, board));
        }

        static IEnumerator Fetch(int rankScore = 0, LbBoard rankBoard = LbBoard.AllTime)
        {
            _busy = true;
            string url = Url + "/scores.json?orderBy=%22score%22&limitToLast=" + FetchLimit;
            using (var req = UnityWebRequest.Get(url))
            {
                req.timeout = 10;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    LoadSnapshot(req.downloadHandler.text);
                    _online = true;
                    _error = "";
                    _version++;
                }
                else
                {
                    // keep showing the last good board rather than blanking out
                    _online = false;
                    _error = req.error + " " + req.responseCode;
                }
            }
            _busy = false;
            if (rankScore > 0) LastRank = RankOf(rankBoard, rankScore);
        }

        /// <summary>Rank the last submitted score took on the worldwide board (0 unknown).</summary>
        public static int LastRank { get; private set; }

        /// <summary>Replaces the cached board from a Firebase REST response. Public so the
        /// same parser can be fed a saved snapshot when the network is unavailable.</summary>
        public static void LoadSnapshot(string json)
        {
            All.Clear();
            if (string.IsNullOrEmpty(json) || json.Trim() == "null") return;
            var root = MiniJson.Parse(json) as Dictionary<string, object>;
            if (root == null) return;
            foreach (var pair in root.Values)
            {
                var d = pair as Dictionary<string, object>;
                if (d == null) continue;
                var e = new LbEntry
                {
                    name = Str(d, "name"),
                    score = Mathf.RoundToInt((float)Num(d, "score")),
                    round = Mathf.RoundToInt((float)Num(d, "round")),
                    mode = Str(d, "mode") == "DAILY" ? "DAILY" : "MAIN",
                    day = Str(d, "day"),
                    ts = (long)Num(d, "ts")
                };
                if (string.IsNullOrEmpty(e.name)) e.name = "AAA";
                if (e.name.Length > 4) e.name = e.name.Substring(0, 4);
                if (e.score <= 0) continue;
                All.Add(e);
            }
        }

        static string Str(Dictionary<string, object> d, string key)
        {
            object v;
            return d.TryGetValue(key, out v) && v != null ? Convert.ToString(v, CultureInfo.InvariantCulture) : "";
        }

        static double Num(Dictionary<string, object> d, string key)
        {
            object v;
            if (!d.TryGetValue(key, out v) || v == null) return 0;
            double n;
            return double.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture),
                NumberStyles.Any, CultureInfo.InvariantCulture, out n) ? n : 0;
        }

        // ------------------------------------------------------------- read helpers

        public static List<LbEntry> Top(LbBoard board, int cap = Lb.Cap)
        {
            var today = Lb.Today;
            var list = new List<LbEntry>();
            foreach (var e in All)
            {
                if (e == null) continue;
                if (board == LbBoard.Daily && !(e.mode == "DAILY" && e.day == today)) continue;
                if (board == LbBoard.Main && e.mode != "MAIN") continue;
                list.Add(e);
            }
            list.Sort((a, b) => b.score != a.score ? b.score.CompareTo(a.score) : a.ts.CompareTo(b.ts));
            if (list.Count > cap) list.RemoveRange(cap, list.Count - cap);
            return list;
        }

        /// <summary>1-based worldwide position for a score on one board, so the
        /// prompt can say exactly where a run lands.</summary>
        public static int RankOf(LbBoard board, int score)
        {
            if (score <= 0) return 0;
            var today = Lb.Today;
            int rank = 1;
            foreach (var e in All)
            {
                if (e == null) continue;
                if (board == LbBoard.Daily && !(e.mode == "DAILY" && e.day == today)) continue;
                if (board == LbBoard.Main && e.mode != "MAIN") continue;
                if (e.score > score) rank++;
            }
            return rank;
        }

        // ------------------------------------------------------------- write

        public static void SubmitScore(string name, int score, int round, string mode, MonoBehaviour host)
        {
            if (!Configured || score <= 0) return;
            if (host == null || host.gameObject == null) return;
            host.StartCoroutine(Post(name, score, round, mode,
                mode == "DAILY" ? LbBoard.Daily : LbBoard.Main));
        }

        static IEnumerator Post(string name, int score, int round, string mode, LbBoard board)
        {
            string body = "{\"name\":\"" + Escape(string.IsNullOrEmpty(name) ? "AAA" : name) + "\"" +
                          ",\"score\":" + score +
                          ",\"round\":" + Mathf.Max(1, round) +
                          ",\"mode\":\"" + (mode == "DAILY" ? "DAILY" : "MAIN") + "\"" +
                          ",\"day\":\"" + Lb.Today + "\"" +
                          ",\"ts\":" + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + "}";

            using (var req = new UnityWebRequest(Url + "/scores.json", "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = 10;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                    _error = req.error + " " + req.responseCode;
            }
            // pull the board again so the prompt can show the worldwide position
            yield return Fetch(score, board);
            _version++;
        }

        static string Escape(string s)
        {
            var sb = new StringBuilder(s.Length + 8);
            foreach (char c in s)
            {
                if (c == '"' || c == '\\') sb.Append('\\').Append(c);
                else if (c < ' ') sb.Append(' ');
                else sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>Minimal JSON reader: the Firebase response is an object of flat
        /// score objects, which JsonUtility cannot parse because the keys vary.</summary>
        static class MiniJson
        {
            public static object Parse(string s)
            {
                int i = 0;
                return Value(s, ref i);
            }

            static void Ws(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

            static object Value(string s, ref int i)
            {
                Ws(s, ref i);
                if (i >= s.Length) return null;
                switch (s[i])
                {
                    case '{': return Obj(s, ref i);
                    case '[': return Arr(s, ref i);
                    case '"': return Txt(s, ref i);
                    case 't': i += 4; return true;
                    case 'f': i += 5; return false;
                    case 'n': i += 4; return null;
                    default: return Number(s, ref i);
                }
            }

            static Dictionary<string, object> Obj(string s, ref int i)
            {
                var d = new Dictionary<string, object>();
                i++; // {
                Ws(s, ref i);
                if (i < s.Length && s[i] == '}') { i++; return d; }
                while (i < s.Length)
                {
                    Ws(s, ref i);
                    string key = Txt(s, ref i);
                    Ws(s, ref i);
                    if (i < s.Length && s[i] == ':') i++;
                    d[key] = Value(s, ref i);
                    Ws(s, ref i);
                    if (i < s.Length && s[i] == ',') { i++; continue; }
                    if (i < s.Length && s[i] == '}') i++;
                    break;
                }
                return d;
            }

            static List<object> Arr(string s, ref int i)
            {
                var list = new List<object>();
                i++; // [
                Ws(s, ref i);
                if (i < s.Length && s[i] == ']') { i++; return list; }
                while (i < s.Length)
                {
                    list.Add(Value(s, ref i));
                    Ws(s, ref i);
                    if (i < s.Length && s[i] == ',') { i++; continue; }
                    if (i < s.Length && s[i] == ']') i++;
                    break;
                }
                return list;
            }

            static string Txt(string s, ref int i)
            {
                if (i >= s.Length || s[i] != '"') return "";
                i++;
                var sb = new StringBuilder();
                while (i < s.Length && s[i] != '"')
                {
                    char c = s[i++];
                    if (c != '\\') { sb.Append(c); continue; }
                    if (i >= s.Length) break;
                    char e = s[i++];
                    switch (e)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'u':
                            if (i + 4 <= s.Length)
                            {
                                int code;
                                if (int.TryParse(s.Substring(i, 4), NumberStyles.HexNumber,
                                                 CultureInfo.InvariantCulture, out code))
                                    sb.Append((char)code);
                                i += 4;
                            }
                            break;
                        default: sb.Append(e); break;
                    }
                }
                if (i < s.Length) i++; // closing quote
                return sb.ToString();
            }

            static double Number(string s, ref int i)
            {
                int start = i;
                if (i < s.Length && (s[i] == '-' || s[i] == '+')) i++;
                while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.' || s[i] == 'e' || s[i] == 'E' ||
                                        s[i] == '-' || s[i] == '+')) i++;
                double n;
                return double.TryParse(s.Substring(start, i - start), NumberStyles.Any,
                                       CultureInfo.InvariantCulture, out n) ? n : 0;
            }
        }
    }
}
