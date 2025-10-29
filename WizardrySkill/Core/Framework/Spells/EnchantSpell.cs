using System;
using StardewValley;
using WizardrySkill.Core.Framework.Schools;
using WizardrySkill.Core.Framework.Spells.Effects;

namespace WizardrySkill.Core.Framework.Spells
{
    public class EnchantSpell : Spell
    {

        private static readonly int[] QualityTiers = { 0, 1, 2, 4 }; // Stardew quality levels

        public bool DoesDisenchant { get; }


        public EnchantSpell(bool dis)
            : base(SchoolId.Arcane, dis ? "disenchant" : "enchant")
        {
            this.DoesDisenchant = dis;
        }

        public override int GetManaCost(Farmer player, int level) => 3;

        public override int GetMaxCastingLevel() => 1;

        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            // Safety check
            if (!player.IsLocalPlayer)
                return null;

            var obj = player.ActiveObject;
            if (obj == null || !obj.QualifiedItemId.StartsWith("(O)"))
                return new SpellFizzle(player);

            // Prevent invalid upgrades/downgrades
            if ((!this.DoesDisenchant && obj.Quality == 4) || (this.DoesDisenchant && obj.Quality == 0))
                return new SpellFizzle(player);

            // Perform quality modification
            var sfx = this.ModifyItemQuality(player, obj, obj.Stack, level, !this.DoesDisenchant);

            return sfx;
        }


        /*********
        ** Core Logic
        *********/
        /// <summary>
        /// Modifies item quality up or down, one at a time.
        /// </summary>
        private IActiveEffect ModifyItemQuality(Farmer player, Item sourceItem, int times, int level, bool upgrade)
        {
            if (sourceItem == null || sourceItem.Stack <= 0 || times <= 0)
                return new SpellFizzle(player);

            int currentIndex = Array.IndexOf(QualityTiers, sourceItem.Quality);
            int direction = upgrade ? 1 : -1;

            int totalLoops = 0;
            for (int i = 0; i < times; i++)
            {

                totalLoops = i;
                if (i > 0 && !this.CanContinueCast(player, level))
                    break;

                if (sourceItem.Stack <= 0)
                    break;

                int nextIndex = currentIndex + direction;
                if (nextIndex < 0 || nextIndex >= QualityTiers.Length)
                    break;


                // Create copy and adjust quality
                Item modified = sourceItem.getOne();
                int oldPrice = modified.sellToStorePrice();
                modified.Quality = QualityTiers[nextIndex];
                int newPrice = modified.sellToStorePrice();

                // Calculate gold difference
                int diff = newPrice - oldPrice;

                if (upgrade)
                {
                    if (i > 0)
                    {
                        int cost = diff * 2; // upgrading costs double the price difference
                        if (player.Money < cost)
                            break;
                        player.Money -= cost;

                    } else
                    {
                        int cost = diff * 2; // upgrading costs double the price difference
                        if (player.Money < cost)
                            return new SpellFizzle(player);
                        player.Money -= cost;
                    }
                }
                else
                {
                    // Downgrading refunds half the lost value
                    int refund = (int)(Math.Abs(diff) * 0.5);
                    player.Money += refund;
                }

                // Remove one from original stack
                sourceItem.Stack--;

                modified.Stack = 1;

                // Try to add to inventory or drop if full
                if (!player.addItemToInventoryBool(modified))
                    Game1.createItemDebris(modified, player.getStandingPosition(), player.FacingDirection);

                // Apply mana and XP rewards
                player.AddMana(-this.GetManaCost(player, level));
            }

            // Remove source if fully consumed
            if (sourceItem.Stack <= 0)
                player.removeItemFromInventory(sourceItem);

            
            return new SpellSuccess(player, "secret1", totalLoops);
        }
    }
}
