using System.Collections.Generic;
using UnityEngine;

namespace Pinball
{
    /// <summary>
    /// One procedural chiptune loop (bass + plucked square lead over a 4-chord progression),
    /// generated once at boot. Each run re-pitches it by seed for a fresh "album cut" feel.
    /// </summary>
    public static class Music
    {
        static AudioSource _src;
        static bool _muted;

        public static bool Muted
        {
            get { return _muted; }
            set
            {
                _muted = value;
                if (_src != null) _src.mute = _muted;
                PlayerPrefs.SetInt("pb_music", _muted ? 1 : 0);
            }
        }

        public static void Boot(Transform parent, int seed)
        {
            if (_src != null) { SetVariant(seed); return; }

            var go = new GameObject("Music");
            go.transform.SetParent(parent, false);
            _src = go.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            _src.loop = true;
            _src.volume = 0.5f;

            const float bpm = 108f;
            float beat = 60f / bpm;
            int sr = AudioSettings.outputSampleRate;
            float loopLen = 8f * beat; // 8 beats: Am - F - C - G, two beats each
            int total = Mathf.CeilToInt(sr * loopLen);
            var data = new float[total];

            int[] roots = { 220, 175, 131, 196 };
            var evStart = new List<float>();
            var evFreq = new List<float>();
            var evWave = new List<int>();
            var evVol = new List<float>();
            var evDecay = new List<float>();

            // note loader: wave 0 = warm triangle, 1 = bright square
            void Add(float start, float freq, int wave, float vol, float decay)
            {
                evStart.Add(start);
                evFreq.Add(freq);
                evWave.Add(wave);
                evVol.Add(vol);
                evDecay.Add(decay);
            }

            int[] arp = { 0, 3, 7, 12 };
            for (int c = 0; c < 4; c++)
            {
                float t0 = c * 2f * beat;
                Add(t0, roots[c] * 0.5f, 0, 0.17f, 2.5f);          // bass root
                Add(t0 + beat, roots[c] * 0.75f, 0, 0.1f, 3.5f);    // bass fifth bump
                for (int e = 0; e < 4; e++)
                {
                    int off = arp[(e + c) % 4] + (e == 3 ? 2 : 0);  // little climb at bar end
                    float f = roots[c] * 2f * Mathf.Pow(2f, off / 12f);
                    Add(t0 + e * beat * 0.5f, f, 1, 0.065f, 9f);
                }
            }

            for (int i = 0; i < total; i++)
            {
                float t = (float)i / sr;
                float s = 0f;
                for (int e = 0; e < evStart.Count; e++)
                {
                    float dt = t - evStart[e];
                    if (dt < 0f) continue;
                    float ph = 6.2831853f * evFreq[e] * dt;
                    float w = evWave[e] == 1
                        ? (Mathf.Sin(ph) >= 0f ? 1f : -1f)
                        : Mathf.Sin(ph) + Mathf.Sin(ph * 2f) * 0.15f;
                    s += w * Mathf.Exp(-evDecay[e] * dt) * evVol[e];
                }
                data[i] = Mathf.Clamp(s, -1f, 1f) * 0.85f;
            }

            var clip = AudioClip.Create("theme", total, 1, sr, false);
            clip.SetData(data, 0);
            _src.clip = clip;
            _muted = PlayerPrefs.GetInt("pb_music", 0) == 1;
            _src.mute = _muted;
            SetVariant(seed);
            _src.Play();
        }

        /// <summary>Re-pitches the loop so every run sounds like its own remix.</summary>
        public static void SetVariant(int seed)
        {
            if (_src == null) return;
            _src.pitch = seed <= 0 ? 1f : new[] { 0.94f, 1f, 1.06f }[((seed % 3) + 3) % 3];
        }
    }
}
