using System.Collections.Generic;
using BirbCore.Attributes;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace ArchaeologySkill
{
    [SAsset(Priority = 0)]
    public class Assets
    {

        public Texture2D IconA => Game1.content.Load<Texture2D>("Mods/moonslime.ArchaeologySkill/interface/ArchaeologyiconA");

        public Texture2D IconB => Game1.content.Load<Texture2D>("Mods/moonslime.ArchaeologySkill/interface/ArchaeologyiconB");

        public Texture2D IconBalt => Game1.content.Load<Texture2D>("Mods/moonslime.ArchaeologySkill/interface/ArchaeologyiconBalt");

        public Texture2D Archaeology5a => Game1.content.Load<Texture2D>("Mods/moonslime.ArchaeologySkill/interface/Archaeology5a");

        public Texture2D Archaeology5b => Game1.content.Load<Texture2D>("Mods/moonslime.ArchaeologySkill/interface/Archaeology5b");

        public Texture2D Archaeology10a1 => Game1.content.Load<Texture2D>("Mods/moonslime.ArchaeologySkill/interface/Archaeology10a1");

        public Texture2D Archaeology10a2 => Game1.content.Load<Texture2D>("Mods/moonslime.ArchaeologySkill/interface/Archaeology10a2");

        public Texture2D Archaeology10b1 => Game1.content.Load<Texture2D>("Mods/moonslime.ArchaeologySkill/interface/Archaeology10b1");

        public Texture2D Archaeology10b2 => Game1.content.Load<Texture2D>("Mods/moonslime.ArchaeologySkill/interface/Archaeology10b2");

        public Texture2D Gold_Rush_Buff => Game1.content.Load<Texture2D>("Mods/moonslime.ArchaeologySkill/interface/gold_rush");



        [SAsset.Asset("assets/itemDefinitions.json")]
        public Dictionary<string, List<string>> ItemDefinitions { get; set; }




    }
}
