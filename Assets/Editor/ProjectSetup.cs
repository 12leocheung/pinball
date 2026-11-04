using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Pinball.EditorTools
{
    public static class ProjectSetup
    {
        [MenuItem("Tools/Pinball/2 - Build Game Scene", false, 2)]
        public static void BuildGameScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("Game");
            go.AddComponent<Pinball.Game>();
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Game.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[Pinball] Game scene built at Assets/Scenes/Game.unity");
        }

        [MenuItem("Tools/Pinball/3 - Generate Art + Scene", false, 3)]
        public static void FullSetup()
        {
            PixelArtGenerator.Generate();
            BuildGameScene();
        }
    }
}
