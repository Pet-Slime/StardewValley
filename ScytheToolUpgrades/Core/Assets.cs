using MoonShared.Asset;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;

namespace ScytheToolUpgrades
{
    [AssetClass]
    public class Assets
    {
        [AssetProperty("assets/sprites.png", Priority = AssetLoadPriority.Medium)]
        public Texture2D Sprites { get; set; }
        public string SpritesPath { get; set; }
    }
}
