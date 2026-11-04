using System.IO;
using UnityEditor;
using UnityEngine;

namespace Pinball.EditorTools
{
    public static class ScreenGrab
    {
        public static string Grab(string name, int width = 960)
        {
            Directory.CreateDirectory("Assets/Screenshots");
            var full = ScreenCapture.CaptureScreenshotAsTexture();
            int h = Mathf.Max(2, Mathf.RoundToInt(width * full.height / (float)full.width));
            var rt = RenderTexture.GetTemporary(width, h, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(full, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var small = new Texture2D(width, h, TextureFormat.RGB24, false);
            small.ReadPixels(new Rect(0, 0, width, h), 0, 0);
            small.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            string path = "Assets/Screenshots/" + name + ".png";
            File.WriteAllBytes(path, small.EncodeToPNG());
            Object.DestroyImmediate(full);
            Object.DestroyImmediate(small);
            AssetDatabase.ImportAsset(path);
            return path;
        }

        /// <summary>Renders the game's pixel render texture through the CRT material and saves a PNG.</summary>
        public static string GrabCRT(string name, int width = 640)
        {
            var pd = Object.FindFirstObjectByType<PixelDisplay>();
            if (pd == null || pd.rt == null) return "no display";
            Directory.CreateDirectory("Assets/Screenshots");
            int h = Mathf.RoundToInt(width * 9f / 16f);
            var rt = RenderTexture.GetTemporary(width, h, 0, RenderTextureFormat.ARGB32);
            rt.filterMode = FilterMode.Point;
            if (pd.crt != null)
            {
                pd.crt.SetVector("_Texel", new Vector4(1f / PixelDisplay.W, 1f / PixelDisplay.H, PixelDisplay.W, PixelDisplay.H));
                Graphics.Blit(pd.rt, rt, pd.crt);
            }
            else Graphics.Blit(pd.rt, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            string path = "Assets/Screenshots/" + name + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            return path;
        }

        [MenuItem("Tools/Pinball/Grab Screenshot")]
        public static void GrabMenu()
        {
            Debug.Log("[Pinball] " + Grab("grab"));
        }
    }
}
