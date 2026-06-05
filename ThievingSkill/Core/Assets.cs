using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using MoonShared.Attributes;
using StardewValley;

namespace ThievingSkill.Core
{
    [SAsset(Priority = 0)]
    public class Assets
    {

        public Texture2D IconA => Game1.content.Load<Texture2D>("Mods/moonslime.ThievingSkill/interface/ThievingiconA");

        public Texture2D IconB1 => Game1.content.Load<Texture2D>("Mods/moonslime.ThievingSkill/interface/ThievingiconB1");
        public Texture2D IconB2 => Game1.content.Load<Texture2D>("Mods/moonslime.ThievingSkill/interface/ThievingiconB2");
        public Texture2D IconB3 => Game1.content.Load<Texture2D>("Mods/moonslime.ThievingSkill/interface/ThievingiconB3");

        public Texture2D Thieving5a => Game1.content.Load<Texture2D>("Mods/moonslime.ThievingSkill/interface/Thieving5a");

        public Texture2D Thieving5b => Game1.content.Load<Texture2D>("Mods/moonslime.ThievingSkill/interface/Thieving5b");

        public Texture2D Thieving10a1 => Game1.content.Load<Texture2D>("Mods/moonslime.ThievingSkill/interface/Thieving10a1");

        public Texture2D Thieving10a2 => Game1.content.Load<Texture2D>("Mods/moonslime.ThievingSkill/interface/Thieving10a2");

        public Texture2D Thieving10b1 => Game1.content.Load<Texture2D>("Mods/moonslime.ThievingSkill/interface/Thieving10b1");

        public Texture2D Thieving10b2 => Game1.content.Load<Texture2D>("Mods/moonslime.ThievingSkill/interface/Thieving10b2");
    }
}
