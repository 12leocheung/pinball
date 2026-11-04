using System.Collections.Generic;
using UnityEngine;

namespace Pinball
{
    /// <summary>Tiny synth bank: every sound is generated once at boot and played back with slight pitch jitter.</summary>
    public static class Sfx
    {
        static readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        static AudioSource _src;

        static bool _muted;
        public static bool Muted
        {
            get { return _muted; }
            set { _muted = value; PlayerPrefs.SetInt("pb_mute", _muted ? 1 : 0); }
        }

        public static void Boot(Transform parent)
        {
            if (_src != null) return;
            var go = new GameObject("Sfx");
            go.transform.SetParent(parent, false);
            go.AddComponent<AudioListener>();
            _src = go.AddComponent<AudioSource>();
            _src.spatialBlend = 0f;
            _src.playOnAwake = false;

            Synth("click", 0.055f, 900f, 680f, 1, 55f);
            Synth("flip", 0.045f, 235f, 150f, 1, 70f);
            Synth("launch", 0.22f, 130f, 560f, 2, 40f);
            Synth("bumper", 0.11f, 560f, 190f, 1, 22f);
            Synth("peg", 0.05f, 720f, 500f, 1, 60f);
            Synth("module", 0.14f, 340f, 540f, 2, 18f);
            Synth("coin", 0.09f, 950f, 1420f, 1, 30f);
            Synth("save", 0.34f, 220f, 470f, 0, 16f);
            Synth("wild", 0.4f, 300f, 920f, 0, 12f);
            Synth("win", 0.5f, 420f, 880f, 0, 14f);
            Synth("lose", 0.55f, 300f, 90f, 2, 7f);
            Synth("drain", 0.2f, 420f, 120f, 0, 14f);
            _muted = PlayerPrefs.GetInt("pb_mute", 0) == 1;
        }

        static void Synth(string name, float dur, float f0, float f1, int wave, float decay)
        {
            int sr = 44100;
            int n = Mathf.Max(8, Mathf.CeilToInt(sr * dur));
            var data = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                float f = Mathf.Lerp(f0, f1, t / dur);
                phase += 6.2831853f * f / sr;
                float s;
                switch (wave)
                {
                    case 1: s = Mathf.Sin(phase) >= 0f ? 1f : -1f; break;
                    case 2: s = Mathf.Repeat(phase / 6.2831853f, 1f) * 2f - 1f; break;
                    default: s = Mathf.Sin(phase); break;
                }
                data[i] = s * Mathf.Exp(-decay * t) * 0.34f;
            }
            var clip = AudioClip.Create("sfx_" + name, n, 1, sr, false);
            clip.SetData(data, 0);
            _clips[name] = clip;
        }

        public static void Play(string name, float pitchVar = 0.08f, float vol = 0.9f)
        {
            if (Muted || _src == null || string.IsNullOrEmpty(name)) return;
            AudioClip c;
            if (!_clips.TryGetValue(name, out c) || c == null) return;
            _src.pitch = 1f + Random.Range(-pitchVar, pitchVar);
            _src.PlayOneShot(c, vol);
        }
    }
}
