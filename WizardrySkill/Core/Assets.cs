using BirbCore.Attributes;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace WizardrySkill.Core
{
    [SAsset(Priority = 0)]
    public class Assets
    {
        public Texture2D IconA => Game1.content.Load<Texture2D>("Mods/moonslime.WizardrySkill/interface/MagiciconA");

        public Texture2D IconB => Game1.content.Load<Texture2D>("Mods/moonslime.WizardrySkill/interface/MagiciconB");

        public Texture2D Magic5a => Game1.content.Load<Texture2D>("Mods/moonslime.WizardrySkill/interface/Magic5a");

        public Texture2D Magic5b => Game1.content.Load<Texture2D>("Mods/moonslime.WizardrySkill/interface/Magic5b");

        public Texture2D Magic10a1 => Game1.content.Load<Texture2D>("Mods/moonslime.WizardrySkill/interface/Magic10a1");

        public Texture2D Magic10a2 => Game1.content.Load<Texture2D>("Mods/moonslime.WizardrySkill/interface/Magic10a2");

        public Texture2D Magic10b1 => Game1.content.Load<Texture2D>("Mods/moonslime.WizardrySkill/interface/Magic10b1");

        public Texture2D Magic10b2 => Game1.content.Load<Texture2D>("Mods/moonslime.WizardrySkill/interface/Magic10b2");



        public Texture2D CloudMount => Game1.content.Load<Texture2D>("Mods/moonslime.WizardrySkill/entities/cloud");

        public Texture2D Spellbg => Game1.content.Load<Texture2D>("Mods/moonslime.WizardrySkill/interface/spellbg");

        public Texture2D Manabg => Game1.content.Load<Texture2D>("Mods/moonslime.WizardrySkill/interface/manabg");
        public Texture2D SpellMenubg => Game1.content.Load<Texture2D>("Mods/moonslime.WizardrySkill/interface/spellMenubg");


    }
}
