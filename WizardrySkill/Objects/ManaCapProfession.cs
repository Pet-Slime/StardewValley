using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WizardrySkill.Framework;
using Microsoft.Xna.Framework.Graphics;
using MoonShared;
using SpaceCore;
using StardewModdingAPI;
using StardewValley;

namespace WizardrySkill.Objects
{
    public class ManaCapProfession : KeyedProfession
    {
        /*********
        ** Public methods
        *********/
        public ManaCapProfession(Skills.Skill skill, string theId, Texture2D icon, ITranslationHelper i18n)
            : base(skill, theId, icon, i18n) { }

        public override void DoImmediateProfessionPerk()
        {
            Game1.player.SetMaxMana(Game1.player.GetMaxMana() + 100);
            Game1.player.SetManaToMax();
            base.DoImmediateProfessionPerk();
        }

        public override void UndoImmediateProfessionPerk()
        {
            Game1.player.SetMaxMana(Game1.player.GetMaxMana() - 100);
            Game1.player.SetManaToMax();
            base.UndoImmediateProfessionPerk();
        }
    }
}

