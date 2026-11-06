using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Pinball
{
    public enum LeaderboardBackend { None, Upstash, Firebase }

    /// <summary>
    /// The worldwide boards. Two interchangeable backends, both plain REST so a
    /// WebGL build needs no SDK and no server:
    ///   Upstash Redis - ZSET (score -> member), best fit for a leaderboard, free tier
    ///   Firebase Realtime Database - object store, used when a database URL is set
    /// Whichever is configured is picked at runtime from index.html, so switching
    /// stores is a page edit rather than a rebuild. With neither configured the
    /// game keeps the on-device boards.
    /// </summary>
    public static class GlobalBoard
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern string Pinbound_RedisUrl();
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern string Pinbound_RedisToken();
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern string Pinbound_FirebaseUrl();
#endif

        const int FetchLimit = 100;
        const string RedisAllKey = "pinbound:all";
        const string RedisMainKey = "pinbound:main";

        static bool _checked;
        static LeaderboardBackend _backend = LeaderboardBackend.None;
        static string _redisUrl = "", _redisToken = "", _fbUrl = "";
        static bool _online, _busy;
        static int _version;
        static string _error = "";

        static readonly List<LbEntry> _all = new List<LbEntry>();

        public static LeaderboardBackend Backend { get { Ensure(); return _backend; } }
        public static bool Configured { get { Ensure(); return _backend != LeaderboardBackend.None; } }
        public static bool Online { get { return _online; } }
        public static string Error { get { return _error; } }
        public static int Version { get { return _version; } }
        public static int LastRank { get; private set; }
        public static List<LbEntry> All { get { return _all; } }

        static void Ensure()
        {
            if (_checked) return;
            _checked = true;
            string ru = "", rt = "", fb = "";
#if UNITY_WEBGL && !UNITY_EDITOR
            try { ru = Pinbound_RedisUrl(); } catch (Exception) { }
            try { rt = Pinbound_RedisToken(); } catch (Exception) { }
            try { fb = Pinbound_FirebaseUrl(); } catch (Exception) { }
#endif
            _redisUrl = Clean(ru);
            _redisToken = Clean(rt);
            _fbUrl = Clean(fb);
            if (_redisUrl.Length > 0 && _redisToken.Length > 0) _backend = LeaderboardBackend.Upstash;
            else if (_fbUrl.Length > 0) _backend = LeaderboardBackend.Firebase;
            else _backend = LeaderboardBackend.None;
        }

        static string Clean(string s) { return (s ?? "").Trim().TrimEnd('/'); }

        static string RedisKey(LbBoard board)
        {
            if (board == LbBoard.Daily) return "pinbound:daily:" + Lb.Today;
            if (board == LbBoard.Main) return RedisMainKey;
            return RedisAllKey;
        }

        // ------------------------------------------------------------- read

        public static void Refresh(MonoBehaviour host)
        {
            Refresh(host, 0, LbBoard.AllTime);
        }

        public static void Refresh(MonoBehaviour host, int score, LbBoard board)
        {
            if (host == null || host.gameObject == null || !Configured || _busy) return;
            host.StartCoroutine(Fetch(score, board));
        }

        static IEnumerator Fetch(int rankScore, LbBoard rankBoard)
        {
            _busy = true;
            if (_backend == LeaderboardBackend.Upstash) yield return FetchRedis();
            else if (_backend == LeaderboardBackend.Firebase) yield return FetchFirebase();
            _busy = false;
            if (rankScore > 0) LastRank = RankOf(rankBoard, rankScore);
        }

        static IEnumerator FetchRedis()
        {
            string body = "[\"ZREVRANGE\",\"" + RedisAllKey + "\",\"0\",\"" + (FetchLimit - 1) + "\",\"WITHSCORES\"]";
            using (var req = new UnityWebRequest(_redisUrl + "/", "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("Authorization", "Bearer " + _redisToken);
                req.timeout = 10;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    LoadRedis(req.downloadHandler.text);
                    _online = true;
                    _error = "";
                    _version++;
                }
                else
                {
                    _online = false;
                    _error = req.error + " " + req.responseCode;
                }
            }
        }

        static IEnumerator FetchFirebase()
        {
            string url = _fbUrl + "/scores.json?orderBy=%22score%22&limitToLast=" + FetchLimit;
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
                    _online = false;
                    _error = req.error + " " + req.responseCode;
                }
            }
        }

        // ------------------------------------------------------------- parsing

        /// <summary>Reads a Redis reply to ZREVRANGE ... WITHSCORES, either raw
        /// ({"result":[...]}) or an element of a pipeline reply.</summary>
        public static void LoadRedis(string json)
        {
            _all.Clear();
            var root = MiniJson.Parse(json);
            var result = Unwrap(root) as List<object>;
            if (result == null) return;
            for (int i = 0; i + 1 < result.Count; i += 2)
            {
                string member = Convert.ToString(result[i], CultureInfo.InvariantCulture);
                double score = ToDouble(result[i + 1]);
                var e = ParseMember(member, score);
                if (e != null) _all.Add(e);
            }
            _all.Sort((a, b) => b.score != a.score ? b.score.CompareTo(a.score) : a.ts.CompareTo(b.ts));
            if (_all.Count > FetchLimit) _all.RemoveRange(FetchLimit, _all.Count - FetchLimit);
        }

        static object Unwrap(object node)
        {
            var d = node as Dictionary<string, object>;
            if (d == null) return node;
            object v;
            return d.TryGetValue("result", out v) ? v : null;
        }

        static LbEntry ParseMember(string member, double score)
        {
            if (string.IsNullOrEmpty(member) || score <= 0) return null;
            var p = member.Split('|');
            var e = new LbEntry
            {
                name = p.Length > 0 && p[0].Length > 0 ? p[0] : "AAA",
                mode = p.Length > 1 && p[1] == "DAILY" ? "DAILY" : "MAIN",
                day = p.Length > 2 ? p[2] : Lb.Today,
                ts = p.Length > 3 ? (long)ToDouble(p[3]) : 0,
                round = p.Length > 4 ? Mathf.Max(1, Mathf.RoundToInt((float)ToDouble(p[4]))) : 1,
                score = Mathf.RoundToInt((float)score)
            };
            if (e.name.Length > 4) e.name = e.name.Substring(0, 4);
            return e;
        }

        /// <summary>Reads a Firebase REST snapshot: an object of score records.</summary>
        public static void LoadSnapshot(string json)
        {
            _all.Clear();
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
                _all.Add(e);
            }
        }

        static double ToDouble(object v)
        {
            double n;
            return double.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture),
                NumberStyles.Any, CultureInfo.InvariantCulture, out n) ? n : 0;
        }

        static string Str(Dictionary<string, object> d, string key)
        {
            object v;
            return d.TryGetValue(key, out v) && v != null ? Convert.ToString(v, CultureInfo.InvariantCulture) : "";
        }

        static double Num(Dictionary<string, object> d, string key)
        {
            object v;
            return d.TryGetValue(key, out v) && v != null ? ToDouble(v) : 0;
        }

        // ------------------------------------------------------------- helpers

        public static List<LbEntry> Top(LbBoard board, int cap = Lb.Cap)
        {
            var today = Lb.Today;
            var list = new List<LbEntry>();
            foreach (var e in _all)
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

        /// <summary>1-based worldwide position for a score on one board.</summary>
        public static int RankOf(LbBoard board, int score)
        {
            if (score <= 0) return 0;
            var today = Lb.Today;
            int rank = 1;
            foreach (var e in _all)
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
            host.StartCoroutine(_backend == LeaderboardBackend.Upstash
                ? PostRedis(name, score, round, mode)
                : PostFirebase(name, score, round, mode));
        }

        static IEnumerator PostRedis(string name, int score, int round, string mode)
        {
            bool daily = mode == "DAILY";
            string boardKey = daily ? "pinbound:daily:" + Lb.Today : RedisMainKey;
            string member = EscapeMember(string.IsNullOrEmpty(name) ? "AAA" : name) + "|" + (daily ? "DAILY" : "MAIN") +
                            "|" + Lb.Today + "|" + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + "|" + Mathf.Max(1, round);

            // add to the board and to all-time in one round trip, then read the exact rank
            string body = "[[\"ZADD\",\"" + RedisAllKey + "\",\"" + score + "\",\"" + member + "\"]," +
                          "[\"ZADD\",\"" + boardKey + "\",\"" + score + "\",\"" + member + "\"]," +
                          "[\"ZREVRANK\",\"" + RedisAllKey + "\",\"" + member + "\"]]";

            using (var req = new UnityWebRequest(_redisUrl + "/pipeline", "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("Authorization", "Bearer " + _redisToken);
                req.timeout = 10;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    var reply = MiniJson.Parse(req.downloadHandler.text) as List<object>;
                    if (reply != null && reply.Count > 2)
                    {
                        var rank = Unwrap(reply[2]);
                        if (rank is string) LastRank = Mathf.RoundToInt((float)ToDouble(rank)) + 1;
                        else if (rank is double || rank is int) LastRank = Mathf.RoundToInt((float)ToDouble(rank)) + 1;
                    }
                }
                else _error = req.error + " " + req.responseCode;
            }
            yield return FetchRedis();
            _version++;
        }

        static IEnumerator PostFirebase(string name, int score, int round, string mode)
        {
            string body = "{\"name\":\"" + EscapeMember(string.IsNullOrEmpty(name) ? "AAA" : name) + "\"" +
                          ",\"score\":" + score +
                          ",\"round\":" + Mathf.Max(1, round) +
                          ",\"mode\":\"" + (mode == "DAILY" ? "DAILY" : "MAIN") + "\"" +
                          ",\"day\":\"" + Lb.Today + "\"" +
                          ",\"ts\":" + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + "}";

            using (var req = new UnityWebRequest(_fbUrl + "/scores.json", "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = 10;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                    _error = req.error + " " + req.responseCode;
            }
            yield return FetchFirebase();
            _version++;
        }

        static string EscapeMember(string s)
        {
            var sb = new StringBuilder(s.Length + 8);
            foreach (char c in s)
            {
                if (c == '"' || c == '\\') sb.Append('\\').Append(c);
                else if (c == '|') sb.Append(' ');
                else if (c < ' ') sb.Append(' ');
                else sb.Append(c);
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------- json

        /// <summary>Minimal JSON reader: both backends return shapes JsonUtility
        /// cannot express (varying keys, mixed arrays).</summary>
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
                i++;
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
                i++;
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
                if (i < s.Length) i++;
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
