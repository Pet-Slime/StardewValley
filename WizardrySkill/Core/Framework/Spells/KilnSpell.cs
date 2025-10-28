using System;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    public class KilnSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public KilnSpell()
            : base(SchoolId.Elemental, "kiln") { }

        public override int GetManaCost(Farmer player, int level)
        {
            int actualLevel = level + 1;
            int manaCost = (int)(0.5 * actualLevel * actualLevel - 0.5 * actualLevel + 5);
            return manaCost;
        }

        public override int GetMaxCastingLevel()
        {
            return 4;
        }

        public override bool CanCast(Farmer player, int level)
        {
            //Player needs wood or drift wood in the inventory
            int actualLevel = level + 1;
            int woodAmount = (int)(-0.5 * actualLevel * actualLevel - 0.5 * actualLevel + 18);
            woodAmount = Math.Max(3, woodAmount); // clamp at 3 for level 5+
            return base.CanCast(player, level) && (
                player.Items.ContainsId("388", woodAmount) ||
                player.Items.ContainsId("169", woodAmount) 
                );
        }

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            int actualLevel = level + 1;
            int woodAmount = (int)(-0.5 * actualLevel * actualLevel - 0.5 * actualLevel + 18);
            woodAmount = Math.Max(3, woodAmount); // clamp at 3 for level 5+
            if (player.Items.ContainsId("169", woodAmount))
            {
                player.Items.ReduceId("169", woodAmount);
                return new SpellSuccess(player, "furnace", 2 * (level + 1));

            }

            if (player.Items.ContainsId("388", woodAmount))
            {
                player.Items.ReduceId("388", woodAmount);
                Game1.createObjectDebris(StardewValley.Object.coal.ToString(), player.TilePoint.X, player.TilePoint.Y, player.currentLocation);
                return new SpellSuccess(player, "furnace", 2 * (level + 1));

            }
            return new SpellFizzle(player, this.GetManaCost(player, level));
        }
    }
}
