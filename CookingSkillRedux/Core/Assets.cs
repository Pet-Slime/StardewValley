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

namespace CookingSkill
{
    [AssetClass]
    public class Assets
    {
        [AssetProperty("assets/cookingiconA.png")]
        public Texture2D IconA { get; set; }

        [AssetProperty("assets/cookingiconB.png")]
        public Texture2D IconB { get; set; }

        [AssetProperty("assets/cooking5a.png")]
        public Texture2D Cooking5a { get; set; }

        [AssetProperty("assets/cooking5b.png")]
        public Texture2D Cooking5b { get; set; }

        [AssetProperty("assets/cooking10a1.png")]
        public Texture2D Cooking10a1 { get; set; }

        [AssetProperty("assets/cooking10a2.png")]
        public Texture2D Cooking10a2 { get; set; }

        [AssetProperty("assets/cooking10b1.png")]
        public Texture2D Cooking10b1 { get; set; }

        [AssetProperty("assets/cooking10b2.png")]
        public Texture2D Cooking10b2 { get; set; }

        // Prestige Icons
        [AssetProperty("assets/cooking5aP.png")]
        public Texture2D Cooking5aP { get; set; }

        [AssetProperty("assets/cooking5bP.png")]
        public Texture2D Cooking5bP { get; set; }

        [AssetProperty("assets/cooking10a1P.png")]
        public Texture2D Cooking10a1P { get; set; }

        [AssetProperty("assets/cooking10a2P.png")]
        public Texture2D Cooking10a2P { get; set; }

        [AssetProperty("assets/cooking10b1P.png")]
        public Texture2D Cooking10b1P { get; set; }

        [AssetProperty("assets/cooking10b2P.png")]
        public Texture2D Cooking10b2P { get; set; }

        [AssetProperty("assets/itemDefinitions.json")]
        public Dictionary<string, List<string>> ItemDefinitions { get; set; }

        [AssetProperty("assets/CookingSkillLevelUpRecipes.json")]
        public Dictionary<string, List<string>> CookingSkillLevelUpRecipes { get; set; }

        public static string ObjectsPackPath { get; private set; } = "assets/ObjectsPack";

    }
}
