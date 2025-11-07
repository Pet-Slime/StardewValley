using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using MoonShared.Attributes;

namespace WizardryManaBar.Core
{
    [SAsset(Priority = 0)]
    public class Assets
    {
        public Texture2D ManaBG => Game1.content.Load<Texture2D>("Mods/moonslime.ManaBarApi/textures");

        public Texture2D ManaSymbol => Game1.content.Load<Texture2D>("Mods/moonslime.ManaBarApi/manaSymbol");
    }
}
