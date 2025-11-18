using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using MoonShared.Attributes;
using Microsoft.Xna.Framework;

namespace WizardryManaBar.Core
{
    [SAsset(Priority = 0)]
    public class Assets
    {
        public static Texture2D ManaBG => Game1.content.Load<Texture2D>("Mods/moonslime.ManaBarApi/textures");


        public static Texture2D ManaBarIcon => Game1.content.Load<Texture2D>("Mods/moonslime.ManaBarApi/ManaBarIcon");

        public static Texture2D ManaSymbol => Game1.content.Load<Texture2D>("Mods/moonslime.ManaBarApi/manaSymbol");
    }
}
