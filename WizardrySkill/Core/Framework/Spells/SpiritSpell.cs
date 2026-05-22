using System.Collections.Generic;
using System.Globalization;
using StardewValley;
using WizardrySkill.Core.Framework;
using WizardrySkill.Core.Framework.Schools;

namespace WizardrySkill.Core.Framework.Spells
{
    // The SpiritSpell allows the player to summon a spirit effect at the cost of some health.
    public class SpiritSpell : Spell
    {
        /*********
        ** Public methods
        *********/

        // Constructor: sets the spell's school (Eldritch) and its ID ("spirit")
        public SpiritSpell()
            : base(SchoolId.Eldritch, "spirit")
        {
        }

        public override SpellSyncMode SyncMode => SpellSyncMode.Summon;

        // Defines how much mana it costs to cast this spell
        public override int GetManaCost(Farmer player, int level)
        {
            return 50;
        }

        // Defines the maximum level at which this spell can be cast
        public override int GetMaxCastingLevel()
        {
            return 1;
        }

        // Checks if the spell can be cast
        public override bool CanCast(Farmer player, int level)
        {
            // Player must meet the base spell requirements and have more than 1/5 of their max health.
            return player != null && base.CanCast(player, level) && player.health > player.maxHealth / 5;
        }

        // Called when the spell is cast by the local player.
        public override IActiveEffect OnCast(Farmer player, int level, int targetX, int targetY)
        {
            if (player == null || player.currentLocation == null)
                return null;

            // Only the caster's own machine should pay health, award EXP, and create/update durable summon state.
            if (!player.IsLocalPlayer)
                return null;

            // Play a ghost sound at the caster's current tile.
            player.currentLocation.LocalSoundAtPixel("ghost", player.Position);

            // Cost to cast: remove 1/5 of the caster's max health.
            player.takeDamage(player.maxHealth / 5, false, null);

            // Give caster experience for casting.
            Utilities.AddEXP(player, 50);

            Dictionary<string, string> data = new()
            {
                [SummonManager.SummonDataKeys.AttackRange] = ModEntry.Config.Spirit_attack_range.ToString(CultureInfo.InvariantCulture)
            };

            // SummonManager owns the durable summon state and creates/recreates the local visual instance.
            SummonManager.TryAddOrReplaceSummon(player, SummonManager.SummonDefs.Spirit, level, data: data, broadcast: true);

            // No active effect is returned here because the local visual is owned by SummonManager.
            return null;
        }
    }
}
