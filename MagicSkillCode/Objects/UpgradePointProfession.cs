using MagicSkillCode.Framework;
using Microsoft.Xna.Framework.Graphics;
using MoonShared;
using SpaceCore;
using StardewModdingAPI;
using StardewValley;

namespace MagicSkillCode.Objects
{
    public class UpgradePointProfession : KeyedProfession
    {
        /*********
        ** Public methods
        *********/
        public UpgradePointProfession(Skills.Skill skill, string theId, Texture2D icon, ITranslationHelper i18n)
            : base(skill, theId, icon, i18n) { }

        public override void DoImmediateProfessionPerk()
        {
            Game1.player.GetSpellBook().UseSpellPoints(-2);
            Game1.player.SetManaToMax();
            base.DoImmediateProfessionPerk();
        }

        public override void UndoImmediateProfessionPerk()
        {
            Game1.player.GetSpellBook().UseSpellPoints(2);
            Game1.player.SetManaToMax();
            base.DoImmediateProfessionPerk();
        }
    }
}
