using System;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    // This class defines an "EnchantSpell" that upgrades or downgrades the quality of an item.
    public class EnchantSpell : Spell
    {
        /*********
        ** Fields
        *********/

        // Stardew quality tiers: 0 = normal, 1 = silver, 2 = gold, 4 = iridium.
        private static readonly int[] QualityTiers = { 0, 1, 2, 4 };


        /*********
        ** Accessors
        *********/

        // Indicates if this spell is a disenchant, which downgrades quality.
        public bool DoesDisenchant { get; }


        /*********
        ** Public methods
        *********/

        // Constructor: choose if this spell is "enchant" or "disenchant".
        public EnchantSpell(bool dis)
            : base(SchoolId.Arcane, dis ? "disenchant" : "enchant")
        {
            this.DoesDisenchant = dis;
        }

        public override SpellSyncMode SyncMode => SpellSyncMode.LocalOnly;

        public override int GetManaCost(Farmer player, int level)
        {
            return 3;
        }

        public override int GetMaxCastingLevel()
        {
            return 1;
        }

        // Called when the spell is cast.
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player == null)
                return null;

            // Only the caster's own machine should modify inventory and money.
            if (!player.IsLocalPlayer)
                return null;

            StardewValley.Object obj = player.ActiveObject;

            // Fizzle if the player is not holding a valid object.
            if (obj == null || !obj.QualifiedItemId.StartsWith("(O)"))
                return new SpellFizzle(player);

            // Prevent upgrading iridium or downgrading normal items.
            if ((!this.DoesDisenchant && obj.Quality == 4) || (this.DoesDisenchant && obj.Quality == 0))
                return new SpellFizzle(player);

            // Perform the quality modification.
            return this.ModifyItemQuality(player, obj, obj.Stack, level, !this.DoesDisenchant);
        }


        /*********
        ** Private methods
        *********/

        /// <summary>Modifies the quality of an item, either upgrading or downgrading one by one.</summary>
        private IActiveEffect ModifyItemQuality(Farmer player, Item sourceItem, int times, int level, bool upgrade)
        {
            if (sourceItem == null || sourceItem.Stack <= 0 || times <= 0)
                return new SpellFizzle(player);

            int currentIndex = Array.IndexOf(QualityTiers, sourceItem.Quality);
            if (currentIndex < 0)
                return new SpellFizzle(player);

            int direction = upgrade ? 1 : -1;
            int modifiedCount = 0;

            for (int i = 0; i < times; i++)
            {
                // Stop if out of mana after the first item.
                if (i > 0 && !this.CanContinueCast(player, level))
                    break;

                if (sourceItem.Stack <= 0)
                    break;

                int nextIndex = currentIndex + direction;

                // Stop if next tier is invalid.
                if (nextIndex < 0 || nextIndex >= QualityTiers.Length)
                    break;

                // Create a copy of the item and set its new quality.
                Item modified = sourceItem.getOne();
                int oldPrice = modified.sellToStorePrice();
                modified.Quality = QualityTiers[nextIndex];
                int newPrice = modified.sellToStorePrice();

                // Calculate gold difference for cost or refund.
                int diff = newPrice - oldPrice;

                if (upgrade)
                {
                    // Upgrading costs double the price difference.
                    int cost = diff * 2;
                    if (player.Money < cost)
                        break;

                    player.Money -= cost;
                }
                else
                {
                    // Disenchant refunds half of the lost value.
                    int refund = (int)(Math.Abs(diff) * 0.5);
                    player.Money += refund;
                }

                // Remove one from the original stack.
                sourceItem.Stack--;

                modified.Stack = 1;

                // Try to add to inventory or drop if full.
                if (!player.addItemToInventoryBool(modified))
                    Game1.createItemDebris(modified, player.getStandingPosition(), player.FacingDirection);

                // Deduct mana for each item modified.
                player.AddMana(-this.GetManaCost(player, level));
                modifiedCount++;
            }

            // Remove the source item if fully consumed.
            if (sourceItem.Stack <= 0)
                player.removeItemFromInventory(sourceItem);

            return modifiedCount > 0
                ? new SpellSuccess(player, "secret1", modifiedCount)
                : new SpellFizzle(player);
        }
    }
}
