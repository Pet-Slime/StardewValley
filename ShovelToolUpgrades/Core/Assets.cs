using MoonShared.Asset;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;

namespace ShovelToolUpgrades
{
    [AssetClass]
    public class Assets
    {
        [AssetProperty("assets/tool_sprites.png", Priority = AssetLoadPriority.Medium)]
        public Texture2D ToolSprites { get; set; }
        public string ToolSpritesPath { get; set; }
    }
}
