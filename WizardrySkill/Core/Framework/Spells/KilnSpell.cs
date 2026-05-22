using System;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;
using SObject = StardewValley.Object;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines a "KilnSpell" that processes wood into coal automatically.
    public class KilnSpell : Spell
    {
        /*********
        ** Public methods
        *********/
        public KilnSpell()
            : base(SchoolId.Toil, "kiln")
        {
            // SchoolId.Toil identifies the spell's magical school.
            // "kiln" is the internal name for this spell.
        }

        public override SpellSyncMode SyncMode => SpellSyncMode.LocalWorld;

        public override int GetManaCost(Farmer player, int level)
        {
            // Mana cost formula scales quadratically with spell level.
            int actualLevel = level + 1;
            return (int)(0.5 * actualLevel * actualLevel - 0.5 * actualLevel + 5);
        }

        public override int GetMaxCastingLevel()
        {
            return 4;
        }

        // Checks whether the player has either valid wood cost.
        public override bool HasItemCost(Farmer player, int level)
        {
            if (player == null)
                return false;

            int woodAmount = GetRequiredWoodAmount(level);

            // Player can pay with either driftwood or regular wood.
            return player.Items.ContainsId("169", woodAmount)
                || player.Items.ContainsId("388", woodAmount);
        }

        // Consumes either driftwood or regular wood for the spell.
        public override bool ConsumeItemCost(Farmer player, int level)
        {
            if (player == null)
                return false;

            if (!this.HasItemCost(player, level))
                return false;

            // Scroll casts should not consume reagent items.
            if (!this.ShouldConsumeItemCost(player))
                return true;

            int woodAmount = GetRequiredWoodAmount(level);

            // Prefer consuming driftwood if available.
            if (player.Items.ContainsId("169", woodAmount))
            {
                player.Items.ReduceId("169", woodAmount);
                return true;
            }

            // Otherwise consume regular wood.
            if (player.Items.ContainsId("388", woodAmount))
            {
                player.Items.ReduceId("388", woodAmount);
                return true;
            }

            return false;
        }

        // Called when the spell is cast by the local player.
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player == null || player.currentLocation == null)
                return null;

            // Only the caster's own machine should consume inventory, spawn coal debris, and award success effects.
            if (!player.IsLocalPlayer)
                return null;

            if (!this.ConsumeItemCost(player, level))
                return new SpellFizzle(player, this.GetManaCost(player, level));

            Game1.createObjectDebris(SObject.coal.ToString(), player.TilePoint.X, player.TilePoint.Y, player.currentLocation);
            return new SpellSuccess(player, "furnace", 2 * (level + 1));
        }


        /*********
        ** Private methods
        *********/
        private static int GetRequiredWoodAmount(int level)
        {
            int actualLevel = level + 1;
            int woodAmount = (int)(-0.5 * actualLevel * actualLevel - 0.5 * actualLevel + 18);
            return Math.Max(3, woodAmount);
        }
    }
}
