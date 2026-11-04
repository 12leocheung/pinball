using UnityEngine;

namespace Pinball
{
    public class Game : MonoBehaviour
    {
        void Awake()
        {
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 1;
            Physics2D.gravity = new Vector2(0f, -9.81f);
            Physics2D.velocityIterations = 8;
            Physics2D.positionIterations = 4;

            var displayGO = new GameObject("Display");
            displayGO.transform.SetParent(transform, false);
            var display = displayGO.AddComponent<PixelDisplay>();
            display.Build();

            var juiceGO = new GameObject("Juice");
            juiceGO.transform.SetParent(display.worldRoot, false);
            juiceGO.AddComponent<Juice>();

            var gmGO = new GameObject("Run");
            gmGO.transform.SetParent(transform, false);
            var gm = gmGO.AddComponent<GameManager>();
            gm.Boot();
        }
    }
}
