using Microsoft.Xna.Framework.Graphics;
using MoonShared.Attributes;
using StardewValley;

namespace LuckSkill.Core
{
    [SAsset(Priority = 0)]
    public class Assets
    {

        public static Texture2D IconA => Game1.content.Load<Texture2D>("Mods/moonslime.LuckSkill/interface/LuckiconA");

        public static Texture2D IconB => Game1.content.Load<Texture2D>("Mods/moonslime.LuckSkill/interface/Luckiconb");

        public static Texture2D Luck5a => Game1.content.Load<Texture2D>("Mods/moonslime.LuckSkill/interface/Luck5a");
        public static Texture2D Luck5b => Game1.content.Load<Texture2D>("Mods/moonslime.LuckSkill/interface/Luck5b");
        public static Texture2D Luck10a1 => Game1.content.Load<Texture2D>("Mods/moonslime.LuckSkill/interface/Luck10a1");
        public static Texture2D Luck10a2 => Game1.content.Load<Texture2D>("Mods/moonslime.LuckSkill/interface/Luck10a2");
        public static Texture2D Luck10b1 => Game1.content.Load<Texture2D>("Mods/moonslime.LuckSkill/interface/Luck10b1");
        public static Texture2D Luck10b2 => Game1.content.Load<Texture2D>("Mods/moonslime.LuckSkill/interface/Luck10b2");
    }
}
