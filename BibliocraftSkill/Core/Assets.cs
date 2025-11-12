using Microsoft.Xna.Framework.Graphics;
using MoonShared.Attributes;
using StardewValley;

namespace BibliocraftSkill.Core
{
    [SAsset(Priority = 0)]
    public class Assets
    {
        public static Texture2D IconA => Game1.content.Load<Texture2D>("Mods/moonslime.BibliocraftSkill/interface/BookiconA");

        public static Texture2D IconB => Game1.content.Load<Texture2D>("Mods/moonslime.BibliocraftSkill/interface/BookiconB");

        public static Texture2D Book5a => Game1.content.Load<Texture2D>("Mods/moonslime.BibliocraftSkill/interface/Book5a");
        public static Texture2D Book5b => Game1.content.Load<Texture2D>("Mods/moonslime.BibliocraftSkill/interface/Book5b");
        public static Texture2D Book10a1 => Game1.content.Load<Texture2D>("Mods/moonslime.BibliocraftSkill/interface/Book10a1");
        public static Texture2D Book10a2 => Game1.content.Load<Texture2D>("Mods/moonslime.BibliocraftSkill/interface/Book10a2");
        public static Texture2D Book10b1  => Game1.content.Load<Texture2D>("Mods/moonslime.BibliocraftSkill/interface/Book10b1");
        public static Texture2D Book10b2 => Game1.content.Load<Texture2D>("Mods/moonslime.BibliocraftSkill/interface/Book10b2");


        public static Texture2D Bookworm_buff => Game1.content.Load<Texture2D>("Mods/moonslime.BibliocraftSkill/interface/bookworm_buff");

    }
}
