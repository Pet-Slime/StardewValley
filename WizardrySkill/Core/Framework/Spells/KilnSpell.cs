using System;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines a "KilnSpell" that processes wood into coal automatically
    public class KilnSpell : Spell
    {
        /*********
        ** Public methods
        *********/

        public KilnSpell()
            : base(SchoolId.Elemental, "kiln")
        {
            // SchoolId.Elemental identifies the spell's magical school
            // "kiln" is the internal name for this spell
        }

        public override int GetManaCost(Farmer player, int level)
        {
            // Mana cost formula scales quadratically with spell level
            int actualLevel = level + 1;
            int manaCost = (int)(0.5 * actualLevel * actualLevel - 0.5 * actualLevel + 5);
            return manaCost;
        }

        public override int GetMaxCastingLevel()
        {
            return 4;
        }

        // Determines if the spell can be cast
        public override bool CanCast(Farmer player, int level)
        {
            int actualLevel = level + 1;

            // Calculate required wood amount based on spell level
            int woodAmount = (int)(-0.5 * actualLevel * actualLevel - 0.5 * actualLevel + 18);
            woodAmount = Math.Max(3, woodAmount); // Minimum of 3 wood required for higher levels

            // Player must have enough regular wood (ID 388) or driftwood (ID 169)
            return base.CanCast(player, level) && (
                player.Items.ContainsId("388", woodAmount) ||
                player.Items.ContainsId("169", woodAmount)
            );
        }

        // Called when the spell is cast
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            // Only run for the local player
            if (!player.IsLocalPlayer)
                return null;

            int actualLevel = level + 1;

            // Calculate required wood amount
            int woodAmount = (int)(-0.5 * actualLevel * actualLevel - 0.5 * actualLevel + 18);
            woodAmount = Math.Max(3, woodAmount); // clamp at 3 for level 5+

            // If player has driftwood, consume it and succeed
            if (player.Items.ContainsId("169", woodAmount))
            {
                player.Items.ReduceId("169", woodAmount);

                // Drop coal at player's location
                Game1.createObjectDebris(StardewValley.Object.coal.ToString(), player.TilePoint.X, player.TilePoint.Y, player.currentLocation);
                return new SpellSuccess(player, "furnace", 2 * (level + 1)); // visual/sound effect and XP
            }

            // If player has regular wood, consume it, give coal, and succeed
            if (player.Items.ContainsId("388", woodAmount))
            {
                player.Items.ReduceId("388", woodAmount);

                // Drop coal at player's location
                Game1.createObjectDebris(StardewValley.Object.coal.ToString(), player.TilePoint.X, player.TilePoint.Y, player.currentLocation);
                return new SpellSuccess(player, "furnace", 2 * (level + 1));  // visual/sound effect and XP
            }

            // Fail the spell if neither type of wood is available
            return new SpellFizzle(player, this.GetManaCost(player, level));
        }
    }
}
