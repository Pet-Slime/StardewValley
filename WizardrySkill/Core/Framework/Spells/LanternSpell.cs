using System;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    public class LanternSpell : Spell
    {
        /*********
        ** Fields
        *********/
        private readonly Func<long> GetNewId;


        /*********
        ** Public methods
        *********/
        public LanternSpell(Func<long> getNewId)
            : base(SchoolId.Nature, "lantern")
        {
            this.GetNewId = getNewId;
        }

        public override int GetManaCost(Farmer player, int level)
        {
            return 3;
        }

        public override bool CanCast(Farmer player, int level)
        {
            return base.CanCast(player, level) && !Game1.currentLightSources.ContainsKey($"LanternSpell_{player.UniqueMultiplayerID}_{level}");
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player != Game1.player)
                return null;


            Utilities.AddEXP(player, (level + 1) * 3);
            return new LanternEffect(player, level);
        }



    }
}
