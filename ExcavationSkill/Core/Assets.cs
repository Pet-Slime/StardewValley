using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MoonShared.Asset;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace ExcavationSkill
{
    [AssetClass]
    public class Assets
    {
        [AssetProperty("assets/excavationiconA.png")]
        public Texture2D IconA { get; set; }

        [AssetProperty("assets/excavationiconB.png")]
        public Texture2D IconB { get; set; }

        [AssetProperty("assets/excavationiconBalt.png")]
        public Texture2D IconBalt { get; set; }

        [AssetProperty("assets/Excavation5a.png")]
        public Texture2D Excavation5a { get; set; }
        [AssetProperty("assets/Excavation5b.png")]
        public Texture2D Excavation5b { get; set; }
        [AssetProperty("assets/Excavation10a1.png")]
        public Texture2D Excavation10a1 { get; set; }
        [AssetProperty("assets/Excavation10a2.png")]
        public Texture2D Excavation10a2 { get; set; }
        [AssetProperty("assets/Excavation10b1.png")]
        public Texture2D Excavation10b1 { get; set; }
        [AssetProperty("assets/Excavation10b2.png")]
        public Texture2D Excavation10b2 { get; set; }

        // Prestige Icons
        [AssetProperty("assets/Excavation5aP.png")]
        public Texture2D Excavation5aP { get; set; }
        [AssetProperty("assets/Excavation5bP.png")]
        public Texture2D Excavation5bP { get; set; }
        [AssetProperty("assets/Excavation10a1P.png")]
        public Texture2D Excavation10a1P { get; set; }
        [AssetProperty("assets/Excavation10a2P.png")]
        public Texture2D Excavation10a2P { get; set; }
        [AssetProperty("assets/Excavation10b1P.png")]
        public Texture2D Excavation10b1P { get; set; }
        [AssetProperty("assets/Excavation10b2P.png")]
        public Texture2D Excavation10b2P { get; set; }



        [AssetProperty("assets/Flooring.png")]
        public Texture2D Flooring { get; set; }
        [AssetProperty("assets/Flooring_winter.png")]
        public Texture2D FlooringWinter { get; set; }



        [AssetProperty("assets/tilesheet.png")]
        public Texture2D tilesheet { get; set; }

        [AssetProperty("assets/totem_volcano_warp.png", Priority = AssetLoadPriority.Medium)]
        public Texture2D Totem_volcano_warp { get; set; }
        public string Totem_volcano_warpPath { get; set; }



        [AssetProperty("assets/itemDefinitions.json")]
        public Dictionary<string, List<string>> ItemDefinitions { get; set; }

        [AssetProperty("assets/excavationSkillLevelUpRecipes.json")]
        public Dictionary<string, List<string>> ExcavationSkillLevelUpRecipes { get; set; }

        public static string BigCraftablesPackPath { get; private set; } = "assets/BigCraftablesPack";
        public static string FencesPackPath { get; private set; } = "assets/FencesPack";
        public static string ObjectsPackPath { get; private set; } = "assets/ObjectsPack";
        public static string PFWPackPath { get; private set; } = "assets/PFWPack";
        public static string DGAPackPath { get; private set; } = "assets/DGAPack";

    }
}
