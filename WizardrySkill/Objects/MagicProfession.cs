using Microsoft.Xna.Framework.Graphics;
using MoonShared;
using SpaceCore;
using StardewModdingAPI;
using StardewValley;
using WizardrySkill.Core.Framework;

namespace WizardrySkill.Objects
{
    public class MagicProfession : KeyedProfession
    {
        /*********
        ** Public methods
        *********/
        public MagicProfession(Skills.Skill skill, string theId, Texture2D icon, ITranslationHelper i18n)
            : base(skill, theId, icon, i18n) { }

        public override void DoImmediateProfessionPerk()
        {
            if (Game1.player.IsLocalPlayer)
            {
                Farmer player = Game1.player;
                string modDataID = this.Skill.Id + "." + this.Id;
                MoonShared.Attributes.Log.Trace("Player now has Profession mod data: " + modDataID);
                player.modData.SetBool(modDataID, true);
            }
            base.DoImmediateProfessionPerk();
        }

        public override void UndoImmediateProfessionPerk()
        {

            Farmer player = Game1.player;
            if (player.IsLocalPlayer)
            {
                string modDataID = this.Skill.Id + "." + this.Id;
                if (player.modData.GetBool(modDataID))
                {
                    SpellBook spellBook = player.GetSpellBook();
                    foreach (PreparedSpellBar spellBar in spellBook.Prepared)
                    {
                        spellBar.Spells.Clear();
                    }
                    MoonShared.Attributes.Log.Trace("Player now removed Profession mod data: " + modDataID);
                    player.modData.SetBool(modDataID, false);
                }
            }
            base.UndoImmediateProfessionPerk();
        }
    }
}
