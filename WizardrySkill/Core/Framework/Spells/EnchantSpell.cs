using System;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines an "EnchantSpell" that upgrades or downgrades the quality of an item
    public class EnchantSpell : Spell
    {
        // Stardew quality tiers: 0 = normal, 1 = silver, 2 = gold, 4 = iridium
        private static readonly int[] QualityTiers = { 0, 1, 2, 4 };

        // Indicates if this spell is a disenchant (downgrades quality)
        public bool DoesDisenchant { get; }

        // Constructor: choose if this spell is "enchant" or "disenchant"
        public EnchantSpell(bool dis)
            : base(SchoolId.Arcane, dis ? "disenchant" : "enchant")
        {
            this.DoesDisenchant = dis;
        }

        public override int GetManaCost(Farmer player, int level) => 3; // Base mana cost

        public override int GetMaxCastingLevel() => 1; // Max level of spell is 1

        // Called when the spell is cast
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            // Only run for the local player
            if (!player.IsLocalPlayer)
                return null;

            var obj = player.ActiveObject; // Get the item the player is holding

            // Fizzle if the player is not holding a valid object
            if (obj == null || !obj.QualifiedItemId.StartsWith("(O)"))
                return new SpellFizzle(player);

            // Prevent upgrading iridium or downgrading normal items
            if ((!this.DoesDisenchant && obj.Quality == 4) || (this.DoesDisenchant && obj.Quality == 0))
                return new SpellFizzle(player);

            // Perform the quality modification
            var sfx = this.ModifyItemQuality(player, obj, obj.Stack, level, !this.DoesDisenchant);

            return sfx;
        }

        /*********
        ** Core Logic
        *********/
        /// <summary>
        /// Modifies the quality of an item, either upgrading or downgrading one by one.
        /// </summary>
        private IActiveEffect ModifyItemQuality(Farmer player, Item sourceItem, int times, int level, bool upgrade)
        {
            // Safety checks
            if (sourceItem == null || sourceItem.Stack <= 0 || times <= 0)
                return new SpellFizzle(player);

            int currentIndex = Array.IndexOf(QualityTiers, sourceItem.Quality); // Current quality index
            int direction = upgrade ? 1 : -1; // Upgrade goes up, disenchant goes down

            int totalLoops = 0; // Track how many items were modified

            for (int i = 0; i < times; i++)
            {
                totalLoops = i;

                // Stop if out of mana
                if (i > 0 && !this.CanContinueCast(player, level))
                    break;

                if (sourceItem.Stack <= 0)
                    break;

                int nextIndex = currentIndex + direction; // Next quality tier

                // Stop if next tier is invalid
                if (nextIndex < 0 || nextIndex >= QualityTiers.Length)
                    break;

                // Create a copy of the item and set its new quality
                Item modified = sourceItem.getOne();
                int oldPrice = modified.sellToStorePrice();
                modified.Quality = QualityTiers[nextIndex];
                int newPrice = modified.sellToStorePrice();

                // Calculate gold difference for cost or refund
                int diff = newPrice - oldPrice;

                if (upgrade)
                {
                    // Upgrading costs double the price difference
                    int cost = diff * 2;
                    if (player.Money < cost)
                        return new SpellFizzle(player); // Not enough money to upgrade
                    player.Money -= cost;
                }
                else
                {
                    // Disenchant refunds half of the lost value
                    int refund = (int)(Math.Abs(diff) * 0.5);
                    player.Money += refund;
                }

                // Remove one from the original stack
                sourceItem.Stack--;

                modified.Stack = 1;

                // Try to add to inventory or drop if full
                if (!player.addItemToInventoryBool(modified))
                    Game1.createItemDebris(modified, player.getStandingPosition(), player.FacingDirection);

                // Deduct mana for each item modified
                player.AddMana(-this.GetManaCost(player, level));
            }

            // Remove the source item if fully consumed
            if (sourceItem.Stack <= 0)
                player.removeItemFromInventory(sourceItem);

            // Spell success with sound "secret1" and total items modified
            return new SpellSuccess(player, "secret1", totalLoops);
        }
    }
}
