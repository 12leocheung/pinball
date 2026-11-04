using System.Collections.Generic;
using UnityEngine;

namespace Pinball
{
    public enum Rarity { Common, Uncommon, Rare, Legendary }

    public enum ItemKind { Bumper, Trampoline, Coin, Mult, Magnet, Hold, Wall, Lantern }

    public enum ConditionKind
    {
        BumperHits,
        SingleBallScore,
        ModuleHits,
        FullCharges,
        AliveTime,
        NoBottomScore,
        ConditionsDone,
        HoldCatches,
        RoundScore
    }

    public enum RewardKind { Mult, Coins, Ball, Score }

    [System.Serializable]
    public class BoardItemDef
    {
        public string id;
        public string name;
        public string desc;
        public Rarity rarity;
        public ItemKind kind;
        public int value;
        public int price;
        public float bounce = 8f;
        public float mult;
        public string sprite;
    }

    [System.Serializable]
    public class WildcardDef
    {
        public string id;
        public string name;
        public string desc;
        public Rarity rarity;
        public ConditionKind cond;
        public float target;
        public RewardKind reward;
        public float rewardValue;
        public int price;
        public string sprite;

        public string ConditionText()
        {
            switch (cond)
            {
                case ConditionKind.BumperHits: return "HIT BUMPERS " + Mathf.RoundToInt(target) + " TIMES";
                case ConditionKind.SingleBallScore: return "SCORE " + Mathf.RoundToInt(target) + " ON ONE BALL";
                case ConditionKind.ModuleHits: return "HIT " + Mathf.RoundToInt(target) + " OBJECTS IN A ROUND";
                case ConditionKind.FullCharges: return "LAUNCH " + Mathf.RoundToInt(target) + " MAX CHARGES";
                case ConditionKind.AliveTime: return "KEEP A BALL " + Mathf.RoundToInt(target) + "s ALIVE";
                case ConditionKind.NoBottomScore: return "SCORE " + Mathf.RoundToInt(target) + " OFF THE BOTTOM";
                case ConditionKind.ConditionsDone: return "FINISH " + Mathf.RoundToInt(target) + " WILDCARDS";
                case ConditionKind.HoldCatches: return "CATCH A BALL IN A HOLD";
                case ConditionKind.RoundScore: return "SCORE " + Mathf.RoundToInt(target) + " THIS ROUND";
                default: return "";
            }
        }

        public string RewardText()
        {
            switch (reward)
            {
                case RewardKind.Mult: return "+" + rewardValue.ToString("0.##") + " MULT";
                case RewardKind.Coins: return "+" + Mathf.RoundToInt(rewardValue) + " COINS";
                case RewardKind.Ball: return "+" + Mathf.RoundToInt(rewardValue) + " BALL";
                case RewardKind.Score: return "+" + Mathf.RoundToInt(rewardValue) + " PTS";
                default: return "";
            }
        }
    }

    [System.Serializable]
    public class BossDef
    {
        public string id;
        public string name;
        public string desc;
        public string sprite;
        public Color tint;
        public float bumperMul = 1f;
        public int ballPenalty;
        public float flipperMul = 1f;
        public float gravityMul = 1f;
        public float progressMul = 1f;
        public bool disablesRarity;
        public Rarity disabledRarity;
    }

    public class WildcardInstance
    {
        public WildcardDef def;
        public float progress;
        public bool completedThisRound;
        public bool disabled;
    }

    public static class Catalog
    {
        public static readonly BoardItemDef[] Items =
        {
            new BoardItemDef { id = "spring", name = "BRASS SPRING", rarity = Rarity.Common, kind = ItemKind.Trampoline, value = 70, price = 4, bounce = 11f, sprite = "item_spring", desc = "BOUNCES THE BALL HARD." },
            new BoardItemDef { id = "coinpress", name = "COIN PRESS", rarity = Rarity.Common, kind = ItemKind.Coin, value = 40, price = 4, bounce = 7f, sprite = "item_coinpress", desc = "+1 COIN PER HIT. 3 PER ROUND." },
            new BoardItemDef { id = "chime", name = "CHIME PEG", rarity = Rarity.Common, kind = ItemKind.Bumper, value = 55, price = 4, bounce = 7f, sprite = "item_chime", desc = "A SMALL BELL. +55." },
            new BoardItemDef { id = "candle", name = "WAX CANDLE", rarity = Rarity.Common, kind = ItemKind.Lantern, value = 24, price = 4, bounce = 5f, sprite = "item_candle", desc = "SCORES WHILE THE BALL IS NEAR." },
            new BoardItemDef { id = "ramp", name = "MARBLE RAMP", rarity = Rarity.Common, kind = ItemKind.Wall, value = 0, price = 4, bounce = 3f, sprite = "item_ramp", desc = "A SOLID RAMP. SHAPES THE BALL." },
            new BoardItemDef { id = "prism", name = "GLASS PRISM", rarity = Rarity.Uncommon, kind = ItemKind.Mult, value = 70, price = 7, bounce = 7f, mult = 0.15f, sprite = "item_prism", desc = "+0.15 MULT ON EVERY HIT." },
            new BoardItemDef { id = "lodestone", name = "LODESTONE", rarity = Rarity.Uncommon, kind = ItemKind.Magnet, value = 100, price = 7, bounce = 8f, sprite = "item_lodestone", desc = "PULLS THE BALL CLOSE. +100." },
            new BoardItemDef { id = "net", name = "LACE NET", rarity = Rarity.Uncommon, kind = ItemKind.Hold, value = 140, price = 7, bounce = 2f, sprite = "item_net", desc = "CATCHES A BALL FOR THE NEXT ROUND." },
            new BoardItemDef { id = "rail", name = "IRON RAIL", rarity = Rarity.Uncommon, kind = ItemKind.Wall, value = 0, price = 7, bounce = 3f, sprite = "item_rail", desc = "A STEEL RAIL. SHAPES THE BALL." },
            new BoardItemDef { id = "gear", name = "CLOCKWORK GEAR", rarity = Rarity.Rare, kind = ItemKind.Bumper, value = 170, price = 10, bounce = 8f, sprite = "item_gear", desc = "GEARS SCORE BIG. +170." },
            new BoardItemDef { id = "locket", name = "STAR LOCKET", rarity = Rarity.Rare, kind = ItemKind.Mult, value = 160, price = 10, bounce = 7f, mult = 0.3f, sprite = "item_locket", desc = "+0.3 MULT ON EVERY HIT." },
            new BoardItemDef { id = "mirror", name = "MIRROR SHARD", rarity = Rarity.Rare, kind = ItemKind.Bumper, value = 220, price = 10, bounce = 10f, sprite = "item_mirror", desc = "SHARP REFLECTIONS. +220." },
            new BoardItemDef { id = "trampoline", name = "VELVET TRAMPOLINE", rarity = Rarity.Legendary, kind = ItemKind.Trampoline, value = 380, price = 15, bounce = 15f, sprite = "item_trampoline", desc = "LAUNCHES THE BALL SKYWARD. +380." },
            new BoardItemDef { id = "idol", name = "GILDED IDOL", rarity = Rarity.Legendary, kind = ItemKind.Bumper, value = 550, price = 15, bounce = 8f, sprite = "item_idol", desc = "THE IDOL PAYS WELL. +550." },
            new BoardItemDef { id = "toad", name = "JADE TOAD", rarity = Rarity.Common, kind = ItemKind.Bumper, value = 85, price = 4, bounce = 7f, sprite = "item_toad", desc = "A LUCKY LITTLE GUARDIAN. +85." },
            new BoardItemDef { id = "torch", name = "TWIN TORCH", rarity = Rarity.Common, kind = ItemKind.Lantern, value = 26, price = 4, bounce = 5f, sprite = "item_torch", desc = "TWO FLAMES THAT SCORE WHEN THE BALL IS NEAR." },
            new BoardItemDef { id = "cannon", name = "COILED CANNON", rarity = Rarity.Uncommon, kind = ItemKind.Trampoline, value = 130, price = 7, bounce = 12.5f, sprite = "item_cannon", desc = "FIRES THE BALL ACROSS THE BOARD. +130." },
            new BoardItemDef { id = "honey", name = "HONEY JAR", rarity = Rarity.Rare, kind = ItemKind.Coin, value = 150, price = 10, bounce = 6.5f, sprite = "item_honey", desc = "+1 COIN PER HIT. SWEET. 3 PER ROUND." },
            new BoardItemDef { id = "starspire", name = "STARSPIRE", rarity = Rarity.Legendary, kind = ItemKind.Magnet, value = 450, price = 15, bounce = 9f, sprite = "item_starspire", desc = "PULLS THE BALL IN. RICH SLINGS. +450." },

            new BoardItemDef { id = "belljar", name = "BELL JAR", rarity = Rarity.Common, kind = ItemKind.Bumper, value = 60, price = 4, bounce = 7f, sprite = "item_belljar", desc = "A SEALED RING. +60." },
            new BoardItemDef { id = "inkbottle", name = "INK BOTTLE", rarity = Rarity.Common, kind = ItemKind.Coin, value = 35, price = 4, bounce = 6.5f, sprite = "item_inkbottle", desc = "+1 COIN PER HIT. 3 PER ROUND." },
            new BoardItemDef { id = "glowcap", name = "GLOW CAP", rarity = Rarity.Common, kind = ItemKind.Lantern, value = 22, price = 4, bounce = 5f, sprite = "item_glowcap", desc = "GLOWS NEAR THE BALL." },
            new BoardItemDef { id = "bonedie", name = "BONE DIE", rarity = Rarity.Common, kind = ItemKind.Bumper, value = 50, price = 4, bounce = 9f, sprite = "item_bonedie", desc = "ROLLS THE BALL OFF. +50." },
            new BoardItemDef { id = "kite", name = "PAPER KITE", rarity = Rarity.Common, kind = ItemKind.Trampoline, value = 65, price = 4, bounce = 10f, sprite = "item_kite", desc = "CATCHES THE BALL AND LIFTS IT." },
            new BoardItemDef { id = "brick", name = "FIRED BRICK", rarity = Rarity.Common, kind = ItemKind.Wall, value = 0, price = 4, bounce = 3f, sprite = "item_brick", desc = "A PLAIN SOLID BLOCK." },
            new BoardItemDef { id = "quill", name = "QUILL", rarity = Rarity.Common, kind = ItemKind.Lantern, value = 20, price = 4, bounce = 5f, sprite = "item_quill", desc = "SCORES WHILE THE BALL IS NEAR." },
            new BoardItemDef { id = "ironacorn", name = "IRON ACORN", rarity = Rarity.Common, kind = ItemKind.Bumper, value = 45, price = 4, bounce = 7f, sprite = "item_ironacorn", desc = "HEAVY LITTLE NUT. +45." },

            new BoardItemDef { id = "compass", name = "BRASS COMPASS", rarity = Rarity.Uncommon, kind = ItemKind.Magnet, value = 95, price = 7, bounce = 8f, sprite = "item_compass", desc = "POINTS THE BALL HOME. +95." },
            new BoardItemDef { id = "drum", name = "WAR DRUM", rarity = Rarity.Uncommon, kind = ItemKind.Bumper, value = 120, price = 7, bounce = 7.5f, sprite = "item_drum", desc = "EVERY HIT ECHOES. +120." },
            new BoardItemDef { id = "anvil", name = "SMALL ANVIL", rarity = Rarity.Uncommon, kind = ItemKind.Wall, value = 0, price = 7, bounce = 3f, sprite = "item_anvil", desc = "A DENSE BLOCK. SHAPES THE BALL." },
            new BoardItemDef { id = "harp", name = "WIND HARP", rarity = Rarity.Uncommon, kind = ItemKind.Lantern, value = 30, price = 7, bounce = 5f, sprite = "item_harp", desc = "HUMS WHEN THE BALL PASSES." },
            new BoardItemDef { id = "vial", name = "SALT VIAL", rarity = Rarity.Uncommon, kind = ItemKind.Mult, value = 65, price = 7, bounce = 7f, mult = 0.12f, sprite = "item_vial", desc = "+0.12 MULT ON EVERY HIT." },
            new BoardItemDef { id = "pinwheel", name = "PINWHEEL", rarity = Rarity.Uncommon, kind = ItemKind.Trampoline, value = 110, price = 7, bounce = 11f, sprite = "item_pinwheel", desc = "SPINS THE BALL AWAY. +110." },

            new BoardItemDef { id = "skull", name = "GLASS SKULL", rarity = Rarity.Rare, kind = ItemKind.Bumper, value = 190, price = 10, bounce = 8f, sprite = "item_skull", desc = "IT REMEMBERS EVERY HIT. +190." },
            new BoardItemDef { id = "hourglass", name = "HOURGLASS", rarity = Rarity.Rare, kind = ItemKind.Mult, value = 150, price = 10, bounce = 7f, mult = 0.22f, sprite = "item_hourglass", desc = "+0.22 MULT. TIME POURS OUT." },
            new BoardItemDef { id = "ghostlantern", name = "GHOST LANTERN", rarity = Rarity.Rare, kind = ItemKind.Lantern, value = 40, price = 10, bounce = 5f, sprite = "item_ghostlantern", desc = "A PALE FLAME THAT SCORES." },
            new BoardItemDef { id = "anchor", name = "IRON ANCHOR", rarity = Rarity.Rare, kind = ItemKind.Wall, value = 0, price = 10, bounce = 3f, sprite = "item_anchor", desc = "HOLDS THE LINE. SOLID." },
            new BoardItemDef { id = "mooncrystal", name = "MOON CRYSTAL", rarity = Rarity.Rare, kind = ItemKind.Magnet, value = 160, price = 10, bounce = 8f, sprite = "item_mooncrystal", desc = "PULLS THE BALL CLOSE. +160." },
            new BoardItemDef { id = "teapot", name = "TEA POT", rarity = Rarity.Rare, kind = ItemKind.Coin, value = 120, price = 10, bounce = 6.5f, sprite = "item_teapot", desc = "+1 COIN PER HIT. POURS COPPER." },

            new BoardItemDef { id = "phoenixegg", name = "PHOENIX EGG", rarity = Rarity.Legendary, kind = ItemKind.Trampoline, value = 300, price = 15, bounce = 14f, sprite = "item_phoenixegg", desc = "IT THROWS THE BALL SKYWARD." },
            new BoardItemDef { id = "throne", name = "TINY THRONE", rarity = Rarity.Legendary, kind = ItemKind.Bumper, value = 480, price = 15, bounce = 8f, sprite = "item_throne", desc = "THE THRONE PAYS TRIBUTE. +480." },
            new BoardItemDef { id = "orrery", name = "ORRERY", rarity = Rarity.Legendary, kind = ItemKind.Magnet, value = 400, price = 15, bounce = 9f, sprite = "item_orrery", desc = "THE PLANETS DRAG THE BALL. +400." },
            new BoardItemDef { id = "goldenslot", name = "GOLDEN SLOT", rarity = Rarity.Legendary, kind = ItemKind.Coin, value = 350, price = 15, bounce = 6f, sprite = "item_goldenslot", desc = "+1 COIN PER HIT. THE HOUSE LOSES." },
            new BoardItemDef { id = "cometstone", name = "COMET STONE", rarity = Rarity.Legendary, kind = ItemKind.Mult, value = 380, price = 15, bounce = 8f, mult = 0.45f, sprite = "item_cometstone", desc = "+0.45 MULT ON EVERY HIT." },
            new BoardItemDef { id = "monolith", name = "BLACK MONOLITH", rarity = Rarity.Legendary, kind = ItemKind.Wall, value = 0, price = 15, bounce = 3f, sprite = "item_monolith", desc = "AN UNMOVING SLAB. SOLID." },

            // ---- content pack III
            new BoardItemDef { id = "marbles", name = "TIN MARBLES", rarity = Rarity.Common, kind = ItemKind.Bumper, value = 58, price = 4, bounce = 7.2f, sprite = "item_marbles", desc = "FIVE LITTLE SPHERES. +58." },
            new BoardItemDef { id = "thimble", name = "SILVER THIMBLE", rarity = Rarity.Common, kind = ItemKind.Lantern, value = 21, price = 4, bounce = 5f, sprite = "item_thimble", desc = "SCORES WHEN THE BALL ROLLS BY." },
            new BoardItemDef { id = "jackstone", name = "JACKSTONE", rarity = Rarity.Common, kind = ItemKind.Bumper, value = 52, price = 4, bounce = 8.6f, sprite = "item_jackstone", desc = "SNAPS THE BALL AWAY. +52." },
            new BoardItemDef { id = "saltlick", name = "SALT LICK", rarity = Rarity.Common, kind = ItemKind.Lantern, value = 23, price = 4, bounce = 5f, sprite = "item_saltlick", desc = "A PALE GLOW NEAR THE BALL." },
            new BoardItemDef { id = "tinwhistle", name = "TIN WHISTLE", rarity = Rarity.Common, kind = ItemKind.Lantern, value = 20, price = 4, bounce = 5f, sprite = "item_tinwhistle", desc = "PIPES WHILE THE BALL IS NEAR." },
            new BoardItemDef { id = "tincog", name = "TIN COG", rarity = Rarity.Common, kind = ItemKind.Bumper, value = 48, price = 4, bounce = 7.8f, sprite = "item_tincog", desc = "A CHEAP LITTLE GEAR. +48." },
            new BoardItemDef { id = "domino", name = "BONE DOMINO", rarity = Rarity.Common, kind = ItemKind.Wall, value = 0, price = 4, bounce = 3f, sprite = "item_domino", desc = "A FLAT TILE. SHAPES THE BALL." },
            new BoardItemDef { id = "inkstamp", name = "INK STAMP", rarity = Rarity.Common, kind = ItemKind.Coin, value = 38, price = 4, bounce = 6.5f, sprite = "item_inkstamp", desc = "+1 COIN PER HIT. 3 PER ROUND." },
            new BoardItemDef { id = "sparkler", name = "SPARKLER", rarity = Rarity.Common, kind = ItemKind.Trampoline, value = 62, price = 4, bounce = 9.4f, sprite = "item_sparkler", desc = "POPS THE BALL UPWARD. +62." },
            new BoardItemDef { id = "teacup", name = "CHIPPED CUP", rarity = Rarity.Common, kind = ItemKind.Coin, value = 34, price = 4, bounce = 6.5f, sprite = "item_teacup", desc = "+1 COIN PER HIT. 3 PER ROUND." },
            new BoardItemDef { id = "acorncap", name = "ACORN CAP", rarity = Rarity.Common, kind = ItemKind.Bumper, value = 44, price = 4, bounce = 7.4f, sprite = "item_acorncap", desc = "A SPRINGY LITTLE CUP. +44." },
            new BoardItemDef { id = "pebbles", name = "PEBBLE PILE", rarity = Rarity.Common, kind = ItemKind.Wall, value = 0, price = 4, bounce = 3f, sprite = "item_pebbles", desc = "A STACK OF STONES. SOLID." },
            new BoardItemDef { id = "beads", name = "GLASS BEADS", rarity = Rarity.Common, kind = ItemKind.Coin, value = 36, price = 4, bounce = 6.5f, sprite = "item_beads", desc = "+1 COIN PER HIT. 3 PER ROUND." },
            new BoardItemDef { id = "mothpeg", name = "MOTH PEG", rarity = Rarity.Common, kind = ItemKind.Bumper, value = 50, price = 4, bounce = 7f, sprite = "item_mothpeg", desc = "DRAWS THE BALL IN. +50." },

            new BoardItemDef { id = "tuningfork", name = "TUNING FORK", rarity = Rarity.Uncommon, kind = ItemKind.Lantern, value = 31, price = 7, bounce = 5f, sprite = "item_tuningfork", desc = "HUM SINGS WHEN THE BALL PASSES." },
            new BoardItemDef { id = "sextant", name = "BRASS SEXTANT", rarity = Rarity.Uncommon, kind = ItemKind.Magnet, value = 98, price = 7, bounce = 8f, sprite = "item_sextant", desc = "MEASURES THE BALL HOME. +98." },
            new BoardItemDef { id = "cogclock", name = "CLOCK COG", rarity = Rarity.Uncommon, kind = ItemKind.Bumper, value = 124, price = 7, bounce = 8.2f, sprite = "item_cogclock", desc = "TICKS THE BALL ALONG. +124." },
            new BoardItemDef { id = "messagebottle", name = "MESSAGE BOTTLE", rarity = Rarity.Uncommon, kind = ItemKind.Coin, value = 105, price = 7, bounce = 6.5f, sprite = "item_messagebottle", desc = "+1 COIN PER HIT. 3 PER ROUND." },
            new BoardItemDef { id = "quiver", name = "QUILL QUIVER", rarity = Rarity.Uncommon, kind = ItemKind.Mult, value = 62, price = 7, bounce = 7f, mult = 0.13f, sprite = "item_quiver", desc = "+0.13 MULT ON EVERY HIT." },
            new BoardItemDef { id = "kettle", name = "IRON KETTLE", rarity = Rarity.Uncommon, kind = ItemKind.Bumper, value = 118, price = 7, bounce = 7.6f, sprite = "item_kettle", desc = "CLANGS ON EVERY HIT. +118." },
            new BoardItemDef { id = "fishingpole", name = "FISHING POLE", rarity = Rarity.Uncommon, kind = ItemKind.Trampoline, value = 112, price = 7, bounce = 11.4f, sprite = "item_fishingpole", desc = "HOOKS THE BALL SKYWARD. +112." },
            new BoardItemDef { id = "paperlantern", name = "PAPER LANTERN", rarity = Rarity.Uncommon, kind = ItemKind.Lantern, value = 33, price = 7, bounce = 5f, sprite = "item_paperlantern", desc = "SOFT LIGHT. STEADY SCORE." },
            new BoardItemDef { id = "gearbox", name = "GEAR BOX", rarity = Rarity.Uncommon, kind = ItemKind.Wall, value = 0, price = 7, bounce = 3f, sprite = "item_gearbox", desc = "HEAVY MACHINERY. SOLID." },
            new BoardItemDef { id = "ferrule", name = "FERRULE RING", rarity = Rarity.Uncommon, kind = ItemKind.Bumper, value = 115, price = 7, bounce = 8.4f, sprite = "item_ferrule", desc = "A BRASS BAND. +115." },
            new BoardItemDef { id = "mothjar", name = "MOTH JAR", rarity = Rarity.Uncommon, kind = ItemKind.Hold, value = 150, price = 7, bounce = 2f, sprite = "item_mothjar", desc = "CATCHES A BALL FOR THE NEXT ROUND." },
            new BoardItemDef { id = "pendulum", name = "PENDULUM", rarity = Rarity.Uncommon, kind = ItemKind.Mult, value = 68, price = 7, bounce = 7f, mult = 0.14f, sprite = "item_pendulum", desc = "+0.14 MULT. SWINGS BOTH WAYS." },

            new BoardItemDef { id = "glasswing", name = "GLASS WING", rarity = Rarity.Rare, kind = ItemKind.Bumper, value = 196, price = 10, bounce = 8.6f, sprite = "item_glasswing", desc = "PALE AND SHARP. +196." },
            new BoardItemDef { id = "starwheel", name = "STAR WHEEL", rarity = Rarity.Rare, kind = ItemKind.Mult, value = 158, price = 10, bounce = 7f, mult = 0.24f, sprite = "item_starwheel", desc = "+0.24 MULT ON EVERY HIT." },
            new BoardItemDef { id = "reliquary", name = "RELIQUARY", rarity = Rarity.Rare, kind = ItemKind.Coin, value = 128, price = 10, bounce = 6.5f, sprite = "item_reliquary", desc = "+1 COIN PER HIT. KEEPS THE TITHE." },
            new BoardItemDef { id = "astrolabe", name = "ASTROLABE", rarity = Rarity.Rare, kind = ItemKind.Magnet, value = 168, price = 10, bounce = 8f, sprite = "item_astrolabe", desc = "PULLS THE BALL CLOSE. +168." },
            new BoardItemDef { id = "greatbell", name = "GREAT BELL", rarity = Rarity.Rare, kind = ItemKind.Bumper, value = 205, price = 10, bounce = 7.4f, sprite = "item_greatbell", desc = "ONE TOLL. BIG SCORE. +205." },
            new BoardItemDef { id = "loom", name = "SILK LOOM", rarity = Rarity.Rare, kind = ItemKind.Trampoline, value = 195, price = 10, bounce = 12f, sprite = "item_loom", desc = "CATAPULTS THE BALL. +195." },
            new BoardItemDef { id = "darkmirror", name = "DARK MIRROR", rarity = Rarity.Rare, kind = ItemKind.Bumper, value = 214, price = 10, bounce = 8f, sprite = "item_darkmirror", desc = "IT SHOWS THE BALL BACK. +214." },
            new BoardItemDef { id = "ivorykey", name = "IVORY KEY", rarity = Rarity.Rare, kind = ItemKind.Mult, value = 146, price = 10, bounce = 7f, mult = 0.2f, sprite = "item_ivorykey", desc = "+0.2 MULT. UNLOCKS THE SWING." },
            new BoardItemDef { id = "tideclock", name = "TIDE CLOCK", rarity = Rarity.Rare, kind = ItemKind.Lantern, value = 42, price = 10, bounce = 5f, sprite = "item_tideclock", desc = "SCORES WITH THE TIDE." },
            new BoardItemDef { id = "preservejar", name = "PRESERVE JAR", rarity = Rarity.Rare, kind = ItemKind.Coin, value = 132, price = 10, bounce = 6.5f, sprite = "item_preservejar", desc = "+1 COIN PER HIT. KEEPS IT FRESH." },

            new BoardItemDef { id = "skywheel", name = "SKY WHEEL", rarity = Rarity.Legendary, kind = ItemKind.Trampoline, value = 330, price = 15, bounce = 14.5f, sprite = "item_skywheel", desc = "THROWS THE BALL INTO THE ROOF. +330." },
            new BoardItemDef { id = "sunstone", name = "SUNSTONE", rarity = Rarity.Legendary, kind = ItemKind.Mult, value = 360, price = 15, bounce = 8f, mult = 0.5f, sprite = "item_sunstone", desc = "+0.5 MULT ON EVERY HIT." },
            new BoardItemDef { id = "dragonhoard", name = "DRAGON HOARD", rarity = Rarity.Legendary, kind = ItemKind.Coin, value = 400, price = 15, bounce = 6f, sprite = "item_dragonhoard", desc = "+1 COIN PER HIT. IT NEVER EMPTIES." },
            new BoardItemDef { id = "obsidian", name = "OBSIDIAN SLAB", rarity = Rarity.Legendary, kind = ItemKind.Wall, value = 0, price = 15, bounce = 3f, sprite = "item_obsidian", desc = "VOLCANIC GLASS. SOLID." },
            new BoardItemDef { id = "singingheart", name = "SINGING HEART", rarity = Rarity.Legendary, kind = ItemKind.Bumper, value = 470, price = 15, bounce = 8f, sprite = "item_singingheart", desc = "EVERY HIT IS A NOTE. +470." },
            new BoardItemDef { id = "stormorb", name = "STORM ORB", rarity = Rarity.Legendary, kind = ItemKind.Magnet, value = 420, price = 15, bounce = 9f, sprite = "item_stormorb", desc = "PULLS THE BALL IN. +420." },
        };

        public static readonly WildcardDef[] Wildcards =
        {
            new WildcardDef { id = "moon", name = "MOONLIT WICK", rarity = Rarity.Common, cond = ConditionKind.BumperHits, target = 12, reward = RewardKind.Mult, rewardValue = 2f, price = 5, sprite = "wild_moon", desc = "A SMALL FLAME IN THE DARK." },
            new WildcardDef { id = "sun", name = "BELL OF PLENTY", rarity = Rarity.Common, cond = ConditionKind.SingleBallScore, target = 700, reward = RewardKind.Coins, rewardValue = 4, price = 5, sprite = "wild_sun", desc = "EVERY RING A COIN." },
            new WildcardDef { id = "comet", name = "COMET TRAIL", rarity = Rarity.Uncommon, cond = ConditionKind.ModuleHits, target = 5, reward = RewardKind.Ball, rewardValue = 1, price = 7, sprite = "wild_comet", desc = "LEAVE A LITTLE LIGHT BEHIND." },
            new WildcardDef { id = "key", name = "IRON KEY", rarity = Rarity.Uncommon, cond = ConditionKind.FullCharges, target = 2, reward = RewardKind.Mult, rewardValue = 1.5f, price = 7, sprite = "wild_key", desc = "THE HARDER THE PULL." },
            new WildcardDef { id = "mask", name = "HOLLOW BELL", rarity = Rarity.Uncommon, cond = ConditionKind.NoBottomScore, target = 1500, reward = RewardKind.Mult, rewardValue = 2f, price = 7, sprite = "wild_mask", desc = "STAY HIGH. STAY HUNGRY." },
            new WildcardDef { id = "feather", name = "ASH FEATHER", rarity = Rarity.Common, cond = ConditionKind.HoldCatches, target = 1, reward = RewardKind.Coins, rewardValue = 6, price = 5, sprite = "wild_feather", desc = "SOFTLY CAUGHT." },
            new WildcardDef { id = "eye", name = "GLASS EYE", rarity = Rarity.Rare, cond = ConditionKind.AliveTime, target = 30, reward = RewardKind.Mult, rewardValue = 3f, price = 10, sprite = "wild_eye", desc = "IT WATCHES THE BALL." },
            new WildcardDef { id = "crown", name = "CRACKED CROWN", rarity = Rarity.Rare, cond = ConditionKind.RoundScore, target = 2500, reward = RewardKind.Coins, rewardValue = 12, price = 10, sprite = "wild_crown", desc = "BROKEN BUT STILL GOLD." },
            new WildcardDef { id = "flame", name = "GILDED HAND", rarity = Rarity.Legendary, cond = ConditionKind.RoundScore, target = 5000, reward = RewardKind.Coins, rewardValue = 20, price = 16, sprite = "wild_flame", desc = "EVERYTHING IT TOUCHES." },
            new WildcardDef { id = "bell", name = "TIDE MASK", rarity = Rarity.Legendary, cond = ConditionKind.ConditionsDone, target = 2, reward = RewardKind.Ball, rewardValue = 1, price = 16, sprite = "wild_bell", desc = "THE TIDE REMEMBERS." },
            new WildcardDef { id = "acorn", name = "COPPER ACORN", rarity = Rarity.Common, cond = ConditionKind.BumperHits, target = 20, reward = RewardKind.Coins, rewardValue = 3, price = 4, sprite = "wild_acorn", desc = "A LUCKY LITTLE NUT." },
            new WildcardDef { id = "shell", name = "TIDE SHELL", rarity = Rarity.Common, cond = ConditionKind.RoundScore, target = 1200, reward = RewardKind.Mult, rewardValue = 1, price = 5, sprite = "wild_shell", desc = "IT HUMS WHEN THE SCORE RISES." },
            new WildcardDef { id = "wind", name = "STORM CHIME", rarity = Rarity.Uncommon, cond = ConditionKind.AliveTime, target = 20, reward = RewardKind.Coins, rewardValue = 7, price = 7, sprite = "wild_wind", desc = "THE WIND KEEPS COUNT." },
            new WildcardDef { id = "vine", name = "GRAPEVINE", rarity = Rarity.Rare, cond = ConditionKind.ModuleHits, target = 7, reward = RewardKind.Score, rewardValue = 1200, price = 10, sprite = "wild_vine", desc = "EVERYTHING CONNECTS." },
            new WildcardDef { id = "sand", name = "SANDGLASS", rarity = Rarity.Legendary, cond = ConditionKind.FullCharges, target = 4, reward = RewardKind.Mult, rewardValue = 4, price = 15, sprite = "wild_sand", desc = "TIME DRAINS. POWER POURS." },
            new WildcardDef { id = "wanlantern", name = "WARD LANTERN", rarity = Rarity.Common, cond = ConditionKind.BumperHits, target = 15, reward = RewardKind.Coins, rewardValue = 4, price = 5, sprite = "wild_lantern", desc = "A LIGHT LEFT BURNING." },
            new WildcardDef { id = "moth", name = "MOTH WING", rarity = Rarity.Common, cond = ConditionKind.ModuleHits, target = 4, reward = RewardKind.Coins, rewardValue = 5, price = 4, sprite = "wild_moth", desc = "DRAWN TO THE LIGHTS." },
            new WildcardDef { id = "knot", name = "KNOT OF WOOL", rarity = Rarity.Uncommon, cond = ConditionKind.AliveTime, target = 25, reward = RewardKind.Mult, rewardValue = 1.2f, price = 7, sprite = "wild_knot", desc = "KEEP IT TANGLED." },
            new WildcardDef { id = "tideglass", name = "TIDEGLASS", rarity = Rarity.Rare, cond = ConditionKind.FullCharges, target = 3, reward = RewardKind.Coins, rewardValue = 10, price = 10, sprite = "wild_tide", desc = "THE TIDE PULLS IN." },
            new WildcardDef { id = "fallenstar", name = "FALLEN STAR", rarity = Rarity.Rare, cond = ConditionKind.SingleBallScore, target = 3000, reward = RewardKind.Mult, rewardValue = 2.5f, price = 10, sprite = "wild_star", desc = "IT FELL FOR YOU." },
            new WildcardDef { id = "silkworm", name = "SILK WORM", rarity = Rarity.Legendary, cond = ConditionKind.ConditionsDone, target = 3, reward = RewardKind.Mult, rewardValue = 3f, price = 16, sprite = "wild_worm", desc = "TWO WILDCARDS FED IT. NOW THREE." },

            // ---- content pack III
            new WildcardDef { id = "inkquill", name = "INK QUILL", rarity = Rarity.Common, cond = ConditionKind.BumperHits, target = 30, reward = RewardKind.Coins, rewardValue = 5, price = 5, sprite = "wild_inkquill", desc = "SIGNED IN COPPER." },
            new WildcardDef { id = "bonecharm", name = "BONE CHARM", rarity = Rarity.Common, cond = ConditionKind.ModuleHits, target = 6, reward = RewardKind.Mult, rewardValue = 1.1f, price = 4, sprite = "wild_bonecharm", desc = "A SMALL LUCKY THING." },
            new WildcardDef { id = "copperring", name = "COPPER RING", rarity = Rarity.Common, cond = ConditionKind.RoundScore, target = 1800, reward = RewardKind.Coins, rewardValue = 6, price = 5, sprite = "wild_copperring", desc = "WORTH ITS WEIGHT." },
            new WildcardDef { id = "glasspetal", name = "GLASS PETAL", rarity = Rarity.Uncommon, cond = ConditionKind.AliveTime, target = 22, reward = RewardKind.Mult, rewardValue = 1.4f, price = 7, sprite = "wild_glasspetal", desc = "FRAGILE. PATIENT. BRIGHT." },
            new WildcardDef { id = "waxseal", name = "WAX SEAL", rarity = Rarity.Uncommon, cond = ConditionKind.FullCharges, target = 2, reward = RewardKind.Score, rewardValue = 900, price = 7, sprite = "wild_waxseal", desc = "PRESSED AND STAMPED." },
            new WildcardDef { id = "ironvow", name = "IRON VOW", rarity = Rarity.Uncommon, cond = ConditionKind.SingleBallScore, target = 2200, reward = RewardKind.Coins, rewardValue = 9, price = 7, sprite = "wild_ironvow", desc = "KEEP IT ON ONE BALL." },
            new WildcardDef { id = "mothlight", name = "MOTH LIGHT", rarity = Rarity.Rare, cond = ConditionKind.HoldCatches, target = 1, reward = RewardKind.Mult, rewardValue = 1.6f, price = 10, sprite = "wild_mothlight", desc = "CAUGHT IN THE GLOW." },
            new WildcardDef { id = "deepbell", name = "DEEP BELL", rarity = Rarity.Rare, cond = ConditionKind.ConditionsDone, target = 2, reward = RewardKind.Coins, rewardValue = 14, price = 10, sprite = "wild_deepbell", desc = "IT RINGS FROM BELOW." },
            new WildcardDef { id = "sunwheel", name = "SUN WHEEL", rarity = Rarity.Rare, cond = ConditionKind.RoundScore, target = 4200, reward = RewardKind.Coins, rewardValue = 18, price = 10, sprite = "wild_sunwheel", desc = "SPIN IT UP." },
            new WildcardDef { id = "blackflame", name = "BLACK FLAME", rarity = Rarity.Legendary, cond = ConditionKind.BumperHits, target = 60, reward = RewardKind.Mult, rewardValue = 3.5f, price = 16, sprite = "wild_blackflame", desc = "IT BURNS WITHOUT LIGHT." },
            new WildcardDef { id = "sundial", name = "SUN DIAL", rarity = Rarity.Legendary, cond = ConditionKind.AliveTime, target = 45, reward = RewardKind.Score, rewardValue = 5000, price = 16, sprite = "wild_sundial", desc = "FORTY FIVE SECONDS OF GRACE." },
            new WildcardDef { id = "kingseye", name = "KINGS EYE", rarity = Rarity.Legendary, cond = ConditionKind.SingleBallScore, target = 6000, reward = RewardKind.Coins, rewardValue = 25, price = 16, sprite = "wild_kingseye", desc = "ONE BALL. ONE CROWN." },
        };

        public static readonly BossDef[] Bosses =
        {
            new BossDef { id = "collector", name = "THE COLLECTOR", desc = "UNCOMMON WILDCARDS OFF.", sprite = "boss_collector", tint = Pal.Rose, disablesRarity = true, disabledRarity = Rarity.Uncommon },
            new BossDef { id = "eclipse", name = "ECLIPSE", desc = "RARE WILDCARDS OFF. BUMPERS -40%.", sprite = "boss_eclipse", tint = Pal.Gold, disablesRarity = true, disabledRarity = Rarity.Rare, bumperMul = 0.6f },
            new BossDef { id = "warden", name = "THE WARDEN", desc = "ONE LESS BALL. FLIPPERS -30%.", sprite = "boss_warden", tint = Pal.Sky, ballPenalty = 1, flipperMul = 0.7f },
            new BossDef { id = "undertow", name = "UNDERTOW", desc = "GRAVITY +40%. WILDCARDS -50%.", sprite = "boss_undertow", tint = Pal.Teal, gravityMul = 1.4f, progressMul = 0.5f },
            new BossDef { id = "hollow", name = "THE HOLLOW", desc = "LEGENDARY OFF. SCORING -25%.", sprite = "boss_hollow", tint = Pal.Lilac, disablesRarity = true, disabledRarity = Rarity.Legendary, bumperMul = 0.75f },
            new BossDef { id = "gardener", name = "THE GARDENER", desc = "BUMPERS -20%. FLIPPERS -15%.", sprite = "boss_gardener", tint = Pal.Mint, bumperMul = 0.8f, flipperMul = 0.85f },
        };

        public static BoardItemDef RandomItem(System.Random rng)
        {
            float roll = (float)rng.NextDouble();
            Rarity r = Rarity.Common;
            if (roll > 0.94f) r = Rarity.Legendary;
            else if (roll > 0.78f) r = Rarity.Rare;
            else if (roll > 0.5f) r = Rarity.Uncommon;
            var pool = new List<BoardItemDef>();
            foreach (var it in Items) if (it.rarity == r) pool.Add(it);
            if (pool.Count == 0) pool.Add(Items[0]);
            return pool[rng.Next(pool.Count)];
        }

        public static WildcardDef RandomWildcard(System.Random rng)
        {
            float roll = (float)rng.NextDouble();
            Rarity r = Rarity.Common;
            if (roll > 0.95f) r = Rarity.Legendary;
            else if (roll > 0.8f) r = Rarity.Rare;
            else if (roll > 0.52f) r = Rarity.Uncommon;
            var pool = new List<WildcardDef>();
            foreach (var w in Wildcards) if (w.rarity == r) pool.Add(w);
            if (pool.Count == 0) pool.Add(Wildcards[0]);
            return pool[rng.Next(pool.Count)];
        }

        public static BoardItemDef ItemById(string id)
        {
            foreach (var i in Items) if (i.id == id) return i;
            return null;
        }

        public static WildcardDef WildById(string id)
        {
            foreach (var w in Wildcards) if (w.id == id) return w;
            return null;
        }

        public static BossDef BossForRound(int round)
        {
            if (round % 5 != 0) return null;
            int index = (round / 5 - 1) % Bosses.Length;
            return Bosses[index];
        }
    }
}
