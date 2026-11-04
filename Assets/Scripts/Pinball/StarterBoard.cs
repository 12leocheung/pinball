using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pinball
{
    /// <summary>
    /// The starter table layout: where the bumpers and pins sit when a run begins.
    /// The Starter Board editor (Tools/Pinball) saves fixture positions into this
    /// asset, and PinballTable.Build reads them. An empty asset keeps the table's
    /// built-in default layout.
    /// </summary>
    [CreateAssetMenu(fileName = "StarterBoard", menuName = "Pinball/Starter Board")]
    public class StarterBoard : ScriptableObject
    {
        [Serializable]
        public class Fixture
        {
            public string kind = "bumper";   // "bumper" | "pin" | "smallpin"
            public float x;
            public float y;

            public Vector2 Pos { get { return new Vector2(x, y); } set { x = value.x; y = value.y; } }
        }

        public List<Fixture> fixtures = new List<Fixture>();

        static StarterBoard _loaded;
        public static StarterBoard Instance
        {
            get
            {
                if (_loaded == null) _loaded = Resources.Load<StarterBoard>("StarterBoard");
                return _loaded;
            }
        }

        /// <summary>The table's built-in default field - also what the editor seeds for first-time editing.</summary>
        public static List<Fixture> DefaultFixtures()
        {
            return new List<Fixture>
            {
                new Fixture { kind = "bumper", x = -3.9f, y = -0.5f },
                new Fixture { kind = "bumper", x = 2.5f, y = -0.5f },
                new Fixture { kind = "pin", x = -2.5f, y = 5.5f },
                new Fixture { kind = "pin", x = 0.9f, y = 4.6f },
                new Fixture { kind = "pin", x = -4.5f, y = 1.5f },
                new Fixture { kind = "pin", x = 3.5f, y = 1.5f },
                new Fixture { kind = "smallpin", x = -3.9f, y = -3.0f },
                new Fixture { kind = "smallpin", x = 2.5f, y = -3.0f },
            };
        }

        public bool HasFixtures
        {
            get { return fixtures != null && fixtures.Count > 0; }
        }

        public void AddFixture(string kind, Vector2 pos)
        {
            if (fixtures == null) fixtures = new List<Fixture>();
            fixtures.Add(new Fixture { kind = kind, x = pos.x, y = pos.y });
        }

        public void SetFixture(int index, string kind, Vector2 pos)
        {
            if (fixtures == null) fixtures = new List<Fixture>();
            if (index >= 0 && index < fixtures.Count)
            {
                fixtures[index].kind = kind;
                fixtures[index].x = pos.x;
                fixtures[index].y = pos.y;
            }
        }

        /// <summary>The fixture nearest to pos, or null beyond maxDist.</summary>
        public Fixture Nearest(Vector2 pos, float maxDist, out int index)
        {
            index = -1;
            if (fixtures == null || fixtures.Count == 0) return null;
            Fixture best = null;
            float bestD = maxDist;
            for (int i = 0; i < fixtures.Count; i++)
            {
                float d = Vector2.Distance(fixtures[i].Pos, pos);
                if (d <= bestD) { bestD = d; best = fixtures[i]; index = i; }
            }
            return best;
        }

        public void RemoveAt(int index)
        {
            if (fixtures != null && index >= 0 && index < fixtures.Count) fixtures.RemoveAt(index);
        }

        public void Clear()
        {
            if (fixtures == null) fixtures = new List<Fixture>();
            fixtures.Clear();
        }
    }
}
