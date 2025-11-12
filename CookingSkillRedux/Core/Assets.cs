using Microsoft.Xna.Framework.Graphics;
using MoonShared.Attributes;
using StardewValley;

namespace CookingSkillRedux
{
    [SAsset(Priority = 0)]
    public class Assets
    {
        public Texture2D IconA => Game1.content.Load<Texture2D>("Mods/moonslime.CookingSkill/interface/cookingiconA");

        public Texture2D IconB_0 => Game1.content.Load<Texture2D>("Mods/moonslime.CookingSkill/interface/cookingiconB_0");

        public Texture2D IconB_1 => Game1.content.Load<Texture2D>("Mods/moonslime.CookingSkill/interface/cookingiconB_1");

        public Texture2D IconB_2 => Game1.content.Load<Texture2D>("Mods/moonslime.CookingSkill/interface/cookingiconB_2");

        public Texture2D Cooking5a => Game1.content.Load<Texture2D>("Mods/moonslime.CookingSkill/interface/cooking5a");

        public Texture2D Cooking5b => Game1.content.Load<Texture2D>("Mods/moonslime.CookingSkill/interface/cooking5b");


        public Texture2D Cooking10a1 => Game1.content.Load<Texture2D>("Mods/moonslime.CookingSkill/interface/cooking10a1");


        public Texture2D Cooking10a2 => Game1.content.Load<Texture2D>("Mods/moonslime.CookingSkill/interface/cooking10a2");


        public Texture2D Cooking10b1 => Game1.content.Load<Texture2D>("Mods/moonslime.CookingSkill/interface/cooking10b1");

        public Texture2D Cooking10b2 => Game1.content.Load<Texture2D>("Mods/moonslime.CookingSkill/interface/cooking10b2");


        public Texture2D Random_Buff => Game1.content.Load<Texture2D>("Mods/moonslime.CookingSkill/interface/random_buff");

    }
}
