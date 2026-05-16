using System;
using StardewModdingAPI;
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
            : base(SchoolId.Toil, "kiln")
        {
            // SchoolId.Elemental identifies the spell's magical school
            // "kiln" is the internal name for this spell
        }

        public override SpellSyncMode SyncMode => SpellSyncMode.HostWorld;

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
            int woodAmount = GetRequiredWoodAmount(level);

            // Player must have enough regular wood (ID 388) or driftwood (ID 169)
            return base.CanCast(player, level) && (
                player.Items.ContainsId("388", woodAmount) ||
                player.Items.ContainsId("169", woodAmount)
            );
        }

        public override string BuildExtraData(Farmer caster, int level, int targetX, int targetY)
        {
            int woodAmount = GetRequiredWoodAmount(level);

            // Prefer driftwood if available, matching the original OnCast behavior.
            if (caster.Items.ContainsId("169", woodAmount))
                return "169";

            if (caster.Items.ContainsId("388", woodAmount))
                return "388";

            return "";
        }

        // Called when the spell is cast
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            string extraData = this.BuildExtraData(player, level, targetX, targetY);
            return this.OnReceiveCast(player, level, targetX, targetY, extraData);
        }

        // Called when the spell is received through the spell sync system
        public override IActiveEffect OnReceiveCast(Farmer caster, int level, int targetX, int targetY, string extraData)
        {
            if (caster == null || caster.currentLocation == null)
                return null;

            int woodAmount = GetRequiredWoodAmount(level);
            string woodId = GetWoodIdToConsume(caster, woodAmount, extraData);

            // Fail the spell if neither type of wood is available
            if (string.IsNullOrWhiteSpace(woodId))
                return caster.IsLocalPlayer ? new SpellFizzle(caster, this.GetManaCost(caster, level)) : null;

            // Only the actual casting player should consume the wood from inventory.
            if (caster.IsLocalPlayer)
                caster.Items.ReduceId(woodId, woodAmount);

            // Only the host should create shared object debris in the world.
            if (Context.IsMainPlayer)
                Game1.createObjectDebris(StardewValley.Object.coal.ToString(), caster.TilePoint.X, caster.TilePoint.Y, caster.currentLocation);

            return caster.IsLocalPlayer
                ? new SpellSuccess(caster, "furnace", 2 * (level + 1))  // visual/sound effect and XP
                : null;
        }

        /*********
        ** Private helpers
        *********/

        private static int GetRequiredWoodAmount(int level)
        {
            int actualLevel = level + 1;

            // Calculate required wood amount based on spell level
            int woodAmount = (int)(-0.5 * actualLevel * actualLevel - 0.5 * actualLevel + 18);
            woodAmount = Math.Max(3, woodAmount); // Minimum of 3 wood required for higher levels

            return woodAmount;
        }

        private static string GetWoodIdToConsume(Farmer caster, int woodAmount, string extraData)
        {
            // Trust the synced choice if the local caster still has enough of that item.
            if ((extraData == "169" || extraData == "388") && caster.IsLocalPlayer && caster.Items.ContainsId(extraData, woodAmount))
                return extraData;

            // Remote clients/host use the synced choice as the spell's selected reagent.
            if (extraData == "169" || extraData == "388")
                return extraData;

            // Fallback for direct local casts or old/empty packets.
            if (caster.Items.ContainsId("169", woodAmount))
                return "169";

            if (caster.Items.ContainsId("388", woodAmount))
                return "388";

            return "";
        }
    }
}
