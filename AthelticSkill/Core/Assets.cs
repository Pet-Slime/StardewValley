using BirbCore.Attributes;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace AthleticSkill.Core
{
    [SAsset(Priority = 0)]
    public class Assets
    {
        public Texture2D IconA  => Game1.content.Load<Texture2D>("Mods/moonslime.AthleticSkill/interface/AthleticiconA");

        public Texture2D SprintingIcon1 => Game1.content.Load<Texture2D>("Mods/moonslime.AthleticSkill/interface/SprintingIcon1");
        public Texture2D SprintingIcon2 => Game1.content.Load<Texture2D>("Mods/moonslime.AthleticSkill/interface/SprintingIcon2");

        public Texture2D IconB => Game1.content.Load<Texture2D>("Mods/moonslime.AthleticSkill/interface/AthleticiconB");
        public Texture2D IconB_alt => Game1.content.Load<Texture2D>("Mods/moonslime.AthleticSkill/interface/AthleticiconB_alt");

        public Texture2D Athletic5a => Game1.content.Load<Texture2D>("Mods/moonslime.AthleticSkill/interface/Athletic5a");

        public Texture2D Athletic5b => Game1.content.Load<Texture2D>("Mods/moonslime.AthleticSkill/interface/Athletic5b");

        public Texture2D Athletic10a1 => Game1.content.Load<Texture2D>("Mods/moonslime.AthleticSkill/interface/Athletic10a1");

        public Texture2D Athletic10a1_alt => Game1.content.Load<Texture2D>("Mods/moonslime.AthleticSkill/interface/Athletic10a1_alt");

        public Texture2D Athletic10a2 => Game1.content.Load<Texture2D>("Mods/moonslime.AthleticSkill/interface/Athletic10a2");

        public Texture2D Athletic10b1 => Game1.content.Load<Texture2D>("Mods/moonslime.AthleticSkill/interface/Athletic10b1");

        public Texture2D Athletic10b2 => Game1.content.Load<Texture2D>("Mods/moonslime.AthleticSkill/interface/Athletic10b2");
    }
}
